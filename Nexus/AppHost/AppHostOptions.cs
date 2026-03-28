namespace Nexus.AppHost;

public sealed class AppHostOptions
{
    public const string SectionName = "AppHost";

    /// <summary>URL pública base da aplicação (ex.: https://api.exemplo.com), sem barra final.</summary>
    public string? BaseUrl { get; set; }
}
