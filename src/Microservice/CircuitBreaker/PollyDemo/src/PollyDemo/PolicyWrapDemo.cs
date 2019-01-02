using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Polly;
using Polly.Timeout;

namespace PollyDemo
{
    public class PolicyWrapDemo
    {
        public static PolicyWrapDemo Default = new PolicyWrapDemo();

        private List<string> services = new List<string> { "localhost:5001", "localhost:5002" };
        private int serviceIndex = 0;
        private HttpClient client = new HttpClient();

        private Task<string> HttpInvokeAsync()
        {
            if (serviceIndex >= services.Count)
            {
                serviceIndex = 0;
            }
            var service = services[serviceIndex++];
            Console.WriteLine(DateTime.Now.ToString() + "-Begin Http Invoke->" + service);
            return client.GetStringAsync("http://" + service + "/api/values");
        }

        public async Task RunAsync()
        {
            // 定义超时策略
            var timeoutPolicy = Policy.TimeoutAsync(1, (context, timespan, task) =>
            {
                Console.WriteLine("It's Timeout, throw TimeoutRejectedException.");
                return Task.CompletedTask;
            });

            // 定义重试策略
            var retryPolicy = Policy.Handle<HttpRequestException>().Or<TimeoutException>().Or<TimeoutRejectedException>()
                .WaitAndRetryAsync(
                    retryCount: 2,
                    sleepDurationProvider: retryAttempt =>
                    {
                        var waitSeconds = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1));
                        Console.WriteLine(DateTime.Now.ToString() + "-Retry:[" + retryAttempt + "], wait " + waitSeconds + "s!");
                        return waitSeconds;
                    });

            // 定义熔断策略
            var circuitBreakerPolicy = Policy.Handle<HttpRequestException>().Or<TimeoutException>().Or<TimeoutRejectedException>()
                .CircuitBreakerAsync(
                    // 熔断前允许出现几次错误
                    exceptionsAllowedBeforeBreaking: 2,
                    // 熔断时间
                    durationOfBreak: TimeSpan.FromSeconds(3),
                    // 熔断时触发
                    onBreak: (ex, breakDelay) =>
                    {
                        Console.WriteLine(DateTime.Now.ToString() + "Breaker->Breaking the circuit for " + breakDelay.TotalMilliseconds + "ms! Exception: ", ex.Message);
                    },
                    // 熔断恢复时触发
                    onReset: () =>
                    {
                        Console.WriteLine(DateTime.Now.ToString() + "Breaker->Call ok! Closed the circuit again.");
                    },
                    // 在熔断时间到了之后触发
                    onHalfOpen: () =>
                    {
                        Console.WriteLine(DateTime.Now.ToString() + "Breaker->Half-open, next call is a trial.");
                    }
                );

            // 定义回退策略
            var fallbackPolicy = Policy<string>.Handle<Exception>()
                .FallbackAsync(
                    fallbackValue: "substitute data",
                    onFallbackAsync: (exception, context) =>
                     {
                         Console.WriteLine("It's Fallback,  Exception->" + exception.Exception.Message + ", return substitute data.");
                         return Task.CompletedTask;
                     });

            for (int i = 0; i < 100; i++)
            {
                Console.WriteLine(DateTime.Now.ToString() + "-Run[" + i + "]-----------------------------");
                var res = await fallbackPolicy.WrapAsync(Policy.WrapAsync(circuitBreakerPolicy, retryPolicy, timeoutPolicy)).ExecuteAsync(HttpInvokeAsync);
                Console.WriteLine(DateTime.Now.ToString() + "-Run[" + i + "]->Response" + ": Ok->" + res);
                await Task.Delay(1000);
                Console.WriteLine("--------------------------------------------------------------------------------------------------------------------");
            }
        }
    }
}
