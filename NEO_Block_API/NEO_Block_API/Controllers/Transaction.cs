using NEO_Block_API.lib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NEO_Block_API.Controllers
{
    public class Transaction
    {
        public string getTransferTxHex(JArray utxoJA,string addrOut, string addrIn, string assetID, decimal amounts)
        {
            ThinNeo.Transaction lastTran;

            //string findFliter = "{addr:'" + addrOut + "',used:''}";
            //JArray outputJA = mh.GetData(mongodbConnStr, mongodbDatabase, "utxo", findFliter);

            //linq查找指定asset最大的utxo
            var query = from utxos in utxoJA.Children()
                        where (string)utxos["asset"] == assetID
                        orderby (decimal)utxos["value"] //descending
                        select utxos;
            //var utxo = query.ToList()[0];

            JArray utxo2pay = new JArray();
            decimal utxo_value = 0; //所有utxo总值
            foreach (JObject utxo in query)
            {
                if (utxo_value < amounts)//如utxo总值小于需支付则继续加utxo
                {
                    utxo2pay.Add(utxo);
                    utxo_value += (decimal)utxo["value"];
                }
                else { break; }//utxo总值大于等于需支付则跳出
            }
            var a = JsonConvert.SerializeObject(utxo2pay);
            //byte[] utxo_txid = ThinNeo.Debug.DebugTool.HexString2Bytes(((string)utxo["txid"]).Replace("0x", "")).Reverse().ToArray();
            //ushort utxo_n = (ushort)utxo["n"];
            //decimal utxo_value = (decimal)utxo["value"];
            byte[] assetBytes = assetID.Replace("0x", "").HexString2Bytes().Reverse().ToArray();

            if (amounts > utxo_value)
            {
                return string.Empty;
            }

            lastTran = new ThinNeo.Transaction
            {
                type = ThinNeo.TransactionType.ContractTransaction,//转账
                attributes = new ThinNeo.Attribute[0],
                inputs = new ThinNeo.TransactionInput[utxo2pay.Count]
            };
            //构造输入
            int i = 0;
            foreach (var utxo in utxo2pay)
            {
                lastTran.inputs[i] = new ThinNeo.TransactionInput
                {
                    hash = ((string)utxo["txid"]).Replace("0x", "").HexString2Bytes().Reverse().ToArray(),
                    index = (ushort)utxo["n"]
                };
                i++;
            }

            bool isNeedRefund = (utxo_value - amounts) > 0 ? true : false;
            if (isNeedRefund)
            {
                lastTran.outputs = new ThinNeo.TransactionOutput[2];
            }
            else {
                lastTran.outputs = new ThinNeo.TransactionOutput[1];
            }
            
            lastTran.outputs[0] = new ThinNeo.TransactionOutput
            {
                assetId = assetBytes,
                toAddress = ThinNeo.Helper.GetPublicKeyHashFromAddress(addrIn),
                value = amounts
            };//给对方转账

            if (isNeedRefund)
            {
                lastTran.outputs[1] = new ThinNeo.TransactionOutput
                {
                    assetId = assetBytes,
                    toAddress = ThinNeo.Helper.GetPublicKeyHashFromAddress(addrOut),
                    value = utxo_value - amounts
                };
            }//如需要，处理给自己找零

            using (var ms = new MemoryStream())
            {
                lastTran.SerializeUnsigned(ms);
                return ms.ToArray().ToHexString();
            }
        }

        public JObject sendTxPlusSign(string neoCliJsonRPCUrl, string txScriptHex, string signHex, string publicKeyHex)
        {
            byte[] txScript = txScriptHex.HexString2Bytes();
            byte[] sign = signHex.HexString2Bytes();
            byte[] pubkey = publicKeyHex.HexString2Bytes();
            //byte[] prikey = privateKeyHex.HexToBytes();

            //byte[] sign = null;

            //sign = ThinNeo.Helper.Sign(txScript, prikey);

            //var pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);

            var addr = ThinNeo.Helper.GetAddressFromPublicKey(pubkey);

            ThinNeo.Transaction lastTran = new ThinNeo.Transaction();
            lastTran.Deserialize(new MemoryStream(txScript));
            lastTran.witnesses = null;
            lastTran.AddWitness(sign, pubkey, addr);

            string TxPlusSignStr = string.Empty;
            using (var ms = new System.IO.MemoryStream())
            {
                lastTran.Serialize(ms);
                TxPlusSignStr = ms.ToArray().ToHexString();
            }

            return sendrawtransaction(neoCliJsonRPCUrl, TxPlusSignStr);
        }

        public JObject sendrawtransaction(string neoCliJsonRPCUrl, string txSigned)
        {
            httpHelper hh = new httpHelper();
            var resp = hh.Post(neoCliJsonRPCUrl, "{'jsonrpc':'2.0','method':'sendrawtransaction','params':['" + txSigned + "'],'id':1}", System.Text.Encoding.UTF8, 1);

            bool isSendSuccess = (bool)JObject.Parse(resp)["result"];
            JObject Jresult = new JObject();
            Jresult.Add("sendrawtransactionresult", isSendSuccess);
            if (isSendSuccess)
            {
                ThinNeo.Transaction lastTran = new ThinNeo.Transaction();
                lastTran.Deserialize(new MemoryStream(txSigned.HexString2Bytes()));
                string txid = lastTran.GetHash().Reverse().ToArray().ToHexString();

                ////从已签名交易体分析出未签名交易体，并做Hash获得txid
                //byte[] txUnsigned = txSigned.Split("014140")[0].HexString2Bytes();
                //string txid = ThinNeo.Helper.Sha256(ThinNeo.Helper.Sha256(txUnsigned)).Reverse().ToArray().ToHexString();

                Jresult.Add("txid", txid);
            }
            else {
                //上链失败则返回空txid
                Jresult.Add("txid", string.Empty);
            }

            return Jresult;
        }
    }
}
