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

        public JArray getAllNep5AssetOfAddress(string address)
        {
            var findStr = "{ Address:'" + address + "'}";
            var nep5States = mh.GetData(mongodbConnStr, mongodbDatabase, "Nep5State", findStr);
            JArray ja = new JArray();
            for (var i = 0; i < nep5States.Count; i++)
            {
                JObject jo = new JObject();
                jo["balance"] = double.Parse((string)nep5States[i]["Balance"]["$numberDecimal"]) / (Math.Pow(10, double.Parse((string)nep5States[i]["AssetDecimals"])));
                jo["symbol"] = nep5States[i]["AssetSymbol"].ToString();
                jo["assetid"] = nep5States[i]["AssetHash"].ToString();
                ja.Add(jo);
            }

            var res = ja.Where(p => filterOldContractInfo(p, ja)).ToArray();
            return new JArray { res };
        }
        private bool filterOldContractInfo(JToken jt, JArray ja)
        {
            var contractHash = jt["assetid"].ToString();
            var relateHashArr = getRelateHashArr(contractHash);
            if (relateHashArr.Count() == 1) return true;

            var findFlag = false;
            foreach(var hash in relateHashArr)
            {
                if(hash == contractHash)
                {
                    findFlag = true;
                    continue;
                }
                if(findFlag)
                {
                    if(ja.Any(p => p["assetid"].ToString() == hash))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        private string[] getRelateHashArr(string asset)
        {
            var contractId = getContractId(asset);
            if (contractId == null) return new string[] { asset };
            //
            var findStr = new JObject { { "contractId", contractId } }.ToString();
            var queryRes = mh.GetData(mongodbConnStr, mongodbDatabase, "contract", findStr);
            //
            var hashArr = queryRes.Select(p => p["contractHash"].ToString()).ToArray();
            return hashArr;

        }
        private string getContractId(string asset)
        {
            var findStr = new JObject { { "contractHash", asset } }.ToString();
            var queryRes = mh.GetData(mongodbConnStr, mongodbDatabase, "contract", findStr);
            if (queryRes.Count == 0) return null;

            return queryRes[0]["contractId"].ToString();
        }
        public JArray getnep5transfersbyasset(string assetid, int pageNum=1, int pageSize=10)
        {
            //string findStr = new JObject() { {"asset", assetid } }.ToString();
            var hashArr = getRelateHashArr(assetid);
            var hashJOs = hashArr.Select(p => new JObject { { "asset", p } }).ToArray();
            var findStr = new JObject { { "$or", new JArray { hashJOs } } }.ToString();

            string sortStr = new JObject() { { "blockindex", -1 } }.ToString();
            long count = mh.GetDataCount(mongodbConnStr, mongodbDatabase, "Nep5Transfer", findStr);
            if (count == 0) return new JArray();

            var queryRes = mh.GetDataPages(mongodbConnStr, mongodbDatabase, "Nep5Transfer", sortStr, pageSize, pageNum, findStr);
            if (queryRes == null || queryRes.Count == 0) return new JArray { };

            long[] blockindexArr = queryRes.Select(p => long.Parse(p["blockindex"].ToString())).ToArray();
            findStr = toFilter(blockindexArr, "index").ToString();
            string fieldStr = new JObject() { { "index", 1 }, { "time", 1 } }.ToString();
            var subQueryRes = mh.GetDataWithField(mongodbConnStr, mongodbDatabase, "block", fieldStr, findStr);
            var blockindexDict = subQueryRes.ToDictionary(k => k["index"].ToString(), v => long.Parse(v["time"].ToString()));
            var res = queryRes.Select(p =>{
                JObject jo = (JObject)p;
                string blockindex = jo["blockindex"].ToString();
                jo["value"] = double.Parse(jo["value"].ToString()) / System.Math.Pow(10,double.Parse(jo["decimals"].ToString()));
                jo.Add("blocktime", blockindexDict.GetValueOrDefault(blockindex));
                return jo;
            }).ToArray();
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
