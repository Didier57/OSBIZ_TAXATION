using System.Windows;
using OsbizTaxation.Helpers;
using OsbizTaxation.Services;

namespace OsbizTaxation;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        ThemeManager.Apply(ConfigService.Load().Theme);
        base.OnStartup(e);
    }
}
