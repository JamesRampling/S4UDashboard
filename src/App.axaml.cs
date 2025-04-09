using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using S4UDashboard.ViewModels;
using S4UDashboard.Views;

namespace S4UDashboard;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();

            ServiceProvider.AddService(new AlertService());
            ServiceProvider.AddService(window);
            ServiceProvider.AddService(window as TopLevel);
            ServiceProvider.AddService(window.StorageProvider);

            window.DataContext = new MainViewModel();
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
