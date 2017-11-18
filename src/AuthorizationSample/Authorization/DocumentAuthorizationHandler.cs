using AuthorizationSample.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System.Threading.Tasks;

namespace AuthorizationSample.Authorization
{
    public class DocumentAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, IDocument>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, IDocument resource)
        {
            if (context.User.IsInRole("admin"))
            {
                context.Succeed(requirement);
            }
            else
            {
                if (requirement == Operations.Create || requirement == Operations.Read)
                {
                    context.Succeed(requirement);
                }
                else
                {
                    if (context.User.Identity.Name == resource.Creator)
                    {
                        context.Succeed(requirement);
                    }
                    else
                    {
                        context.Fail();
                    }
                }
            }
            return Task.CompletedTask;
        }
    }
}
