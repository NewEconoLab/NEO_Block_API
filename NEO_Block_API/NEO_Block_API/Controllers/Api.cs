using NEO_Block_API.lib;
using NEO_Block_API.RPC;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using NEO_Block_API.Services;

namespace NEO_Block_API.Controllers
{
    public class Api
    {
        private string netnode { get; set; }
        private string mongodbConnStr { get; set; }
        private string mongodbDatabase { get; set; }
        private string neoCliJsonRPCUrl { get; set; }

        httpHelper hh = new httpHelper();
        mongoHelper mh = new mongoHelper();
        Transaction tx = new Transaction();
        Contract ct = new Contract();
        Claim claim = new Claim();
        NotifyService notifyService = null;
        BlockService blockService = null;
        NeoCliService neoCliService = null;

        private static Api testApi = new Api("testnet");
        private static Api mainApi = new Api("mainnet");
        public static Api getTestApi() { return testApi; }
        public static Api getMainApi() { return mainApi; }
        private Monitor monitor;

        public Api(string node) {
            netnode = node;
            switch (netnode) {
                case "testnet":
                    neoCliService = new NeoCliService
                    {
                        hh = hh,
                        neoCliJsonRPCUrl = mh.neoCliJsonRPCUrl_testnet
                    };
                    mongodbConnStr = mh.mongodbConnStr_testnet;
                    mongodbDatabase = mh.mongodbDatabase_testnet;
                    neoCliJsonRPCUrl = mh.neoCliJsonRPCUrl_testnet;
                    notifyService = new NotifyService
                    {
                        mh = mh,
                        mongodbConnStr = mh.mongodbConnStr_testnet,
                        mongodbDatabase = mh.mongodbDatabase_testnet,
                    };
                    blockService = new BlockService
                    {
                        mh = mh,
                        mongodbConnStr = mh.mongodbConnStr_testnet,
                        mongodbDatabase = mh.mongodbDatabase_testnet,
                    };
                    break;
                case "mainnet":
                    neoCliService = new NeoCliService
                    {
                        hh = hh,
                        neoCliJsonRPCUrl = mh.neoCliJsonRPCUrl_mainnet
                    };
                    mongodbConnStr = mh.mongodbConnStr_mainnet;
                    mongodbDatabase = mh.mongodbDatabase_mainnet;
                    neoCliJsonRPCUrl = mh.neoCliJsonRPCUrl_mainnet;
                    notifyService = new NotifyService
                    {
                        mh = mh,
                        mongodbConnStr = mh.mongodbConnStr_mainnet,
                        mongodbDatabase = mh.mongodbDatabase_mainnet,
                    };
                    blockService = new BlockService
                    {
                        mh = mh,
                        mongodbConnStr = mh.mongodbConnStr_mainnet,
                        mongodbDatabase = mh.mongodbDatabase_mainnet,
                    };
                    break;
            }

            initMonitor();
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
                point(req.method);
                switch (req.method)
                {
                    case "getTxidFromMemPool":
                        result = neoCliService.getTxidFromMemPool(req.@params[0].ToString());
                        break;
                    case "getRawMemPoolList":
                        result = neoCliService.getRawMemPoolList();
                        break;
                    case "getRawMemPoolGroup":
                        result = neoCliService.getRawMemPoolGroup();
                        break;
                    case "getRawMemPoolCount":
                        if(req.@params.Length < 1)
                        {
                            result = neoCliService.getRawMemPoolCount();
                        } else
                        {
                            result = neoCliService.getRawMemPoolCount(req.@params[0].ToString());
                        }
                        
                        break;
                    case "getnodetype":
                        JArray JA = new JArray
                        {
                            new JObject {
                                { "nodeType",netnode }
                            }
                        };
                        result = JA;
                        break;
                    case "getcliversion":
                        result = getJAbyKV("cliversion", (string)JObject.Parse(hh.Post(neoCliJsonRPCUrl, "{'jsonrpc':'2.0','method':'getversion','params':[],'id':1}", System.Text.Encoding.UTF8, 1))["result"]["useragent"]);
                        break;
                    case "getclirawmempool":
                        JObject rawmempoolJ = new JObject();
                        rawmempoolJ.Add("clirawmempool", JObject.Parse(hh.Post(neoCliJsonRPCUrl, "{'jsonrpc':'2.0','method':'getrawmempool','params':[],'id':1}", System.Text.Encoding.UTF8, 1))["result"]);
                        result = getJAbyJ(rawmempoolJ);
                        break;
                    case "getcliblockcount":
                        var resp = hh.Post(neoCliJsonRPCUrl, "{'jsonrpc':'2.0','method':'getblockcount','params':[],'id':1}", System.Text.Encoding.UTF8, 1);

                        string cliResultStr = (string)JObject.Parse(resp)["result"];
                        result = getJAbyKV("cliblockcount", cliResultStr);
                        break;
                    case "getdatablockheight":
                        result = mh.Getdatablockheight(mongodbConnStr, mongodbDatabase);
                        break;
                    case "getblockcount":
                        result = getJAbyKV("blockcount", (long)(mh.GetData(mongodbConnStr, mongodbDatabase, "system_counter", "{counter:'block'}")[0]["lastBlockindex"]) + 1);
                        break;
                    case "gettxcount":
                        findFliter = "{}";
                        result = getJAbyKV("txcount", mh.GetDataCount(mongodbConnStr, mongodbDatabase, "tx", findFliter));
                        break;
                    case "getaddrcount":
                        result = getJAbyKV("addrcount", mh.GetDataCount(mongodbConnStr, mongodbDatabase, "address"));
                        break;
                    case "getblock":
                        findFliter = "{index:" + req.@params[0] + "}";
                        result = mh.GetData(mongodbConnStr, mongodbDatabase, "block", findFliter);
                        var txs = (JArray)result[0]["tx"];
                        for (var i = 0; i < txs.Count; i++)
                        {
                            txs[i]["sys_fee"] = double.Parse((string)txs[i]["sysfee"]) / Math.Pow(10, 8);
                            txs[i]["net_fee"] = double.Parse((string)txs[i]["netfee"]) / Math.Pow(10, 8);
                        }
                        break;
                    case "getblocktime":
                        findFliter = "{index:" + req.@params[0] + "}";
                        var time = (Int32)mh.GetData(mongodbConnStr, mongodbDatabase, "block", findFliter)[0]["time"];
                        result = getJAbyKV("time", time);
                        break;
                    case "getblocks":
                        long lastBlockindex = (long)(mh.GetData(mongodbConnStr, mongodbDatabase, "system_counter", "{counter:'block'}")[0]["lastBlockindex"]);
                        sortStr = "{index:-1}"; // 15  10 
                        int pageCount = int.Parse(req.@params[0].ToString());
                        int pageNum = int.Parse(req.@params[1].ToString());
                        string fieldStr = new JObject() { { "_id", 0},{ "index",1 }, { "size", 1 }, { "time", 1 }, { "tx", 1 } }.ToString();
                        string filter = new JObject() { {"index", new JObject() { { "$gt", lastBlockindex - pageCount * pageNum}, { "$lte", lastBlockindex - pageCount*(pageNum-1) } } } }.ToString();
                        result = mh.GetDataPagesWithField(mongodbConnStr, mongodbDatabase, "block", fieldStr, sortStr, pageCount, 1, filter);
                        result = new JArray() { result.Select(p => {
                            JObject jo = (JObject)p;
                            jo.Add("txcount", ((JArray)p["tx"]).Count);
                            jo.Remove("tx");
                            return jo;
                        }).ToArray() };
                        break;
                    case "getrawtransaction":
                        //findFliter = "{txid:'" + ((string)req.@params[0]).formatHexStr() + "'}";
                        findFliter = "{hash:'" + ((string)req.@params[0]).formatHexStr() + "'}";
                        result = mh.GetData(mongodbConnStr, mongodbDatabase, "tx", findFliter);
                        //result[0]["sys_fee"] = double.Parse((string)result[0]["sys_fee"]) / Math.Pow(10,8);
                        result[0]["sys_fee"] = double.Parse((string)result[0]["sysfee"]) / Math.Pow(10, 8);
                        //result[0]["net_fee"] = double.Parse((string)result[0]["net_fee"]) / Math.Pow(10,8);
                        result[0]["net_fee"] = double.Parse((string)result[0]["netfee"]) / Math.Pow(10, 8);
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
                    case "getaddr":
                        string addr = req.@params[0].ToString();
                        findFliter = "{addr:'" + addr + "'}";
                        result = mh.GetData(mongodbConnStr, mongodbDatabase, "address", findFliter);
                        break;
                    case "getaddresstxs":
                        string findBson = "{'addr':'" + req.@params[0].ToString() + "'}";
                        sortStr = "{'blockindex' : -1}";
                        result = mh.GetDataPages(mongodbConnStr, mongodbDatabase, "address_tx", sortStr, int.Parse(req.@params[1].ToString()), int.Parse(req.@params[2].ToString()), findBson);
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
                            result = mh.GetData(mongodbConnStr, mongodbDatabase, "utxo", findFliter);
                        }
                        else if (req.@params.Count() == 2)
                        {
                            if ((Int64)req.@params[1] == 1)
                            {
                                findFliter = "{addr:'" + req.@params[0] + "'}";
                            }
                            result = mh.GetData(mongodbConnStr, mongodbDatabase, "utxo", findFliter);
                        }
                        else if (req.@params.Count() == 3)
                        {
                            findFliter = "{addr:'" + req.@params[0] + "',used:''}";
                            sortStr = "{'createHeight':1,'txid':1,'n':1}";
                            result = mh.GetDataPages(mongodbConnStr, mongodbDatabase, "utxo", sortStr, int.Parse(req.@params[1].ToString()), int.Parse(req.@params[2].ToString()), findFliter);
                        }
                        else if (req.@params.Count() == 4)
                        {
                            if ((Int64)req.@params[1] == 1)
                            {
                                findFliter = "{addr:'" + req.@params[0] + "'}";
                            }
                            sortStr = "{'createHeight':1,'txid':1,'n':1}";
                            result = mh.GetDataPages(mongodbConnStr, mongodbDatabase, "utxo", sortStr, int.Parse(req.@params[2].ToString()), int.Parse(req.@params[3].ToString()), findFliter);
                        }
                        break;
                    case "getutxocount":
                        addr = req.@params[0].ToString();
                        if (addr != null && addr != string.Empty)
                        {
                            findFliter = "{addr:\"" + addr + "\"}";
                        }
                        result = getJAbyKV("utxocount", mh.GetDataCount(mongodbConnStr, mongodbDatabase, "utxo", findFliter));
                        break;
                    case "getutxostopay":
                        string address = (string)req.@params[0];
                        string assetID = ((string)req.@params[1]).formatHexStr();
                        decimal amount = decimal.Parse(req.@params[2].ToString(), NumberStyles.Float);
                        bool isBigFirst = false; //默认先用小的。

                        if (req.@params.Count() == 4)
                        {
                            if ((Int64)req.@params[3] == 1)
                            {
                                isBigFirst = true;//加可选参数可以先用大的。
                            }
                        }

                        findFliter = "{addr:'" + address + "',used:''}";
                        JArray utxoJA = mh.GetData(mongodbConnStr, mongodbDatabase, "utxo", findFliter);

                        result = tx.getUtxo2Pay(utxoJA, address, assetID, amount, isBigFirst);
                        break;
                    case "getclaimgasUtxoList":
                        if (req.@params.Count() == 1)
                        {
                            result = claim.getClaimGasUtxoList(mongodbConnStr, mongodbDatabase, req.@params[0].ToString());
                        }
                        else if (req.@params.Count() == 2)
                        {
                            result = claim.getClaimGasUtxoList(mongodbConnStr, mongodbDatabase, req.@params[0].ToString(), req.@params[1].ToString() == "1");
                        }
                        else if (req.@params.Count() == 4)
                        {
                            result = claim.getClaimGasUtxoList(mongodbConnStr, mongodbDatabase, req.@params[0].ToString(), req.@params[1].ToString() == "1", int.Parse(req.@params[2].ToString()), int.Parse(req.@params[3].ToString()));
                        }
                        break;
                    case "getclaimgas":
                        JObject claimsJ = new JObject();
                        if (req.@params.Count() == 1)
                        {
                            claimsJ = claim.getClaimGas(mongodbConnStr, mongodbDatabase, req.@params[0].ToString(), true); //限制50,默认值
                        };
                        if (req.@params.Count() == 2)
                        {
                            if ((Int64)req.@params[1] == 1)
                            {
                                claimsJ = claim.getClaimGas(mongodbConnStr, mongodbDatabase, req.@params[0].ToString(), false); //限制50,默认值
                            }
                        }
                        if (req.@params.Count() == 3)
                        {
                            claimsJ = claim.getClaimGas(mongodbConnStr, mongodbDatabase, req.@params[0].ToString(), req.@params[1].ToString() == "0", req.@params[2].ToString() == "0"); //限制50,默认值
                        }
                        if (req.@params.Count() == 4)
                        {
                            claimsJ = claim.getClaimGas(mongodbConnStr, mongodbDatabase, req.@params[0].ToString(), req.@params[1].ToString() == "0", req.@params[2].ToString() == "0", req.@params[3].ToString() == "0"); //限制50,默认值
                        }
                        result = getJAbyJ(claimsJ);
                        break;
                    case "getclaimtxhex":
                        string addrClaim = (string)req.@params[0];

                        JObject claimgasJ = claim.getClaimGas(mongodbConnStr, mongodbDatabase, addrClaim, true, false);//  不限制50
                        if (claimgasJ["errorCode"] != null)
                        {
                            result = getJAbyJ(claimgasJ);
                        }
                        else
                        {
                            result = getJAbyKV("claimtxhex", tx.getClaimTxHex(addrClaim, claimgasJ));
                        }
                        
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

                        result = getJAbyKV("transfertxhex", tx.getTransferTxHex(outputJA, (string)req.@params[0], (string)req.@params[1], (string)req.@params[2], decimal.Parse(req.@params[3].ToString(), NumberStyles.Float)));

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
                        result = getJAbyJ(ct.callContractForTest(neoCliJsonRPCUrl, new List<string> { (string)req.@params[0] }, new JArray() { (JArray)req.@params[1] }));

                        break;
                    case "publishcontractfortest":
                        result = getJAbyJ(ct.publishContractForTest(neoCliJsonRPCUrl, (string)req.@params[0], (JObject)req.@params[1]));
                        break;
                    case "getinvoketxhex":
                        string addrPayFee = (string)req.@params[0];
                        findFliter = "{addr:'" + addrPayFee + "',used:''}";
                        JArray outputJAPayFee = mh.GetData(mongodbConnStr, mongodbDatabase, "utxo", findFliter);

                        string invokeScript = (string)req.@params[1];
                        decimal invokeScriptFee = decimal.Parse(req.@params[2].ToString(), NumberStyles.Float);

                        result = getJAbyKV("invoketxhex", tx.getInvokeTxHex(outputJAPayFee, addrPayFee, invokeScript, invokeScriptFee));
                        break;
                    case "getstorage":
                        result = getJAbyJ(ct.getStorage(neoCliJsonRPCUrl, (string)req.@params[0], (string)req.@params[1]));

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
                    case "getnep5balanceofaddress":
                        string NEP5scripthash = (string)req.@params[0];
                        if(!NEP5scripthash.StartsWith("0x"))
                        {
                            NEP5scripthash = "0x" + NEP5scripthash;
                        }
                        string NEP5address = (string)req.@params[1];
                        byte[] NEP5addrHash = ThinNeo.Helper.GetPublicKeyHashFromAddress(NEP5address);
                        string NEP5addrHashHex = ThinNeo.Helper.Bytes2HexString(NEP5addrHash.Reverse().ToArray());
                        JObject NEP5balanceOfJ = ct.callContractForTest(neoCliJsonRPCUrl,new List<string>{ NEP5scripthash }, new JArray() { JArray.Parse("['(str)balanceOf',['(hex)" + NEP5addrHashHex + "']]") });
                        string balanceStr = (string)((JArray)NEP5balanceOfJ["stack"])[0]["value"];
                        string balanceType = (string)((JArray)NEP5balanceOfJ["stack"])[0]["type"];

                        string balanceBigint = "0";

                        if (balanceStr != string.Empty)
                        {
                            //获取NEP5资产信息，获取精度
                            NEP5.Asset NEP5asset = new NEP5.Asset(mongodbConnStr, mongodbDatabase, NEP5scripthash);

                            balanceBigint = NEP5.getNumStrFromStr(balanceType,balanceStr, NEP5asset.decimals);
                        }

                        result = getJAbyKV("nep5balance", balanceBigint);
                        break;
                    case "getallnep5assetofaddress":
                        /*
                        string NEP5addr = (string)req.@params[0];
                        string findTransferTo = "{ Address:'" + NEP5addr + "'}";
                        var nep5States = mh.GetData(mongodbConnStr,mongodbDatabase, "Nep5State", findTransferTo);
                        JArray ja = new JArray();
                        for (var i = 0; i < nep5States.Count; i++)
                        {
                            JObject jo = new JObject();
                            jo["balance"] = double.Parse((string)nep5States[i]["Balance"]["$numberDecimal"]) /(Math.Pow(10,double.Parse((string)nep5States[i]["AssetDecimals"])));
                            jo["symbol"] = nep5States[i]["AssetSymbol"].ToString();
                            jo["assetid"] = nep5States[i]["AssetHash"].ToString();
                            ja.Add(jo);
                        }
                        result = ja;
                        */
                        result = blockService.getAllNep5AssetOfAddress((string)req.@params[0]);
                        break;
                    case "getnep5asset":
                        findFliter = "{assetid:'" + ((string)req.@params[0]).formatHexStr() + "'}";
                        result = mh.GetData(mongodbConnStr, mongodbDatabase, "Nep5AssetInfo", findFliter);
                        result[0]["totalsupply"] = double.Parse((string)result[0]["totalsupply"]) / Math.Pow(10,double.Parse((string)result[0]["decimals"])); 
                        break;
                    case "getallnep5asset":
                        findFliter = "{}";
                        result = mh.GetData(mongodbConnStr, mongodbDatabase, "Nep5AssetInfo", findFliter);
                        for (var i = 0; i < result.Count; i++)
                        {
                            result[i]["totalsupply"] = double.Parse((string)result[i]["totalsupply"]) / Math.Pow(10, double.Parse((string)result[i]["decimals"]));
                        }
                        break;
                    case "getnep5transferbytxid":
                        string txid = ((string)req.@params[0]).formatHexStr();
                        findFliter = "{txid:'" + txid + "'}";
                        result = mh.GetData(mongodbConnStr, mongodbDatabase, "Nep5Transfer", findFliter);
                        for (var i = 0; i < result.Count; i++)
                        {
                            result[i]["value"] = double.Parse((string)result[i]["value"]) / Math.Pow(10,double.Parse((string)result[i]["decimals"]));
                        }
                        break;
                    case "getnep5transferbyaddress":
                        //sortStr = "{'blockindex':1,'txid':1,'n':1}";
                        sortStr = "{}";
                        string NEP5transferAddress = (string)req.@params[0];
                        string NEP5transferAddressType = (string)req.@params[1];
                        findFliter = "{'" + NEP5transferAddressType + "':'" + NEP5transferAddress + "'}";
                        result = mh.GetDataPages(mongodbConnStr, mongodbDatabase, "Nep5Transfer", sortStr, int.Parse(req.@params[2].ToString()), int.Parse(req.@params[3].ToString()),findFliter);
                        break;
                    case "getnep5transfers":
                        //sortStr = "{'blockindex':1,'txid':1,'n':1}";
                        sortStr = "{}";
                        result = mh.GetDataPages(mongodbConnStr, mongodbDatabase, "Nep5Transfer", sortStr, int.Parse(req.@params[0].ToString()), int.Parse(req.@params[1].ToString()));
                        break;
                    case "getnep5transfersbyasset":
                        /*
                        string str_asset = ((string)req.@params[0]).formatHexStr();
                        findFliter = "{asset:'" + str_asset + "'}";
                        //sortStr = "{'blockindex':1,'txid':1,'n':1}";
                        sortStr = "{}";
                        if (req.@params.Count() ==3)
                            result = mh.GetDataPages(mongodbConnStr, mongodbDatabase, "NEP5transfer", sortStr, int.Parse(req.@params[1].ToString()), int.Parse(req.@params[2].ToString()), findFliter);
                        else
                            result = mh.GetData(mongodbConnStr, mongodbDatabase, "NEP5transfer",findFliter);
                            */
                        //
                        result = blockService.getnep5transfersbyasset(req.@params[0].ToString().formatHexStr(), int.Parse(req.@params[1].ToString()), int.Parse(req.@params[2].ToString()));
                        break;
                    case "getnep5count":
                        findFliter = "{}";
                        if (req.@params.Count() == 2)
                        {
                            string key = (string)req.@params[0];
                            string value = (string)req.@params[1];
                            findFliter = "{\"" + key + "\":\"" + value + "\"}";
                        }
                        result = getJAbyKV("nep5count", mh.GetDataCount(mongodbConnStr, mongodbDatabase, "Nep5Transfer", findFliter));
                        break;
                    case "getnep5transferbyblockindex":
                        Int64 blockindex = (Int64)req.@params[0];
                        findFliter = "{blockindex:" + blockindex + "}";
                        result = mh.GetData(mongodbConnStr, mongodbDatabase, "Nep5Transfer", findFliter);
                        break;
                    case "getaddresstxbyblockindex":
                        blockindex = (Int64)req.@params[0];
                        findFliter = "{blockindex:" + blockindex + "}";
                        result = mh.GetData(mongodbConnStr, mongodbDatabase, "address_tx", findFliter);
                        break;
                    case "gettxinfo":
                        txid = ((string)req.@params[0]).formatHexStr();
                        findFliter = "{txid:'" + (txid).formatHexStr() + "'}";
                        JArray JATx = mh.GetData(mongodbConnStr, mongodbDatabase, "tx", findFliter);
                        JObject JOTx = (JObject)JATx[0];
                        var heightforblock = (int)JOTx["blockindex"];
                        var indexforblock = -1;
                        findFliter = "{index:" + heightforblock + "}";
                        result = (JArray)mh.GetData(mongodbConnStr, mongodbDatabase, "block", findFliter)[0]["tx"];
                        for (var i = 0; i < result.Count; i++)
                        {
                            JObject Jo = (JObject)result[i];
                            if (txid == (string)Jo["txid"])
                            {
                                indexforblock = i;
                            }
                        }
                        JObject JOresult = new JObject();
                        JOresult["heightforblock"] = heightforblock;
                        JOresult["indexforblock"] = indexforblock;
                        result = new JArray() { JOresult };
                        break;
                    case "uxtoinfo":
                        var starttxid = ((string)req.@params[0]).formatHexStr();
                        var voutN = (Int64)req.@params[1];

                        findFliter = "{txid:'" + (starttxid).formatHexStr() + "'}";
                        JATx = mh.GetData(mongodbConnStr, mongodbDatabase, "tx", findFliter);
                        JOTx = (JObject)JATx[0];
                        int starttxblockheight = (int)JOTx["blockindex"];
                        int starttxblockindex = -1;
                        findFliter = "{index:" + starttxblockheight + "}";
                        result = (JArray)mh.GetData(mongodbConnStr, mongodbDatabase, "block", findFliter)[0]["tx"];
                        for (var i = 0; i < result.Count; i++)
                        {
                            JObject Jo = (JObject)result[i];
                            if (starttxid == (string)Jo["txid"])
                            {
                                starttxblockindex = i;
                            }
                        }
                        //根据txid和n获取utxo信息
                        findFliter = "{txid:\"" + starttxid + "\",n:"+ voutN + "}";
                        var endtxid = (string)mh.GetData(mongodbConnStr, mongodbDatabase, "utxo", findFliter)[0]["used"];
                        int endtxblockheight = -1;
                        int endtxblockindex = -1;
                        int vinputN = -1;
                        if (!string.IsNullOrEmpty(endtxid))
                        {
                            findFliter = "{txid:'" + (endtxid).formatHexStr() + "'}";
                            JATx = mh.GetData(mongodbConnStr, mongodbDatabase, "tx", findFliter);
                            JOTx = (JObject)JATx[0];
                            endtxblockheight = (int)JOTx["blockindex"];
                            JArray JAvin = (JArray)JOTx["vin"];
                            findFliter = "{index:" + endtxblockheight + "}";
                            result = (JArray)mh.GetData(mongodbConnStr, mongodbDatabase, "block", findFliter)[0]["tx"];
                            for (var i = 0; i < result.Count; i++)
                            {
                                JObject Jo = (JObject)result[i];
                                if (endtxid == (string)Jo["txid"])
                                {
                                    endtxblockindex = i;
                                }
                            }
                            for (var i = 0; i < JAvin.Count; i++)
                            {
                                JObject Jo = (JObject)JAvin[i];
                                if ((string)Jo["txid"] == starttxid && voutN == i)
                                {
                                    vinputN = i;
                                }
                            }

                        }
                        else
                        {
                        }

                        JOresult = new JObject();
                        JOresult["starttxid"] = starttxid;
                        JOresult["starttxblockheight"] = starttxblockheight;
                        JOresult["starttxblockindex"] = starttxblockindex;
                        JOresult["voutN"] = voutN;
                        JOresult["endtxid"] = endtxid;
                        JOresult["endtxblockheight"] = endtxblockheight;
                        JOresult["endtxblockindex"] = endtxblockindex;
                        JOresult["vinputN"] = vinputN;
                        result = new JArray() { JOresult };

                        break;

                    // ChangeLog: 新增获取notify接口
                    case "getnotifycounter":
                        result = notifyService.getNotifyCounter();
                        break;
                    case "getnotifybyhash":
                        var cc = req.@params.Count();
                        if(req.@params.Count() <3)
                        {
                            result = notifyService.getNotifyByHash(JArray.Parse(req.@params[1].ToString()), int.Parse(req.@params[0].ToString()));
                        }
                        else {
                            result = notifyService.getNotifyByHash(JArray.Parse(req.@params[2].ToString()), int.Parse(req.@params[0].ToString()), int.Parse(req.@params[1].ToString()));
                        }
                        break;
                    case "getBlockInfo":
                        JArray filterJA = null;
                        if(req.@params.Count() > 0)
                        {
                            filterJA = JArray.Parse(req.@params[0].ToString());
                        }
                        result = notifyService.getBlockInfo(filterJA);
                        break;
                    case "getNep5AssetInfo":
                        result = notifyService.getNep5AssetInfo(JArray.Parse(req.@params[0].ToString()));
                        break;

                }
                if (result != null && result.Count > 0 && result[0]["errorCode"] != null)
                {
                    JsonPRCresponse_Error resE = new JsonPRCresponse_Error(req.id, (int)result[0]["errorCode"], (string)result[0]["errorMsg"], (string)result[0]["errorData"]);

                    return resE;
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

        private void initMonitor()
        {
            string startMonitorFlag = mh.startMonitorFlag;
            if (startMonitorFlag == "1")
            {
                monitor = new Monitor();
            }
        }
        private void point(string method)
        {
            if (monitor != null)
            {
                monitor.point(netnode, method);
            }
        }
    }
}
