using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NEO_Block_API.lib
{
    public static class logHelper
    {
        public static string logInfoFormat(object inputJson,object outputJson,DateTime start)
        {
            return "\r\n input:\r\n" 
                + JsonConvert.SerializeObject(inputJson) 
                + "\r\n output \r\n" 
                + JsonConvert.SerializeObject(outputJson) 
                + "\r\n exetime \r\n" 
                + DateTime.Now.Subtract(start).TotalMilliseconds 
                + "ms \r\n";
        }
    }
}
