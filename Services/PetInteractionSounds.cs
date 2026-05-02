using System.IO;
using System.Media;
using PetWindow.Models;

namespace PetWindow.Services;

public static class PetInteractionSounds
{
    private static readonly Random Rng = new();
    private static DateTime _lastHoverUtc = DateTime.MinValue;

    public static void PlayTap(AppSettings settings)
    {
        try
        {
            var path = settings.ClickSoundPath;
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                using var p = new SoundPlayer(path);
                p.Play();
                return;
            }

            PlayTapDefaultSound();
        }
        catch
        {
            try { SystemSounds.Beep.Play(); } catch { /* ignore */ }
        }
    }

    public static void PlayDragStart()
    {
        try
        {
            switch (Rng.Next(3))
            {
                case 0: SystemSounds.Question.Play(); break;
                case 1: SystemSounds.Hand.Play(); break;
                default: SystemSounds.Asterisk.Play(); break;
            }
        }
        catch { /* ignore */ }
    }

    public static void PlayDragEnd()
    {
        try
        {
            switch (Rng.Next(3))
            {
                case 0: SystemSounds.Hand.Play(); break;
                case 1: SystemSounds.Asterisk.Play(); break;
                default: SystemSounds.Beep.Play(); break;
            }
        }
        catch { /* ignore */ }
    }

    public static void PlayHover()
    {
        if ((DateTime.UtcNow - _lastHoverUtc).TotalSeconds < 28)
            return;
        _lastHoverUtc = DateTime.UtcNow;
        try { PlayTapDefaultSound(); } catch { /* ignore */ }
    }

    public static void PlayMenuOpen()
    {
        try
        {
            switch (Rng.Next(3))
            {
                case 0: SystemSounds.Beep.Play(); break;
                case 1: SystemSounds.Hand.Play(); break;
                default: SystemSounds.Asterisk.Play(); break;
            }
        }
        catch { /* ignore */ }
    }

    /// <summary>主动问候时轻微提示（与点击错开音色池）。</summary>
    public static void PlayCarePing()
    {
        try
        {
            switch (Rng.Next(4))
            {
                case 0: SystemSounds.Question.Play(); break;
                case 1: SystemSounds.Exclamation.Play(); break;
                case 2: SystemSounds.Hand.Play(); break;
                default: SystemSounds.Asterisk.Play(); break;
            }
        }
        catch { /* ignore */ }
    }

    /// <summary>无自定义 wav 时：点击 / 悬停共用较柔和的系统音池。</summary>
    private static void PlayTapDefaultSound()
    {
        switch (Rng.Next(4))
        {
            case 0: SystemSounds.Asterisk.Play(); break;
            case 1: SystemSounds.Hand.Play(); break;
            case 2: SystemSounds.Question.Play(); break;
            default: SystemSounds.Beep.Play(); break;
        }
    }
}
