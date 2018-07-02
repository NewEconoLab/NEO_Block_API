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
        httpHelper hh = new httpHelper();

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

        public JObject getStorage(string neoCliJsonRPCUrl, string contractHash, string keyHexstring)
        {
            var resp = hh.Post(neoCliJsonRPCUrl, "{'jsonrpc':'2.0','method':'getstorage','params':['" + contractHash + "','" + keyHexstring + "'],'id':1}", System.Text.Encoding.UTF8, 1);
            string storageValue = (string)JObject.Parse(resp)["result"];

            JObject resultJ = new JObject();
            resultJ.Add("storagevalue", storageValue);

            return resultJ;
        }

        public JObject invokeScript(string neoCliJsonRPCUrl, string script)
        {          
            var resp = hh.Post(neoCliJsonRPCUrl, "{'jsonrpc':'2.0','method':'invokescript','params':['" + script + "'],'id':1}", System.Text.Encoding.UTF8, 1);

            JObject resultJ = (JObject)JObject.Parse(resp)["result"];

            return resultJ;
        }

        
        public JObject callContractForTest(string neoCliJsonRPCUrl, List<string> scripthashs, JArray paramsJA)
        {
            //string script = (string)getContractState(neoCliJsonRPCUrl, scripthash)["script"];
            /*
            int n = 0;
            ThinNeo.ScriptBuilder sb = new ThinNeo.ScriptBuilder();

            foreach (var scripthash in scripthashs)
            {

                httpHelper hh = new httpHelper();

                var json = MyJson.Parse(JsonConvert.SerializeObject(paramsJA[n])).AsList();

                var list = json.AsList();
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    sb.EmitParamJson(list[i]);
                }

                var scripthashReverse = ThinNeo.Helper.HexString2Bytes(scripthash).Reverse().ToArray();
                sb.EmitAppCall(scripthashReverse);

                n++;
            }

            string scriptPlusParams = ThinNeo.Helper.Bytes2HexString(sb.ToArray());
            return invokeScript(neoCliJsonRPCUrl, scriptPlusParams);
            */


            /// ChangeLog:
            /// 批量调用invokeScript时，个别出错会导致，出错之后结果数据丢失。所以修改为单个单个查询，
            /// 为保证外层解析数据的顺序性，需将出错的结果位置填充数值
            ///
            JObject res = new JObject();
            JArray stackList = new JArray();
            int n = 0;
            foreach (var scripthash in scripthashs)
            {
                ThinNeo.ScriptBuilder tmpSb = new ThinNeo.ScriptBuilder();
                httpHelper hh = new httpHelper();
                var json = MyJson.Parse(JsonConvert.SerializeObject(paramsJA[n])).AsList();
                var list = json.AsList();
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    tmpSb.EmitParamJson(list[i]);
                }

                var scripthashReverse = ThinNeo.Helper.HexString2Bytes(scripthash).Reverse().ToArray();
                tmpSb.EmitAppCall(scripthashReverse);
                string invokeSc = ThinNeo.Helper.Bytes2HexString(tmpSb.ToArray());
                JObject invokeRs = invokeScript(neoCliJsonRPCUrl, invokeSc);

                string state = invokeRs["state"].ToString();
                JArray stack = (JArray)invokeRs["stack"];
                JObject stack1 = null;
                if (state.StartsWith("FAULT"))
                {
                    // 调用合约出错，填充占位
                    stack1 = new JObject();
                    stack1.Add("type", "FAULT");
                    stack1.Add("value", "");
                }
                else
                {
                    stack1 = (JObject)stack[0];
                }
                stackList.Add(stack1);
            }
            res.Add("stack", stackList);

            // debug info
            /*
            n = 0;
            List<JObject> rlist = new List<JObject>();  
            foreach (var scripthash in scripthashs)
            {
                ThinNeo.ScriptBuilder tmpSb = new ThinNeo.ScriptBuilder();
                httpHelper hh = new httpHelper();
                var json = MyJson.Parse(JsonConvert.SerializeObject(paramsJA[n])).AsList();
                var list = json.AsList();
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    tmpSb.EmitParamJson(list[i]);
                }
                var scripthashReverse = ThinNeo.Helper.HexString2Bytes(scripthash).Reverse().ToArray();
                tmpSb.EmitAppCall(scripthashReverse);
                string invokeSc = ThinNeo.Helper.Bytes2HexString(tmpSb.ToArray());
                JObject invokeRs = invokeScript(neoCliJsonRPCUrl, invokeSc);
                JObject obj = new JObject();
                obj.Add("sc", scripthash);
                obj.Add("ic", invokeSc);
                obj.Add("rs", invokeRs);
                rlist.Add(obj);
            }
            */
            return res;

        }

        public JObject publishContractForTest(string neoCliJsonRPCUrl, string avmHexstring, JObject infoJ)
        {
            string cName = (string)infoJ["cName"];
            string cVersion = (string)infoJ["cVersion"];
            string cAuthor = (string)infoJ["cAuthor"];
            string cEmail = (string)infoJ["cEmail"];
            string cDescription = (string)infoJ["cDescription"];         
            bool iStorage = (bool)infoJ["iStorage"];
            bool iDyncall = (bool)infoJ["iDyncall"];
            string inputParamsType = (string)infoJ["inputParamsType"];
            string outputParamsType = (string)infoJ["outputParamsType"];

            //实例化脚本构造器
            ThinNeo.ScriptBuilder sb = new ThinNeo.ScriptBuilder();
            //加入合约基本信息
            sb.EmitPushString(cDescription);
            sb.EmitPushString(cEmail);
            sb.EmitPushString(cAuthor);
            sb.EmitPushString(cVersion);
            sb.EmitPushString(cName);

            //加入是否需要私有存储区、是否需要动态合约调用信息
            int need_storage = iStorage == true ? 1 : 0;
            int need_nep4 = iDyncall == true ? 2 : 0;
            sb.EmitPushNumber(need_storage | need_nep4);//二进制或操作

            //加入输入输出参数类型信息
            var outputType = ThinNeo.Helper.HexString2Bytes(outputParamsType);
            var inputType = ThinNeo.Helper.HexString2Bytes(inputParamsType);
            sb.EmitPushBytes(outputType);
            sb.EmitPushBytes(inputType);

            //加入合约编译后二进制码
            var contractScript = ThinNeo.Helper.HexString2Bytes(avmHexstring);
            sb.EmitPushBytes(contractScript);

            sb.EmitSysCall("Neo.Contract.Create");

            string scriptPublish = ThinNeo.Helper.Bytes2HexString(sb.ToArray());

            //调用cli RPC 用neoVM试运行，并获得费用估算
            return invokeScript(neoCliJsonRPCUrl, scriptPublish);
        }
    }
}
