using Microsoft.AspNetCore.Mvc;
using RestIdentity.Shared.Wrapper;

namespace RestIdentity.Server.Extensions;

internal static class MvcBuilderExtensions
{
    public static IMvcBuilder AddInvalidModelStateResponse(this IMvcBuilder mvcBuilder)
    {
        return mvcBuilder.ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = context =>
        {
            var problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "One or more model validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Detail = "See the errors property for details",
                Instance = context.HttpContext.Request.Path
            };

            return new BadRequestObjectResult(Result<ValidationProblemDetails>.Fail(problemDetails, "One or more model validation errors occurred.").AsBadRequest());
        });
    }
}
