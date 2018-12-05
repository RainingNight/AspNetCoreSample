using System.Threading.Tasks;
using ClientDemo.ServiceDiscoveryProvider;

namespace ClientDemo.LoadBalancer
{
    public class RoundRobinLoadBalancer : ILoadBalancer
    {
        private readonly IServiceDiscoveryProvider _sdProvider;

        public RoundRobinLoadBalancer(IServiceDiscoveryProvider sdProvider)
        {
            _sdProvider = sdProvider;
        }

        private readonly object _lock = new object();
        private int _index = 0;

        public async Task<string> GetServiceAsync()
        {
            var services = await _sdProvider.GetServicesAsync();
            lock (_lock)
            {
                if (_index >= services.Count)
                {
                    _index = 0;
                }
                return services[_index++];
            }
        }
    }
}