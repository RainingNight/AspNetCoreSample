using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClientDemo.ServiceDiscoveryProvider
{
    public interface IServiceDiscoveryProvider
    {
        Task<List<string>> GetServicesAsync();
    }
}