using System.IO;
using System.IO.Compression;
using System.Text;

namespace OsbizTaxation.Services;

/// <summary>
/// Ecrit un classeur Excel (.xlsx) minimal, sans dependance externe.
/// Chaque ligne peut recevoir une couleur de fond (RGB 6 chiffres hexa) pour
/// reproduire la coloration des groupes d'appels.
/// </summary>
public static class ExcelExporter
{
    private static readonly UTF8Encoding Utf8 = new(false);
    private const string Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    public static void Export(
        string filePath,
        IReadOnlyList<string> headers,
        IReadOnlyList<(string[] Cells, string? Fill)> rows,
        string sheetName = "Feuille1")
    {
        var fills = rows
            .Select(r => r.Fill)
            .Where(f => !string.IsNullOrEmpty(f))
            .Select(f => f!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var fillIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < fills.Count; i++)
            fillIndex[fills[i]] = i;

        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Create);

        AddEntry(zip, "[Content_Types].xml", ContentTypes());
        AddEntry(zip, "_rels/.rels", RootRels());
        AddEntry(zip, "xl/workbook.xml", Workbook(sheetName));
        AddEntry(zip, "xl/_rels/workbook.xml.rels", WorkbookRels());
        AddEntry(zip, "xl/styles.xml", Styles(fills));
        AddEntry(zip, "xl/worksheets/sheet1.xml", Sheet(headers, rows, fillIndex));
    }

    private static void AddEntry(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), Utf8);
        writer.Write(content);
    }

    private static string ContentTypes() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
        "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
        "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
        "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
        "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
        "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
        "</Types>";

    private static string RootRels() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
        "</Relationships>";

    private static string Workbook(string sheetName) =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        $"<workbook xmlns=\"{Ns}\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
        $"<sheets><sheet name=\"{Escape(Shorten(sheetName, 31))}\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
        "</workbook>";

    private static string WorkbookRels() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
        "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
        "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
        "</Relationships>";

    private static string Styles(IReadOnlyList<string> fills)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append($"<styleSheet xmlns=\"{Ns}\">");

        sb.Append("<fonts count=\"2\">");
        sb.Append("<font><sz val=\"11\"/><name val=\"Calibri\"/></font>");
        sb.Append("<font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font>");
        sb.Append("</fonts>");

        sb.Append($"<fills count=\"{2 + fills.Count}\">");
        sb.Append("<fill><patternFill patternType=\"none\"/></fill>");
        sb.Append("<fill><patternFill patternType=\"gray125\"/></fill>");
        foreach (var fill in fills)
            sb.Append($"<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF{fill.ToUpperInvariant()}\"/><bgColor indexed=\"64\"/></patternFill></fill>");
        sb.Append("</fills>");

        sb.Append("<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>");
        sb.Append("<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>");

        sb.Append($"<cellXfs count=\"{2 + fills.Count}\">");
        sb.Append("<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>");
        sb.Append("<xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>");
        for (var i = 0; i < fills.Count; i++)
            sb.Append($"<xf numFmtId=\"0\" fontId=\"0\" fillId=\"{2 + i}\" borderId=\"0\" xfId=\"0\" applyFill=\"1\"/>");
        sb.Append("</cellXfs>");

        sb.Append("<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>");
        sb.Append("</styleSheet>");
        return sb.ToString();
    }

    private static string Sheet(
        IReadOnlyList<string> headers,
        IReadOnlyList<(string[] Cells, string? Fill)> rows,
        IReadOnlyDictionary<string, int> fillIndex)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append($"<worksheet xmlns=\"{Ns}\"><sheetData>");

        sb.Append("<row r=\"1\">");
        for (var c = 0; c < headers.Count; c++)
            sb.Append($"<c r=\"{ColumnName(c)}1\" s=\"1\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{Escape(headers[c])}</t></is></c>");
        sb.Append("</row>");

        for (var r = 0; r < rows.Count; r++)
        {
            var (cells, fill) = rows[r];
            var rowNumber = r + 2;
            var style = 0;
            if (!string.IsNullOrEmpty(fill) && fillIndex.TryGetValue(fill, out var index))
                style = 2 + index;

            sb.Append($"<row r=\"{rowNumber}\">");
            for (var c = 0; c < cells.Length; c++)
            {
                var reference = $"{ColumnName(c)}{rowNumber}";
                var text = cells[c] ?? string.Empty;
                if (style == 0)
                    sb.Append($"<c r=\"{reference}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{Escape(text)}</t></is></c>");
                else
                    sb.Append($"<c r=\"{reference}\" s=\"{style}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{Escape(text)}</t></is></c>");
            }
            sb.Append("</row>");
        }

        sb.Append("</sheetData></worksheet>");
        return sb.ToString();
    }

    private static string ColumnName(int index)
    {
        var name = string.Empty;
        var i = index;
        while (true)
        {
            name = (char)('A' + i % 26) + name;
            i = i / 26 - 1;
            if (i < 0)
                break;
        }
        return name;
    }

    private static string Shorten(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private static string Escape(string? value) =>
        (value ?? string.Empty)
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
}
