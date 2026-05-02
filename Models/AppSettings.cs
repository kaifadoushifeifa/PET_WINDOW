namespace PetWindow.Models;

public sealed class AppSettings
{
    public double Opacity { get; set; } = 1.0;
    public double Scale { get; set; } = 1.0;
    public bool RunAtStartup { get; set; }
    public bool PetVisible { get; set; } = true;
    public string SkinName { get; set; } = "Default";
    public double WindowLeft { get; set; } = double.NaN;
    public double WindowTop { get; set; } = double.NaN;
    public bool EdgeAutoHide { get; set; }
    public bool HourlyChime { get; set; }
    public bool ShowBubbleTips { get; set; } = true;
    public bool ShowSidePanel { get; set; }
    public double WeatherLatitude { get; set; } = 39.9042;
    public double WeatherLongitude { get; set; } = 116.4074;
    public string? ClickSoundPath { get; set; }
}
