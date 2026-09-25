using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using OsbizTaxation.Helpers;
using OsbizTaxation.ViewModels;

namespace OsbizTaxation.Views;

public partial class StatistiquesWindow : Window
{
    private readonly MainViewModel _main;
    private bool _initialise = true;

    public StatistiquesWindow(MainViewModel main)
    {
        _main = main;
        InitializeComponent();
        Icon = WindowIcons.Create(WindowIcons.Chart);
        SourceInitialized += (_, _) => TitleBarTheme.Apply(this);

        var today = DateTime.Today;

        foreach (var site in main.Config.Sites)
        {
            CmdSite.Items.Add(site.Nom);
            CmdSiteHeure.Items.Add(site.Nom);
        }

        if (CmdSite.Items.Count > 0)
            CmdSite.SelectedIndex = 0;
        if (CmdSiteHeure.Items.Count > 0)
            CmdSiteHeure.SelectedIndex = 0;

        PopulateYears(today.Year);

        for (int m = 1; m <= 12; m++)
            CmdMois.Items.Add(m.ToString("00"));
        CmdMois.SelectedIndex = today.Month - 1;

        DpHeureDate.SelectedDate = today;

        _initialise = false;
        Refresh();
        RefreshHeure();
    }

    private void PopulateYears(int defaultYear)
    {
        var years = new List<int>();
        if (CmdSite.SelectedItem is string site && !string.IsNullOrWhiteSpace(site))
        {
            foreach (var y in _main.Repository.GetYears(site))
                if (int.TryParse(y, out var v) && !years.Contains(v))
                    years.Add(v);
        }

        if (!years.Contains(defaultYear))
            years.Add(defaultYear);
        years.Sort();

        CmdAnnee.Items.Clear();
        foreach (var y in years)
            CmdAnnee.Items.Add(y.ToString(CultureInfo.InvariantCulture));

        var wanted = defaultYear.ToString(CultureInfo.InvariantCulture);
        CmdAnnee.SelectedItem = wanted;
        if (CmdAnnee.SelectedIndex < 0 && CmdAnnee.Items.Count > 0)
            CmdAnnee.SelectedIndex = CmdAnnee.Items.Count - 1;
    }

    private void OnFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initialise)
            return;

        if (ReferenceEquals(sender, CmdSite))
        {
            var current = CmdAnnee.SelectedItem as string;
            _initialise = true;
            PopulateYears(int.TryParse(current, out var y) ? y : DateTime.Today.Year);
            _initialise = false;
        }

        Refresh();
    }

    private void Refresh()
    {
        if (CmdSite.SelectedItem is not string site || string.IsNullOrWhiteSpace(site)
            || CmdAnnee.SelectedItem is not string annee || !int.TryParse(annee, out var year)
            || CmdMois.SelectedItem is not string mois || !int.TryParse(mois, out var month))
        {
            Chart.Items = Array.Empty<StackedBarItem>();
            TxtEntrant.Text = TxtSortant.Text = TxtTotal.Text = "0";
            return;
        }

        var debut = new DateTime(year, month, 1);
        var fin = debut.AddMonths(1).AddDays(-1);
        var fromIso = debut.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var toIso = fin.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var counts = _main.Repository.CountByDay(site, fromIso, toIso);

        var items = new List<StackedBarItem>(counts.Count);
        int totalEntrant = 0, totalSortant = 0;
        foreach (var c in counts)
        {
            items.Add(new StackedBarItem(FormatDay(c.DateIso), c.Entrant, c.Sortant));
            totalEntrant += c.Entrant;
            totalSortant += c.Sortant;
        }

        Chart.Items = items;
        TxtEntrant.Text = totalEntrant.ToString(CultureInfo.InvariantCulture);
        TxtSortant.Text = totalSortant.ToString(CultureInfo.InvariantCulture);
        TxtTotal.Text = (totalEntrant + totalSortant).ToString(CultureInfo.InvariantCulture);
    }

    private void OnHeureFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initialise)
            return;
        RefreshHeure();
    }

    private void OnHeureDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initialise)
            return;
        RefreshHeure();
    }

    private void OnHeurePrecClick(object sender, RoutedEventArgs e)
    {
        DpHeureDate.SelectedDate = (DpHeureDate.SelectedDate ?? DateTime.Today).AddDays(-1);
        RefreshHeure();
    }

    private void OnHeureSuivClick(object sender, RoutedEventArgs e)
    {
        DpHeureDate.SelectedDate = (DpHeureDate.SelectedDate ?? DateTime.Today).AddDays(1);
        RefreshHeure();
    }

    private void RefreshHeure()
    {
        if (CmdSiteHeure.SelectedItem is not string site || string.IsNullOrWhiteSpace(site))
        {
            ChartHeure.Items = Array.Empty<StackedBarItem>();
            TxtHeureEntrant.Text = TxtHeureSortant.Text = TxtHeureTotal.Text = "0";
            return;
        }

        var date = DpHeureDate.SelectedDate ?? DateTime.Today;
        var dateIso = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var counts = _main.Repository.CountByHour(site, dateIso);
        var entrant = new int[24];
        var sortant = new int[24];
        foreach (var c in counts)
        {
            if (c.Hour < 0 || c.Hour > 23)
                continue;
            entrant[c.Hour] = c.Entrant;
            sortant[c.Hour] = c.Sortant;
        }

        var items = new List<StackedBarItem>(24);
        int totalEntrant = 0, totalSortant = 0;
        for (int h = 0; h < 24; h++)
        {
            items.Add(new StackedBarItem(h.ToString("00") + "h", entrant[h], sortant[h]));
            totalEntrant += entrant[h];
            totalSortant += sortant[h];
        }

        ChartHeure.Items = items;
        TxtHeureEntrant.Text = totalEntrant.ToString(CultureInfo.InvariantCulture);
        TxtHeureSortant.Text = totalSortant.ToString(CultureInfo.InvariantCulture);
        TxtHeureTotal.Text = (totalEntrant + totalSortant).ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatDay(string dateIso)
        => DateTime.TryParseExact(dateIso, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
            : dateIso;

    private void OnFermerClick(object sender, RoutedEventArgs e) => Close();
}
