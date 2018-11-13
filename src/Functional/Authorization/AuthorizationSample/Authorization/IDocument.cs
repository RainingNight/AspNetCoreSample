using AuthorizationSample.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System.Threading.Tasks;

namespace AuthorizationSample.Authorization
{
    public interface IDocument
    {
        string Creator { get; set; }
    }
}