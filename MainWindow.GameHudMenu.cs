using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Forms = System.Windows.Forms;
using PetWindow.Services;

namespace PetWindow;

public partial class MainWindow
{
    private System.Windows.Controls.Primitives.Popup? _gameMenuPopup;

    private void WindowOnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!IsVisible)
            return;
        ShowGameHudMenu(e.GetPosition(this));
        e.Handled = true;
    }

    private void CloseGameMenu()
    {
        if (_gameMenuPopup != null)
            _gameMenuPopup.IsOpen = false;
    }

    private Style? HudStyle(string key) => TryFindResource(key) as Style;

    private void ShowGameHudMenu(System.Windows.Point positionInWindow)
    {
        PetInteractionSounds.PlayMenuOpen();

        var popup = new System.Windows.Controls.Primitives.Popup
        {
            PlacementTarget = this,
            Placement = PlacementMode.RelativePoint,
            HorizontalOffset = positionInWindow.X,
            VerticalOffset = positionInWindow.Y,
            StaysOpen = false,
            AllowsTransparency = true,
            PopupAnimation = PopupAnimation.Fade
        };

        _gameMenuPopup = popup;
        popup.Closed += (_, _) => _gameMenuPopup = null;

        var panel = new Border { Style = HudStyle("GameHudPanelBorder"), MinWidth = 220 };
        var stack = new StackPanel();

        var title = new TextBlock { Style = HudStyle("GameHudTitle"), Text = "✿ 宠物菜单" };
        stack.Children.Add(title);

        void RefreshToggle(object tag, string text) => RefreshBtnRecursive(stack, tag, text);

        stack.Children.Add(MkExpandableGroup("外观",
            MkFlyoutBtn("透明度 ▸", BuildOpacityFlyout(popup)),
            MkFlyoutBtn("大小缩放 ▸", BuildScaleFlyout(popup)),
            MkFlyoutBtn("切换皮肤 ▸", BuildSkinFlyout(popup))));

        stack.Children.Add(MkExpandableGroup("窗口与显示",
            MkBtn(ToggleLabel("开机自启", _settings.RunAtStartup), (_, _) =>
            {
                _settings.RunAtStartup = !_settings.RunAtStartup;
                var exe = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (!string.IsNullOrEmpty(exe))
                    StartupHelper.SetEnabled(_settings.RunAtStartup, exe);
                Persist();
                RefreshToggle("startup", ToggleLabel("开机自启", _settings.RunAtStartup));
            }, "startup"),
            MkBtn(ToggleLabel("宠物可见", _settings.PetVisible), (_, _) =>
            {
                TogglePetVisible();
                RefreshToggle("petvis", ToggleLabel("宠物可见", _settings.PetVisible));
            }, "petvis"),
            MkBtn(ToggleLabel("边缘自动隐藏", _settings.EdgeAutoHide), (_, _) =>
            {
                _settings.EdgeAutoHide = !_settings.EdgeAutoHide;
                if (!_settings.EdgeAutoHide)
                    RestoreFromEdgeHide();
                Persist();
                RefreshToggle("edge", ToggleLabel("边缘自动隐藏", _settings.EdgeAutoHide));
            }, "edge"),
            MkBtn(ToggleLabel("天气 / 时间 / 网速侧栏", _settings.ShowSidePanel), (_, _) =>
            {
                _settings.ShowSidePanel = !_settings.ShowSidePanel;
                ApplySidePanelVisibility();
                if (_settings.ShowSidePanel)
                    _netTimer.Start();
                else
                    _netTimer.Stop();
                Persist();
                RefreshToggle("panel", ToggleLabel("天气 / 时间 / 网速侧栏", _settings.ShowSidePanel));
            }, "panel")));

        stack.Children.Add(MkExpandableGroup("提醒与气泡",
            MkBtn(ToggleLabel("整点报时", _settings.HourlyChime), (_, _) =>
            {
                _settings.HourlyChime = !_settings.HourlyChime;
                Persist();
                RefreshToggle("chime", ToggleLabel("整点报时", _settings.HourlyChime));
            }, "chime"),
            MkBtn(ToggleLabel("点击吐槽气泡", _settings.ShowBubbleTips), (_, _) =>
            {
                _settings.ShowBubbleTips = !_settings.ShowBubbleTips;
                Persist();
                RefreshToggle("bubble", ToggleLabel("点击吐槽气泡", _settings.ShowBubbleTips));
            }, "bubble"),
            MkBtn(ToggleLabel("主动问候关心", _settings.ProactiveCareEnabled), (_, _) =>
            {
                _settings.ProactiveCareEnabled = !_settings.ProactiveCareEnabled;
                Persist();
                RefreshToggle("care", ToggleLabel("主动问候关心", _settings.ProactiveCareEnabled));
            }, "care"),
            MkBtn(ToggleLabel("问候轻提示音", _settings.ProactiveCareSound), (_, _) =>
            {
                _settings.ProactiveCareSound = !_settings.ProactiveCareSound;
                Persist();
                RefreshToggle("caresnd", ToggleLabel("问候轻提示音", _settings.ProactiveCareSound));
            }, "caresnd")));

        stack.Children.Add(MkExpandableGroup("生成与配置",
            MkBtn("✦ 从照片生成二次元皮肤", (_, _) =>
            {
                CloseGameMenu();
                _ = GenerateAnimeSkinFromPhotoAsync();
            }),
            MkBtn("⚙ 配置云端 Token / 模型…", (_, _) =>
            {
                CloseGameMenu();
                var dlg = new HudTokenWindow(_settings) { Owner = this };
                dlg.ShowDialog();
            })));

        stack.Children.Add(MkSep());

        stack.Children.Add(MkBtn("最小化到托盘", (_, _) =>
        {
            CloseGameMenu();
            MinimizeToTray();
        }));

        stack.Children.Add(MkBtn("退出程序", (_, _) =>
        {
            CloseGameMenu();
            ExitApplication();
        }));

        panel.Child = stack;
        popup.Child = panel;
        popup.IsOpen = true;
    }

    private static string ToggleLabel(string name, bool on) =>
        on ? $"● {name} · ON" : $"○ {name} · OFF";

    private System.Windows.Controls.Button MkBtn(string text, RoutedEventHandler click, object? tag = null)
    {
        var b = new System.Windows.Controls.Button
        {
            Content = text,
            Style = TryFindResource("GameHudButton") as Style,
            Tag = tag
        };
        b.Click += click;
        return b;
    }

    private static void RefreshBtnRecursive(DependencyObject root, object tag, string text)
    {
        if (root is System.Windows.Controls.Button b && Equals(b.Tag, tag))
        {
            b.Content = text;
            return;
        }

        var n = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < n; i++)
            RefreshBtnRecursive(VisualTreeHelper.GetChild(root, i), tag, text);
    }

    private UIElement MkExpandableGroup(string title, params UIElement[] items)
    {
        var outer = new StackPanel();
        var head = new System.Windows.Controls.Button
        {
            Content = $"▶ {title}",
            Style = TryFindResource("GameHudButton") as Style,
            HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left,
            Tag = "expand_header"
        };
        var body = new StackPanel
        {
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(6, 0, 0, 4)
        };
        foreach (var el in items)
            body.Children.Add(el);

        head.Click += (_, _) =>
        {
            var open = body.Visibility != Visibility.Visible;
            body.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
            head.Content = open ? $"▼ {title}" : $"▶ {title}";
        };

        outer.Children.Add(head);
        outer.Children.Add(body);
        return outer;
    }

    private Border MkSep() => new() { Style = HudStyle("GameHudSep") };

    private System.Windows.Controls.Button MkFlyoutBtn(string title, UIElement flyoutContent)
    {
        var b = new System.Windows.Controls.Button
        {
            Content = title,
            Style = TryFindResource("GameHudButton") as Style
        };
        b.Click += (_, _) =>
        {
            var fly = new System.Windows.Controls.Primitives.Popup
            {
                PlacementTarget = b,
                Placement = PlacementMode.Right,
                HorizontalOffset = 4,
                StaysOpen = false,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Fade,
                Child = new Border
                {
                    Style = HudStyle("GameHudPanelBorder"),
                    Padding = new Thickness(10, 8, 10, 8),
                    Child = flyoutContent,
                    MinWidth = 128
                }
            };
            fly.IsOpen = true;
        };
        return b;
    }

    private UIElement BuildOpacityFlyout(System.Windows.Controls.Primitives.Popup rootPopup)
    {
        var sp = new StackPanel();
        foreach (var op in new[] { 1.0, 0.9, 0.75, 0.6, 0.45 })
        {
            var v = op;
            var row = new System.Windows.Controls.Button
            {
                Content = $"{(int)(op * 100)}%",
                Style = TryFindResource("GameHudButton") as Style,
                Margin = new Thickness(0, 1, 0, 1)
            };
            row.Click += (_, _) =>
            {
                _settings.Opacity = v;
                Opacity = v;
                Persist();
                rootPopup.IsOpen = false;
            };
            sp.Children.Add(row);
        }

        return sp;
    }

    private UIElement BuildScaleFlyout(System.Windows.Controls.Primitives.Popup rootPopup)
    {
        var sp = new StackPanel();
        foreach (var s in new[] { 0.75, 1.0, 1.25, 1.5, 2.0 })
        {
            var v = s;
            var row = new System.Windows.Controls.Button
            {
                Content = $"{(int)(s * 100)}%",
                Style = TryFindResource("GameHudButton") as Style,
                Margin = new Thickness(0, 1, 0, 1)
            };
            row.Click += (_, _) =>
            {
                ApplyPetScale(v, persist: true);
                rootPopup.IsOpen = false;
            };
            sp.Children.Add(row);
        }

        return sp;
    }

    private UIElement BuildSkinFlyout(System.Windows.Controls.Primitives.Popup rootPopup)
    {
        var scroll = new ScrollViewer { Style = HudStyle("GameHudMenuScroll") };
        var sp = new StackPanel();
        foreach (var name in _skinLoader.ListSkinNames())
        {
            var n = name;
            var row = new System.Windows.Controls.Button
            {
                Content = n == _settings.SkinName ? $"♦ {n}" : n,
                Style = TryFindResource("GameHudButton") as Style,
                Margin = new Thickness(0, 1, 0, 1),
                HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left
            };
            row.Click += (_, _) =>
            {
                _settings.SkinName = n;
                ReloadSkinFrames();
                Persist();
                rootPopup.IsOpen = false;
            };
            sp.Children.Add(row);
        }

        if (sp.Children.Count == 0)
            sp.Children.Add(new TextBlock { Text = "Skins 目录暂无皮肤", Foreground = System.Windows.Media.Brushes.LightGray, Margin = new Thickness(4) });

        scroll.Content = sp;
        return scroll;
    }

    private async Task GenerateAnimeSkinFromPhotoAsync()
    {
        using var dlg = new Forms.OpenFileDialog
        {
            Filter = "图片|*.png;*.jpg;*.jpeg;*.bmp",
            Title = "选择实体宠物照片"
        };
        if (dlg.ShowDialog() != Forms.DialogResult.OK)
            return;

        try
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            var gen = new AnimeSkinGenerator(Path.Combine(AppContext.BaseDirectory, "Skins"));
            var skinName = await gen.GenerateFromPhotoAsync(dlg.FileName, _settings, CancellationToken.None).ConfigureAwait(true);
            _settings.SkinName = skinName;
            ReloadSkinFrames();
            Persist();

            var hint = string.IsNullOrWhiteSpace(_settings.HuggingFaceApiToken)
                ? "\n（当前为本地滤镜；配置 HF Token 可尝试云端 AnimeGAN。）"
                : "";
            System.Windows.MessageBox.Show(this,
                $"已生成并切换到「{skinName}」。{hint}",
                "二次元皮肤",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, ex.Message, "生成失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }
}
