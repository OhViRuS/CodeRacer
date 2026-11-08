using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CodeRacer.Server.Tests.TestDoubles;

/// <summary>
/// Puts a user on a controller the way JWT authentication would, without running the HTTP pipeline.
/// </summary>
public static class ControllerTestHelpers
{
    public static T SignedInAs<T>(this T controller, Guid userId)
        where T : ControllerBase
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Test");
        return controller.WithUser(new ClaimsPrincipal(identity));
    }

    public static T SignedOut<T>(this T controller)
        where T : ControllerBase
    {
        return controller.WithUser(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    private static T WithUser<T>(this T controller, ClaimsPrincipal user)
        where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        return controller;
    }
}
