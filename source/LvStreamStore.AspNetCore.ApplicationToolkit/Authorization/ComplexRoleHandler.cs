namespace LvStreamStore.ApplicationToolkit.Authorization {
    using IdentityModel;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc.Infrastructure;

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// <para>The use of this authorization handler is expecting the claims principal to have one or more claims of type `role` with values that are `*/{roleName}` or `{AccountId:Guid}/{roleName}`</para>
    /// <para>TBD: We may enforce that all account ids are Guids in the "N" format, from dotnet.</para>
    /// </remarks>
    public class ComplexRoleHandler : AuthorizationHandler<ComplexRoleRequirement> {
        private readonly IActionContextAccessor _actionContextAccessor;

        public ComplexRoleHandler(IActionContextAccessor actionContextAccessor) {
            _actionContextAccessor = actionContextAccessor ?? throw new ArgumentNullException();
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ComplexRoleRequirement requirement) {
            var roles = context.User.Claims.Where(c => c.Type == JwtClaimTypes.Role)
                .Select(c => c.Value)
                .Select(role => role.Split("/", StringSplitOptions.None))
                .Select(split => {
                    var accountKey = split[0];
                    var accountId = accountKey.Equals("*")
                        ? accountKey
                        : new Guid(accountKey).ToString("N");

                    return new { AccountId = accountId, RoleName = split[1] };
                })
                .GroupBy(x => x.AccountId)
                .ToDictionary(x => x.Key, x => x.Select(val => val.RoleName).ToArray());

            

            if ((roles.ContainsKey("*") && roles["*"].Contains(requirement.RoleName, StringComparer.OrdinalIgnoreCase))
                ||
                (
                    _actionContextAccessor.ActionContext.RouteData.Values.TryGetValue(requirement.RouteParameter, out var routeValue) &&
                    (routeValue != null) &&
                    (Guid.TryParse(routeValue.ToString(), out var routeParameterValue)) &&
                    (roles.TryGetValue(routeParameterValue.ToString("N"), out var accountRoles)) &&
                    (accountRoles.Contains(requirement.RoleName))
                )
               ) {
                // role is available globally.
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
    }
}
