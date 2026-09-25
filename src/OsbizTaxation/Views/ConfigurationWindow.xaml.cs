using System.Windows;
using OsbizTaxation.Models;
using OsbizTaxation.Services;
using OsbizTaxation.ViewModels;

namespace OsbizTaxation.Views;

public partial class ConfigurationWindow : Window
{
    private readonly MainViewModel _main;
    private readonly ConfigViewModel _viewModel;

    public ConfigurationWindow(MainViewModel main)
    {
        InitializeComponent();
        _main = main;
        _viewModel = new ConfigViewModel(main.Config);
        DataContext = _viewModel;

        ColSitePays.ItemsSource = Countries.All;
    }

    private void OnAjouterSiteClick(object sender, RoutedEventArgs e)
    {
        var site = new SiteConfig { Nom = "NOUVEAU" };
        _viewModel.Sites.Add(site);
        GridSites.SelectedItem = site;
        GridSites.ScrollIntoView(site);
    }

    private void OnSupprimerSiteClick(object sender, RoutedEventArgs e)
    {
        if (GridSites.SelectedItem is SiteConfig site)
            _viewModel.Sites.Remove(site);
        else
            Warn("Selectionnez un site a supprimer.");
    }

    private void OnAjouterLigneClick(object sender, RoutedEventArgs e)
    {
        var ligne = new LineConfig { NumDebut = 1, NumFin = 1, Site = _viewModel.Sites.FirstOrDefault()?.Nom ?? "" };
        _viewModel.Lignes.Add(ligne);
        GridLignes.SelectedItem = ligne;
        GridLignes.ScrollIntoView(ligne);
    }

    private void OnSupprimerLigneClick(object sender, RoutedEventArgs e)
    {
        if (GridLignes.SelectedItem is LineConfig ligne)
            _viewModel.Lignes.Remove(ligne);
        else
            Warn("Selectionnez une ligne a supprimer.");
    }

    private void OnReattribuerClick(object sender, RoutedEventArgs e)
    {
        var answer = MessageBox.Show(
            "Reattribuer les noms de lignes a tous les appels deja enregistres, d'apres la configuration actuelle ?",
            "Reattribution", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes)
            return;

        var updated = _main.Repository.ReassignLineNames(_viewModel.Lignes.ToList());
        _main.ReloadRecords();
        MessageBox.Show($"{updated} appel(s) mis a jour.", "Reattribution",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnEnregistrerClick(object sender, RoutedEventArgs e)
    {
        var config = _viewModel.ToConfig();

        if (config.AutoTransferIntervalMinutes < 1)
        {
            Warn("L'intervalle de transfert doit etre un nombre entier de minutes superieur ou egal a 1.");
            return;
        }

        if (!ConfigService.Save(config))
        {
            Warn("Erreur d'enregistrement : " + ConfigService.LastError);
            return;
        }

        _main.ApplyConfig(config);
        MessageBox.Show("Configuration enregistree.", "Configuration",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnFermerClick(object sender, RoutedEventArgs e) => Close();

    private static void Warn(string message)
        => MessageBox.Show(message, "Configuration", MessageBoxButton.OK, MessageBoxImage.Warning);
}
