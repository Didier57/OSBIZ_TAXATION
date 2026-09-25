namespace OsbizTaxation.Models;

public sealed class AppConfig
{
    public string SourceUrl { get; set; } = string.Empty;

    public string OutputFile { get; set; } = string.Empty;

    public bool AutoFetchEnabled { get; set; }

    public int AutoFetchIntervalMinutes { get; set; } = 60;

    public bool CheckUpdatesOnStartup { get; set; } = true;
}
