// @anchors
//   code: CNAFX
//   layer: test
//   updated_at: 2026-11-09
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Consolidation.Api;
using Consolidation.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Consolidation.Tests;

/// <summary>
/// Hosts the real Consolidation API in memory. Tokens are signed here with a local key and still go through
/// the real bearer validation (issuer, audience, signature); the store is the in-memory fake.
/// </summary>
public sealed class ConsolidationApiFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "https://issuer.test/realms/daily-cash-flow";
    public const string Audience = "daily-cash-flow-api";

    private static readonly SymmetricSecurityKey SigningKey = new(RandomNumberGenerator.GetBytes(32));

    internal InMemoryBalanceStore Store { get; } = new();

    internal RecordingLoggerProvider Logs { get; } = new();

    public int PermitsPerSecond { get; init; } = 10_000;

    public static string Token(string? merchant, string scope, string audience = Audience)
    {
        var claims = new Dictionary<string, object> { ["scope"] = scope, ["azp"] = "test-client" };
        if (merchant is not null)
        {
            claims[ApiSecurity.MerchantClaim] = merchant;
        }

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = audience,
            Claims = claims,
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256),
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:Consolidation", "Host=unused.invalid;Database=consolidation");
        builder.UseSetting("Auth:Authority", Issuer);
        builder.UseSetting("Auth:Audience", Audience);
        builder.UseSetting("RateLimit:PermitsPerSecond", PermitsPerSecond.ToString());
        builder.ConfigureLogging(logging => logging.AddProvider(Logs));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IBalanceStore>();
            services.AddSingleton<IBalanceStore>(Store);
            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // A fixed configuration replaces the discovery document, so no request leaves the process.
                options.Authority = null;
                options.Configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
                options.TokenValidationParameters.ValidIssuer = Issuer;
                options.TokenValidationParameters.IssuerSigningKey = SigningKey;
            });
        });
    }
}

internal sealed class RecordingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<(string Category, string Message)> entries = new();

    public ILogger CreateLogger(string categoryName) => new RecordingLogger(categoryName, entries);

    /// <summary>The audit line is written after the response starts, so the test waits for it briefly.</summary>
    public async Task<bool> WaitForAsync(Func<string, bool> audited)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (entries.Any(entry => entry.Category == "Audit" && audited(entry.Message)))
            {
                return true;
            }

            await Task.Delay(20);
        }

        return false;
    }

    public void Dispose()
    {
    }

    private sealed class RecordingLogger(string category, ConcurrentQueue<(string, string)> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            entries.Enqueue((category, formatter(state, exception)));
    }
}

/// <summary>Builds requests against the in-memory API and checks the problem format of refusals.</summary>
internal static class ConsolidationHttp
{
    public const string BalancesRead = "balances.read";

    public static readonly DateOnly Day = new(2026, 11, 5);

    public static string Daily => $"/v1/balances/daily?date={Day:yyyy-MM-dd}";

    public static string Period => $"/v1/balances/period?start={Day.AddDays(-1):yyyy-MM-dd}&end={Day:yyyy-MM-dd}";

    public static string Merchant(Guid merchant, string scope = BalancesRead) => ConsolidationApiFactory.Token(merchant.ToString(), scope);

    public static Task<HttpResponseMessage> Get(this HttpClient client, string path, string? token = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return client.SendAsync(request);
    }

    /// <summary>Sends until the rate limit answers, so a refill between two requests cannot hide it.</summary>
    public static async Task<HttpResponseMessage?> UntilLimited(Func<Task<HttpResponseMessage>> send)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var response = await send();
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return response;
            }
        }

        return null;
    }

    public static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode status, string? type = null)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)status, problem.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("title").GetString()));
        if (type is not null)
        {
            Assert.Equal(type, problem.GetProperty("type").GetString());
        }
    }
}
