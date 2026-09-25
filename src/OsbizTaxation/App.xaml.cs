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
    private const string ActivateEventName = @"Local\OsbizTaxation.Activate";
    private Mutex? _instanceMutex;
    private EventWaitHandle? _activateEvent;
    private bool _ownsMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        AppIcon.SetAppUserModelId("Didier57.OsbizTaxation");

        _activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivateEventName);

        _instanceMutex = new Mutex(true, InstanceMutexName, out bool createdNew);
        _ownsMutex = createdNew;

        if (!createdNew)
        {
            _activateEvent.Set();
            _activateEvent.Dispose();
            _activateEvent = null;
            _instanceMutex.Dispose();
            _instanceMutex = null;
            Shutdown();
            return;
        }

        StartActivationListener();

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

    private void StartActivationListener()
    {
        var thread = new Thread(() =>
        {
            try
            {
                while (_activateEvent != null && _activateEvent.WaitOne())
                {
                    Dispatcher.Invoke(() => (MainWindow as MainWindow)?.RestoreFromTray());
                }
            }
            catch
            {
            }
        })
        {
            IsBackground = true,
            Name = "OsbizTaxation.Activation",
        };
        thread.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_activateEvent != null)
        {
            _activateEvent.Dispose();
            _activateEvent = null;
        }

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
