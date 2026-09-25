using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace OsbizTaxation.Helpers;

/// <summary>Une part du camembert : libelle (numero interne), valeur et detail affiche sous le libelle.</summary>
public sealed record PieSlice(string Label, double Value, string Detail);

/// <summary>Camembert dessine nativement en WPF, sans dependance externe.</summary>
public sealed class PieChart : FrameworkElement
{
    private static readonly Color[] Palette =
    {
        Color.FromRgb(0xE8, 0x56, 0x4F),
        Color.FromRgb(0x4C, 0xAF, 0x50),
        Color.FromRgb(0xF5, 0xA6, 0x23),
        Color.FromRgb(0x26, 0xA6, 0x9A),
        Color.FromRgb(0x5C, 0x6B, 0xC0),
        Color.FromRgb(0xAB, 0x47, 0xBC),
        Color.FromRgb(0x8D, 0x6E, 0x63),
        Color.FromRgb(0xEC, 0x40, 0x7A),
        Color.FromRgb(0x29, 0xB6, 0xF6),
        Color.FromRgb(0x9C, 0xCC, 0x65),
        Color.FromRgb(0xFF, 0x70, 0x43),
        Color.FromRgb(0x78, 0x90, 0x9C),
    };

    private IReadOnlyList<PieSlice> _items = Array.Empty<PieSlice>();
    private string _title = "";

    public IReadOnlyList<PieSlice> Items
    {
        get => _items;
        set
        {
            _items = value ?? Array.Empty<PieSlice>();
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
        if (w <= 40 || h <= 40)
            return;

        var dark = ThemeManager.IsDark;
        var textBrush = new SolidColorBrush(dark ? Color.FromRgb(0xDD, 0xDD, 0xDD) : Color.FromRgb(0x33, 0x33, 0x33));
        var whitePen = new Pen(Brushes.White, 1.5);
        var typeface = new Typeface("Segoe UI");
        var boldTypeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
        double dip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        const double titleH = 30;
        if (!string.IsNullOrEmpty(_title))
        {
            var titleText = Text(_title, boldTypeface, 15, textBrush, dip);
            dc.DrawText(titleText, new Point((w - titleText.Width) / 2, 4));
        }

        double total = 0;
        foreach (var it in _items)
        {
            if (it.Value > 0)
                total += it.Value;
        }

        if (_items.Count == 0 || total <= 0)
        {
            var empty = Text("Aucune donnée", typeface, 13, textBrush, dip);
            dc.DrawText(empty, new Point((w - empty.Width) / 2, titleH + (h - titleH - empty.Height) / 2));
            return;
        }

        double availH = h - titleH - 16;
        double diameter = Math.Min(w - 60, availH);
        if (diameter < 60)
            return;

        double r = diameter / 2;
        double cx = w / 2;
        double cy = titleH + (h - titleH) / 2;

        double startAngle = -90;
        for (int i = 0; i < _items.Count; i++)
        {
            var it = _items[i];
            if (it.Value <= 0)
                continue;

            double sweep = 360.0 * it.Value / total;
            double endAngle = startAngle + sweep;
            double percent = 100.0 * it.Value / total;

            var brush = new SolidColorBrush(Palette[i % Palette.Length]);
            brush.Freeze();

            if (sweep >= 359.9)
            {
                dc.DrawEllipse(brush, whitePen, new Point(cx, cy), r, r);
                DrawSliceLabel(dc, cx, cy, r, startAngle, endAngle, it, percent, brush, typeface, boldTypeface, dip);
                startAngle = endAngle;
                continue;
            }

            var geo = new StreamGeometry();
            using (var ctx = geo.Open())
            {
                ctx.BeginFigure(new Point(cx, cy), true, true);
                ctx.LineTo(PointOnCircle(cx, cy, r, startAngle), true, false);
                bool large = sweep > 180;
                ctx.ArcTo(PointOnCircle(cx, cy, r, endAngle), new Size(r, r), 0, large, SweepDirection.Clockwise, true, false);
            }

            geo.Freeze();
            dc.DrawGeometry(brush, whitePen, geo);

            DrawSliceLabel(dc, cx, cy, r, startAngle, endAngle, it, percent, brush, typeface, boldTypeface, dip);

            startAngle = endAngle;
        }
    }

    private static void DrawSliceLabel(DrawingContext dc, double cx, double cy, double r,
        double startAngle, double endAngle, PieSlice it, double percent, Brush sliceBrush,
        Typeface typeface, Typeface boldTypeface, double dip)
    {
        if (percent < 0.9)
            return;

        double mid = (startAngle + endAngle) / 2;
        var center = PointOnCircle(cx, cy, r * 0.62, mid);

        var fg = Luminance(sliceBrush) > 200 ? Brushes.Black : Brushes.White;

        var lines = new List<FormattedText>(3);
        if (percent >= 6)
        {
            lines.Add(Text(it.Label, boldTypeface, 12, fg, dip));
            lines.Add(Text(it.Detail, typeface, 10, fg, dip));
        }
        else if (percent >= 2)
        {
            lines.Add(Text(it.Label, boldTypeface, 11, fg, dip));
        }

        lines.Add(Text(percent.ToString("0", CultureInfo.CurrentCulture) + " %", typeface, 10, fg, dip));

        double totalH = 0;
        foreach (var l in lines)
            totalH += l.Height;

        double y = center.Y - totalH / 2;
        foreach (var l in lines)
        {
            dc.DrawText(l, new Point(center.X - l.Width / 2, y));
            y += l.Height;
        }
    }

    private static double Luminance(Brush brush)
    {
        if (brush is SolidColorBrush scb)
        {
            var c = scb.Color;
            return 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
        }

        return 255;
    }

    private static Point PointOnCircle(double cx, double cy, double r, double angleDegrees)
    {
        double rad = angleDegrees * Math.PI / 180.0;
        return new Point(cx + r * Math.Cos(rad), cy + r * Math.Sin(rad));
    }

    private static FormattedText Text(string s, Typeface typeface, double size, Brush brush, double dip)
        => new(s, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, size, brush, dip);
}
