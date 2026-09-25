namespace OsbizTaxation.Helpers;

/// <summary>Mise en forme des durees (en secondes) pour l'affichage.</summary>
public static class DurationFormat
{
    /// <summary>Ex. 9607 -> "2 h 40 m 7 s".</summary>
    public static string Hms(long secondes)
    {
        if (secondes < 0)
            secondes = 0;

        long h = secondes / 3600;
        long m = secondes % 3600 / 60;
        long s = secondes % 60;

        var parts = new List<string>(3);
        if (h > 0)
            parts.Add(h + " h");
        if (h > 0 || m > 0)
            parts.Add(m + " m");
        parts.Add(s + " s");

        return string.Join(' ', parts);
    }

    /// <summary>Version compacte. Ex. 9607 -> "2h40m7s".</summary>
    public static string Compact(long secondes)
    {
        if (secondes < 0)
            secondes = 0;

        long h = secondes / 3600;
        long m = secondes % 3600 / 60;
        long s = secondes % 60;

        if (h > 0)
            return $"{h}h{m:00}m{s:00}s";
        if (m > 0)
            return $"{m}m{s:00}s";
        return $"{s}s";
    }
}
