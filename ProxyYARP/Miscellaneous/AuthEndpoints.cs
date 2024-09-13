using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;

namespace ProxyYARP.Miscellaneous;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder routeGroup = endpoints.MapGroup("/auth");

        routeGroup.MapGet("/login",  handleLogin);
        routeGroup.MapGet("/logout", handleLogout).RequireAuthorization();
    }

    private static SignInHttpResult handleLogin()
    {
        Claim[] claims = [new Claim(ClaimTypes.Name, "UserNameId")];

        var claimsIdentity = new ClaimsIdentity(claims, Program.AuthScheme);

        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        return TypedResults.SignIn(claimsPrincipal, authenticationScheme: Program.AuthScheme);
    }

    private static SignOutHttpResult handleLogout()
    {
        return TypedResults.SignOut(authenticationSchemes: [Program.AuthScheme]);
    }
}
