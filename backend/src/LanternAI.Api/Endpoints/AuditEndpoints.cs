using LanternAI.Api.Infrastructure;

namespace LanternAI.Api.Endpoints;

public static class AuditEndpoints
{
    public static void MapAuditEndpoints(this IEndpointRouteBuilder app, bool requireAuthorization)
    {
        var endpoint = app.MapGet("/api/audit/recent", (IAuditStore store) => Results.Ok(store.GetRecent()));
        endpoint.WithName("GetRecentAuditEvents").WithSummary("List recent query audit events.");
        if (requireAuthorization) endpoint.RequireAuthorization("Lantern.Admin");
    }
}