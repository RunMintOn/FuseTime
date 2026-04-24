using System.Drawing;

namespace ZenTimeBox.Demo;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private static readonly (int Minutes, string Label)[] Presets =
    [
        (5, "Start 5 min"),
        (10, "Start 10 min"),
        (15, "Start 15 min"),
        (25, "Start 25 min"),
        (30, "Start 30 min"),
        (45, "Start 45 min"),
        (60, "Start 60 min"),
        (90, "Start 90 min"),
        (120, "Start 120 min"),
    ];

    private readonly NotifyIcon notifyIcon;
    private readonly System.Windows.Forms.Timer uiTimer;
    private readonly TrayIconRenderer renderer = new();
    private readonly TimerController timerController = new();
    private readonly CompletionNotificationService notificationService = new();
    private readonly DailyPulseService dailyPulseService = new();
    private readonly DailyPulseView dailyPulseView = new();
    private readonly TimerMenuSettingsService menuSettingsService = new();
    private readonly ContextMenuStrip contextMenu;
    private readonly Dictionary<DemoSchemeId, ToolStripMenuItem> schemeItems = [];
    private readonly Dictionary<ThemeMode, ToolStripMenuItem> themeItems = [];
    private readonly List<ToolStripItem> favoriteMenuItems = [];
    private ToolStripSeparator timerMenuSeparator = null!;
    private ToolStripMenuItem stopItem = null!;
    private ToolStripMenuItem resetItem = null!;
    private DurationSettingsForm? settingsForm;
    private Icon? currentIcon;

    private DemoSchemeId currentScheme = DemoSchemeId.SegoeBold;
    private ThemeMode currentThemeMode = ThemeMode.Auto;

    public TrayApplicationContext()
    {
        uiTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        uiTimer.Tick += (_, _) => RefreshFromClock();

        contextMenu = BuildMenu();

        notifyIcon = new NotifyIcon
        {
            Visible = true,
            ContextMenuStrip = contextMenu,
            Text = "ZenTimeBox Demo",
        };
        notifyIcon.MouseDown += (_, args) =>
        {
            if (args.Button == MouseButtons.Right)
            {
                ImeSuppressor.MatchForegroundKeyboardLayout();
            }
        };

        uiTimer.Start();
        RefreshFromClock();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            uiTimer.Stop();
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            uiTimer.Dispose();
            currentIcon?.Dispose();
            renderer.Dispose();
            contextMenu.Dispose();
        }

        base.Dispose(disposing);
    }

    private ContextMenuStrip BuildMenu()
    {
        ContextMenuStrip menu = new()
        {
            ImeMode = ImeMode.Disable,
        };
        menu.HandleCreated += (_, _) => ImeSuppressor.DisableForHandle(menu.Handle);
        menu.Opening += (_, _) =>
        {
            RebuildTimerMenuItems();
            ImeSuppressor.MatchForegroundKeyboardLayout();
        };

        ToolStripControlHost pulseHost = new(dailyPulseView)
        {
            AutoSize = false,
            Size = dailyPulseView.Size,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };

        menu.Items.Add(pulseHost);
        menu.Items.Add(new ToolStripSeparator());
        RebuildTimerMenuItems(menu);

        stopItem = new ToolStripMenuItem("Stop", null, (_, _) =>
        {
            timerController.Stop();
            RefreshFromClock();
        });

        resetItem = new ToolStripMenuItem("Reset", null, (_, _) =>
        {
            timerController.Reset(DateTimeOffset.UtcNow);
            RefreshFromClock();
        });

        ToolStripMenuItem settingsItem = new("Settings...", null, (_, _) => OpenSettings());

        ToolStripMenuItem rendererMenu = new("Renderer");
        foreach (IDigitScheme scheme in DigitSchemes.All)
        {
            ToolStripMenuItem item = new(scheme.DisplayName, null, (_, _) =>
            {
                currentScheme = scheme.Id;
                RefreshFromClock();
            });
            schemeItems[scheme.Id] = item;
            rendererMenu.DropDownItems.Add(item);
        }

        ToolStripMenuItem themeMenu = new("Theme");
        AddThemeItem(themeMenu, ThemeMode.Auto, "Auto");
        AddThemeItem(themeMenu, ThemeMode.Dark, "Dark");
        AddThemeItem(themeMenu, ThemeMode.Light, "Light");

        ToolStripMenuItem exitItem = new("Exit", null, (_, _) => ExitThread());

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(stopItem);
        menu.Items.Add(resetItem);
        menu.Items.Add(settingsItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(rendererMenu);
        menu.Items.Add(themeMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        UpdateMenuState(timerController.GetSnapshot(DateTimeOffset.UtcNow));
        return menu;
    }

    private void RebuildTimerMenuItems(ContextMenuStrip? targetMenu = null)
    {
        ContextMenuStrip menu = targetMenu ?? contextMenu;
        foreach (ToolStripItem item in favoriteMenuItems)
        {
            menu.Items.Remove(item);
            item.Dispose();
        }

        favoriteMenuItems.Clear();
        if (timerMenuSeparator is not null)
        {
            menu.Items.Remove(timerMenuSeparator);
            timerMenuSeparator.Dispose();
        }

        int insertIndex = 2;
        TimerMenuSettings settings = menuSettingsService.GetSettings();
        foreach (int minutes in settings.FavoriteMinutes)
        {
            ToolStripMenuItem item = new($"Start {minutes} min", null, (_, _) => StartPreset(minutes));
            favoriteMenuItems.Add(item);
            menu.Items.Insert(insertIndex++, item);
        }

        timerMenuSeparator = new ToolStripSeparator();
        menu.Items.Insert(insertIndex, timerMenuSeparator);
    }

    private void AddThemeItem(ToolStripMenuItem menu, ThemeMode mode, string label)
    {
        ToolStripMenuItem item = new(label, null, (_, _) =>
        {
            currentThemeMode = mode;
            RefreshFromClock();
        });

        themeItems[mode] = item;
        menu.DropDownItems.Add(item);
    }

    private void StartPreset(int minutes)
    {
        timerController.Start(minutes, DateTimeOffset.UtcNow);
        RefreshFromClock();
    }

    private void OpenSettings()
    {
        if (settingsForm is { IsDisposed: false })
        {
            settingsForm.Activate();
            return;
        }

        settingsForm = new DurationSettingsForm(menuSettingsService, () => RebuildTimerMenuItems());
        settingsForm.FormClosed += (_, _) => settingsForm = null;
        settingsForm.Show();
    }

    private void RefreshFromClock()
    {
        try
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            TimerSnapshot snapshot = timerController.GetSnapshot(now);
            if (snapshot.CompletedThisTick)
            {
                dailyPulseService.RecordCompletion(now);
                notificationService.ShowCompleted(notifyIcon);
            }

            float dpiScale = GetDpiScale();
            Size iconSize = GetIconSize(dpiScale);
            DemoRenderRequest request = new(
                DisplayText: snapshot.DisplayText,
                VisualMode: snapshot.VisualMode,
                SchemeId: currentScheme,
                ThemeMode: currentThemeMode,
                StateColor: snapshot.StateColor,
                ShowSecondBorder: snapshot.ShowSecondBorder,
                SecondBorderRatio: snapshot.SecondBorderRatio,
                DpiScale: dpiScale,
                IconSize: iconSize);

            Icon nextIcon = renderer.Render(request);
            Icon? previousIcon = currentIcon;
            currentIcon = nextIcon;
            notifyIcon.Icon = nextIcon;
            previousIcon?.Dispose();
            notifyIcon.Text = BuildTooltip(snapshot, dpiScale);
            UpdateMenuState(snapshot);
            dailyPulseView.UpdateSnapshot(dailyPulseService.GetTodaySnapshot(now), currentThemeMode);
        }
        catch (Exception exception)
        {
            AppDiagnostics.LogException(exception, "RefreshFromClock failed");
        }
    }

    private void UpdateMenuState(TimerSnapshot snapshot)
    {
        foreach ((DemoSchemeId schemeId, ToolStripMenuItem item) in schemeItems)
        {
            item.Checked = schemeId == currentScheme;
        }

        foreach ((ThemeMode themeMode, ToolStripMenuItem item) in themeItems)
        {
            item.Checked = themeMode == currentThemeMode;
        }

        bool hasActiveTimer = snapshot.Phase is TimerPhase.Running or TimerPhase.Overtime;
        stopItem.Enabled = hasActiveTimer;
        resetItem.Enabled = snapshot.LastStartedMinutes.HasValue;
    }

    private string BuildTooltip(TimerSnapshot snapshot, float dpiScale)
    {
        string schemeLabel = DigitSchemes.Get(currentScheme).DisplayName;
        string themeLabel = currentThemeMode.ToString();
        int iconPixels = GetIconSize(dpiScale).Width;
        string timerLabel = snapshot.Phase switch
        {
            TimerPhase.Running when snapshot.VisualMode == TrayIconVisualMode.Text => $"Remaining {snapshot.DisplayText}",
            TimerPhase.Overtime => $"Overtime {FormatDuration(snapshot.Overtime)}",
            TimerPhase.Completed => "Completed",
            _ => "Idle",
        };

        string tooltip = $"ZenTimeBox | {timerLabel} | {schemeLabel} | {themeLabel} | {(dpiScale * 100):0}% | {iconPixels}px";
        return tooltip.Length <= 63 ? tooltip : tooltip[..63];
    }

    private static string FormatDuration(TimeSpan duration)
    {
        int totalMinutes = Math.Max(0, (int)Math.Floor(duration.TotalMinutes));
        int seconds = Math.Max(0, duration.Seconds);
        return $"{totalMinutes:0}:{seconds:00}";
    }

    private static float GetDpiScale()
    {
        using Graphics graphics = Graphics.FromHwnd(IntPtr.Zero);
        return graphics.DpiX / 96f;
    }

    private static Size GetIconSize(float dpiScale)
    {
        int size = Math.Clamp((int)Math.Round(16 * dpiScale), 16, 32);
        return new Size(size, size);
    }
}
