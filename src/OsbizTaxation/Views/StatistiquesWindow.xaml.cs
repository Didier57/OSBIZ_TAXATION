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
            CmdSite.Items.Add(site.Nom);
        if (CmdSite.Items.Count > 0)
            CmdSite.SelectedIndex = 0;

        PopulateYears(today.Year);

        for (int m = 1; m <= 12; m++)
            CmdMois.Items.Add(m.ToString("00"));
        CmdMois.SelectedIndex = today.Month - 1;

        _initialise = false;
        Refresh();
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

    private static string FormatDay(string dateIso)
        => DateTime.TryParseExact(dateIso, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
            : dateIso;

    private void OnFermerClick(object sender, RoutedEventArgs e) => Close();
}
