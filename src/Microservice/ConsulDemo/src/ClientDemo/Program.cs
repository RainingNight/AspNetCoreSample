using System;
using System.Net.Http;
using System.Threading.Tasks;
using ClientDemo.LoadBalancer;
using ClientDemo.ServiceDiscoveryProvider;
using Polly;
using Polly.Retry;

namespace ClientDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            ILoadBalancer balancer = new RoundLoadBalancer(new PollingConsulServiceProvider());
            var client = new HttpClient();

            RetryPolicy _serverRetryPolicy = Policy.Handle<HttpRequestException>().RetryAsync((exception, retryCount) =>
            {
                Console.WriteLine("Retry......" + retryCount);
            });

            async Task<string> ExecuteRequestAsync()
            {
                var service = await balancer.GetServiceAsync();
                return DateTime.Now.ToString() + ":" + await client.GetStringAsync("http://" + service + "/api/values") + " --> " + "Request from " + service;
            }

            Console.WriteLine("Request by RoundLoadBalancer....");
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine(await _serverRetryPolicy.ExecuteAsync(async () => await ExecuteRequestAsync()));
            }

            Console.WriteLine("Request by RandomLoadBalancer....");
            balancer = new RandomLoadBalancer(new PollingConsulServiceProvider());
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine(await _serverRetryPolicy.ExecuteAsync(async () => await ExecuteRequestAsync()));
            }

            Console.ReadKey();
        }
    }
}
