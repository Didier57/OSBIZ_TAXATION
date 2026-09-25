using System.Globalization;
using OsbizTaxation.Models;

namespace OsbizTaxation.Services;

/// <summary>
/// Analyse le contenu d'un fichier CDR OpenScape Business
/// (format compresse 14/17 champs ou decompresse 9 champs avec en-tete).
/// </summary>
public static class CdrParser
{
    private static readonly string[] InfoLibelles =
    {
        "Info appel",              // 0
        "Entrant",                 // 1
        "Sortant",                 // 2
        "Entrant autre service",   // 3
        "Sortant autre service",   // 4
        "Entrant route",           // 5
        "Sortant route",           // 6
        "Conference entrante",     // 7
        "Conference sortante",     // 8
        "Sortant deviation externe"// 9
    };

    public static string InfoLibelle(int code)
    {
        var baseCode = code % 10;
        if (baseCode < 0 || baseCode >= InfoLibelles.Length)
            return code.ToString(CultureInfo.InvariantCulture);
        return InfoLibelles[baseCode];
    }

    public static List<CdrRecord> Parse(
        string content,
        string site,
        string pays,
        string sourceFile,
        IReadOnlyList<LineConfig> lignes)
    {
        var records = new List<CdrRecord>();
        if (string.IsNullOrWhiteSpace(content))
            return records;

        var lines = content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
                continue;

            // Ligne d'en-tete du format decompresse : ignoree.
            if (trimmed.StartsWith("Date|", StringComparison.OrdinalIgnoreCase))
                continue;

            var record = ParseLine(trimmed, site, sourceFile, lignes);
            if (record != null)
            {
                record.Pays = pays;
                records.Add(record);
            }
        }

        return records;
    }

    private static CdrRecord? ParseLine(
        string line,
        string site,
        string sourceFile,
        IReadOnlyList<LineConfig> lignes)
    {
        var fields = line.Split('|');
        if (fields.Length < 6)
            return null;

        string Get(int index) => index < fields.Length ? fields[index].Trim() : string.Empty;

        var dateRaw = Get(0);
        var heureFinRaw = Get(1);
        var ligne = Get(2);
        var numeroInterne = Get(3);
        var dureeSonnerie = Get(4);
        var dureeAppel = Get(5);
        var numeroExterne = Get(6);
        var infoRaw = Get(8);

        // Format US = 17 champs (le n° de station est en position 17),
        // format standard = 14 champs (le n° de station est en position 14).
        var numeroExtra = fields.Length >= 17 ? Get(16) : Get(13);

        var date = ParseDate(dateRaw);
        var fin = CombineDateHeure(date, heureFinRaw);
        var duree = ParseDuration(dureeAppel);

        var heureDebut = string.Empty;
        if (fin.HasValue && duree.HasValue)
            heureDebut = (fin.Value - duree.Value).ToString("HH:mm:ss");
        else if (fin.HasValue)
            heureDebut = fin.Value.ToString("HH:mm:ss");

        int.TryParse(infoRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var infoCode);

        var record = new CdrRecord
        {
            Site = site,
            Date = date?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? dateRaw,
            DateIso = date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
            HeureDebut = heureDebut,
            HeureFin = fin?.ToString("HH:mm:ss", CultureInfo.InvariantCulture) ?? heureFinRaw,
            Ligne = ligne,
            NumeroInterne = numeroInterne,
            DureeSonnerie = dureeSonnerie,
            DureeAppel = dureeAppel,
            NumeroExterne = numeroExterne,
            Information = InfoLibelle(infoCode),
            InfoCode = infoCode,
            NumeroExtra = numeroExtra,
            DureeAppelSecondes = duree.HasValue ? (int)Math.Round(duree.Value.TotalSeconds) : 0,
            RawLine = line,
            SourceFile = sourceFile
        };

        record.NomLigne = ResolveNomLigne(site, ligne, lignes);
        return record;
    }

    private static string ResolveNomLigne(string site, string ligne, IReadOnlyList<LineConfig> lignes)
    {
        if (!int.TryParse(ligne, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numero))
            return string.Empty;

        foreach (var cfg in lignes)
        {
            if (string.Equals(cfg.Site, site, StringComparison.OrdinalIgnoreCase) && cfg.Contient(numero))
                return cfg.NomLigne;
        }

        return string.Empty;
    }

    private static DateTime? ParseDate(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        string[] formats = { "dd.MM.yy", "dd.MM.yyyy", "d.M.yy", "d.M.yyyy" };
        if (DateTime.TryParseExact(raw, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            return dt;

        return null;
    }

    private static DateTime? CombineDateHeure(DateTime? date, string heure)
    {
        if (date == null)
            return null;

        if (TimeSpan.TryParseExact(heure, new[] { "hh\\:mm\\:ss", "h\\:mm\\:ss", "hh\\:mm", "h\\:mm" },
                CultureInfo.InvariantCulture, out var t))
            return date.Value.Date + t;

        return date;
    }

    private static TimeSpan? ParseDuration(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (TimeSpan.TryParseExact(raw, new[] { "hh\\:mm\\:ss", "h\\:mm\\:ss", "hh\\:mm", "h\\:mm", "mm\\:ss" },
                CultureInfo.InvariantCulture, out var t))
            return t;

        return null;
    }
}
