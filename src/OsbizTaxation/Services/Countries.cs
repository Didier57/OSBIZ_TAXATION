using System.Globalization;

namespace OsbizTaxation.Services;

public sealed record Country(string Code, string Name)
{
    public override string ToString() => Name;
}

public static class Countries
{
    private static readonly CultureInfo FrenchCulture = CultureInfo.GetCultureInfo("fr-FR");

    public static IReadOnlyList<Country> All { get; } = Build();

    public static string NameFor(string code)
        => All.FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase))?.Name ?? code;

    private static IReadOnlyList<Country> Build()
    {
        // On collecte d'abord les codes de region (et non les cultures completes),
        // afin de pouvoir afficher le nom du pays en francais quel que soit le
        // pays d'origine de la culture.
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            try
            {
                var region = new RegionInfo(culture.Name);
                if (region.TwoLetterISORegionName.Length == 2)
                    codes.Add(region.TwoLetterISORegionName);
            }
            catch
            {
                // Culture sans region exploitable : ignoree.
            }
        }

        var previousUi = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = FrenchCulture;
        try
        {
            var list = new List<Country>();
            foreach (var code in codes)
            {
                try
                {
                    var region = new RegionInfo(code);
                    list.Add(new Country(region.TwoLetterISORegionName, region.DisplayName));
                }
                catch
                {
                    // Code region sans nom exploitable : ignore.
                }
            }

            return list
                .OrderBy(c => c.Name, StringComparer.Create(FrenchCulture, true))
                .ToList();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUi;
        }
    }
}
