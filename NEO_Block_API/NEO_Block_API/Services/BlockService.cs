using NEO_Block_API.Controllers;
using NEO_Block_API.lib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NEO_Block_API.Services
{
    public class BlockService
    {
        public mongoHelper mh { get; set; }
        public string mongodbConnStr { get; set; }
        public string mongodbDatabase { get; set; }
        public string Ana_mongodbConnStr { get; set; }
        public string Ana_mongodbDatabase { get; set; }
        public string neoCliJsonRPCUrl { get; set; }
        Contract ct = new Contract();


        private string getNewContractHash(string hash)
        {
            var findStr = new JObject { { "updateBeforeHash", hash } }.ToString();
            var queryRes = mh.GetData(Ana_mongodbConnStr, Ana_mongodbDatabase, "contract_update_info", findStr);
            if (queryRes.Count == 0) return hash;

            return queryRes[0]["hash"].ToString();
        }
        private string getSymbol(string asset)
        {
            var findStr = new JObject { { "assetid", asset } }.ToString();
            var queryRes = mh.GetData(mongodbConnStr, mongodbDatabase, "NEP5asset", findStr);
            if (queryRes.Count == 0) return "nil";

            var item = queryRes[0];
            return item["symbol"].ToString();
        }
        public JArray getallnep5assetofaddress(string address, int balanceFlag = 0)
        {
            var findStr = new JObject { { "to", address } }.ToString();
            var queryRes = mh.GetData(mongodbConnStr, mongodbDatabase, "NEP5transfer", findStr);
            if (queryRes.Count == 0)
            {
                return new JArray();
            }

            //var assetInfoDict = queryRes.ToDictionary(k => k["asset"].ToString(), v=>getSymbol(v["asset"].ToString()));
            var assets = queryRes.Select(p => p["asset"].ToString()).Distinct().ToArray();
            var assetArr = assets.Select(p =>
            {
                var jo = new JObject();
                jo["asset"] = p;
                jo["symbol"] = getSymbol(p);
                return jo;
            }).ToArray();
            if(balanceFlag == 0)
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
                var asset = item["asset"].ToString();
                var newAsset = getNewContractHash(asset);
                nep5Hashs.Add(newAsset);
                queryParams.Add(JArray.Parse("['(str)balanceOf',['(hex)" + NEP5allAssetOfAddrHashHex + "']]"));
            }

            JArray NEP5allAssetBalanceJA = (JArray)ct.callContractForTestMulti(neoCliJsonRPCUrl, nep5Hashs, queryParams)["stack"];
            foreach (var abt in assetArr)
            {
                try
                {
                    var index = new JArray { assetArr }.IndexOf(abt);
                    string allBalanceStr = (string)NEP5allAssetBalanceJA[index]["value"];
                    string allBalanceType = (string)NEP5allAssetBalanceJA[index]["type"];

                    //获取NEP5资产信息，获取精度
                    NEP5.Asset NEP5asset = new NEP5.Asset(mongodbConnStr, mongodbDatabase, abt["asset"].ToString());

                    abt["balance"] = NEP5.getNumStrFromStr(allBalanceType, allBalanceStr, NEP5asset.decimals);
                }
                catch (Exception e)
                {
                    Console.WriteLine(abt["asset"].ToString() + ",ConvertTypeFailed,errMsg:" + e.Message);
                    abt["balance"] = "";
                }
            }
            var res = assetArr.Where(p => p["balance"].ToString() != "" && p["balance"].ToString() != "0").ToArray();

            return new JArray { res };      
        }
        public JArray getnep5transfersbyasset(string assetid, int pageNum=1, int pageSize=10)
        {
            string findStr = new JObject() { {"asset", assetid } }.ToString();
            long count = mh.GetDataCount(mongodbConnStr, mongodbDatabase, "NEP5transfer", findStr);
            if (count == 0) return new JArray();

            var sortStr = new JObject { { "blockindex",-1} }.ToString();
            var queryRes = mh.GetDataPages(mongodbConnStr, mongodbDatabase, "NEP5transfer", sortStr, pageSize, pageNum, findStr);
            if (queryRes == null || queryRes.Count == 0) return new JArray { };

            long[] blockindexArr = queryRes.Select(p => long.Parse(p["blockindex"].ToString())).ToArray();
            findStr = toFilter(blockindexArr, "index").ToString();
            string fieldStr = new JObject() { { "index", 1 }, { "time", 1 } }.ToString();
            var subQueryRes = mh.GetDataWithField(mongodbConnStr, mongodbDatabase, "block", fieldStr, findStr);
            var blockindexDict = subQueryRes.ToDictionary(k => k["index"].ToString(), v => long.Parse(v["time"].ToString()));
            var res = queryRes.Select(p =>{
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

        public JObject toFilter(long[] arr, string field, string logicalOperator = "$or")
        {
            if (arr.Count() == 1)
            {
                return new JObject() { { field, arr[0] } };
            }
            return new JObject() { { logicalOperator, new JArray() { arr.Select(item => new JObject() { { field, item } }).ToArray() } } };
        }
    }
}
