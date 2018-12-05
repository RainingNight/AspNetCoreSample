using System;
using System.Net.Http;
using System.Threading.Tasks;
using ClientDemo.LoadBalancer;
using ClientDemo.ServiceDiscoveryProvider;

namespace ClientDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            ILoadBalancer balancer = new RoundRobinLoadBalancer(new PollingConsulServiceProvider());
            var client = new HttpClient();

            Console.WriteLine("Request by RoundRobinLoadBalancer....");
            for (int i = 0; i < 10; i++)
            {
                var service = await balancer.GetServiceAsync();

                Console.WriteLine(DateTime.Now.ToString() + "-RoundRobin:" +
                    await client.GetStringAsync("http://" + service + "/api/values") + " --> " + "Request from " + service);
            }

            Console.WriteLine("Request by RandomLoadBalancer....");
            balancer = new RandomLoadBalancer(new PollingConsulServiceProvider());
            for (int i = 0; i < 10; i++)
            {
                var service = await balancer.GetServiceAsync();

                Console.WriteLine(DateTime.Now.ToString() + "-Random:" +
                    await client.GetStringAsync("http://" + service + "/api/values") + " --> " + "Request from " + service);
            }

            Console.ReadKey();
        }
    }
}
