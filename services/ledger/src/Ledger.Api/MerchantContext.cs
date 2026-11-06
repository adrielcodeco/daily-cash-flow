// @anchors
//   code: LDMCT
//   layer: host
//   updated_at: 2026-11-05
namespace Ledger.Api;

/// <summary>
/// Local placeholder until token validation lands in milestone M6.
/// The merchant must come from the token, never from a client-controlled header.
/// </summary>
public static class MerchantContext
{
    public static bool TryRead(HttpContext context, out Guid merchantId) =>
        Guid.TryParse(context.Request.Headers["X-Merchant-Id"], out merchantId);
}
