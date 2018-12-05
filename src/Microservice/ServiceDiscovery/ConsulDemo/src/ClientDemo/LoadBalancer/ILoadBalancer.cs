using System.Threading.Tasks;

namespace ClientDemo.LoadBalancer
{
    public interface ILoadBalancer
    {
        Task<string> GetServiceAsync();
    }
}