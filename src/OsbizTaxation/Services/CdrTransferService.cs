using System.IO;
using OsbizTaxation.Models;

namespace OsbizTaxation.Services;

public sealed record SiteTransferResult(string Site, int Appels, string? FichierBrut, bool Supprime);

/// <summary>Orchestre le transfert d'un site : GET, copie brute, analyse, insertion, DELETE.</summary>
public sealed class CdrTransferService
{
    private readonly OpenScapeClient _client = new();

    public async Task<SiteTransferResult> TransferAsync(
        SiteConfig site,
        AppConfig config,
        CdrRepository repository,
        IProgress<string> log,
        CancellationToken ct = default)
    {
        log.Report($"[{site.Nom}] Telechargement...");
        var content = await _client.DownloadAsync(site, ct).ConfigureAwait(false);

        string? rawPath = null;
        if (!string.IsNullOrWhiteSpace(content))
        {
            Directory.CreateDirectory(AppPaths.RawDirectory);
            var fileName = $"{Sanitize(site.Nom)}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            rawPath = Path.Combine(AppPaths.RawDirectory, fileName);
            await File.WriteAllTextAsync(rawPath, content, ct).ConfigureAwait(false);
            log.Report($"[{site.Nom}] Copie brute : {fileName}");
        }
        else
        {
            log.Report($"[{site.Nom}] Aucune donnee renvoyee par le PBX.");
        }

        var pays = Countries.NameFor(site.PaysCode);
        var records = CdrParser.Parse(content, site.Nom, pays, Path.GetFileName(rawPath) ?? string.Empty, config.Lignes);
        var horodatage = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        foreach (var record in records)
            record.DateTransfert = horodatage;

        var inserted = repository.Insert(records);
        log.Report($"[{site.Nom}] {inserted} appel(s) importe(s).");

        var supprime = false;
        if (site.SupprimerApresTransfert)
        {
            await _client.DeleteAsync(site, ct).ConfigureAwait(false);
            supprime = true;
            log.Report($"[{site.Nom}] Fichier supprime sur le PBX (DELETE).");
        }

        return new SiteTransferResult(site.Nom, inserted, rawPath, supprime);
    }

    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "site" : cleaned;
    }
}
