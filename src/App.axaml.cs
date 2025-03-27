using Avalonia;
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
            window.DataContext = new MainViewModel(window);

            desktop.MainWindow = window;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel(null)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
