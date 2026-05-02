using System.Windows;
using PetWindow.Models;
using PetWindow.Services;

namespace PetWindow;

public partial class HudTokenWindow : Window
{
    private readonly AppSettings _settings;

    public HudTokenWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        TokenBox.Text = settings.HuggingFaceApiToken ?? "";
        ModelBox.Text = string.IsNullOrWhiteSpace(settings.AnimeStyleModelId)
            ? AnimeSkinDefaults.DefaultModelId
            : settings.AnimeStyleModelId;
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnSave_OnClick(object sender, RoutedEventArgs e)
    {
        _settings.HuggingFaceApiToken = string.IsNullOrWhiteSpace(TokenBox.Text) ? null : TokenBox.Text.Trim();
        _settings.AnimeStyleModelId = string.IsNullOrWhiteSpace(ModelBox.Text)
            ? AnimeSkinDefaults.DefaultModelId
            : ModelBox.Text.Trim();
        SettingsStore.Save(_settings);
        DialogResult = true;
        Close();
    }
}
