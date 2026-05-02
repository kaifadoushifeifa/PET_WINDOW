using System.Windows;

namespace PetWindow;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var splash = new StartupSplashWindow();
        splash.FadeCompleted += (_, _) =>
        {
            var main = new MainWindow();
            main.Show();
            main.Activate();
        };
        splash.Show();
    }
}
