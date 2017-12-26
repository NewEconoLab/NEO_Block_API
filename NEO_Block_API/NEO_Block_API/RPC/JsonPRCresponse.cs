using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NEO_Block_API.RPC
{
    public class JsonPRCresponse
    {
        public string jsonrpc { get; set; }
        public long id { get; set; }
        public JArray result { get; set; }
    }
}
