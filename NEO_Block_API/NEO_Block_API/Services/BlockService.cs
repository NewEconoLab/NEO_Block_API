using NEO_Block_API.lib;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace NEO_Block_API.Services
{
    public class BlockService
    {
        public mongoHelper mh { get; set; }
        public string mongodbConnStr { get; set; }
        public string mongodbDatabase { get; set; }

        public JArray getnep5transfersbyasset(string assetid, int pageNum=1, int pageSize=10)
        {
            string findStr = new JObject() { {"asset", assetid } }.ToString();
            long count = mh.GetDataCount(mongodbConnStr, mongodbDatabase, "NEP5transfer", findStr);
            if (count == 0) return new JArray();

            var queryRes = mh.GetDataPages(mongodbConnStr, mongodbDatabase, "NEP5transfer", "{}", pageSize, pageNum, findStr);
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
