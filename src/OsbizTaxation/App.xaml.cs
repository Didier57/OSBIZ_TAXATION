using System;
using System.Linq;
using System.Threading;
using System.Windows;
using OsbizTaxation.Helpers;
using OsbizTaxation.Services;

namespace OsbizTaxation;

public partial class App : Application
{
    private const string InstanceMutexName = @"Local\OsbizTaxation.SingleInstance";
    private Mutex? _instanceMutex;
    private bool _ownsMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _instanceMutex = new Mutex(true, InstanceMutexName, out bool createdNew);
        _ownsMutex = createdNew;

        if (!createdNew)
        {
            MessageBox.Show(
                "Taxation OSBIZ est déjà ouvert. Un seul lancement est autorisé.",
                "Taxation OSBIZ",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            _instanceMutex.Dispose();
            _instanceMutex = null;
            Shutdown();
            return;
        }

        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var config = ConfigService.Load();
        ThemeManager.Apply(config.Theme);

        StartupManager.Sync(config.StartWithWindows);

        var main = new MainWindow();
        MainWindow = main;

        bool launchMinimized = config.StartMinimized
            || e.Args.Any(a => string.Equals(a, "--minimized", StringComparison.OrdinalIgnoreCase));

        if (launchMinimized)
            main.StartHiddenToTray();
        else
            main.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_instanceMutex != null)
        {
            if (_ownsMutex)
            {
                _instanceMutex.ReleaseMutex();
            }

            _instanceMutex.Dispose();
            _instanceMutex = null;
        }

        base.OnExit(e);
    }
}
