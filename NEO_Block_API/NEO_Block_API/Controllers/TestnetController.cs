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
    [Route("api/[controller]")]
    public class TestnetController : Controller
    {
        mongoHelper mh = new mongoHelper();
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "testnet1", "testnet2" };
        }

        [HttpPost]
        public string Post([FromBody]JsonRPCrequest jsonR)
        {


            JsonRPCrequest req = jsonR; //new JsonRPCrequest(jsonR);
            string findFliter = "{index:" + jsonR.@params[0] + "}";
            JArray result = mh.GetData(mh.mongodbConnStr_testnet, mh.mongodbDatabase_testnet, "block", findFliter);

            JsonPRCresponse res = new JsonPRCresponse();
            res.jsonrpc = req.jsonrpc;
            res.id = req.id;
            res.result = result;

            return JsonConvert.SerializeObject(res);

            //return "Hello World ";
        }
    }
}