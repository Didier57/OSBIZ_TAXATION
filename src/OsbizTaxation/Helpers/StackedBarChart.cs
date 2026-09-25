using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace OsbizTaxation.Helpers;

/// <summary>Une barre de l'histogramme : un libelle et les valeurs empilees Entrant/Sortant.</summary>
public sealed record StackedBarItem(string Label, int Entrant, int Sortant);

/// <summary>Histogramme empile (Entrant / Sortant) dessine nativement en WPF, sans dependance externe.</summary>
public sealed class StackedBarChart : FrameworkElement
{
    private static readonly Color EntrantColor = Color.FromRgb(0xF2, 0x9C, 0xA0);
    private static readonly Color SortantColor = Color.FromRgb(0x8F, 0xDF, 0x8F);

    private IReadOnlyList<StackedBarItem> _items = Array.Empty<StackedBarItem>();

    public IReadOnlyList<StackedBarItem> Items
    {
        get => _items;
        set
        {
            _items = value ?? Array.Empty<StackedBarItem>();
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
        var textBrush = new SolidColorBrush(dark ? Color.FromRgb(0xDD, 0xDD, 0xDD) : Color.FromRgb(0x33, 0x33, 0x33));
        var gridPen = new Pen(new SolidColorBrush(dark ? Color.FromRgb(0x3A, 0x3A, 0x3D) : Color.FromRgb(0xDC, 0xDC, 0xDC)), 1);
        var axisPen = new Pen(textBrush, 1);
        var entrantBrush = new SolidColorBrush(EntrantColor);
        var sortantBrush = new SolidColorBrush(SortantColor);
        var typeface = new Typeface("Segoe UI");
        var boldTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
        double dip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        const double left = 48, right = 110, top = 18, bottom = 76;
        double plotW = w - left - right;
        double plotH = h - top - bottom;
        if (plotW <= 20 || plotH <= 20)
            return;

        var items = _items;
        double max = 0;
        foreach (var it in items)
            max = Math.Max(max, it.Entrant + it.Sortant);

        if (items.Count == 0 || max <= 0)
        {
            var empty = Text("Aucune donnée", typeface, 13, textBrush, dip);
            dc.DrawText(empty, new Point((w - empty.Width) / 2, (h - empty.Height) / 2));
            return;
        }

        int yMax = (int)Math.Ceiling(max);
        int step = 1;
        while (yMax / step > 10)
            step++;
        yMax = (int)(Math.Ceiling(max / step) * step);

        double baseY = top + plotH;

        for (int v = 0; v <= yMax; v += step)
        {
            double y = baseY - plotH * ((double)v / yMax);
            dc.DrawLine(gridPen, new Point(left, y), new Point(left + plotW, y));
            var tick = Text(v.ToString(CultureInfo.InvariantCulture), typeface, 11, textBrush, dip);
            dc.DrawText(tick, new Point(left - 8 - tick.Width, y - tick.Height / 2));
        }

        dc.DrawLine(axisPen, new Point(left, top), new Point(left, baseY));
        dc.DrawLine(axisPen, new Point(left, baseY), new Point(left + plotW, baseY));

        int n = items.Count;
        double slot = plotW / n;
        double barW = Math.Max(6, Math.Min(46, slot * 0.6));

        for (int i = 0; i < n; i++)
        {
            var it = items[i];
            double cx = left + slot * (i + 0.5);
            double x = cx - barW / 2;

            double hE = plotH * it.Entrant / yMax;
            double hS = plotH * it.Sortant / yMax;

            if (it.Entrant > 0)
            {
                dc.DrawRectangle(entrantBrush, null, new Rect(x, baseY - hE, barW, hE));
                if (hE >= 14)
                {
                    var t = Text(it.Entrant.ToString(CultureInfo.InvariantCulture), typeface, 10, Brushes.Black, dip);
                    dc.DrawText(t, new Point(cx - t.Width / 2, baseY - hE + (hE - t.Height) / 2));
                }
            }

            if (it.Sortant > 0)
            {
                dc.DrawRectangle(sortantBrush, null, new Rect(x, baseY - hE - hS, barW, hS));
                if (hS >= 14)
                {
                    var t = Text(it.Sortant.ToString(CultureInfo.InvariantCulture), typeface, 10, Brushes.Black, dip);
                    dc.DrawText(t, new Point(cx - t.Width / 2, baseY - hE - hS + (hS - t.Height) / 2));
                }
            }

            var total = Text((it.Entrant + it.Sortant).ToString(CultureInfo.InvariantCulture), boldTypeface, 11, textBrush, dip);
            dc.DrawText(total, new Point(cx - total.Width / 2, baseY - hE - hS - total.Height - 2));

            var xl = Text(it.Label, typeface, 10, textBrush, dip);
            double lh = xl.Height;
            dc.PushTransform(new RotateTransform(90, cx + lh / 2, baseY + 6));
            dc.DrawText(xl, new Point(cx + lh / 2, baseY + 6));
            dc.Pop();
        }

        double lx = left + plotW + 16;
        double ly = top + 10;
        DrawLegend(dc, entrantBrush, "Entrant", typeface, textBrush, dip, lx, ly);
        DrawLegend(dc, sortantBrush, "Sortant", typeface, textBrush, dip, lx, ly + 24);
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
