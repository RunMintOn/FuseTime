using System.Text.Json;

namespace ZenTimeBox.Demo;

internal sealed class TimerMenuSettingsService
{
    private static readonly int[] BuiltInMinutes = [5, 10, 15, 25, 30, 45, 60, 90, 120];
    private static readonly int[] DefaultFavorites = [15, 25, 45, 60];
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string storagePath;
    private TimerMenuSettings? settings;

    public TimerMenuSettingsService()
    {
        storagePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ZenTimeBox",
            "settings.json");
    }

    public IReadOnlyList<int> BuiltIns => BuiltInMinutes;

    public TimerMenuSettings GetSettings()
    {
        settings ??= LoadSettings();
        return settings;
    }

    public void SaveSettings(TimerMenuSettings nextSettings)
    {
        settings = Normalize(nextSettings);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(storagePath)!);
            File.WriteAllText(storagePath, JsonSerializer.Serialize(settings, JsonOptions));
        }
        catch (Exception exception)
        {
            AppDiagnostics.LogException(exception, "Timer menu settings save failed");
        }
    }

    public int[] GetAllMinutes()
    {
        TimerMenuSettings current = GetSettings();
        return BuiltInMinutes
            .Concat(current.CustomMinutes)
            .Where(IsValidMinute)
            .Distinct()
            .Order()
            .ToArray();
    }

    public bool IsBuiltIn(int minutes) => BuiltInMinutes.Contains(minutes);

    private TimerMenuSettings LoadSettings()
    {
        try
        {
            if (!File.Exists(storagePath))
            {
                return CreateDefaultSettings();
            }

            string json = File.ReadAllText(storagePath);
            TimerMenuSettings loaded = JsonSerializer.Deserialize<TimerMenuSettings>(json, JsonOptions) ?? CreateDefaultSettings();
            return Normalize(loaded);
        }
        catch (Exception exception)
        {
            AppDiagnostics.LogException(exception, "Timer menu settings load failed");
            return CreateDefaultSettings();
        }
    }

    private static TimerMenuSettings CreateDefaultSettings()
    {
        return new TimerMenuSettings
        {
            FavoriteMinutes = [.. DefaultFavorites],
            CustomMinutes = [],
        };
    }

    private static TimerMenuSettings Normalize(TimerMenuSettings source)
    {
        int[] custom = source.CustomMinutes
            .Where(IsValidMinute)
            .Distinct()
            .Order()
            .ToArray();

        int[] all = BuiltInMinutes.Concat(custom).Distinct().ToArray();
        int[] favorites = source.FavoriteMinutes
            .Where(minute => all.Contains(minute))
            .Distinct()
            .Order()
            .ToArray();

        if (favorites.Length == 0)
        {
            favorites = [.. DefaultFavorites];
        }

        return new TimerMenuSettings
        {
            FavoriteMinutes = favorites,
            CustomMinutes = custom,
        };
    }

    private static bool IsValidMinute(int minutes) => minutes is >= 1 and <= 999;
}

internal sealed class TimerMenuSettings
{
    public int[] FavoriteMinutes { get; set; } = [];

    public int[] CustomMinutes { get; set; } = [];
}
