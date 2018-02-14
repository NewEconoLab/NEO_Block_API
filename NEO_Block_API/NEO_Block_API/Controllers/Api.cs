using NEO_Block_API.lib;
using NEO_Block_API.RPC;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NEO_Block_API.Controllers
{
    public class Api
    {
        private string netnode { get; set; }
        private string mongodbConnStr { get; set; }
        private string mongodbDatabase { get; set; }
        private string neoCliJsonRPCUrl { get; set; }

        mongoHelper mh = new mongoHelper();
        Transaction tx = new Transaction();
        Contract ct = new Contract();

        public Api(string node) {
            netnode = node;
            switch (netnode) {
                case "testnet":
                    mongodbConnStr = mh.mongodbConnStr_testnet;
                    mongodbDatabase = mh.mongodbDatabase_testnet;
                    neoCliJsonRPCUrl = mh.neoCliJsonRPCUrl_testnet;
                    break;
                case "mainnet":
                    mongodbConnStr = mh.mongodbConnStr_mainnet;
                    mongodbDatabase = mh.mongodbDatabase_mainnet;
                    neoCliJsonRPCUrl = mh.neoCliJsonRPCUrl_mainnet;
                    break;
            }
        }

        private JArray getJAbyKV(string key, object value)
        {
            return  new JArray
                        {
                            new JObject
                            {
                                {
                                    key,
                                    value.ToString()
                                }
                            }
                        };
        }

        private JArray getJAbyJ(JObject J)
        {
            return new JArray
                        {
                            J
                        };
        }

        public object getRes(JsonRPCrequest req,string reqAddr)
        {
            JArray result = new JArray();
            string resultStr = string.Empty;
            string findFliter = string.Empty;
            string sortStr = string.Empty;
            try
            {
                switch (req.method)
                {
                    case "getnoderpcapi":
                        JArray JA = new JArray
                        {
                            new JObject {
                                { "nodeType",netnode },
                                { "nodeList",new JArray{
                                    neoCliJsonRPCUrl}
                                }
                            }
                        };
                        result = JA;
                        break;
                    case "getdatablockheight":
                        result = mh.Getdatablockheight(mongodbConnStr, mongodbDatabase);
                        break;
                    case "getblockcount":
                        //resultStr = "[{blockcount:" + mh.GetDataCount(mongodbConnStr, mongodbDatabase, "block") + "}]";
                        result = getJAbyKV("blockcount", mh.GetDataCount(mongodbConnStr, mongodbDatabase, "block"));
                        break;
                    case "gettxcount":
                        //resultStr = "[{txcount:" + mh.GetDataCount(mongodbConnStr, mongodbDatabase, "tx") + "}]";
                        result = getJAbyKV("txcount", mh.GetDataCount(mongodbConnStr, mongodbDatabase, "tx"));
                        break;
                    case "getaddrcount":
                        //resultStr = "[{addrcount:" + mh.GetDataCount(mongodbConnStr, mongodbDatabase, "address") + "}]";
                        result = getJAbyKV("addrcount", mh.GetDataCount(mongodbConnStr, mongodbDatabase, "address"));
                        break;
                    case "getblock":
                        findFliter = "{index:" + req.@params[0] + "}";
                        result = mh.GetData(mongodbConnStr, mongodbDatabase, "block", findFliter);
                        break;
                    case "getblocks":
                        sortStr = "{index:-1}";
                        result = mh.GetDataPages(mongodbConnStr, mongodbDatabase, "block", sortStr, int.Parse(req.@params[0].ToString()), int.Parse(req.@params[1].ToString()));
                        break;
                    case "getrawtransaction":
                        findFliter = "{txid:'" + ((string)req.@params[0]).formatHexStr() + "'}";
                        result = mh.GetData(mongodbConnStr, mongodbDatabase, "tx", findFliter);
                        break;
                    case "getrawtransactions":
                        sortStr = "{blockindex:-1,txid:-1}";
                        findFliter = "{}";
                        if (req.@params.Count() > 2)
                        {
                            string txType = req.@params[2].ToString();

                            if (txType != null && txType != string.Empty)
                            { findFliter = "{type:'" + txType + "'}"; }
                        }
                        result = mh.GetDataPages(mongodbConnStr, mongodbDatabase, "tx", sortStr, int.Parse(req.@params[0].ToString()), int.Parse(req.@params[1].ToString()), findFliter);
                        break;
                    case "getaddrs":
                        sortStr = "{'lastuse.blockindex' : -1,'lastuse.txid' : -1}";
                        result = mh.GetDataPages(mongodbConnStr, mongodbDatabase, "address", sortStr, int.Parse(req.@params[0].ToString()), int.Parse(req.@params[1].ToString()));
                        break;
                    case "getaddresstxs":
                        string findBson = "{'addr':'" + req.@params[0].ToString() + "'}";
                        sortStr = "{'blockindex' : -1}";
                        result = mh.GetDataPages(mongodbConnStr, mongodbDatabase, "address_tx", sortStr, int.Parse(req.@params[1].ToString()), int.Parse(req.@params[2].ToString()),findBson);
                        break;
                    case "getasset":
                        findFliter = "{id:'" + ((string)req.@params[0]).formatHexStr() + "'}";
                        result = mh.GetData(mongodbConnStr, mongodbDatabase, "asset", findFliter);
                        break;
                    case "getallasset":
                        findFliter = "{}";
                        result = mh.GetData(mongodbConnStr, mongodbDatabase, "asset", findFliter);
                        break;
                    case "getfulllog":
                        findFliter = "{txid:'" + ((string)req.@params[0]).formatHexStr() + "'}";
                        result = mh.GetData(mongodbConnStr, mongodbDatabase, "fulllog", findFliter);
                        break;
                    case "getnotify":
                        findFliter = "{txid:'" + ((string)req.@params[0]).formatHexStr() + "'}";
                        result = mh.GetData(mongodbConnStr, mongodbDatabase, "notify", findFliter);
                        break;
                    case "getutxo":
                        if (req.@params.Count() == 1)
                        {
                            findFliter = "{addr:'" + req.@params[0] + "',used:''}";
                        };
                        if (req.@params.Count() == 2)
                        {
                            if ((Int64)req.@params[1] == 1)
                            {
                                findFliter = "{addr:'" + req.@params[0] + "'}";
                            }
                        }
                        result = mh.GetData(mongodbConnStr, mongodbDatabase, "utxo", findFliter);
                        break;
                    case "getbalance":
                        findFliter = "{addr:'" + req.@params[0] + "',used:''}";
                        JArray utxos = mh.GetData(mongodbConnStr, mongodbDatabase, "utxo", findFliter);
                        Dictionary<string, decimal> balance = new Dictionary<string, decimal>();
                        foreach (JObject j in utxos)
                        {
                            if (!balance.ContainsKey((string)j["asset"]))
                            {
                                balance.Add((string)j["asset"], (decimal)j["value"]);
                            }
                            else
                            {
                                balance[(string)j["asset"]] += (decimal)j["value"];
                            }
                        }
                        JArray balanceJA = new JArray();
                        foreach (KeyValuePair<string, decimal> kv in balance)
                        {
                            JObject j = new JObject();
                            j.Add("asset", kv.Key);
                            j.Add("balance", kv.Value);
                            JObject asset = (JObject)mh.GetData(mongodbConnStr, mongodbDatabase, "asset", "{id:'" + kv.Key + "'}")[0];
                            JArray name = (JArray)asset["name"];
                            j.Add("name", name);
                            balanceJA.Add(j);
                        }
                        result = balanceJA;
                        break;
                    case "getcontractscript":
                        findFliter = "{hash:'" + ((string)req.@params[0]).formatHexStr() + "'}";
                        result = mh.GetData(mh.mongodbConnStr_NeonOnline, mh.mongodbDatabase_NeonOnline, "contractWarehouse", findFliter);
                        break;
                    case "gettransfertxhex":
                        string addrOut = (string)req.@params[0];
                        findFliter = "{addr:'" + addrOut + "',used:''}";
                        JArray outputJA = mh.GetData(mongodbConnStr, mongodbDatabase, "utxo", findFliter);

                        result = getJAbyKV("transfertxhex", tx.getTransferTxHex(outputJA, (string)req.@params[0], (string)req.@params[1], (string)req.@params[2], decimal.Parse(req.@params[3].ToString())));

                        //result = new JArray
                        //{
                        //    new JObject
                        //    {
                        //        {
                        //            "transfertxhex",
                        //            tx.getTransferTxHex(outputJA,(string)req.@params[0], (string)req.@params[1], (string)req.@params[2], decimal.Parse(req.@params[3].ToString()))
                        //        }
                        //    }
                        //};
                        break;
                    case "sendtxplussign":
                        result = getJAbyJ(tx.sendTxPlusSign(neoCliJsonRPCUrl, (string)req.@params[0], (string)req.@params[1], (string)req.@params[2]));
                        break;
                    case "verifytxsign":
                        result = getJAbyKV("sign", tx.verifyTxSign((string)req.@params[0], (string)req.@params[1]));
                        break;
                    case "sendrawtransaction":
                        result = getJAbyJ(tx.sendrawtransaction(neoCliJsonRPCUrl, (string)req.@params[0]));

                        //result = new JArray
                        //{
                        //    new JObject
                        //    {
                        //        {
                        //            "sendrawtransactionresult",
                        //            tx.sendrawtransaction(neoCliJsonRPCUrl,(string)req.@params[0])
                        //        }
                        //    }
                        //};
                        break;
                    case "getcontractstate":
                        result = getJAbyJ(ct.getContractState(neoCliJsonRPCUrl, (string)req.@params[0]));

                        break;
                    case "invokescript":
                        result = getJAbyJ(ct.invokeScript(neoCliJsonRPCUrl, (string)req.@params[0]));

                        break;
                    case "callcontractfortest":
                        result = getJAbyJ(ct.callContractForTest(neoCliJsonRPCUrl, (string)req.@params[0], (JArray)req.@params[1]));

                        break;
                    case "getinvoketxhex":
                        string addrPayFee = (string)req.@params[0];
                        findFliter = "{addr:'" + addrPayFee + "',used:''}";
                        JArray outputJAPayFee = mh.GetData(mongodbConnStr, mongodbDatabase, "utxo", findFliter);

                        string invokeScript = (string)req.@params[1];
                        decimal invokeScriptFee = decimal.Parse(req.@params[2].ToString());

                        result = getJAbyKV("invoketxhex", tx.getInvokeTxHex(outputJAPayFee, addrPayFee, invokeScript, invokeScriptFee));
                        break;
                    case "setcontractscript":
                        JObject J = JObject.Parse((string)req.@params[0]);
                        string hash = (string)J["hash"];
                        //string hash = (string)req.@params[0];
                        //J.Add("hash", hash);
                        //J.Add("avm", (string)req.@params[1]);
                        //J.Add("cs", (string)req.@params[2]);

                        //string mapStr = (string)req.@params[3];
                        //string abiStr = (string)req.@params[4];

                        //if (mapStr != null && mapStr != string.Empty)
                        //{
                        //    J.Add("map", JArray.Parse((string)req.@params[3]));
                        //}
                        //else
                        //{
                        //    J.Add("map", string.Empty);
                        //}

                        //if (abiStr != null && abiStr != string.Empty)
                        //{
                        //    J.Add("abi", JObject.Parse((string)req.@params[4]));
                        //}
                        //else
                        //{
                        //    J.Add("abi", string.Empty);
                        //}

                        J.Add("requestIP", reqAddr);

                        mh.InsertOneDataByCheckKey(mh.mongodbConnStr_NeonOnline, mh.mongodbDatabase_NeonOnline, "contractWarehouse", J, "hash", hash);
                        result = getJAbyKV("isSetSuccess", true);

                        //result = new JArray
                        //{
                        //    new JObject{
                        //        { "isSetSuccess",true }
                        //    }
                        //};

                        break;
                }
                if (result.Count == 0)
                {
                    JsonPRCresponse_Error resE = new JsonPRCresponse_Error(req.id, -1, "No Data", "Data does not exist");

                    return resE;
                }
            }
            catch (Exception e)
            {
                JsonPRCresponse_Error resE = new JsonPRCresponse_Error(req.id, -100, "Parameter Error", e.Message);

                return resE;

            }

            JsonPRCresponse res = new JsonPRCresponse();
            res.jsonrpc = req.jsonrpc;
            res.id = req.id;
            res.result = result;

            return res;
        }
    }
}
