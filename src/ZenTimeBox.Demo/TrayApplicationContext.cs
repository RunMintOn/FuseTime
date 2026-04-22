using System.Drawing;

namespace ZenTimeBox.Demo;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private static readonly (int Minutes, string Label)[] Presets =
    [
        (15, "Start 15 min"),
        (25, "Start 25 min"),
        (45, "Start 45 min"),
        (60, "Start 60 min"),
    ];

    private readonly NotifyIcon notifyIcon;
    private readonly System.Windows.Forms.Timer uiTimer;
    private readonly TrayIconRenderer renderer = new();
    private readonly TimerController timerController = new();
    private readonly CompletionNotificationService notificationService = new();
    private readonly Dictionary<DemoSchemeId, ToolStripMenuItem> schemeItems = [];
    private readonly Dictionary<ThemeMode, ToolStripMenuItem> themeItems = [];
    private readonly List<ToolStripMenuItem> presetItems = [];
    private ToolStripMenuItem stopItem = null!;
    private ToolStripMenuItem resetItem = null!;

    private DemoSchemeId currentScheme = DemoSchemeId.SegoeBold;
    private ThemeMode currentThemeMode = ThemeMode.Auto;

    public TrayApplicationContext()
    {
        uiTimer = new System.Windows.Forms.Timer { Interval = 250 };
        uiTimer.Tick += (_, _) => RefreshFromClock();

        ContextMenuStrip contextMenu = BuildMenu();

        notifyIcon = new NotifyIcon
        {
            Visible = true,
            ContextMenuStrip = contextMenu,
            Text = "ZenTimeBox Demo",
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
            renderer.Dispose();
        }

        base.Dispose(disposing);
    }

    private ContextMenuStrip BuildMenu()
    {
        ContextMenuStrip menu = new();
        foreach ((int minutes, string label) in Presets)
        {
            ToolStripMenuItem item = new(label, null, (_, _) => StartPreset(minutes));
            presetItems.Add(item);
            menu.Items.Add(item);
        }

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
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(rendererMenu);
        menu.Items.Add(themeMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        UpdateMenuState(timerController.GetSnapshot(DateTimeOffset.UtcNow));
        return menu;
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

    private void RefreshFromClock()
    {
        TimerSnapshot snapshot = timerController.GetSnapshot(DateTimeOffset.UtcNow);
        if (snapshot.CompletedThisTick)
        {
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

        notifyIcon.Icon = renderer.Render(request);
        notifyIcon.Text = BuildTooltip(snapshot, dpiScale);
        UpdateMenuState(snapshot);
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

        bool hasActiveTimer = snapshot.Phase == TimerPhase.Running;
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
            TimerPhase.Completed => "Completed",
            _ => "Idle",
        };

        return $"ZenTimeBox | {timerLabel} | {schemeLabel} | {themeLabel} | {(dpiScale * 100):0}% | {iconPixels}px";
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
