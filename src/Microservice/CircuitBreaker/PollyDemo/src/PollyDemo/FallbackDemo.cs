using System;
using System.Net.Http;
using System.Threading.Tasks;
using Polly;
using Polly.Timeout;

namespace PollyDemo
{
    public class FallbackDemo
    {
        public static FallbackDemo Default = new FallbackDemo();

        private HttpClient client = new HttpClient();

        private Task<string> HttpInvokeAsync()
        {
            Console.WriteLine(DateTime.Now.ToString() + "-Begin Http Invoke...");
            return client.GetStringAsync("http://localhost:5001/api/values");
        }

        public async Task RunAsync()
        {
            var policy = Policy<string>.Handle<HttpRequestException>().FallbackAsync("substitute data", (exception, context) =>
            {
                Console.WriteLine("It's Fallback, return substitute data.");
                return Task.CompletedTask;
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
