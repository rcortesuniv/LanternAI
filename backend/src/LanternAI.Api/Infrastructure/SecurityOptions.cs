namespace LanternAI.Api.Infrastructure;

public sealed class SecurityOptions
{
    public const string SectionName = "Authentication";

    public bool Enabled { get; set; }
    public string Authority { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string TenantClaim { get; set; } = "tid";
    public string[] RequiredRoles { get; set; } = ["Lantern.User"];
}