using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NEO_Block_API.RPC;
using NEO_Block_API.lib;

namespace NEO_Block_API.Controllers
{
    //[RpcRoute("api/[controller]")]
    [Route("api/[controller]")]
    public class TestnetController : Controller
    {
        mongoHelper mh = new mongoHelper();

        private JsonResult getRes(JsonRPCrequest req)
        {
            JArray result = new JArray();
            string findFliter = string.Empty;
            switch (req.method)
            {
                case "getblock":
                    findFliter = "{index:" + req.@params[0] + "}";
                    result = mh.GetData(mh.mongodbConnStr_testnet, mh.mongodbDatabase_testnet, "block", findFliter);
                    break;
                case "gettransaction":
                    findFliter = "{txid:'" + req.@params[0] + "'}";
                    result = mh.GetData(mh.mongodbConnStr_testnet, mh.mongodbDatabase_testnet, "tx", findFliter);
                    break;
                case "getfulllog":
                    findFliter = "{txid:'" + req.@params[0] + "'}";
                    result = mh.GetData(mh.mongodbConnStr_testnet, mh.mongodbDatabase_testnet, "fulllog", findFliter);
                    break;
                case "getnotify":
                    findFliter = "{txid:'" + req.@params[0] + "'}";
                    result = mh.GetData(mh.mongodbConnStr_testnet, mh.mongodbDatabase_testnet, "notify", findFliter);
                    break;
                case "getutxo":
                    findFliter = "{Addr:'" + req.@params[0] + "'}";
                    result = mh.GetData(mh.mongodbConnStr_testnet, mh.mongodbDatabase_testnet, "utxo", findFliter);
                    break;
            }

            JsonPRCresponse res = new JsonPRCresponse();
            res.jsonrpc = req.jsonrpc;
            res.id = req.id;
            res.result = result;

            return Json(res);
        }

        [HttpGet]
        public JsonResult Get(string @jsonrpc, string @method, string @params, long @id)
        {

            try
            {
                JsonRPCrequest req = new JsonRPCrequest
                {
                    jsonrpc = @jsonrpc,
                    method = @method,
                    @params = JsonConvert.DeserializeObject<object[]>(JsonConvert.SerializeObject(JArray.Parse(@params))),
                    id = @id
                };

                return getRes(req);
            }
            catch (Exception e)
            {
                JsonPRCresponse_Error resE = new JsonPRCresponse_Error();
                resE.jsonrpc = "2.0";
                resE.id = 0;
                resE.error.code = -100;
                resE.error.message = "Parameter error";
                resE.error.data = e.Message;

                return Json(resE);

            }
        }

        [HttpPost]
        public JsonResult Post([FromBody]JsonRPCrequest req)
        {
            return getRes(req);
        }

    }
}