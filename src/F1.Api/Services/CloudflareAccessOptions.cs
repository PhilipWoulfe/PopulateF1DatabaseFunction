namespace F1.Api.Services;

public class CloudflareAccessOptions
{
    public string CertsUrl { get; set; } = "https://f1-team.cloudflareaccess.com/cdn-cgi/access/certs";
    public string Issuer { get; set; } = "https://f1-team.cloudflareaccess.com";
    public string Audience { get; set; } = string.Empty;
    public int JwksCacheHours { get; set; } = 24;
    public bool AllowLegacyHeaderBypass { get; set; }
}
