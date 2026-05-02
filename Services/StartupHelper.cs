using Microsoft.Win32;

namespace PetWindow.Services;

public static class StartupHelper
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "PetWindow";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
        var v = key?.GetValue(ValueName) as string;
        return !string.IsNullOrEmpty(v);
    }

    public static void SetEnabled(bool enabled, string executablePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
        if (key == null)
            return;
        if (enabled)
            key.SetValue(ValueName, $"\"{executablePath}\"");
        else
            key.DeleteValue(ValueName, false);
    }
}
