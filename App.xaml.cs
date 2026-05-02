using System.Windows;

namespace PetWindow;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var w = new MainWindow();
        w.Show();
    }
}
