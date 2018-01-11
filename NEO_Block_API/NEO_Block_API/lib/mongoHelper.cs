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
        public string mongodbConnStr_NeonOnline = string.Empty;
        public string mongodbDatabase_NeonOnline = string.Empty;

        public mongoHelper() {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection()    //将配置文件的数据加载到内存中
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())   //指定配置文件所在的目录
                .AddJsonFile("mongodbsettings.json", optional: true, reloadOnChange: true)  //指定加载的配置文件
                .Build();    //编译成对象  
            mongodbConnStr_testnet = config["mongodbConnStr_testnet"];
            mongodbDatabase_testnet = config["mongodbDatabase_testnet"];
            mongodbConnStr_NeonOnline = config["mongodbConnStr_NeonOnline"];
            mongodbDatabase_NeonOnline = config["mongodbDatabase_NeonOnline"];
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

        public JArray GetDataPages(string mongodbConnStr, string mongodbDatabase, string coll,string sortStr, int pageCount, int pageNum)
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>(coll);

            List<BsonDocument> query = collection.Find(new BsonDocument()).Sort(sortStr).Skip(pageCount * pageNum).Limit(pageCount).ToList();
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

        public long GetDataCount(string mongodbConnStr, string mongodbDatabase,string coll)
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>(coll);

            var txCount = collection.Find(new BsonDocument()).Count();

            client = null;

            return txCount;
        }

        public JArray Getdatablockheight(string mongodbConnStr, string mongodbDatabase)
        {
            int blockDataHeight = -1;
            int txDataHeight = -1;
            int utxoDataHeight = -1;
            int notifyDataHeight = -1;
            int fulllogDataHeight = -1;

            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);

            var collection = database.GetCollection<BsonDocument>("block");
            var sortBson = BsonDocument.Parse("{index:-1}");
            var query = collection.Find(new BsonDocument()).Sort(sortBson).Limit(1).ToList();
            if (query.Count > 0)
            {blockDataHeight = (int)query[0]["index"];}

            collection = database.GetCollection<BsonDocument>("tx");
            sortBson = BsonDocument.Parse("{blockindex:-1}");
            query = collection.Find(new BsonDocument()).Sort(sortBson).Limit(1).ToList();
            if (query.Count > 0)
            { txDataHeight = (int)query[0]["blockindex"]; }

            collection = database.GetCollection<BsonDocument>("system_counter");
            query = collection.Find(new BsonDocument()).ToList();
            if (query.Count > 0)
            {
                foreach (var q in query)
                {
                    if ((string)q["counter"] == "utxo") { utxoDataHeight = (int)q["lastBlockindex"]; };
                    if ((string)q["counter"] == "notify") { notifyDataHeight = (int)q["lastBlockindex"]; };
                    if ((string)q["counter"] == "fulllog") { fulllogDataHeight = (int)q["lastBlockindex"]; };
                }
            }

            client = null;

            JObject J = new JObject
            {
                { "blockDataHeight", blockDataHeight },
                { "txDataHeight", txDataHeight },
                { "utxoDataHeight", utxoDataHeight },
                { "notifyDataHeight", notifyDataHeight },
                { "fulllogDataHeight", fulllogDataHeight }
            };
            JArray JA = new JArray
            {
                J
            };

            return JA;
        }

        public void InsertOneDataByCheckKey(string mongodbConnStr, string mongodbDatabase, string coll, JObject Jdata,string key,string value)
        {
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>(coll);

            var query = collection.Find("{'" + key + "':'" + value +"'}").ToList();
            if (query.Count == 0) {
                string strData = Newtonsoft.Json.JsonConvert.SerializeObject(Jdata);
                BsonDocument bson = BsonDocument.Parse(strData);
                bson.Add("getTime", DateTime.Now);
                collection.InsertOne(bson);
            }

            client = null;
        }
    }
}
