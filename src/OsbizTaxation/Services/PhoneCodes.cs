using System.Globalization;

namespace OsbizTaxation.Services;

/// <summary>
/// Determine le pays de destination d'un numero externe a partir de son
/// indicatif international (numeros commencant par 00).
/// </summary>
public static class PhoneCodes
{
    // Indicatif (sans le 00) -> code ISO 3166-1 alpha-2.
    private static readonly Dictionary<string, string> CodeToIso = new()
    {
        ["1"] = "US", ["7"] = "RU", ["20"] = "EG", ["27"] = "ZA", ["30"] = "GR",
        ["31"] = "NL", ["32"] = "BE", ["33"] = "FR", ["34"] = "ES", ["36"] = "HU",
        ["39"] = "IT", ["40"] = "RO", ["41"] = "CH", ["43"] = "AT", ["44"] = "GB",
        ["45"] = "DK", ["46"] = "SE", ["47"] = "NO", ["48"] = "PL", ["49"] = "DE",
        ["51"] = "PE", ["52"] = "MX", ["53"] = "CU", ["54"] = "AR", ["55"] = "BR",
        ["56"] = "CL", ["57"] = "CO", ["58"] = "VE", ["60"] = "MY", ["61"] = "AU",
        ["62"] = "ID", ["63"] = "PH", ["64"] = "NZ", ["65"] = "SG", ["66"] = "TH",
        ["81"] = "JP", ["82"] = "KR", ["84"] = "VN", ["86"] = "CN", ["90"] = "TR",
        ["91"] = "IN", ["92"] = "PK", ["93"] = "AF", ["94"] = "LK", ["95"] = "MM",
        ["98"] = "IR",
        ["211"] = "SS", ["212"] = "MA", ["213"] = "DZ", ["216"] = "TN", ["218"] = "LY",
        ["220"] = "GM", ["221"] = "SN", ["222"] = "MR", ["223"] = "ML", ["224"] = "GN",
        ["225"] = "CI", ["226"] = "BF", ["227"] = "NE", ["228"] = "TG", ["229"] = "BJ",
        ["230"] = "MU", ["231"] = "LR", ["232"] = "SL", ["233"] = "GH", ["234"] = "NG",
        ["235"] = "TD", ["236"] = "CF", ["237"] = "CM", ["238"] = "CV", ["239"] = "ST",
        ["240"] = "GQ", ["241"] = "GA", ["242"] = "CG", ["243"] = "CD", ["244"] = "AO",
        ["245"] = "GW", ["246"] = "IO", ["247"] = "AC", ["248"] = "SC", ["249"] = "SD",
        ["250"] = "RW", ["251"] = "ET", ["252"] = "SO", ["253"] = "DJ", ["254"] = "KE",
        ["255"] = "TZ", ["256"] = "UG", ["257"] = "BI", ["258"] = "MZ", ["260"] = "ZM",
        ["261"] = "MG", ["262"] = "RE", ["263"] = "ZW", ["264"] = "NA", ["265"] = "MW",
        ["266"] = "LS", ["267"] = "BW", ["268"] = "SZ", ["269"] = "KM", ["290"] = "SH",
        ["291"] = "ER", ["297"] = "AW", ["298"] = "FO", ["299"] = "GL",
        ["350"] = "GI", ["351"] = "PT", ["352"] = "LU", ["353"] = "IE", ["354"] = "IS",
        ["355"] = "AL", ["356"] = "MT", ["357"] = "CY", ["358"] = "FI", ["359"] = "BG",
        ["370"] = "LT", ["371"] = "LV", ["372"] = "EE", ["373"] = "MD", ["374"] = "AM",
        ["375"] = "BY", ["376"] = "AD", ["377"] = "MC", ["378"] = "SM", ["380"] = "UA",
        ["381"] = "RS", ["382"] = "ME", ["383"] = "XK", ["385"] = "HR", ["386"] = "SI",
        ["387"] = "BA", ["389"] = "MK",
        ["420"] = "CZ", ["421"] = "SK", ["423"] = "LI",
        ["500"] = "FK", ["501"] = "BZ", ["502"] = "GT", ["503"] = "SV", ["504"] = "HN",
        ["505"] = "NI", ["506"] = "CR", ["507"] = "PA", ["508"] = "PM", ["509"] = "HT",
        ["590"] = "GP", ["591"] = "BO", ["592"] = "GY", ["593"] = "EC", ["594"] = "GF",
        ["595"] = "PY", ["596"] = "MQ", ["597"] = "SR", ["598"] = "UY", ["599"] = "CW",
        ["670"] = "TL", ["672"] = "NF", ["673"] = "BN", ["674"] = "NR", ["675"] = "PG",
        ["676"] = "TO", ["677"] = "SB", ["678"] = "VU", ["679"] = "FJ", ["680"] = "PW",
        ["681"] = "WF", ["682"] = "CK", ["683"] = "NU", ["685"] = "WS", ["686"] = "KI",
        ["687"] = "NC", ["688"] = "TV", ["689"] = "PF", ["690"] = "TK", ["691"] = "FM",
        ["692"] = "MH",
        ["850"] = "KP", ["852"] = "HK", ["853"] = "MO", ["855"] = "KH", ["856"] = "LA",
        ["880"] = "BD", ["886"] = "TW",
        ["960"] = "MV", ["961"] = "LB", ["962"] = "JO", ["963"] = "SY", ["964"] = "IQ",
        ["965"] = "KW", ["966"] = "SA", ["967"] = "YE", ["968"] = "OM", ["970"] = "PS",
        ["971"] = "AE", ["972"] = "IL", ["973"] = "BH", ["974"] = "QA", ["975"] = "BT",
        ["976"] = "MN", ["977"] = "NP",
        ["992"] = "TJ", ["993"] = "TM", ["994"] = "AZ", ["995"] = "GE", ["996"] = "KG",
        ["998"] = "UZ"
    };

    // Noms francais de secours pour les territoires absents de RegionInfo.
    private static readonly Dictionary<string, string> NameOverrides = new(StringComparer.OrdinalIgnoreCase)
    {
        ["XK"] = "Kosovo",
        ["AC"] = "Île de l'Ascension",
        ["TA"] = "Tristan da Cunha",
        ["IC"] = "Îles Canaries",
        ["EA"] = "Ceuta et Melilla",
        ["CW"] = "Curaçao",
        ["SX"] = "Saint-Martin (Pays-Bas)",
        ["BQ"] = "Bonaire, Saint-Eustache et Saba",
        ["MF"] = "Saint-Martin (France)",
        ["BL"] = "Saint-Barthélemy",
        ["SS"] = "Soudan du Sud",
        ["TL"] = "Timor oriental",
        ["IO"] = "Territoire britannique de l'océan Indien",
        ["SH"] = "Sainte-Hélène",
        ["TK"] = "Tokelau",
        ["NU"] = "Niue",
        ["WF"] = "Wallis-et-Futuna",
        ["EH"] = "Sahara occidental"
    };

    /// <summary>
    /// Retourne le nom du pays de destination si le numero commence par 00,
    /// sinon null (appel national dans le pays d'origine du site).
    /// </summary>
    public static string? CountryNameForNumber(string? number)
    {
        if (string.IsNullOrWhiteSpace(number))
            return null;

        var digits = new string(number.Where(char.IsDigit).ToArray());
        if (!digits.StartsWith("00", StringComparison.Ordinal))
            return null;

        var rest = digits.Substring(2);
        for (var length = 3; length >= 1; length--)
        {
            if (rest.Length < length)
                continue;
            if (CodeToIso.TryGetValue(rest.Substring(0, length), out var iso))
                return CountryName(iso);
        }

        return null;
    }

    /// <summary>Nom francais d'un pays a partir de son code ISO.</summary>
    public static string CountryName(string iso)
    {
        var name = Countries.NameFor(iso);
        if (!string.Equals(name, iso, StringComparison.OrdinalIgnoreCase))
            return name;
        return NameOverrides.TryGetValue(iso, out var fallback) ? fallback : iso;
    }
}
