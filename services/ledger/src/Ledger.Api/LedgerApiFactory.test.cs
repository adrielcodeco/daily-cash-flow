// @anchors
//   code: LDAFX
//   layer: test
//   updated_at: 2026-11-09
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Ledger.Api;
using Ledger.Core;
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

namespace Ledger.Tests;

/// <summary>
/// Hosts the real Ledger API in memory. Tokens are signed here with a local key and still go through
/// the real bearer validation (issuer, audience, signature); the store is the in-memory fake.
/// </summary>
public sealed class LedgerApiFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "https://issuer.test/realms/daily-cash-flow";
    public const string Audience = "daily-cash-flow-api";

    private static readonly SymmetricSecurityKey SigningKey = new(RandomNumberGenerator.GetBytes(32));

    internal InMemoryEntryStore Store { get; } = new();

    internal RecordingLoggerProvider Logs { get; } = new();

    public int PermitsPerSecond { get; init; } = 10_000;

    public static string Token(string? merchant, string scope, string audience = Audience, SecurityKey? key = null)
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
            SigningCredentials = new SigningCredentials(key ?? SigningKey, SecurityAlgorithms.HmacSha256),
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:Ledger", "Host=unused.invalid;Database=ledger");
        builder.UseSetting("Auth:Authority", Issuer);
        builder.UseSetting("Auth:Audience", Audience);
        builder.UseSetting("RateLimit:PermitsPerSecond", PermitsPerSecond.ToString());
        builder.ConfigureLogging(logging => logging.AddProvider(Logs));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEntryStore>();
            services.AddSingleton<IEntryStore>(Store);
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
internal static class LedgerHttp
{
    public const string ReadAndWrite = "entries.read entries.write";

    public static readonly DateOnly Yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

    public static string ListPath => $"/v1/entries?date={Yesterday:yyyy-MM-dd}";

    public static object Credit() => new { entryType = "CREDIT", amountCents = 1500, accountingDate = Yesterday, description = "sale" };

    public static string NewKey() => Guid.NewGuid().ToString("N");

    public static string Merchant(Guid merchant, string scope = ReadAndWrite) => LedgerApiFactory.Token(merchant.ToString(), scope);

    public static HttpRequestMessage Request(HttpMethod method, string path, string? token = null, string? key = null, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (key is not null)
        {
            request.Headers.Add("Idempotency-Key", key);
        }

        if (body is not null)
        {
            request.Content = body as HttpContent ?? JsonContent.Create(body);
        }

        return request;
    }

    public static Task<HttpResponseMessage> Send(this HttpClient client, HttpMethod method, string path, string? token = null, string? key = null, object? body = null) =>
        client.SendAsync(Request(method, path, token, key, body));

    public static async Task<Guid> RecordFor(this HttpClient client, Guid merchant)
    {
        var response = await client.Send(HttpMethod.Post, "/v1/entries", Merchant(merchant), NewKey(), Credit());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
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
