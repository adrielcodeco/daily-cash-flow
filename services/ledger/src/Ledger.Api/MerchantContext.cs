// @anchors
//   code: LDMCT
//   ref: LDMRC
//   layer: host
//   updated_at: 2026-11-09
namespace Ledger.Api;

/// <summary>The merchant comes from the validated token, never from anything the client chooses.</summary>
public static class MerchantContext
{
    public static bool TryRead(HttpContext context, out Guid merchantId) =>
        Guid.TryParse(context.User.FindFirst(ApiSecurity.MerchantClaim)?.Value, out merchantId) && merchantId != Guid.Empty;
}
