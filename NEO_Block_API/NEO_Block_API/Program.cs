using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NEO_Block_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        private static int getServerPort() {
            int serverPort = 0;

            //尝试从配置文件读取，失败则默认82
            try
            {
                var config = new ConfigurationBuilder()
               .AddInMemoryCollection()    //将配置文件的数据加载到内存中
               .SetBasePath(Directory.GetCurrentDirectory())   //指定配置文件所在的目录
               .AddJsonFile("mongodbsettings.json", optional: true, reloadOnChange: true)  //指定加载的配置文件
               .Build();    //编译成对象        

                serverPort = int.Parse(config["serverPort"].ToString());
            }
            catch
            {
                serverPort = 82;
            }

            return serverPort;            
        }

        public static IWebHost BuildWebHost(string[] args) =>
             WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Any, getServerPort());
                    //options.Listen(IPAddress.Loopback, 5001, listenOptions =>
                    //{
                    //    listenOptions.UseHttps("testCert.pfx", "testPassword");
                    //});
                })
                .Build();
    }
}
