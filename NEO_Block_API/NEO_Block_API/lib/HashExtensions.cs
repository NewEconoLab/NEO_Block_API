using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThinNeo;

namespace NEO_Block_API.lib
{
    public static class HashExtensions
    {
        public static string toHexData(this string scHash, string scMethod, string[] scParams)
        {
            byte[] data = null;
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                MyJson.JsonNode_Array array = new MyJson.JsonNode_Array();
                for (var i = 0; i < scParams.Length; i++)
                {
                    array.AddArrayValue(scParams[i]);
                }
                sb.EmitParamJson(array);
                sb.EmitPushString(scMethod);
                sb.EmitAppCall(new Hash160(scHash));
                data = sb.ToArray();
            }
            var script = Helper.Bytes2HexString(data);
            return script;
        }
        public static string formatHash(this string hash)
        {
            if (hash.StartsWith("0x")) return hash;
            return "0x" + hash;
        }

        public static JObject toFilter(this long[] arr, string field, string logicalOperator = "$or")
        {
            if (arr.Count() == 1)
            {
                return new JObject() { { field, arr[0] } };
            }
            return new JObject() { { logicalOperator, new JArray() { arr.Select(item => new JObject() { { field, item } }).ToArray() } } };
        }

        public static JObject toFilter(this IEnumerable<string> arr, string field, string logicalOperator = "$or")
        {
            if (arr.Count() == 1)
            {
                return new JObject() { { field, arr.ToList()[0].ToString() } };
            }
            return new JObject() { { logicalOperator, new JArray() { arr.Select(item => new JObject() { { field, item } }).ToArray() } } };
        }
    }
}
