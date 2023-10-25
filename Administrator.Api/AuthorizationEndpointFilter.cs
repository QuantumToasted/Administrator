using Microsoft.AspNetCore.Http;

namespace Administrator.Api;

public class AuthorizationEndpointFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        throw new NotImplementedException();
    }
}