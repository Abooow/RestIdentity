using Microsoft.AspNetCore.Mvc;

namespace RestIdentity.Server.Extensions;

public static class UrlHelperExtensions
{
    public static string AbsoluteAction(this IUrlHelper url, string controllerName, string actionName, object routeValues = null)
    {
        return url.Action(actionName, controllerName, routeValues, url.ActionContext.HttpContext.Request.Scheme);
    }
}
