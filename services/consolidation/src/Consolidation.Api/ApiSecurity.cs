// @anchors
//   code: CNSEC
//   ref: CNSCP
//   layer: host
//   updated_at: 2026-11-09
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Consolidation.Api;

public static class ApiSecurity
{
    public const string MerchantClaim = "merchant_id";

    public const string BalancesRead = "balances.read";

    private static readonly string[] Scopes = [BalancesRead];

    private static readonly TimeSpan AllowedClockSkew = TimeSpan.FromSeconds(30);

    public static void AddApiSecurity(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection("Auth");
        var authority = section["Authority"] ?? throw new InvalidOperationException("Auth:Authority is required.");
        var audience = section["Audience"] ?? throw new InvalidOperationException("Auth:Audience is required.");
        var permitsPerSecond = builder.Configuration.GetValue("RateLimit:PermitsPerSecond", 100);

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = section.GetValue("RequireHttps", true);
                options.MapInboundClaims = false;
                options.TokenValidationParameters.ClockSkew = AllowedClockSkew;
            });

        builder.Services.AddAuthorization(options =>
        {
            foreach (var scope in Scopes)
            {
                options.AddPolicy(scope, policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(MerchantClaim)
                    .RequireAssertion(context => HasScope(context.User, scope)));
            }
        });

        builder.Services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => Partition(context, permitsPerSecond));
            options.OnRejected = async (rejected, cancellationToken) =>
            {
                rejected.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                rejected.HttpContext.Response.Headers.RetryAfter = "1";
                await Results.Problem(title: "Too many requests.", statusCode: StatusCodes.Status429TooManyRequests, type: "rate_limited")
                    .ExecuteAsync(rejected.HttpContext);
            };
        });
    }

    public static void UseApiSecurity(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();
    }

    private static bool HasScope(ClaimsPrincipal user, string scope) =>
        user.FindFirstValue("scope")?.Split(' ').Contains(scope) ?? false;

    /// <summary>Each merchant has its own budget, so one client cannot exhaust the others. Health checks are never limited.</summary>
    private static RateLimitPartition<string> Partition(HttpContext context, int permitsPerSecond)
    {
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            return RateLimitPartition.GetNoLimiter("health");
        }

        // Prefixes keep a claim from landing in another kind of partition, such as one named after an address.
        var merchant = context.User.FindFirstValue(MerchantClaim);
        var key = merchant is not null ? $"merchant:{merchant}" : $"address:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
        return RateLimitPartition.GetTokenBucketLimiter(key, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = permitsPerSecond,
            TokensPerPeriod = permitsPerSecond,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            QueueLimit = 0,
        });
    }
}
