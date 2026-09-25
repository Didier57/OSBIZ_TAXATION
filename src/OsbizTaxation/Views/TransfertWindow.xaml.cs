using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using OsbizTaxation.Helpers;
using OsbizTaxation.ViewModels;

namespace OsbizTaxation.Views;

public partial class TransfertWindow : Window
{
    private readonly MainViewModel _main;
    private readonly StringBuilder _journal = new();

    public TransfertWindow(MainViewModel main)
    {
        InitializeComponent();
        Icon = WindowIcons.Create(WindowIcons.Download);
        _main = main;

        foreach (var site in main.Config.Sites)
            Sites.Add(new SiteSelectionItem(site));

        DataContext = this;
    }

    public ObservableCollection<SiteSelectionItem> Sites { get; } = new();

    private void OnToutSelectionnerClick(object sender, RoutedEventArgs e) => SetAll(true);

    private void OnToutDeselectionnerClick(object sender, RoutedEventArgs e) => SetAll(false);

    private void SetAll(bool selected)
    {
        foreach (var item in Sites)
            item.IsSelected = selected;
    }

    private async void OnTransfererClick(object sender, RoutedEventArgs e)
    {
        var selected = Sites.Where(s => s.IsSelected).Select(s => s.Site).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("Selectionnez au moins un site.", "Transfert",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        BtnTransferer.IsEnabled = false;
        var progress = new Progress<string>(message => Log(message));

        try
        {
            var total = await _main.TransferSitesAsync(selected, progress);
            Log($"Transfert termine : {total} appel(s) importe(s).");
        }
        catch (Exception ex)
        {
            Log($"Erreur : {ex.Message}");
        }
        finally
        {
            BtnTransferer.IsEnabled = true;
        }
    }

    private void OnFermerClick(object sender, RoutedEventArgs e) => Close();

    private void Log(string message)
    {
        _journal.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        TxtJournal.Text = _journal.ToString();
    }
}
