using NEO_Block_API.lib;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace NEO_Block_API.Services
{
    public class NotifyService
    {
        public mongoHelper mh { get; set; }
        public string mongodbConnStr { get; set; }
        public string mongodbDatabase { get; set; }

        public JArray getNotifyCounter()
        {
            string findStr = new JObject() { {"counter", "notify" } }.ToString();
            var queryRes = mh.GetData(mongodbConnStr, mongodbDatabase, "system_counter", findStr);

            long lastBlockindex = 0;
            if(queryRes != null && queryRes.Count() > 0)
            {
                lastBlockindex = long.Parse(queryRes[0]["lastBlockindex"].ToString());
            }

            return new JArray { new JObject() { { "lastBlockindex", lastBlockindex } } };
        }

        public JArray getNotifyByHash(JArray hashJA, int startBlockindex, int pageSize=10)
        {
            if (pageSize > 10) pageSize = 10;
            var hashs = hashJA.Select(p => p.ToString()).ToArray();
            var findJO = toFilter(hashs, "executions.notifications.contract");
            findJO.Add("blockindex", new JObject() { {"$gt", startBlockindex }, { "$lte", startBlockindex+pageSize} });
            string findStr = findJO.ToString();
            return mh.GetData(mongodbConnStr, mongodbDatabase, "notify", findStr);
        }

        private JObject toFilter(string[] hashs, string field)
        {
            if (hashs == null || hashs.Count() == 0) return null;
            if (hashs.Count() == 1) return new JObject() { { field, hashs[0]} };
            return new JObject(){{ "$or", new JArray{
                hashs.Distinct().Select(p => new JObject() { { field, p } }).ToArray()
            } } };
        }
        private JObject toFilter(long[] indexs, string field)
        {
            if (indexs == null || indexs.Count() == 0) return null;
            if (indexs.Count() == 1) return new JObject() { { field, indexs[0] } };
            return new JObject(){{ "$or", new JArray{
                indexs.Distinct().Select(p => new JObject() { { field, p } }).ToArray()
            } } };
        }
        private JObject toReturn(string[] fields, bool removeId=true)
        {
            JObject obj = new JObject();
            foreach (var field in fields)
            {
                obj.Add(field, 1);
            }
            if (removeId) obj.Add("_id", 0);
            return obj;
        }


        public JArray getNep5AssetInfo(JArray hashJA)
        {
            var hashs = hashJA.Select(p => p.ToString()).ToArray();
            string findStr = toFilter(hashs, "assetid").ToString();
            return mh.GetData(mongodbConnStr, mongodbDatabase, "Nep5AssetInfo", findStr);
        }

        public JArray getBlockInfo(JArray indexJA)
        {
            if (indexJA == null) return getBlockInfoTop3();
            var indexs = indexJA.Select(p => long.Parse(p.ToString())).ToArray();
            string findStr = toFilter(indexs, "index").ToString();
            string fieldStr = toReturn(new string[] {"index", "time", "hash"}).ToString();

            return mh.GetDataWithField(mongodbConnStr, mongodbDatabase, "block", fieldStr, findStr);
        }
        private JArray getBlockInfoTop3()
        {
            string findStr = "{}";
            string fieldStr = new JObject() { { "index", 1 }, { "hash", 1 }, { "time", 1 }, { "_id", 0 } }.ToString();
            string sortStr = new JObject() { { "index", -1 } }.ToString();
            return mh.GetDataPagesWithField(mongodbConnStr, mongodbDatabase, "block", fieldStr, sortStr, 3, 1, findStr);
        }
    }
}
