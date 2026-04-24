using System.Text.Json;

namespace ZenTimeBox.Demo;

internal sealed class DailyPulseService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string storagePath;
    private DailyPulseStore? store;

    public DailyPulseService()
    {
        storagePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ZenTimeBox",
            "daily-pulse.json");
    }

    public void RecordCompletion(DateTimeOffset now)
    {
        DailyPulseStore currentStore = LoadStore();
        DateOnly today = DateOnly.FromDateTime(now.LocalDateTime);
        string key = today.ToString("yyyy-MM-dd");
        DailyPulseDay day = GetOrCreateDay(currentStore, key);
        int hour = Math.Clamp(now.LocalDateTime.Hour, 0, 23);

        day.CompletionCount++;
        day.CompletedHours[hour] = true;
        SaveStore(currentStore);
    }

    public DailyPulseSnapshot GetTodaySnapshot(DateTimeOffset now)
    {
        DailyPulseStore currentStore = LoadStore();
        DateOnly today = DateOnly.FromDateTime(now.LocalDateTime);
        string key = today.ToString("yyyy-MM-dd");

        if (!currentStore.Days.TryGetValue(key, out DailyPulseDay? day))
        {
            day = new DailyPulseDay();
        }

        bool[] completedHours = NormalizeHours(day.CompletedHours);
        return new DailyPulseSnapshot(today, Math.Clamp(now.LocalDateTime.Hour, 0, 23), completedHours, Math.Max(0, day.CompletionCount));
    }

    private DailyPulseStore LoadStore()
    {
        if (store is not null)
        {
            return store;
        }

        try
        {
            if (!File.Exists(storagePath))
            {
                store = new DailyPulseStore();
                return store;
            }

            string json = File.ReadAllText(storagePath);
            store = JsonSerializer.Deserialize<DailyPulseStore>(json, JsonOptions) ?? new DailyPulseStore();
            return store;
        }
        catch (Exception exception)
        {
            AppDiagnostics.LogException(exception, "DailyPulse load failed");
            store = new DailyPulseStore();
            return store;
        }
    }

    private void SaveStore(DailyPulseStore currentStore)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(storagePath)!);
            string json = JsonSerializer.Serialize(currentStore, JsonOptions);
            File.WriteAllText(storagePath, json);
        }
        catch (Exception exception)
        {
            AppDiagnostics.LogException(exception, "DailyPulse save failed");
        }
    }

    private static DailyPulseDay GetOrCreateDay(DailyPulseStore currentStore, string key)
    {
        if (!currentStore.Days.TryGetValue(key, out DailyPulseDay? day))
        {
            day = new DailyPulseDay();
            currentStore.Days[key] = day;
        }

        day.CompletedHours = NormalizeHours(day.CompletedHours);
        day.CompletionCount = Math.Max(0, day.CompletionCount);
        return day;
    }

    private static bool[] NormalizeHours(bool[]? hours)
    {
        bool[] normalized = new bool[24];
        if (hours is null)
        {
            return normalized;
        }

        Array.Copy(hours, normalized, Math.Min(hours.Length, normalized.Length));
        return normalized;
    }
}

internal sealed class DailyPulseStore
{
    public Dictionary<string, DailyPulseDay> Days { get; set; } = [];
}

internal sealed class DailyPulseDay
{
    public bool[] CompletedHours { get; set; } = new bool[24];

    public int CompletionCount { get; set; }
}
