using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Consul;

namespace ClientDemo.ServiceDiscoveryProvider
{
    public class ConsulServiceProvider : IServiceDiscoveryProvider
    {
        public async Task<List<string>> GetServicesAsync()
        {
            var consuleClient = new ConsulClient(consulConfig =>
            {
                consulConfig.Address = new Uri("http://localhost:8500");
            });

            var queryResult = await consuleClient.Health.Service("Service", string.Empty, true);

            while (queryResult.Response.Length == 0)
            {
                Console.WriteLine("No services found, wait 1s....");
                await Task.Delay(1000);
                queryResult = await consuleClient.Health.Service("Service", string.Empty, true);
            }

            var result = new List<string>();
            foreach (var serviceEntry in queryResult.Response)
            {
                result.Add(serviceEntry.Service.Address + ":" + serviceEntry.Service.Port);
            }
            return result;
        }
    }
}
