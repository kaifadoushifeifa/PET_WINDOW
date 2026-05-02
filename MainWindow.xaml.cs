using System.Diagnostics;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using PetWindow.Models;
using PetWindow.Services;

namespace PetWindow;

public partial class MainWindow : Window
{
    private const double SnapPx = 28;
    private const double PeekPx = 40;
    private readonly AppSettings _settings = SettingsStore.Load();
    private readonly SkinLoader _skinLoader;
    private readonly DispatcherTimer _animTimer;
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _netTimer;
    private readonly WeatherClient _weather = new();
    private readonly NetworkSpeedMeter _netMeter = new();
    private Forms.NotifyIcon? _notifyIcon;
    private IReadOnlyList<BitmapImage> _frames = Array.Empty<BitmapImage>();
    private int _frameIndex;
    private System.Windows.Point _dragScreenStart;
    private double _winLeftStart;
    private double _winTopStart;
    private bool _hasDragged;
    private bool _edgeHiddenActive;
    private int _lastChimeHour = -1;
    private DispatcherTimer? _edgeHideDelayTimer;
    private bool _shuttingDown;

    public MainWindow()
    {
        InitializeComponent();
        var skinsRoot = Path.Combine(AppContext.BaseDirectory, "Skins");
        _skinLoader = new SkinLoader(skinsRoot);

        _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
        _animTimer.Tick += (_, _) => AdvanceFrame();

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => OnClockTick();

        _netTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _netTimer.Tick += (_, _) => RefreshNetworkLabel();

        Loaded += OnLoaded;
        Closing += OnWindowClosing;
        SourceInitialized += (_, _) =>
        {
            var helper = new WindowInteropHelper(this);
            helper.EnsureHandle();
        };
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PetHitArea.MouseLeftButtonDown += PetHitAreaOnMouseLeftButtonDown;
        PetHitArea.MouseMove += PetHitAreaOnMouseMove;
        PetHitArea.MouseLeftButtonUp += PetHitAreaOnMouseLeftButtonUp;
        PetHitArea.MouseEnter += (_, _) => ExpandEdgeHidden();
        PetHitArea.MouseLeave += (_, _) => ScheduleEdgeHide();

        MouseRightButtonUp += WindowOnMouseRightButtonUp;

        ApplySettingsFromStore();
        ReloadSkinFrames();
        EnsureTrayIcon();

        _clockTimer.Start();
        if (_settings.ShowSidePanel)
            _netTimer.Start();
    }

    private void PetHitAreaOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragScreenStart = PointToScreen(e.GetPosition(this));
        _winLeftStart = Left;
        _winTopStart = Top;
        _hasDragged = false;
        PetHitArea.CaptureMouse();
        e.Handled = true;
    }

    private void PetHitAreaOnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!PetHitArea.IsMouseCaptured)
            return;
        var cur = PointToScreen(e.GetPosition(this));
        var dx = cur.X - _dragScreenStart.X;
        var dy = cur.Y - _dragScreenStart.Y;
        if (Math.Abs(dx) > 4 || Math.Abs(dy) > 4)
            _hasDragged = true;
        Left = _winLeftStart + dx;
        Top = _winTopStart + dy;
    }

    private void PetHitAreaOnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (PetHitArea.IsMouseCaptured)
            PetHitArea.ReleaseMouseCapture();

        if (!_hasDragged)
            OnPetTap();
        else
            SnapAndMaybeEdgeHide();

        e.Handled = true;
    }

    private void OnPetTap()
    {
        TryPlayClickSound();
        if (_settings.ShowBubbleTips)
            ShowBubble(BubbleQuotes.Next());
    }

    private void TryPlayClickSound()
    {
        try
        {
            var soundPath = _settings.ClickSoundPath;
            if (!string.IsNullOrWhiteSpace(soundPath) && File.Exists(soundPath))
            {
                using var p = new SoundPlayer(soundPath);
                p.Play();
            }
            else
            {
                SystemSounds.Asterisk.Play();
            }
        }
        catch
        {
            SystemSounds.Beep.Play();
        }
    }

    private void ShowBubble(string text)
    {
        BubbleText.Text = text;
        BubblePopup.IsOpen = false;
        BubblePopup.IsOpen = true;
    }

    private void WindowOnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!IsVisible)
            return;
        var menu = BuildMainContextMenu();
        menu.PlacementTarget = this;
        menu.IsOpen = true;
        e.Handled = true;
    }

    private ContextMenu BuildMainContextMenu()
    {
        var menu = new ContextMenu();

        var exitItem = new MenuItem { Header = "退出程序" };
        exitItem.Click += (_, _) => ExitApplication();
        menu.Items.Add(exitItem);

        var startupItem = new MenuItem
        {
            Header = "开机自启",
            IsCheckable = true,
            IsChecked = _settings.RunAtStartup
        };
        startupItem.Click += (_, _) =>
        {
            _settings.RunAtStartup = !_settings.RunAtStartup;
            startupItem.IsChecked = _settings.RunAtStartup;
            var exe = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "";
            if (!string.IsNullOrEmpty(exe))
                StartupHelper.SetEnabled(_settings.RunAtStartup, exe);
            Persist();
        };
        menu.Items.Add(startupItem);

        menu.Items.Add(new Separator());

        menu.Items.Add(BuildOpacitySubmenu());
        menu.Items.Add(BuildScaleSubmenu());

        var hideItem = new MenuItem { Header = _settings.PetVisible ? "隐藏宠物" : "显示宠物" };
        hideItem.Click += (_, _) =>
        {
            TogglePetVisible();
            hideItem.Header = _settings.PetVisible ? "隐藏宠物" : "显示宠物";
        };
        menu.Items.Add(hideItem);

        menu.Items.Add(new Separator());

        var edgeItem = new MenuItem
        {
            Header = "边缘自动隐藏",
            IsCheckable = true,
            IsChecked = _settings.EdgeAutoHide
        };
        edgeItem.Click += (_, _) =>
        {
            _settings.EdgeAutoHide = !_settings.EdgeAutoHide;
            edgeItem.IsChecked = _settings.EdgeAutoHide;
            if (!_settings.EdgeAutoHide)
                RestoreFromEdgeHide();
            Persist();
        };
        menu.Items.Add(edgeItem);

        var panelItem = new MenuItem
        {
            Header = "显示天气/时间/网速",
            IsCheckable = true,
            IsChecked = _settings.ShowSidePanel
        };
        panelItem.Click += (_, _) =>
        {
            _settings.ShowSidePanel = !_settings.ShowSidePanel;
            panelItem.IsChecked = _settings.ShowSidePanel;
            ApplySidePanelVisibility();
            if (_settings.ShowSidePanel)
                _netTimer.Start();
            else
                _netTimer.Stop();
            Persist();
        };
        menu.Items.Add(panelItem);

        var chimeItem = new MenuItem
        {
            Header = "整点报时",
            IsCheckable = true,
            IsChecked = _settings.HourlyChime
        };
        chimeItem.Click += (_, _) =>
        {
            _settings.HourlyChime = !_settings.HourlyChime;
            chimeItem.IsChecked = _settings.HourlyChime;
            Persist();
        };
        menu.Items.Add(chimeItem);

        var bubbleItem = new MenuItem
        {
            Header = "点击显示吐槽气泡",
            IsCheckable = true,
            IsChecked = _settings.ShowBubbleTips
        };
        bubbleItem.Click += (_, _) =>
        {
            _settings.ShowBubbleTips = !_settings.ShowBubbleTips;
            bubbleItem.IsChecked = _settings.ShowBubbleTips;
            Persist();
        };
        menu.Items.Add(bubbleItem);

        menu.Items.Add(new Separator());
        menu.Items.Add(BuildSkinSubmenu());

        var trayItem = new MenuItem { Header = "最小化到托盘" };
        trayItem.Click += (_, _) => MinimizeToTray();
        menu.Items.Add(trayItem);

        return menu;
    }

    private MenuItem BuildOpacitySubmenu()
    {
        var root = new MenuItem { Header = "透明度" };
        foreach (var op in new[] { 1.0, 0.9, 0.75, 0.6, 0.45 })
        {
            var v = op;
            var item = new MenuItem { Header = $"{(int)(op * 100)}%" };
            item.Click += (_, _) =>
            {
                _settings.Opacity = v;
                Opacity = v;
                Persist();
            };
            root.Items.Add(item);
        }

        return root;
    }

    private MenuItem BuildScaleSubmenu()
    {
        var root = new MenuItem { Header = "大小缩放" };
        foreach (var s in new[] { 0.75, 1.0, 1.25, 1.5, 2.0 })
        {
            var v = s;
            var item = new MenuItem { Header = $"{(int)(s * 100)}%" };
            item.Click += (_, _) =>
            {
                _settings.Scale = v;
                RootScale.ScaleX = v;
                RootScale.ScaleY = v;
                Persist();
            };
            root.Items.Add(item);
        }

        return root;
    }

    private MenuItem BuildSkinSubmenu()
    {
        var root = new MenuItem { Header = "切换皮肤" };
        foreach (var name in _skinLoader.ListSkinNames())
        {
            var n = name;
            var item = new MenuItem { Header = n, IsCheckable = true, IsChecked = n == _settings.SkinName };
            item.Click += (_, _) =>
            {
                _settings.SkinName = n;
                ReloadSkinFrames();
                foreach (MenuItem child in root.Items)
                    child.IsChecked = child.Header as string == n;
                Persist();
            };
            root.Items.Add(item);
        }

        if (root.Items.Count == 0)
            root.Items.Add(new MenuItem { Header = "(放入 Skins\\名称\\*.png)", IsEnabled = false });

        return root;
    }

    private void TogglePetVisible()
    {
        SetPetVisible(!_settings.PetVisible, persist: true);
    }

    private void SetPetVisible(bool visible, bool persist)
    {
        _settings.PetVisible = visible;
        ApplyPetVisibility();
        if (persist)
            Persist();
    }

    private void ApplyPetVisibility()
    {
        if (_settings.PetVisible)
        {
            PetHitArea.Visibility = Visibility.Visible;
            PetImage.Visibility = _frames.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            FallbackPet.Visibility = _frames.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            PetHitArea.Visibility = Visibility.Hidden;
        }
    }

    private void ApplySidePanelVisibility()
    {
        var vis = _settings.ShowSidePanel ? Visibility.Visible : Visibility.Collapsed;
        SidePanelLeft.Visibility = vis;
        SidePanelRight.Visibility = vis;
        _ = RefreshWeatherAsync();
    }

    private void SnapAndMaybeEdgeHide()
    {
        var wa = SystemParameters.WorkArea;
        var margin = SnapPx;

        if (Left < wa.Left + margin)
            Left = wa.Left;
        else if (Left + ActualWidth > wa.Right - margin)
            Left = wa.Right - ActualWidth;

        if (Top < wa.Top + margin)
            Top = wa.Top;
        else if (Top + ActualHeight > wa.Bottom - margin)
            Top = wa.Bottom - ActualHeight;

        if (_settings.EdgeAutoHide)
            ApplyEdgeHideLayout();
        else
            _edgeHiddenActive = false;

        PersistPosition();
    }

    private void ApplyEdgeHideLayout()
    {
        var wa = SystemParameters.WorkArea;
        _edgeHiddenActive = false;
        if (Math.Abs(Left - wa.Left) < 6)
        {
            Left = wa.Left - ActualWidth + PeekPx;
            _edgeHiddenActive = true;
        }
        else if (Math.Abs(Left + ActualWidth - wa.Right) < 6)
        {
            Left = wa.Right - PeekPx;
            _edgeHiddenActive = true;
        }
    }

    private void ExpandEdgeHidden()
    {
        _edgeHideDelayTimer?.Stop();
        if (!_settings.EdgeAutoHide || !_edgeHiddenActive)
            return;

        var wa = SystemParameters.WorkArea;
        if (Left < wa.Left)
            Left = wa.Left;
        else if (Left + ActualWidth > wa.Right)
            Left = wa.Right - ActualWidth;
    }

    private void ScheduleEdgeHide()
    {
        if (!_settings.EdgeAutoHide || !_hasDragged)
            return;

        if (_edgeHideDelayTimer == null)
        {
            _edgeHideDelayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
            _edgeHideDelayTimer.Tick += (_, _) =>
            {
                _edgeHideDelayTimer?.Stop();
                if (!_settings.EdgeAutoHide)
                    return;
                ApplyEdgeHideLayout();
            };
        }

        _edgeHideDelayTimer.Stop();
        _edgeHideDelayTimer.Start();
    }

    private void RestoreFromEdgeHide()
    {
        var wa = SystemParameters.WorkArea;
        if (Left < wa.Left)
            Left = wa.Left;
        if (Left + ActualWidth > wa.Right)
            Left = wa.Right - ActualWidth;
        _edgeHiddenActive = false;
    }

    private void ReloadSkinFrames()
    {
        _animTimer.Stop();
        _frames = _skinLoader.LoadFrames(_settings.SkinName);
        PetImage.Source = _frames.Count > 0 ? _frames[0] : null;
        ApplyPetVisibility();
        _frameIndex = 0;
        if (_frames.Count > 1)
            _animTimer.Start();
    }

    private void AdvanceFrame()
    {
        if (_frames.Count <= 1)
            return;
        _frameIndex = (_frameIndex + 1) % _frames.Count;
        PetImage.Source = _frames[_frameIndex];
    }

    private void ApplySettingsFromStore()
    {
        if (_settings.RunAtStartup != StartupHelper.IsEnabled())
        {
            var exe = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "";
            if (!string.IsNullOrEmpty(exe))
                StartupHelper.SetEnabled(_settings.RunAtStartup, exe);
        }

        Opacity = Math.Clamp(_settings.Opacity, 0.3, 1.0);
        _settings.Opacity = Opacity;
        var scale = Math.Clamp(_settings.Scale, 0.5, 2.5);
        _settings.Scale = scale;
        RootScale.ScaleX = scale;
        RootScale.ScaleY = scale;

        if (!double.IsNaN(_settings.WindowLeft) && !double.IsNaN(_settings.WindowTop))
        {
            Left = _settings.WindowLeft;
            Top = _settings.WindowTop;
        }
        else
        {
            Left = SystemParameters.WorkArea.Right - Width - 40;
            Top = SystemParameters.WorkArea.Bottom - Height - 40;
        }

        ApplyPetVisibility();
        ApplySidePanelVisibility();
    }

    private void Persist()
    {
        SettingsStore.Save(_settings);
    }

    private void PersistPosition()
    {
        _settings.WindowLeft = Left;
        _settings.WindowTop = Top;
        Persist();
    }

    private void OnClockTick()
    {
        if (_settings.ShowSidePanel)
            TxtTime.Text = DateTime.Now.ToString("HH:mm:ss");

        if (_settings.HourlyChime)
        {
            var now = DateTime.Now;
            if (now.Minute == 0 && now.Second <= 1 && _lastChimeHour != now.Hour)
            {
                _lastChimeHour = now.Hour;
                try { SystemSounds.Exclamation.Play(); } catch { /* ignore */ }
            }
        }

        if (_settings.ShowSidePanel && DateTime.Now.Second % 12 == 0)
            _ = RefreshWeatherAsync();
    }

    private async Task RefreshWeatherAsync()
    {
        if (!_settings.ShowSidePanel)
            return;
        var brief = await _weather.GetBriefAsync(_settings.WeatherLatitude, _settings.WeatherLongitude)
            .ConfigureAwait(true);
        TxtWeather.Text = brief ?? "天气 —";
    }

    private void RefreshNetworkLabel()
    {
        if (!_settings.ShowSidePanel)
            return;
        TxtNet.Text = _netMeter.Sample();
    }

    private void EnsureTrayIcon()
    {
        if (_notifyIcon != null)
            return;

        _notifyIcon = new Forms.NotifyIcon
        {
            Visible = true,
            Text = "桌面宠物 PetWindow",
            Icon = System.Drawing.SystemIcons.Application
        };
        _notifyIcon.MouseDoubleClick += (_, _) => RestoreFromTray();
        _notifyIcon.ContextMenuStrip = BuildTrayContextMenu();
    }

    private Forms.ContextMenuStrip BuildTrayContextMenu()
    {
        var strip = new Forms.ContextMenuStrip();
        strip.Items.Add("显示宠物", null, (_, _) => RestoreFromTray());
        strip.Items.Add("隐藏到托盘（当前已在后台）", null, (_, _) => MinimizeToTray());
        strip.Items.Add(new Forms.ToolStripSeparator());
        strip.Items.Add("退出", null, (_, _) => ExitApplication());
        return strip;
    }

    private void MinimizeToTray()
    {
        Hide();
        BubblePopup.IsOpen = false;
        _notifyIcon?.ShowBalloonTip(2000, "桌面宠物", "已最小化到托盘，双击图标可唤回。", Forms.ToolTipIcon.Info);
    }

    private void RestoreFromTray()
    {
        SetPetVisible(true, persist: true);
        Show();
        Activate();
        WindowState = WindowState.Normal;
    }

    private void ExitApplication()
    {
        _shuttingDown = true;
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
        PersistPosition();
        System.Windows.Application.Current.Shutdown();
    }

    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_shuttingDown)
            return;
        e.Cancel = true;
        MinimizeToTray();
    }

    protected override void OnClosed(EventArgs e)
    {
        _animTimer.Stop();
        _clockTimer.Stop();
        _netTimer.Stop();
        _notifyIcon?.Dispose();
        base.OnClosed(e);
    }
}
