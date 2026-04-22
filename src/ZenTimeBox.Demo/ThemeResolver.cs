using Microsoft.Win32;
using System.Drawing;

namespace ZenTimeBox.Demo;

internal sealed class ThemeResolver
{
    public ThemePalette Resolve(ThemeMode mode, StateColor stateColor)
    {
        ThemeMode effectiveMode = mode == ThemeMode.Auto ? DetectSystemMode() : mode;
        bool dark = effectiveMode != ThemeMode.Light;

        return stateColor switch
        {
            StateColor.Warn => dark
                ? new ThemePalette(Color.FromArgb(255, 229, 64), Color.FromArgb(110, 80, 0), Color.FromArgb(210, 218, 228), Color.FromArgb(82, 210, 218, 228), Color.Transparent)
                : new ThemePalette(Color.FromArgb(168, 103, 0), Color.FromArgb(255, 240, 170), Color.FromArgb(92, 99, 109), Color.FromArgb(72, 92, 99, 109), Color.Transparent),
            StateColor.Critical => dark
                ? new ThemePalette(Color.FromArgb(255, 82, 82), Color.FromArgb(120, 0, 0), Color.FromArgb(210, 218, 228), Color.FromArgb(82, 210, 218, 228), Color.Transparent)
                : new ThemePalette(Color.FromArgb(170, 0, 0), Color.FromArgb(255, 214, 214), Color.FromArgb(92, 99, 109), Color.FromArgb(72, 92, 99, 109), Color.Transparent),
            _ => dark
                ? new ThemePalette(Color.FromArgb(246, 248, 252), Color.FromArgb(32, 40, 48), Color.FromArgb(210, 218, 228), Color.FromArgb(82, 210, 218, 228), Color.Transparent)
                : new ThemePalette(Color.FromArgb(18, 22, 28), Color.FromArgb(225, 229, 236), Color.FromArgb(92, 99, 109), Color.FromArgb(72, 92, 99, 109), Color.Transparent),
        };
    }

    private static ThemeMode DetectSystemMode()
    {
        const string personalizePath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        const string valueName = "AppsUseLightTheme";

        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(personalizePath, writable: false);
        object? value = key?.GetValue(valueName);

        return value is int intValue && intValue > 0 ? ThemeMode.Light : ThemeMode.Dark;
    }
}
