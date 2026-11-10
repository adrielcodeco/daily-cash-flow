// @anchors
//   code: LDAUD
//   ref: LDADS
//   layer: host
//   updated_at: 2026-11-09
using System.Security.Claims;

namespace Ledger.Api;

/// <summary>
/// Records writes and refused access in the Audit log category, kept apart from operational logs.
/// A read of another merchant's data answers 404, so a 404 is recorded as refused access too.
/// </summary>
public sealed class AuditMiddleware(RequestDelegate next, ILoggerFactory loggers)
{
    private readonly ILogger logger = loggers.CreateLogger("Audit");

    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        var status = context.Response.StatusCode;
        var refused = status is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden or StatusCodes.Status404NotFound or StatusCodes.Status429TooManyRequests;
        if (refused || !HttpMethods.IsGet(context.Request.Method))
        {
            logger.LogInformation(
                "{Method} {Path} answered {Status} for client {ClientId} and merchant {MerchantId}.",
                context.Request.Method,
                context.Request.Path.Value,
                status,
                context.User.FindFirstValue("azp"),
                context.User.FindFirstValue(ApiSecurity.MerchantClaim));
        }
    }
}
