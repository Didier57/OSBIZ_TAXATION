using System.Net.Http;

namespace OsbizTaxation.Services;

public sealed class DataFetcher : IDisposable
{
    private readonly HttpClient _http;

    public DataFetcher()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("OsbizTaxation/1.0");
    }

    // TODO: adapter la recuperation une fois la structure des donnees et la
    // methode d'acces connues (GET simple, POST, parametres, entetes, etc.).
    public async Task<string> FetchAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("L'URL source n'est pas configuree.");

        using var response = await _http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public void Dispose() => _http.Dispose();
}
