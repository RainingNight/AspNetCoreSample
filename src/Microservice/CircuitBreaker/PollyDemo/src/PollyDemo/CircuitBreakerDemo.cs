using System;
using System.Net.Http;
using System.Threading.Tasks;
using Polly;

namespace PollyDemo
{
    public class CircuitBreakerDemo
    {
        public static CircuitBreakerDemo Default = new CircuitBreakerDemo();

        private HttpClient client = new HttpClient();

        private Task<string> HttpInvokeAsync()
        {
            Console.WriteLine(DateTime.Now.ToString() + "-Begin Http Invoke...");
            return client.GetStringAsync("http://localhost:5001/api/values");
        }

        public async Task RunAsync()
        {
            var policy = Policy.Handle<HttpRequestException>().Or<TimeoutException>()
                .CircuitBreakerAsync(
                    // 熔断前出现允许错误几次
                    exceptionsAllowedBeforeBreaking: 3,
                    // 熔断时间
                    durationOfBreak: TimeSpan.FromSeconds(5),
                    // 熔断时触发
                    onBreak: (ex, breakDelay) =>
                    {
                        Console.WriteLine("Breaking the circuit for " + breakDelay.TotalMilliseconds + "ms! Exception: ", ex.Message);
                    },
                    // 熔断恢复时触发
                    onReset: () =>
                    {
                        Console.WriteLine("Call ok! Closed the circuit again.");
                    },
                    // 在熔断时间到了之后触发
                    onHalfOpen: () =>
                    {
                        Console.WriteLine("Half-open, next call is a trial.");
                    }
                );

            for (int i = 0; i < 1000000; i++)
            {
                try
                {
                    var res = await policy.ExecuteAsync(HttpInvokeAsync);
                    Console.WriteLine(DateTime.Now.ToString() + "-Run[" + i + "]->Response" + ": Ok->" + res);
                }
                catch (Polly.CircuitBreaker.BrokenCircuitException ex)
                {
                    Console.WriteLine(DateTime.Now.ToString() + "-Run[" + i + "]->Response" + ": BrokenCircuitException->" + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(DateTime.Now.ToString() + "-Run[" + i + "]->Response" + ": Exception->" + ex.Message);
                }
                await Task.Delay(1000);
            }
        }
    }
}
