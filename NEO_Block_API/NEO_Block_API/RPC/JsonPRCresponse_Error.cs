using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NEO_Block_API.RPC
{
    public class JsonPRCresponse_Error
    {
        public JsonPRCresponse_Error()
        {
            error = new JsonError();
        }

        public JsonPRCresponse_Error(long reqID,int errCode,string errMsg,string errData)
        {
            error = new JsonError();

            jsonrpc = "2.0";
            id = reqID;
            error.code = errCode;
            error.message = errMsg;
            error.data = errData;
        }

        public string jsonrpc { get; set; }
        public long id { get; set; }
        public JsonError error { get; set; }
    }

    public class JsonError
    {
        public int code { get; set; }
        public string message { get; set; }
        public string data { get; set; }
    }
}
