using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;

namespace OsbizTaxation.Services;

public sealed class UpdateService : IDisposable
{
    private const string Owner = "Didier57";
    private const string Repository = "OSBIZ_TAXATION";
    private const string ReleasesApiUrl = $"https://api.github.com/repos/{Owner}/{Repository}/releases/latest";
    private const string ExeFileName = "OsbizTaxation.exe";

    private readonly HttpClient _http;

    public UpdateService()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("OsbizTaxation-Updater/1.0");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }

    public Version CurrentVersion
        => Version.TryParse(GetInformationalVersion(), out var version) ? version : new Version(1, 0, 0);

    public async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync(ReleasesApiUrl, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var release = await response.Content.ReadFromJsonAsync<GitHubRelease>(cancellationToken: cancellationToken);
        if (release?.TagName is null)
            return null;

        var tag = release.TagName.TrimStart('v', 'V');
        if (!Version.TryParse(tag, out var remoteVersion) || remoteVersion <= CurrentVersion)
            return null;

        var asset =
            release.Assets?.FirstOrDefault(a => string.Equals(a.Name, ExeFileName, StringComparison.OrdinalIgnoreCase))
            ?? release.Assets?.FirstOrDefault(a => a.Name?.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == true);

        if (string.IsNullOrWhiteSpace(asset?.BrowserDownloadUrl))
            return null;

        return new UpdateInfo(remoteVersion, release.Name ?? release.TagName, asset.BrowserDownloadUrl, asset.Size);
    }

    public async Task<string> DownloadAsync(UpdateInfo update, CancellationToken cancellationToken = default)
    {
        var target = Path.Combine(Path.GetTempPath(), $"OsbizTaxation_{update.Version}.exe");

        using var response = await _http.GetAsync(
            update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var destination = File.Create(target);
        await source.CopyToAsync(destination, cancellationToken);

        return target;
    }

    /// <summary>
    /// Lance un script qui attend la fermeture de l'application, remplace l'executable
    /// courant par la version telechargee puis relance l'application.
    /// </summary>
    public void ApplyUpdateAndRestart(string downloadedExePath)
    {
        var currentExe = Environment.ProcessPath
            ?? throw new InvalidOperationException("Impossible de determiner le chemin de l'executable courant.");

        if (string.Equals(
                Path.GetFullPath(downloadedExePath),
                Path.GetFullPath(currentExe),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Le fichier telecharge est identique a l'executable courant.");
        }

        var scriptPath = Path.Combine(Path.GetTempPath(), $"osbiz_update_{Guid.NewGuid():N}.cmd");
        var processId = Environment.ProcessId;

        var script = $"""
            @echo off
            :wait
            tasklist /FI "PID eq {processId}" 2>NUL | find "{processId}" >NUL
            if not errorlevel 1 (
                timeout /t 1 /nobreak >NUL
                goto wait
            )
            move /Y "{downloadedExePath}" "{currentExe}" >NUL
            start "" "{currentExe}"
            del "%~f0"
            """;

        File.WriteAllText(scriptPath, script);

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe", $"/C \"{scriptPath}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        });
    }

    public void Dispose() => _http.Dispose();

    private static string GetInformationalVersion()
    {
        var attribute = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        var value = attribute?.InformationalVersion ?? "1.0.0";

        var metadataSeparator = value.IndexOf('+');
        return metadataSeparator >= 0 ? value[..metadataSeparator] : value;
    }

    public sealed record UpdateInfo(Version Version, string Title, string DownloadUrl, long Size);

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubAsset>? Assets { get; set; }
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }
}
