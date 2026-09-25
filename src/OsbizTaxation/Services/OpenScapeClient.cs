using System.Net.Http;
using OsbizTaxation.Models;

namespace OsbizTaxation.Services;

/// <summary>
/// Client HTTP vers le portlet HipathAccountingDownload d'OpenScape Business.
/// Les certificats auto-signes sont acceptes (valideur permissif).
/// </summary>
public sealed class OpenScapeClient : IDisposable
{
    private readonly HttpClient _http;

    public OpenScapeClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        _http = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(2) };
    }

    public async Task<string> DownloadAsync(SiteConfig site, CancellationToken ct = default)
    {
        var url = BuildUrl(site, "get");
        using var response = await _http.GetAsync(url, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(SiteConfig site, CancellationToken ct = default)
    {
        var url = BuildUrl(site, "delete");

        // Le portlet OSBiz traite la suppression via GET (comme le telechargement).
        using (var getResponse = await _http.GetAsync(url, ct).ConfigureAwait(false))
        {
            if (getResponse.IsSuccessStatusCode)
                return;
        }

        // Repli sur POST si le GET n'est pas accepte.
        using var content = new StringContent(string.Empty);
        using var postResponse = await _http.PostAsync(url, content, ct).ConfigureAwait(false);
        postResponse.EnsureSuccessStatusCode();
    }

    internal static string BuildUrl(SiteConfig site, string action)
    {
        var host = (site.Adresse ?? string.Empty).Trim();
        if (host.Length == 0)
            throw new InvalidOperationException("Adresse du site non configuree.");

        if (!host.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !host.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            host = "https://" + host;

        host = host.TrimEnd('/');

        var user = (site.Utilisateur ?? string.Empty).Trim();
        if (user.Length > 0 && !user.Contains('@'))
            user += "@system";

        var query =
            "/management/portlet/?portlet=hipath-accountingdownload::HiPathAccountingDownloadPortlet" +
            "&entity=accounting" +
            $"&action={Uri.EscapeDataString(action)}" +
            $"&username={Uri.EscapeDataString(user)}" +
            $"&password={Uri.EscapeDataString(site.MotDePasse ?? string.Empty)}";

        return host + query;
    }

    public void Dispose() => _http.Dispose();
}
