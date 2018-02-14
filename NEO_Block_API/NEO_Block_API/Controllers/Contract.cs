using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NEO_Block_API.lib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NEO_Block_API.Controllers
{
    public class Contract
    {
        public string hexstring2String(string hexstring){
           return  Encoding.UTF8.GetString(hexstring.HexString2Bytes());
        }

        public JObject getContractState(string neoCliJsonRPCUrl, string scripthash)
        {
            httpHelper hh = new httpHelper();
            var resp = hh.Post(neoCliJsonRPCUrl, "{'jsonrpc':'2.0','method':'getcontractstate','params':['" + scripthash + "'],'id':1}", System.Text.Encoding.UTF8, 1);

            JObject resultJ = (JObject)JObject.Parse(resp)["result"];

            return resultJ;
        }

        public JObject invokeScript(string neoCliJsonRPCUrl, string script)
        {
            httpHelper hh = new httpHelper();
            var resp = hh.Post(neoCliJsonRPCUrl, "{'jsonrpc':'2.0','method':'invokescript','params':['" + script + "'],'id':1}", System.Text.Encoding.UTF8, 1);

            JObject resultJ = (JObject)JObject.Parse(resp)["result"];

            return resultJ;
        }

        public JObject callContractForTest(string neoCliJsonRPCUrl, string scripthash, JArray paramsJA)
        {
            string script = (string)getContractState(neoCliJsonRPCUrl, scripthash)["script"];

            var json = MyJson.Parse(JsonConvert.SerializeObject(paramsJA)).AsList();
            
            ThinNeo.ScriptBuilder sb = new ThinNeo.ScriptBuilder();
            var list = json.AsList();
            for (int i = list.Count - 1; i >= 0; i--)
            {
                sb.EmitParamJson(list[i]);
            }

            var scripthashReverse = ThinNeo.Helper.HexString2Bytes(scripthash).Reverse().ToArray();
            sb.EmitAppCall(scripthashReverse);

            string scriptPlusParams = ThinNeo.Helper.Bytes2HexString(sb.ToArray());

            return invokeScript(neoCliJsonRPCUrl, scriptPlusParams);
        }
    }
}
