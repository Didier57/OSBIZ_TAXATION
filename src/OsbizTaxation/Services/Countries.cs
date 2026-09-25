using System.Globalization;

namespace OsbizTaxation.Services;

public sealed record Country(string Code, string Name)
{
    public override string ToString() => Name;
}

public static class Countries
{
    public static IReadOnlyList<Country> All { get; } = Build();

    public static string NameFor(string code)
        => All.FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase))?.Name ?? code;

    private static IReadOnlyList<Country> Build()
    {
        var list = new List<Country>();
        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            try
            {
                var region = new RegionInfo(culture.Name);
                if (list.All(c => c.Code != region.TwoLetterISORegionName))
                    list.Add(new Country(region.TwoLetterISORegionName, region.DisplayName));
            }
            catch
            {
                // Culture sans region exploitable : ignoree.
            }
        }

        return list.OrderBy(c => c.Name, StringComparer.CurrentCulture).ToList();
    }
}
