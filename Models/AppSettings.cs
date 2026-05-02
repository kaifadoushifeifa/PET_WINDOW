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

    /// <summary>定时主动弹出关心问候（与点击吐槽无关）。</summary>
    public bool ProactiveCareEnabled { get; set; } = true;

    /// <summary>主动问候时是否带轻微系统音效。</summary>
    public bool ProactiveCareSound { get; set; } = true;
    public bool ShowSidePanel { get; set; }
    public double WeatherLatitude { get; set; } = 39.9042;
    public double WeatherLongitude { get; set; } = 116.4074;
    public string? ClickSoundPath { get; set; }

    /// <summary>Hugging Face Read Token，用于云端 AnimeGAN 推理；为空则仅用本地滤镜。</summary>
    public string? HuggingFaceApiToken { get; set; }

    /// <summary>HF Inference 模型 ID，例如 Xingren987/animegan2-pytorch。</summary>
    public string AnimeStyleModelId { get; set; } = "Xingren987/animegan2-pytorch";
}
