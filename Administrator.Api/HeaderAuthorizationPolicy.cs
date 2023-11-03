using Microsoft.AspNetCore.Authorization;

namespace Administrator.Api;

public sealed class HeaderAuthorizationPolicy : AuthorizationPolicy
{
    public HeaderAuthorizationPolicy(IEnumerable<IAuthorizationRequirement> requirements, IEnumerable<string> authenticationSchemes) : base(requirements, authenticationSchemes)
    {
    }
}