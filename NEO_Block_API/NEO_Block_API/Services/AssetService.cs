﻿using NEO_Block_API.Controllers;
using NEO_Block_API.lib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NEO_Block_API.Services
{
    public class AssetService
    {
        public mongoHelper mh { get; set; }
        public string block_mongodbConnStr { get; set; }
        public string block_mongodbDatabase { get; set; }
        public string analy_mongodbConnStr { get; set; }
        public string analy_mongodbDatabase { get; set; }
        public string BlockCol { get; set; } = "block";
        public string NEP5assetCol { get; set; } = "NEP5asset";
        public string NEP5transferCol { get; set; } = "NEP5transfer";
        public string ContractExecDetailCol { get; set; } = "contract_exec_detail";
        private AssetHelper ah = new AssetHelper();
        //
        public string neoCliJsonRPCUrl { get; set; }
        Contract ct = new Contract();

        private JArray getRes(JToken res = null) => res == null ? new JArray { }: new JArray { res };

        public JArray getallnep5asset()
        {
            var findStr = "{}";
            return mh.GetData(block_mongodbConnStr, block_mongodbDatabase, NEP5assetCol, findStr);
        }
        public JArray getnep5asset(string assetid)
        {
            assetid = assetid.formatHash();
            if (hasFormatAssetIdToNext(assetid)) return getRes();
            var findStr = new JObject { { "assetid", assetid } }.ToString();
            var queryRes = mh.GetData(block_mongodbConnStr, block_mongodbDatabase, NEP5assetCol, findStr);
            if (queryRes.Count == 0) return getRes();

            var item = queryRes[0];
            var res = new JObject();
            res["assetid"] = item["assetid"];
            //res["totalsupply"] = item["totalsupply"];
            res["totalsupply"] = ah.getTotalSupply(neoCliJsonRPCUrl, assetid, int.Parse(item["decimals"].ToString()));
            res["name"] = item["name"];
            res["symbol"] = item["symbol"];
            res["decimals"] = item["decimals"];
            return getRes(res);
        }
        public JArray getnep5count(string key="", string val="")
        {
            var findStr = "{}";
            if(key != "" || val != "")
            {
                if (key == "asset")
                {
                    val = val.formatHash();
                    if(hasFormatAssetIdToNext(val))
                    {
                        return getRes(new JObject { { "nep5count", 0 } });
                    }
                    findStr = formatAssetIdToPrevMany(val).toFilter(key).ToString();
                } else
                {
                    findStr = new JObject { { key, val } }.ToString();
                }
                
            }
            var count = mh.GetDataCount(block_mongodbConnStr, block_mongodbDatabase, NEP5transferCol, findStr);
            return getRes(new JObject { { "nep5count", count } });
        }
        public JArray getnep5transfersbyasset(string assetid, int pageNum=1, int pageSize=10)
        {
            assetid = assetid.formatHash();
            if (hasFormatAssetIdToNext(assetid))
            {
                return getRes();
            }
            var findStr = formatAssetIdToPrevMany(assetid).toFilter("asset").ToString();
            //var findStr = new JObject() { { "asset", assetid } }.ToString();
            var count = mh.GetDataCount(block_mongodbConnStr, block_mongodbDatabase, NEP5transferCol, findStr);
            if (count == 0) return getRes();

            var sortStr = new JObject { { "blockindex", -1 } }.ToString();
            var queryRes = mh.GetDataPages(block_mongodbConnStr, block_mongodbDatabase, NEP5transferCol, sortStr, pageSize, pageNum, findStr);
            if (queryRes == null || queryRes.Count == 0) return getRes();

            var blockindexArr = queryRes.Select(p => long.Parse(p["blockindex"].ToString())).ToArray();
            findStr = blockindexArr.toFilter("index").ToString();
            var fieldStr = new JObject() { { "index", 1 }, { "time", 1 } }.ToString();
            var subQueryRes = mh.GetDataWithField(block_mongodbConnStr, block_mongodbDatabase, BlockCol, fieldStr, findStr);
            var blockindexDict = subQueryRes.ToDictionary(k => k["index"].ToString(), v => long.Parse(v["time"].ToString()));
            var res = queryRes.Select(p => {
                JObject jo = (JObject)p;
                string blockindex = jo["blockindex"].ToString();
                jo.Add("blocktime", blockindexDict.GetValueOrDefault(blockindex));
                return jo;
            }).OrderByDescending(p => long.Parse(p["blockindex"].ToString())).ToArray();
            //
            return new JArray { new JObject() {
                {"count", count },
                {"list", new JArray { res } }
            } };
        }
        public JArray getallnep5assetofaddress(string address, int balanceFlag = 0)
        {
            var findStr = new JObject { { "to", address } }.ToString();
            var queryRes = mh.GetData(block_mongodbConnStr, block_mongodbDatabase, "NEP5transfer", findStr);
            if (queryRes.Count == 0)
            {
                return new JArray();
            }

            //var assetInfoDict = queryRes.ToDictionary(k => k["asset"].ToString(), v=>getSymbol(v["asset"].ToString()));
            var assets = queryRes.Select(p => p["asset"].ToString()).Distinct().ToArray();
            var assetArr = assets.Select(p =>
            {
                var jo = new JObject();
                jo["assetid"] = p;
                jo["symbol"] = getSymbol(p);
                return jo;
            }).ToArray();
            if (balanceFlag == 0)
            {
                return new JArray { assetArr };
            }

            //
            byte[] NEP5allAssetOfAddrHash = ThinNeo.Helper.GetPublicKeyHashFromAddress(address);
            string NEP5allAssetOfAddrHashHex = ThinNeo.Helper.Bytes2HexString(NEP5allAssetOfAddrHash.Reverse().ToArray());
            List<string> nep5Hashs = new List<string>();
            JArray queryParams = new JArray();
            foreach (var item in assetArr)
            {
                var asset = item["assetid"].ToString();
                var newAsset = formatAssetIdToLast(asset);
                nep5Hashs.Add(newAsset);
                queryParams.Add(JArray.Parse("['(str)balanceOf',['(hex)" + NEP5allAssetOfAddrHashHex + "']]"));
            }

                JArray NEP5allAssetBalanceJA = (JArray)ct.callContractForTestMulti(neoCliJsonRPCUrl, nep5Hashs, queryParams)["stack"];
                foreach(var abt in assetArr)
                {
                    try
                    {
                        var asset = abt["assetid"].ToString();
                        var item = NEP5allAssetBalanceJA.Where(p => p["hash"].ToString() == asset).ToArray()[0];
                        if (item["value"] == null) continue;
                        if (item["value"].ToString() == "") continue;
                        var allBalanceStr = item["value"].ToString();
                        var allBalanceType = item["type"].ToString();

                        //获取NEP5资产信息，获取精度
                        NEP5.Asset NEP5asset = new NEP5.Asset(block_mongodbConnStr, block_mongodbDatabase, abt["assetid"].ToString());

                        abt["balance"] = NEP5.getNumStrFromStr(allBalanceType, allBalanceStr, NEP5asset.decimals);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(abt["assetid"].ToString() + ",ConvertTypeFailed,errMsg:" + e.Message);
                        abt["balance"] = "";
                    }
                }
            /*
            foreach (var abt in assetArr)
            {
                try
                {
                    var index = new JArray { assetArr }.IndexOf(abt);
                    string allBalanceStr = (string)NEP5allAssetBalanceJA[index]["value"];
                    string allBalanceType = (string)NEP5allAssetBalanceJA[index]["type"];

                    //获取NEP5资产信息，获取精度
                    NEP5.Asset NEP5asset = new NEP5.Asset(block_mongodbConnStr, block_mongodbDatabase, abt["assetid"].ToString());

                    abt["balance"] = NEP5.getNumStrFromStr(allBalanceType, allBalanceStr, NEP5asset.decimals);
                }
                catch (Exception e)
                {
                    Console.WriteLine(abt["assetid"].ToString() + ",ConvertTypeFailed,errMsg:" + e.Message);
                    abt["balance"] = "";
                }
            }
            */
            var res = assetArr.Where(p => p["balance"] !=null && p["balance"].ToString() != "" && p["balance"].ToString() != "0").ToArray();

            return new JArray { res };
        }

        private string formatAssetIdToLast(string assetid)
        {
            var limitCount = 20;
            while (--limitCount >= 0)
            {
                if (!formatAssetIdAfterUpdate(assetid, out string newAssetId, "from")) break;
                assetid = newAssetId;
            }
            return assetid;
        }
        private List<string> formatAssetIdToPrevMany(string assetid)
        {
            var list = new List<string>();
            var limitCount = 20;
            while (--limitCount >= 0)
            {
                list.Add(assetid);
                if (!formatAssetIdAfterUpdate(assetid, out string newAssetId, "to")) break;
                assetid = newAssetId;
            }
            return list;
        }
        private bool hasFormatAssetIdToNext(string assetid)
        {
            return formatAssetIdAfterUpdate(assetid, out _, "from");
        }
        private bool formatAssetIdAfterUpdate(string assetid, out string updateAssetId, string key= "from")
        {
            var resKey = key == "from" ? "to" : "from";
            updateAssetId = "";
            var findStr = new JObject { { "type", 3/*升级合约*/}, { key, assetid } }.ToString();
            var queryRes = mh.GetData(analy_mongodbConnStr, analy_mongodbDatabase, ContractExecDetailCol, findStr);
            if (queryRes.Count == 0) return false;

            updateAssetId = queryRes[0][resKey].ToString();
            return true;
        }

        private string getSymbol(string asset)
        {
            var findStr = new JObject { { "assetid", asset } }.ToString();
            var queryRes = mh.GetData(block_mongodbConnStr, block_mongodbDatabase, "NEP5asset", findStr);
            if (queryRes.Count == 0) return "nil";

            var item = queryRes[0];
            return item["symbol"].ToString();
        }
    }



    class AssetHelper
    {
        private httpHelper hh = new httpHelper();
        public AssetHelper()
        {
        }
        public int getDecimals(string neoCliJsonRPCUrl, string assetid)
        {
            var scHash = assetid.formatHash();
            var scMethod = "decimals";
            var script = scHash.toHexData(scMethod, new string[0]);
            var res = invokescript(neoCliJsonRPCUrl, script);

            var resStr = "0";
            try
            {
                var type = (((JArray)JObject.Parse(res)["result"]["stack"])[0]["type"].ToString());
                var value = (((JArray)JObject.Parse(res)["result"]["stack"])[0]["value"].ToString());
                resStr = NEP5.getNumStrFromStr(type, value, 0);
            }
            catch {}
            return int.Parse(res);
        }
        public string getTotalSupply(string neoCliJsonRPCUrl, string assetid, int decimals)
        {
            var scHash = assetid.formatHash();
            var scMethod = "totalSupply";
            var script = scHash.toHexData(scMethod, new string[0]);
            var res = invokescript(neoCliJsonRPCUrl, script);

            var resStr = "0";
            try
            {
                var type = (((JArray)JObject.Parse(res)["result"]["stack"])[0]["type"].ToString());
                var value = (((JArray)JObject.Parse(res)["result"]["stack"])[0]["value"].ToString());
                resStr = NEP5.getNumStrFromStr(type, value, decimals);
            }
            catch { }
            return resStr;
        }
        private string invokescript(string neoCliJsonRPCUrl, string script)
        {
            var postData = new JObject {
                {"jsonrpc", "2.0" },
                {"method", "invokescript" },
                {"params", new JArray{ script} },
                {"id",1 }
            }.ToString();
            var res = hh.Post(neoCliJsonRPCUrl, postData, System.Text.Encoding.UTF8, 1);
            return res;
        }
    }

    
}
