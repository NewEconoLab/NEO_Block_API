using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NEO_Block_API.Controllers
{
    public class Monitor
    {
        /**
         * 统计接口tps
         * 
         *  getnotype: 10000/s, test=9300, main=700
         */

        ConcurrentDictionary<string, long> testnetDict = new ConcurrentDictionary<string, long>();
        ConcurrentDictionary<string, long> mainnetDict = new ConcurrentDictionary<string, long>();


        public Monitor()
        {
            new Task(() => calcTps()).Start();
        }

        private void calcTps()
        {
            while (true)
            {
                try {
                    Thread.Sleep(1000);
                    process();
                } catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }

        private void process()
        {
            DateTime now = DateTime.Now;
            var tmpTestnetDict = new Dictionary<string, long>(testnetDict);
            var tmpMainnetDict = new Dictionary<string, long>(mainnetDict);

            var keys = new List<string>();
            keys.AddRange(tmpTestnetDict.Keys.ToArray());
            keys.AddRange(tmpMainnetDict.Keys.ToArray());
            if (keys.Count == 0) return;

            //
            StringBuilder sb = new StringBuilder();
            long sum = 0;
            foreach (var key in keys.Distinct())
            {
                long t1 = tmpTestnetDict.GetValueOrDefault(key, 0);
                long t2 = tmpMainnetDict.GetValueOrDefault(key, 0);
                if (t1 == 0 && t2 == 0) continue;

                if (t1 > 0) testnetDict.AddOrUpdate(key, 0, (k, oldV) => oldV - t1);
                if (t2 > 0) mainnetDict.AddOrUpdate(key, 0, (k, oldV) => oldV - t2);
                long tt = t1 + t2;
                sum += tt;
                sb.Append(key.PadLeft(48)).Append(": ").Append(tt.ToString().PadLeft(6)).Append("/s, ").Append("testnet=" + t1.ToString().PadLeft(5)).Append(",mainnet=" + t2.ToString().PadLeft(5)).Append("\r\n");
            }
            if (sum == 0) return;

            sb.Append("sum:" + sum.ToString().PadLeft(6) + "\t");

            //
            log(now, sb);
        }

        private void log(DateTime now, StringBuilder sb)
        {
            string path = string.Format("tps_{0}.txt", now.ToString("yyyyMMdd"));
            string data = sb.Append(now.ToString("yyyyMMdd HH:mm:ss.fff")).Append("\r\n").ToString();
            data = data.Replace("\r\nsum:", "\tsum:");
            File.AppendAllText(path, data);
        }

        public void point(string networktype, string method)
        {
            if (networktype == "testnet")
            {
                testnetDict.AddOrUpdate(method, 1, (k, oldv) => oldv + 1);
                return;
            }
            if (networktype == "mainnet")
            {
                mainnetDict.AddOrUpdate(method, 1, (k, oldv) => oldv + 1);
                return;
            }
        }
    }
}
