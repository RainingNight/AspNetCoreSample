using System;
using System.Threading.Tasks;

namespace PollyDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 重试策略
            //await RetryDemo.Default.RunAsync();

            // 超时策略
            //await TimeoutDemo.Default.RunAsync();

            // 回退策略
            //await FallbackDemo.Default.RunAsync();

            // 熔断策略
            //await CircuitBreakerDemo.Default.RunAsync();

            // 组合模式
            await PolicyWrapDemo.Default.RunAsync();

            Console.ReadKey();
        }
    }
}
