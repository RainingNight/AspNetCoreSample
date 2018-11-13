using System;
using System.Threading.Tasks;
using ClientDemo.ServiceDiscoveryProvider;

namespace ClientDemo.LoadBalancer
{
    public class RandomLoadBalancer : ILoadBalancer
    {
        private readonly IServiceDiscoveryProvider _sdProvider;

        public RandomLoadBalancer(IServiceDiscoveryProvider sdProvider)
        {
            _sdProvider = sdProvider;
        }

        private Random _random = new Random();

        public async Task<string> GetServiceAsync()
        {
            var services = await _sdProvider.GetServicesAsync();
            return services[_random.Next(services.Count)];
        }
    }
}