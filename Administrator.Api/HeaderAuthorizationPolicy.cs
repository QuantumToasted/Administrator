using Microsoft.AspNetCore.Authorization;

namespace Administrator.Api;

public class HeaderAuthorizationPolicy : AuthorizationPolicy
{
    public HeaderAuthorizationPolicy(IEnumerable<IAuthorizationRequirement> requirements, IEnumerable<string> authenticationSchemes) : base(requirements, authenticationSchemes)
    {
    }
}