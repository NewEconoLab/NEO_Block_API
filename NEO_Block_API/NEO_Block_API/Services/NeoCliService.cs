using NEO_Block_API.lib;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace NEO_Block_API.Services
{
    public class NeoCliService
    {
        public httpHelper hh { get; set; }
        public string neoCliJsonRPCUrl { get; set; }

        public JArray getRawTransaction(string txid)
        {
            var res = HttpPost(neoCliJsonRPCUrl, "getrawtransaction", new JArray { new JValue(txid), 1 });
            var result = JObject.Parse(res)["result"];
            return new JArray { result };
        }

        public JArray getTxidFromMemPool(string txid)
        {
            if (!txid.StartsWith("0x")) txid = "0x" + txid;
            var res = HttpPost(neoCliJsonRPCUrl, "getrawmempool", new JArray { });
            var result = JObject.Parse(res)["result"];
            if(result != null && result is JArray ja)
            {
                int memPoolCount = ja.Count();
                bool isExistPool = ja.ToList().Any(p => p.ToString() == txid);
                return new JArray { new JObject { { "isExistPool", isExistPool}, { "memPoolCount", memPoolCount} } };
            }
            return new JArray { JObject.Parse(res)["error"] };
        }

        public JArray getRawMemPoolList()
        {
            var res = HttpPost(neoCliJsonRPCUrl, "getrawmempool", new JArray { });
            var result = JObject.Parse(res)["result"];
            if (result != null)
            {
                return new JArray { new JObject() { { "info", result } } };
            }
            return new JArray { JObject.Parse(res)["error"] };
        }
        public JArray getRawMemPoolGroup()
        {
            var res = HttpPost(neoCliJsonRPCUrl, "getrawmempool", new JArray { new JValue(true) });
            var result = JObject.Parse(res)["result"];
            if(result != null)
            {
                return new JArray { new JObject() { { "info", result } } } ;
            }
            return new JArray { JObject.Parse(res)["error"] };
        }

        public JArray getRawMemPoolCount(string hasVerified="all"/*取值all/verified/unverified*/)
        {
            var res = HttpPost(neoCliJsonRPCUrl, "getrawmempool", new JArray { new JValue(true) });
            var result = JObject.Parse(res)["result"];

            if(result != null)
            {
                int count = 0;
                if (result is JArray ja)
                {
                    count = ja.Count;
                } else
                {
                    hasVerified = hasVerified.ToLower();
                    if(hasVerified == "verified")
                    {
                        count = ((JArray)result["verified"]).Count;
                    } else if (hasVerified == "unverified")
                    {
                        count = ((JArray)result["unverified"]).Count;
                    } else
                    {
                        count = ((JArray)result["verified"]).Count + ((JArray)result["unverified"]).Count;
                    }
                } 
                return new JArray { new JObject() { { "count", count } } };
            }
            return new JArray { JObject.Parse(res)["error"] };
        }

        private string HttpPost(string url, string method, JArray param)
        {
            string filter = new JObject() {
                {"jsonrpc", "2.0" },
                {"method", method },
                {"params", param },
                {"id", "1" }
            }.ToString();
            return hh.Post(url, filter, System.Text.Encoding.UTF8);
        }
    }
}
