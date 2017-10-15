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

namespace JwtBearerSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            new WebHostBuilder()
            .UseKestrel()
            .UseUrls("http://localhost:5200")
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConsole();
            })
            .UseStartup<Startup>()
            .Build();
    }
}
