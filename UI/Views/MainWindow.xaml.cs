using System;
using System.ComponentModel;
using System.Windows;
using AmbilightHA.Services;
using AmbilightHA.UI.ViewModels;
using WpfApplication = System.Windows.Application;

namespace AmbilightHA.UI.Views;

public partial class MainWindow : Window
{
    private SystemTrayService? _trayService;
    private bool _isExplicitExit;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            _trayService = new SystemTrayService(
                vm,
                ShowWindow,
                ExitApplication
            );
        }
    }

    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private async void ExitApplication()
    {
        _isExplicitExit = true;
        _trayService?.Dispose();

        if (DataContext is MainViewModel vm && vm.IsRunning)
        {
            vm.StopCommand.Execute(null);
            await System.Threading.Tasks.Task.Delay(300);
        }

        WpfApplication.Current.Shutdown();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isExplicitExit)
        {
            e.Cancel = true;
            Hide();
            _trayService?.ShowNotification(
                "Ambilight Home Assistant",
                "L'application continue de fonctionner en arrière-plan dans la zone de notification."
            );
        }
        else
        {
            _trayService?.Dispose();
            base.OnClosing(e);
        }
    }
}
