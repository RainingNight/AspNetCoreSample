using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClientDemo.ServiceDiscoveryProvider
{
    public class PollingConsulServiceProvider : IServiceDiscoveryProvider
    {
        private List<string> _services = new List<string>();
        private bool _polling;

        public PollingConsulServiceProvider()
        {
            var _timer = new Timer(async _ =>
            {
                if (_polling)
                {
                    return;
                }

                _polling = true;
                await Poll();
                _polling = false;

            }, null, 0, 1000);
        }

        public async Task<List<string>> GetServicesAsync()
        {
            if (_services.Count == 0) await Poll();
            return _services;
        }

        private async Task Poll()
        {
            _services = await new ConsulServiceProvider().GetServicesAsync();
        }
    }
}
