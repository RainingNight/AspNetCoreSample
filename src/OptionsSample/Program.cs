using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace OptionsSample
{

    public class MyOptions
    {
        public string Name { get; set; }
    }

    class Program
    {
        private IOptionsMonitor<MyOptions> _options;

        public Program()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var serviceCollection = new ServiceCollection();

            serviceCollection.Configure<MyOptions>(configuration);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            _options = serviceProvider.GetRequiredService<IOptionsMonitor<MyOptions>>();
        }

        public static void Main(string[] args)
        {
            new Program().Execute(args);
        }

        public void Execute(string[] args)
        {
            Console.WriteLine(_options.CurrentValue.Name);

            // 手动修改配置文件，将触发OnChange事件。
            _options.OnChange(_ => Console.WriteLine(_.Name));

            Console.ReadKey();
        }
    }
}
