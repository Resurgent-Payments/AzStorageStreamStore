namespace LvStreamStore.ApplicationToolkit.Authorization {
    using Microsoft.AspNetCore.Authorization;

    public class ComplexRoleRequirement : IAuthorizationRequirement {
        public readonly string RoleName;
        public readonly string RouteParameter;

        public ComplexRoleRequirement(string roleName, string routeParameter) {
            RoleName = roleName;
            RouteParameter = routeParameter;
        }
    }
}
