using System;
using System.Net.Http;
using System.Threading.Tasks;
using Polly;
using Polly.Timeout;

namespace PollyDemo
{
    public class TimeoutDemo
    {
        public static TimeoutDemo Default = new TimeoutDemo();

        private HttpClient client = new HttpClient();

        private Task<string> HttpInvokeAsync()
        {
            Console.WriteLine(DateTime.Now.ToString() + "-Begin Http Invoke...");
            return client.GetStringAsync("http://localhost:5001/api/values");
        }

        public async Task RunAsync()
        {
            var policy = Policy.TimeoutAsync(1, (context, timespan, task) =>
            {
                Console.WriteLine("It's Timeout, throw TimeoutRejectedException.");
                return Task.CompletedTask;
            });

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    var res = await policy.ExecuteAsync(HttpInvokeAsync);
                    Console.WriteLine(DateTime.Now.ToString() + "-Run[" + i + "]->Response" + ": Ok->" + res);
                }
                catch (TimeoutRejectedException ex)
                {
                    Console.WriteLine(DateTime.Now.ToString() + "-Run[" + i + "]->Response" + ": TimeoutRejectedException->" + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(DateTime.Now.ToString() + "-Run[" + i + "]->Response" + ": Exception->" + ex.Message);
                }
            }
        }
    }
}
