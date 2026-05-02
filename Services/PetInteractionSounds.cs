using System.IO;
using System.Media;
using PetWindow.Models;

namespace PetWindow.Services;

public static class PetInteractionSounds
{
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

            SystemSounds.Asterisk.Play();
        }
        catch
        {
            SystemSounds.Beep.Play();
        }
    }

    public static void PlayDragStart()
    {
        try { SystemSounds.Question.Play(); } catch { /* ignore */ }
    }

    public static void PlayDragEnd()
    {
        try { SystemSounds.Hand.Play(); } catch { /* ignore */ }
    }

    public static void PlayHover()
    {
        if ((DateTime.UtcNow - _lastHoverUtc).TotalSeconds < 28)
            return;
        _lastHoverUtc = DateTime.UtcNow;
        try { SystemSounds.Hand.Play(); } catch { /* ignore */ }
    }

    public static void PlayMenuOpen()
    {
        try { SystemSounds.Beep.Play(); } catch { /* ignore */ }
    }

    /// <summary>主动问候时轻微提示（不与点击同款，避免腻）。</summary>
    public static void PlayCarePing()
    {
        try { SystemSounds.Question.Play(); } catch { /* ignore */ }
    }
}
