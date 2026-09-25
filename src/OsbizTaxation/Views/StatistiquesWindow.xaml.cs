using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using OsbizTaxation.Helpers;
using OsbizTaxation.Models;
using OsbizTaxation.Services;
using OsbizTaxation.ViewModels;

namespace OsbizTaxation.Views;

public partial class StatistiquesWindow : Window
{
    /// <summary>Ligne de la table TOP : texte affiche + couleur reprise du camembert.</summary>
    public sealed class TopRow
    {
        public string NumeroInterne { get; init; } = "";
        public string DureeAffichee { get; init; } = "";
        public Brush? Couleur { get; init; }
        public Brush? Texte { get; init; }
    }

    private readonly MainViewModel _main;
    private bool _initialise = true;

    public StatistiquesWindow(MainViewModel main)
    {
        _main = main;
        InitializeComponent();
        Icon = WindowIcons.Create(WindowIcons.Chart);
        SourceInitialized += (_, _) => TitleBarTheme.Apply(this);

        var today = DateTime.Today;

        CmdTcdSite.Items.Add("(tous les sites)");
        foreach (var site in main.Config.Sites)
        {
            CmdSite.Items.Add(site.Nom);
            CmdSiteHeure.Items.Add(site.Nom);
            CmdTopSite.Items.Add(site.Nom);
            CmdPaysSite.Items.Add(site.Nom);
            CmdTcdSite.Items.Add(site.Nom);
        }

        if (CmdSite.Items.Count > 0)
            CmdSite.SelectedIndex = 0;
        if (CmdSiteHeure.Items.Count > 0)
            CmdSiteHeure.SelectedIndex = 0;
        if (CmdTopSite.Items.Count > 0)
            CmdTopSite.SelectedIndex = 0;
        if (CmdPaysSite.Items.Count > 0)
            CmdPaysSite.SelectedIndex = 0;
        if (CmdTcdSite.Items.Count > 0)
            CmdTcdSite.SelectedIndex = 0;

        PopulateYears(today.Year);

        for (int m = 1; m <= 12; m++)
            CmdMois.Items.Add(m.ToString("00"));
        CmdMois.SelectedIndex = today.Month - 1;

        DpHeureDate.SelectedDate = today;

        CmdTopPeriode.SelectedIndex = 1;
        CmdTopType.SelectedIndex = 0;
        for (int i = 5; i <= 10; i++)
            CmdTopNombre.Items.Add(i);
        CmdTopNombre.SelectedIndex = 0;
        DpTopDate.SelectedDate = today;

        CmdPaysPeriode.SelectedIndex = 1;
        CmdPaysType.SelectedIndex = 0;
        DpPaysDate.SelectedDate = today;

        PopulateTcdYears(today.Year);

        _initialise = false;
        Refresh();
        RefreshHeure();
        RefreshTop();
        RefreshPays();
        RefreshTcd();
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

    private void PopulateTcdYears(int defaultYear)
    {
        var years = new List<int>();
        foreach (var y in _main.Repository.GetYears(null))
            if (int.TryParse(y, out var v) && !years.Contains(v))
                years.Add(v);

        if (!years.Contains(defaultYear))
            years.Add(defaultYear);
        years.Sort();

        CmdTcdAnnee.Items.Clear();
        foreach (var y in years)
            CmdTcdAnnee.Items.Add(y.ToString(CultureInfo.InvariantCulture));

        var wanted = defaultYear.ToString(CultureInfo.InvariantCulture);
        CmdTcdAnnee.SelectedItem = wanted;
        if (CmdTcdAnnee.SelectedIndex < 0 && CmdTcdAnnee.Items.Count > 0)
            CmdTcdAnnee.SelectedIndex = CmdTcdAnnee.Items.Count - 1;
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

    private void OnHeureAujourdhuiClick(object sender, RoutedEventArgs e)
    {
        DpHeureDate.SelectedDate = DateTime.Today;
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

    private string TopPeriode
        => (CmdTopPeriode.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Mois";

    private void OnTopFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initialise)
            return;
        RefreshTop();
    }

    private void OnTopDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initialise)
            return;
        RefreshTop();
    }

    private void OnTopPrecClick(object sender, RoutedEventArgs e) => ShiftTop(-1);

    private void OnTopSuivClick(object sender, RoutedEventArgs e) => ShiftTop(1);

    private void OnTopAujourdhuiClick(object sender, RoutedEventArgs e)
    {
        DpTopDate.SelectedDate = DateTime.Today;
        RefreshTop();
    }

    private void ShiftTop(int sens)
    {
        var date = DpTopDate.SelectedDate ?? DateTime.Today;
        date = TopPeriode switch
        {
            "Mois" => date.AddMonths(sens),
            "Année" => date.AddYears(sens),
            _ => date.AddDays(sens)
        };
        DpTopDate.SelectedDate = date;
        RefreshTop();
    }

    private static string Iso(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private (string Debut, string Fin) GetTopPlage()
    {
        var date = DpTopDate.SelectedDate ?? DateTime.Today;
        switch (TopPeriode)
        {
            case "Mois":
                var premier = new DateTime(date.Year, date.Month, 1);
                return (Iso(premier), Iso(premier.AddMonths(1).AddDays(-1)));
            case "Année":
                return (Iso(new DateTime(date.Year, 1, 1)), Iso(new DateTime(date.Year, 12, 31)));
            default:
                return (Iso(date), Iso(date));
        }
    }

    private void RefreshTop()
    {
        if (CmdTopSite.SelectedItem is not string site || string.IsNullOrWhiteSpace(site))
        {
            TopChart.Items = Array.Empty<PieSlice>();
            GridTop.ItemsSource = null;
            return;
        }

        var (debut, fin) = GetTopPlage();
        bool? entrant = (CmdTopType.SelectedItem as ComboBoxItem)?.Content?.ToString() switch
        {
            "Sortant" => false,
            "Entrant/Sortant" => null,
            _ => true,
        };
        int top = CmdTopNombre.SelectedItem is int n ? n : 5;

        var rows = _main.Repository.GetTopDurations(site, debut, fin, entrant, top);

        var slices = new List<PieSlice>(rows.Count);
        var table = new List<TopRow>(rows.Count);
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            slices.Add(new PieSlice(r.NumeroInterne, r.Secondes, DurationFormat.Compact(r.Secondes)));
            table.Add(new TopRow
            {
                NumeroInterne = r.NumeroInterne,
                DureeAffichee = r.DureeAffichee,
                Couleur = PieChart.SliceBrush(i),
                Texte = PieChart.SliceForeground(i),
            });
        }

        GridTop.ItemsSource = table;
        TopChart.Items = slices;
        TopChart.Title = rows.Count > 0 ? $"TOP {rows.Count} Durée" : "TOP Durée";
    }

    private string PaysPeriode
        => (CmdPaysPeriode.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Mois";

    private void OnPaysFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initialise)
            return;
        RefreshPays();
    }

    private void OnPaysDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initialise)
            return;
        RefreshPays();
    }

    private void OnPaysPrecClick(object sender, RoutedEventArgs e) => ShiftPays(-1);

    private void OnPaysSuivClick(object sender, RoutedEventArgs e) => ShiftPays(1);

    private void OnPaysAujourdhuiClick(object sender, RoutedEventArgs e)
    {
        DpPaysDate.SelectedDate = DateTime.Today;
        RefreshPays();
    }

    private void ShiftPays(int sens)
    {
        var date = DpPaysDate.SelectedDate ?? DateTime.Today;
        date = PaysPeriode switch
        {
            "Mois" => date.AddMonths(sens),
            "Année" => date.AddYears(sens),
            _ => date.AddDays(sens)
        };
        DpPaysDate.SelectedDate = date;
        RefreshPays();
    }

    private (string Debut, string Fin) GetPaysPlage()
    {
        var date = DpPaysDate.SelectedDate ?? DateTime.Today;
        switch (PaysPeriode)
        {
            case "Mois":
                var premier = new DateTime(date.Year, date.Month, 1);
                return (Iso(premier), Iso(premier.AddMonths(1).AddDays(-1)));
            case "Année":
                return (Iso(new DateTime(date.Year, 1, 1)), Iso(new DateTime(date.Year, 12, 31)));
            default:
                return (Iso(date), Iso(date));
        }
    }

    private void RefreshPays()
    {
        if (CmdPaysSite.SelectedItem is not string site || string.IsNullOrWhiteSpace(site))
        {
            ChartPays.Items = Array.Empty<BarItem>();
            return;
        }

        var (debut, fin) = GetPaysPlage();
        bool? entrant = (CmdPaysType.SelectedItem as ComboBoxItem)?.Content?.ToString() switch
        {
            "Sortant" => false,
            "Entrant/Sortant" => null,
            _ => true,
        };

        var rows = _main.Repository.CountByCountry(site, debut, fin, entrant);

        var items = new List<BarItem>(rows.Count);
        foreach (var r in rows)
            if (!string.IsNullOrWhiteSpace(r.Pays))
                items.Add(new BarItem(r.Pays, r.Total));

        ChartPays.Items = items;
        ChartPays.Title = "Appel par pays";
    }

    private void OnTcdFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initialise)
            return;
        RefreshTcd();
    }

    private void RefreshTcd()
    {
        if (CmdTcdAnnee.SelectedItem is not string annee || !int.TryParse(annee, out var year))
        {
            Tcd.SetData(Array.Empty<PivotCall>(), "");
            return;
        }

        var begin = new DateTime(year, 1, 1);
        var end = new DateTime(year, 12, 31);
        string? site = CmdTcdSite.SelectedIndex > 0 ? CmdTcdSite.SelectedItem as string : null;

        var data = _main.Repository.GetPivotCalls(site, Iso(begin), Iso(end));
        Tcd.SetData(data, annee);
    }

    private void OnTcdExportClick(object sender, RoutedEventArgs e)
    {
        var grid = Tcd.BuildExport();
        if (grid is null || grid.Rows.Count == 0)
        {
            MessageBox.Show("Aucune donnée à exporter.", "TCD", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var annee = CmdTcdAnnee.SelectedItem as string ?? "";
        var dialog = new SaveFileDialog
        {
            Filter = "Classeur Excel (*.xlsx)|*.xlsx",
            FileName = $"TCD_{annee}.xlsx",
            InitialDirectory = AppPaths.AppDirectory
        };
        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            ExcelExporter.Export(dialog.FileName, grid, "TCD");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Export impossible : " + ex.Message, "TCD", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show($"Tableau exporté vers :\n{dialog.FileName}\n\nVoulez-vous ouvrir le fichier ?",
                "TCD", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            OuvrirFichier(dialog.FileName);
    }

    private static void OuvrirFichier(string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show("Impossible d'ouvrir le fichier : " + ex.Message, "TCD", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnFermerClick(object sender, RoutedEventArgs e) => Close();
}
