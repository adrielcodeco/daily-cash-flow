// @anchors
//   code: LDMRT
//   ref: LDMRC
//   updated_at: 2026-11-09
//   layer: test
// Refs:
//   feature : services/ledger/src/Ledger.Api/MerchantContext.feature
//   spec    : services/ledger/src/Ledger.Api/MerchantContext.spec.md
using System.Security.Claims;
using Ledger.Api;
using Microsoft.AspNetCore.Http;

namespace Ledger.Tests;

public sealed class MerchantContextTests
{
    private static HttpContext With(params Claim[] claims) =>
        new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer")) };

    [Fact(DisplayName = "LDMRC-R01 the merchant is read from the token")]
    public void ReadsMerchant()
    {
        var merchant = Guid.NewGuid();

        var read = MerchantContext.TryRead(With(new Claim(ApiSecurity.MerchantClaim, merchant.ToString())), out var merchantId);

        Assert.True(read);
        Assert.Equal(merchant, merchantId);
    }

    [Theory(DisplayName = "LDMRC-R02 a merchant that is not a valid identifier is not read")]
    [InlineData("not-a-guid")]
    [InlineData("")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void RefusesInvalidMerchant(string value)
    {
        Assert.False(MerchantContext.TryRead(With(new Claim(ApiSecurity.MerchantClaim, value)), out _));
    }

    [Fact(DisplayName = "LDMRC-R02 a token without the merchant is not read")]
    public void RefusesMissingMerchant()
    {
        Assert.False(MerchantContext.TryRead(With(new Claim("sub", Guid.NewGuid().ToString())), out _));
    }

    [Fact(DisplayName = "LDMRC-R03 a merchant chosen by the client is ignored")]
    public void IgnoresClientChoice()
    {
        var context = With();
        context.Request.Headers["X-Merchant-Id"] = Guid.NewGuid().ToString();
        context.Request.QueryString = new QueryString($"?merchantId={Guid.NewGuid()}");

        Assert.False(MerchantContext.TryRead(context, out _));
    }
}
