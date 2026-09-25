using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using OsbizTaxation.Models;

namespace OsbizTaxation.Helpers;

/// <summary>Tableau croise dynamique (Site / Type d'appel x Annee / Mois / Jour), repliable, exportable vers Excel.</summary>
public sealed class PivotTable : UserControl
{
    private readonly Grid _grid = new();
    private readonly HashSet<int> _collapsedMonths = new();
    private readonly HashSet<string> _collapsedSites = new();
    private List<PivotCall> _data = new();
    private string _year = "";
    private Brush _cellBorder = Brushes.Gray;
    private bool _collapseAllMonths;

    public PivotTable()
    {
        Content = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _grid
        };
    }

    /// <summary>Alimente le tableau avec les donnees et l'annee affichee, puis reconstruit la vue.</summary>
    public void SetData(IEnumerable<PivotCall> data, string year)
    {
        _data = data?.ToList() ?? new List<PivotCall>();
        _year = year ?? "";
        _collapsedMonths.Clear();
        _collapsedSites.Clear();
        _collapseAllMonths = true;
        Build();
    }

    private sealed record PivotData(
        List<DateTime> Days,
        List<int> Months,
        List<string> Sites,
        Dictionary<string, List<string>> TypesBySite,
        Dictionary<string, int> DayType,
        Dictionary<string, int> MonthType,
        Dictionary<string, int> DaySite,
        Dictionary<string, int> MonthSite,
        Dictionary<string, int> DayAll,
        Dictionary<string, int> MonthAll,
        Dictionary<string, int> TypeGrand,
        Dictionary<string, int> SiteGrand,
        int Grand);

    private PivotData Compute()
    {
        var days = new List<DateTime>();
        var seenDays = new HashSet<string>();
        var months = new List<int>();
        var dayType = new Dictionary<string, int>();
        var monthType = new Dictionary<string, int>();
        var daySite = new Dictionary<string, int>();
        var monthSite = new Dictionary<string, int>();
        var dayAll = new Dictionary<string, int>();
        var monthAll = new Dictionary<string, int>();
        var typeGrand = new Dictionary<string, int>();
        var siteGrand = new Dictionary<string, int>();
        var typesBySite = new Dictionary<string, List<string>>();
        var grand = 0;

        foreach (var p in _data)
        {
            var parsed = ParseIso(p.DateIso);
            if (parsed.HasValue)
            {
                var day = parsed.Value.Date;
                if (seenDays.Add(p.DateIso))
                    days.Add(day);
                if (!months.Contains(day.Month))
                    months.Add(day.Month);
            }

            var mm = p.DateIso.Length >= 7 ? p.DateIso.Substring(5, 2) : "";
            Inc(dayType, K(p.Site, p.Information, p.DateIso), p.Count);
            Inc(monthType, K(p.Site, p.Information, mm), p.Count);
            Inc(daySite, K(p.Site, "", p.DateIso), p.Count);
            Inc(monthSite, K(p.Site, "", mm), p.Count);
            Inc(dayAll, p.DateIso, p.Count);
            Inc(monthAll, mm, p.Count);
            Inc(typeGrand, K(p.Site, p.Information, ""), p.Count);
            Inc(siteGrand, p.Site, p.Count);
            grand += p.Count;

            if (!typesBySite.TryGetValue(p.Site, out var list))
            {
                list = new List<string>();
                typesBySite[p.Site] = list;
            }

            if (!list.Contains(p.Information))
                list.Add(p.Information);
        }

        days.Sort();
        months.Sort();
        var sites = new List<string>(typesBySite.Keys);
        sites.Sort(StringComparer.CurrentCultureIgnoreCase);
        foreach (var list in typesBySite.Values)
            list.Sort(StringComparer.CurrentCultureIgnoreCase);

        return new PivotData(days, months, sites, typesBySite, dayType, monthType,
            daySite, monthSite, dayAll, monthAll, typeGrand, siteGrand, grand);
    }

    private void Build()
    {
        _grid.Children.Clear();
        _grid.RowDefinitions.Clear();
        _grid.ColumnDefinitions.Clear();
        _grid.Background = Brushes.Transparent;

        var dark = ThemeManager.IsDark;
        var fg = Frozen(dark ? 0xDDDDDD : 0x1F1F1F);
        var headerBg = Frozen(dark ? 0x333337 : 0xEAEAEA);
        var siteBg = Frozen(dark ? 0x2A2A2E : 0xF4F4F4);
        var totalBg = Frozen(dark ? 0x243A4E : 0xE3EFFB);
        _cellBorder = Frozen(dark ? 0x3A3A3D : 0xC8C8C8);

        if (_data.Count == 0)
        {
            EnsureRow(0);
            _grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            AddCell("Aucune donnée", 0, 0, 1, 1, fg, null, false, TextAlignment.Center);
            return;
        }

        var d = Compute();
        if (d.Days.Count == 0)
        {
            EnsureRow(0);
            _grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            AddCell("Aucune donnée", 0, 0, 1, 1, fg, null, false, TextAlignment.Center);
            return;
        }

        if (_collapseAllMonths)
        {
            _collapsedMonths.Clear();
            foreach (var m in d.Months)
                _collapsedMonths.Add(m);
            _collapseAllMonths = false;
        }

        var leaves = new List<(int Month, DateTime? Day)>();
        foreach (var m in d.Months)
        {
            if (_collapsedMonths.Contains(m))
                leaves.Add((m, null));
            else
                foreach (var day in d.Days)
                    if (day.Month == m)
                        leaves.Add((m, day));
        }

        var leafCount = leaves.Count;
        const int colSite = 0;
        const int colType = 1;
        const int colFirstLeaf = 2;
        var colTotal = 2 + leafCount;

        _grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        for (var i = 0; i < leafCount; i++)
            _grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        _grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

        for (var r = 0; r < 3; r++)
            _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        AddCell("Site", 0, colSite, 3, 1, fg, headerBg, true, TextAlignment.Left);
        AddCell("Type d'appel", 0, colType, 3, 1, fg, headerBg, true, TextAlignment.Left);
        AddCell(_year, 0, colFirstLeaf, 1, leafCount, fg, headerBg, true, TextAlignment.Center);
        AddCell("Total", 0, colTotal, 3, 1, fg, headerBg, true, TextAlignment.Center);

        var idx = 0;
        while (idx < leaves.Count)
        {
            var m = leaves[idx].Month;
            var span = 0;
            while (idx + span < leaves.Count && leaves[idx + span].Month == m)
                span++;
            var capturedMonth = m;
            AddToggleCell(MonthName(m), _collapsedMonths.Contains(m),
                () =>
                {
                    if (!_collapsedMonths.Add(capturedMonth))
                        _collapsedMonths.Remove(capturedMonth);
                },
                1, colFirstLeaf + idx, 1, span, headerBg, fg);

            for (var k = 0; k < span; k++)
            {
                var leaf = leaves[idx + k];
                var label = leaf.Day is DateTime day ? day.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : "";
                AddCell(label, 2, colFirstLeaf + idx + k, 1, 1, fg, headerBg, false, TextAlignment.Center);
            }

            idx += span;
        }

        var row = 3;
        foreach (var site in d.Sites)
        {
            var types = d.TypesBySite.TryGetValue(site, out var list) ? list : new List<string>();
            var siteCollapsed = _collapsedSites.Contains(site);
            var blockRows = siteCollapsed ? 1 : 1 + types.Count;
            var capturedSite = site;

            AddToggleCell(site, siteCollapsed,
                () =>
                {
                    if (!_collapsedSites.Add(capturedSite))
                        _collapsedSites.Remove(capturedSite);
                },
                row, colSite, blockRows, 1, siteBg, fg);

            AddCell("", row, colType, 1, 1, fg, siteBg, true, TextAlignment.Left);
            for (var i = 0; i < leafCount; i++)
                AddCell(SiteLeaf(d, site, leaves[i]).ToString(CultureInfo.InvariantCulture), row, colFirstLeaf + i, 1, 1, fg, siteBg, true, TextAlignment.Right);
            AddCell(Get(d.SiteGrand, site).ToString(CultureInfo.InvariantCulture), row, colTotal, 1, 1, fg, siteBg, true, TextAlignment.Right);
            row++;

            if (!siteCollapsed)
            {
                foreach (var type in types)
                {
                    AddCell(type, row, colType, 1, 1, fg, null, false, TextAlignment.Left);
                    for (var i = 0; i < leafCount; i++)
                        AddCell(TypeLeaf(d, site, type, leaves[i]).ToString(CultureInfo.InvariantCulture), row, colFirstLeaf + i, 1, 1, fg, null, false, TextAlignment.Right);
                    AddCell(Get(d.TypeGrand, K(site, type, "")).ToString(CultureInfo.InvariantCulture), row, colTotal, 1, 1, fg, null, false, TextAlignment.Right);
                    row++;
                }
            }
        }

        AddCell("Total", row, colSite, 1, 2, fg, totalBg, true, TextAlignment.Left);
        for (var i = 0; i < leafCount; i++)
            AddCell(AllLeaf(d, leaves[i]).ToString(CultureInfo.InvariantCulture), row, colFirstLeaf + i, 1, 1, fg, totalBg, true, TextAlignment.Right);
        AddCell(d.Grand.ToString(CultureInfo.InvariantCulture), row, colTotal, 1, 1, fg, totalBg, true, TextAlignment.Right);
    }

    /// <summary>Construit une grille Excel entierement depliee (sans etat de repli). null si aucune donnee.</summary>
    public ExcelGrid? BuildExport()
    {
        if (_data.Count == 0)
            return null;

        var d = Compute();
        if (d.Days.Count == 0)
            return null;

        const int colSite = 0;
        const int colType = 1;
        const int colFirstLeaf = 2;
        var colTotal = 2 + d.Days.Count;

        var grid = new ExcelGrid { ColumnCount = colTotal + 1 };
        grid.ColumnWidths[colSite] = 16;
        grid.ColumnWidths[colType] = 20;
        for (var i = 0; i < d.Days.Count; i++)
            grid.ColumnWidths[colFirstLeaf + i] = 11;
        grid.ColumnWidths[colTotal] = 9;

        List<ExcelGridCell> NewRow() => Enumerable.Range(0, grid.ColumnCount)
            .Select(_ => ExcelGridCell.Empty).ToList();

        static ExcelGridCell Hdr(string text) => new() { Text = text, Header = true };
        static ExcelGridCell Txt(string text) => new() { Text = text, Bold = true };
        static ExcelGridCell Num(int value, bool bold = false) => new() { Number = value, Bold = bold };

        var h0 = NewRow();
        h0[colSite] = Hdr("Site");
        h0[colType] = Hdr("Type d'appel");
        h0[colFirstLeaf] = Hdr(_year);
        h0[colTotal] = Hdr("Total");
        grid.Rows.Add(h0);

        var h1 = NewRow();
        var offset = 0;
        foreach (var m in d.Months)
        {
            var span = d.Days.Count(day => day.Month == m);
            h1[colFirstLeaf + offset] = Hdr(MonthName(m));
            offset += span;
        }
        grid.Rows.Add(h1);

        var h2 = NewRow();
        for (var i = 0; i < d.Days.Count; i++)
            h2[colFirstLeaf + i] = Hdr(d.Days[i].ToString("dd/MM/yyyy", CultureInfo.InvariantCulture));
        grid.Rows.Add(h2);

        grid.Merges.Add((0, colSite, 3, 1));
        grid.Merges.Add((0, colType, 3, 1));
        grid.Merges.Add((0, colFirstLeaf, 1, d.Days.Count));
        grid.Merges.Add((0, colTotal, 3, 1));
        offset = 0;
        foreach (var m in d.Months)
        {
            var span = d.Days.Count(day => day.Month == m);
            grid.Merges.Add((1, colFirstLeaf + offset, 1, span));
            offset += span;
        }

        var rowIndex = 3;
        foreach (var site in d.Sites)
        {
            var types = d.TypesBySite.TryGetValue(site, out var list) ? list : new List<string>();

            var siteRow = NewRow();
            siteRow[colSite] = Txt(site);
            for (var i = 0; i < d.Days.Count; i++)
                siteRow[colFirstLeaf + i] = Num(Get(d.DaySite, K(site, "", Iso(d.Days[i]))));
            siteRow[colTotal] = Num(Get(d.SiteGrand, site), true);
            grid.Rows.Add(siteRow);
            grid.Merges.Add((rowIndex, colSite, 1 + types.Count, 1));

            foreach (var type in types)
            {
                var typeRow = NewRow();
                typeRow[colType] = Txt(type);
                for (var i = 0; i < d.Days.Count; i++)
                    typeRow[colFirstLeaf + i] = Num(Get(d.DayType, K(site, type, Iso(d.Days[i]))));
                typeRow[colTotal] = Num(Get(d.TypeGrand, K(site, type, "")));
                grid.Rows.Add(typeRow);
            }

            rowIndex += 1 + types.Count;
        }

        var totalRow = NewRow();
        totalRow[colSite] = Hdr("Total");
        for (var i = 0; i < d.Days.Count; i++)
            totalRow[colFirstLeaf + i] = Num(Get(d.DayAll, Iso(d.Days[i])), true);
        totalRow[colTotal] = Num(d.Grand, true);
        grid.Rows.Add(totalRow);
        grid.Merges.Add((rowIndex, colSite, 1, 2));

        return grid;
    }

    private static int SiteLeaf(PivotData d, string site, (int Month, DateTime? Day) leaf)
        => leaf.Day is DateTime day ? Get(d.DaySite, K(site, "", Iso(day))) : Get(d.MonthSite, K(site, "", leaf.Month.ToString("00", CultureInfo.InvariantCulture)));

    private static int TypeLeaf(PivotData d, string site, string type, (int Month, DateTime? Day) leaf)
        => leaf.Day is DateTime day ? Get(d.DayType, K(site, type, Iso(day))) : Get(d.MonthType, K(site, type, leaf.Month.ToString("00", CultureInfo.InvariantCulture)));

    private static int AllLeaf(PivotData d, (int Month, DateTime? Day) leaf)
        => leaf.Day is DateTime day ? Get(d.DayAll, Iso(day)) : Get(d.MonthAll, leaf.Month.ToString("00", CultureInfo.InvariantCulture));

    private void EnsureRow(int row)
    {
        while (_grid.RowDefinitions.Count <= row)
            _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
    }

    private Border AddCell(string text, int row, int col, int rowSpan, int colSpan, Brush fg, Brush? bg, bool bold, TextAlignment align)
    {
        EnsureRow(row);
        var textBlock = new TextBlock
        {
            Text = text,
            Foreground = fg,
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
            TextAlignment = align,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var border = new Border
        {
            Background = bg ?? Brushes.Transparent,
            BorderBrush = _cellBorder,
            BorderThickness = new Thickness(0, 0, 1, 1),
            Padding = new Thickness(5, 2, 5, 2),
            MinHeight = 24,
            Child = textBlock
        };
        Grid.SetRow(border, row);
        Grid.SetColumn(border, col);
        Grid.SetRowSpan(border, rowSpan);
        Grid.SetColumnSpan(border, colSpan);
        _grid.Children.Add(border);
        return border;
    }

    private Border AddToggleCell(string label, bool collapsed, Action onToggle, int row, int col, int rowSpan, int colSpan, Brush bg, Brush fg)
    {
        EnsureRow(row);
        var button = new Button
        {
            Content = collapsed ? "\u25B6" : "\u25BC",
            Width = 16,
            Height = 16,
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            Focusable = false,
            FontSize = 9,
            Foreground = fg,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 3, 0)
        };
        button.Click += (_, _) =>
        {
            onToggle();
            Build();
        };

        var panel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        panel.Children.Add(button);
        panel.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = fg,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });

        var border = new Border
        {
            Background = bg,
            BorderBrush = _cellBorder,
            BorderThickness = new Thickness(0, 0, 1, 1),
            Padding = new Thickness(5, 2, 5, 2),
            MinHeight = 24,
            Child = panel
        };
        Grid.SetRow(border, row);
        Grid.SetColumn(border, col);
        Grid.SetRowSpan(border, rowSpan);
        Grid.SetColumnSpan(border, colSpan);
        _grid.Children.Add(border);
        return border;
    }

    private static SolidColorBrush Frozen(int rgb)
    {
        var brush = new SolidColorBrush(Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb));
        brush.Freeze();
        return brush;
    }

    private static void Inc(Dictionary<string, int> map, string key, int value)
        => map[key] = Get(map, key) + value;

    private static int Get(Dictionary<string, int> map, string key)
        => map.TryGetValue(key, out var value) ? value : 0;

    private static string K(string a, string b, string c) => a + "\u0001" + b + "\u0001" + c;

    private static string Iso(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static DateTime? ParseIso(string iso)
        => DateTime.TryParseExact(iso, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : null;

    private static string MonthName(int month)
        => CultureInfo.GetCultureInfo("fr-FR").DateTimeFormat.GetMonthName(month);
}
