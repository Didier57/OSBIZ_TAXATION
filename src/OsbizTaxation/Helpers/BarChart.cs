using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace OsbizTaxation.Helpers;

/// <summary>Une barre de l'histogramme : un libelle et une valeur.</summary>
public sealed record BarItem(string Label, double Value);

/// <summary>Histogramme simple (une valeur par barre, couleur distincte par barre) avec legende a droite.</summary>
public sealed class BarChart : FrameworkElement
{
    private static readonly Color TextLight = Color.FromRgb(0xDD, 0xDD, 0xDD);
    private static readonly Color TextDark = Color.FromRgb(0x33, 0x33, 0x33);

    private IReadOnlyList<BarItem> _items = Array.Empty<BarItem>();
    private string _title = "";

    public IReadOnlyList<BarItem> Items
    {
        get => _items;
        set
        {
            _items = value ?? Array.Empty<BarItem>();
            InvalidateVisual();
        }
    }

    public string Title
    {
        get => _title;
        set
        {
            _title = value ?? "";
            InvalidateVisual();
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        double w = ActualWidth, h = ActualHeight;
        if (w <= 20 || h <= 20)
            return;

        var dark = ThemeManager.IsDark;
        var textBrush = new SolidColorBrush(dark ? TextLight : TextDark);
        var gridPen = new Pen(new SolidColorBrush(dark ? Color.FromRgb(0x3A, 0x3A, 0x3D) : Color.FromRgb(0xDC, 0xDC, 0xDC)), 1);
        var axisPen = new Pen(textBrush, 1);
        var typeface = new Typeface("Segoe UI");
        var boldTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
        double dip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        const double titleH = 30;
        const double left = 48, right = 150, top = 18, bottom = 76;
        double plotTop = top + titleH;
        double plotW = w - left - right;
        double plotH = h - plotTop - bottom;
        if (plotW <= 20 || plotH <= 20)
            return;

        if (!string.IsNullOrEmpty(_title))
        {
            var titleText = Text(_title, boldTypeface, 15, textBrush, dip);
            dc.DrawText(titleText, new Point((w - titleText.Width) / 2, 4));
        }

        var items = _items;

        double max = 0;
        foreach (var it in items)
            max = Math.Max(max, it.Value);

        if (items.Count == 0 || max <= 0)
        {
            var empty = Text("Aucune donnée", typeface, 13, textBrush, dip);
            dc.DrawText(empty, new Point((w - empty.Width) / 2, (h + titleH - empty.Height) / 2));
            return;
        }

        int yMax = (int)Math.Ceiling(max);
        int step = 1;
        while (yMax / step > 10)
            step++;
        yMax = (int)(Math.Ceiling(max / step) * step);

        double baseY = plotTop + plotH;

        for (int v = 0; v <= yMax; v += step)
        {
            double y = baseY - plotH * ((double)v / yMax);
            dc.DrawLine(gridPen, new Point(left, y), new Point(left + plotW, y));
            var tick = Text(v.ToString(CultureInfo.InvariantCulture), typeface, 11, textBrush, dip);
            dc.DrawText(tick, new Point(left - 8 - tick.Width, y - tick.Height / 2));
        }

        dc.DrawLine(axisPen, new Point(left, plotTop), new Point(left, baseY));
        dc.DrawLine(axisPen, new Point(left, baseY), new Point(left + plotW, baseY));

        int n = items.Count;
        double slot = plotW / n;
        double barW = Math.Max(6, slot * 0.85);

        for (int i = 0; i < n; i++)
        {
            var it = items[i];
            double cx = left + slot * (i + 0.5);
            double x = cx - barW / 2;
            double bh = plotH * it.Value / yMax;
            var brush = PieChart.SliceBrush(i);

            dc.DrawRectangle(brush, null, new Rect(x, baseY - bh, barW, bh));

            var valueText = Text(it.Value.ToString(CultureInfo.InvariantCulture), boldTypeface, 12,
                bh >= 18 ? PieChart.SliceForeground(i) : textBrush, dip);
            double vy = bh >= 18 ? baseY - bh + (bh - valueText.Height) / 2 : baseY - bh - valueText.Height - 2;
            dc.DrawText(valueText, new Point(cx - valueText.Width / 2, vy));

            var xl = Text(it.Label, typeface, 10, textBrush, dip);
            if (xl.Width <= slot - 4)
            {
                dc.DrawText(xl, new Point(cx - xl.Width / 2, baseY + 6));
            }
            else
            {
                double lh = xl.Height;
                dc.PushTransform(new RotateTransform(90, cx + lh / 2, baseY + 6));
                dc.DrawText(xl, new Point(cx + lh / 2, baseY + 6));
                dc.Pop();
            }
        }

        double lx = left + plotW + 20;
        double ly = plotTop + 6;
        for (int i = 0; i < n; i++)
        {
            DrawLegend(dc, PieChart.SliceBrush(i), items[i].Label, typeface, textBrush, dip, lx, ly);
            ly += 22;
        }
    }

    private static void DrawLegend(DrawingContext dc, Brush brush, string label, Typeface typeface, Brush textBrush, double dip, double x, double y)
    {
        dc.DrawRectangle(brush, null, new Rect(x, y, 14, 14));
        var t = Text(label, typeface, 12, textBrush, dip);
        dc.DrawText(t, new Point(x + 20, y + (14 - t.Height) / 2));
    }

    private static FormattedText Text(string s, Typeface typeface, double size, Brush brush, double dip)
        => new(s, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, size, brush, dip);
}
