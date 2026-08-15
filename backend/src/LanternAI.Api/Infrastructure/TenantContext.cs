using System.Security.Claims;

namespace LanternAI.Api.Infrastructure;

public sealed record TenantContext(string TenantId, string SubjectId);

public static class TenantContextExtensions
{
    public static TenantContext? GetTenantContext(this ClaimsPrincipal principal, string claimType = "tid")
    {
        var tenant = principal.FindFirst(claimType)?.Value;
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("oid");
        return string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(subject) ? null : new TenantContext(tenant, subject);
    }
}