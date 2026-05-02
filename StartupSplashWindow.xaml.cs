using System.Windows;
using System.Windows.Threading;

namespace PetWindow;

public partial class StartupSplashWindow : Window
{
    /// <summary>淡出结束后由 App 打开主窗口。</summary>
    public event EventHandler? FadeCompleted;

    public StartupSplashWindow()
    {
        InitializeComponent();
        Loaded += OnLoadedOnce;
    }

    private void OnLoadedOnce(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoadedOnce;

        var hold = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(420) };
        hold.Tick += (_, _) =>
        {
            hold.Stop();
            var anim = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromSeconds(1.55));
            anim.Completed += (_, _) =>
            {
                FadeCompleted?.Invoke(this, EventArgs.Empty);
                Close();
            };
            BeginAnimation(OpacityProperty, anim);
        };
        hold.Start();
    }
}
