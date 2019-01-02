using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Polly;

namespace PollyDemo
{
    public class RetryDemo
    {
        public static RetryDemo Default = new RetryDemo();

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
            var policy = Policy.Handle<HttpRequestException>().WaitAndRetryAsync(3, retryAttempt =>
            {
                var waitSeconds = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1));
                Console.WriteLine(DateTime.Now.ToString() + "-Retry:[" + retryAttempt + "], wait " + waitSeconds + "s...");
                return waitSeconds;
            });

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    var res = await policy.ExecuteAsync(HttpInvokeAsync);
                    Console.WriteLine(DateTime.Now.ToString() +"-Run[" + i + "]->Response" +  ": Ok->" + res);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(DateTime.Now.ToString() +"-Run[" + i + "]->Response" +  ": Exception->" + ex.Message);
                }
            }
        }
    }
}
