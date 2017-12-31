using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MongoDB.Bson.IO;

namespace NEO_Block_API.lib
{
    public class mongoHelper
    {
        public string mongodbConnStr_testnet = string.Empty;
        public string mongodbDatabase_testnet = string.Empty;

        public mongoHelper() {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection()    //将配置文件的数据加载到内存中
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())   //指定配置文件所在的目录
                .AddJsonFile("mongodbsettings.json", optional: true, reloadOnChange: true)  //指定加载的配置文件
                .Build();    //编译成对象  
            mongodbConnStr_testnet = config["mongodbConnStr_testnet"];
            mongodbDatabase_testnet = config["mongodbDatabase_testnet"];

        }

        public JArray GetData(string mongodbConnStr,string mongodbDatabase, string coll,string findFliter)
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>(coll);

            List<BsonDocument> query = collection.Find(BsonDocument.Parse(findFliter)).ToList();
            client = null;

            if (query.Count > 0)
            {

                var jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
                JArray JA = JArray.Parse(query.ToJson(jsonWriterSettings));
                foreach (JObject j in JA)
                {
                    j.Remove("_id");
                }
                return JA;
            }
            else { return new JArray(); }      
        }

        public int GetBlockMaxIndex(string mongodbConnStr, string mongodbDatabase)
        {
            int maxIndex = -1;
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>("block");

            var sortBson = BsonDocument.Parse("{index:-1}");
            var query = collection.Find(new BsonDocument()).Sort(sortBson).Limit(1).ToList();
            if (query.Count == 0)
            {
                maxIndex = -1;
            }
            else
            {
                maxIndex = (int)query[0]["index"];
            }

            client = null;

            return maxIndex;
        }
    }
}
