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
            var hashs = hashJA.Select(p => p.ToString()).ToArray();
            var findJO = toNotifyFilter(hashs);
            findJO.Add("blockindex", new JObject() { {"$gt", startBlockindex }, { "$lte", startBlockindex+pageSize} });
            string findStr = findJO.ToString();
            return mh.GetData(mongodbConnStr, mongodbDatabase, "notify", findStr);
        }

        private JObject toNotifyFilter(string[] hashs)
        {
            if (hashs == null || hashs.Count() == 0) return null;
            if (hashs.Count() == 1) return new JObject() { { "executions.notifications.contract", hashs[0]} };
            return new JObject(){{ "$or", new JArray{
                hashs.Distinct().Select(p => new JObject() { "executions.notifications.contract", p}).ToArray()
            } } };
        }
    }
}
