using System.Drawing;

namespace ZenTimeBox.Demo;

internal enum DemoSchemeId
{
    SegoeBold,
    TahomaBold,
}

internal enum ThemeMode
{
    Auto,
    Dark,
    Light,
}

internal enum StateColor
{
    Focus,
    Warn,
    Critical,
}

internal enum TrayIconVisualMode
{
    Logo,
    Text,
}

internal enum TimerPhase
{
    Idle,
    Running,
    Completed,
}

internal sealed record DemoRenderRequest(
    string? DisplayText,
    TrayIconVisualMode VisualMode,
    DemoSchemeId SchemeId,
    ThemeMode ThemeMode,
    StateColor StateColor,
    bool ShowSecondBorder,
    double SecondBorderRatio,
    float DpiScale,
    Size IconSize);

internal sealed record ThemePalette(
    Color Foreground,
    Color Outline,
    Color BorderProgress,
    Color BorderTrack,
    Color TransparentColor);

internal sealed record TimerSession(
    int DurationMinutes,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset EndAtUtc);

internal sealed record TimerSnapshot(
    TimerPhase Phase,
    string? DisplayText,
    TrayIconVisualMode VisualMode,
    StateColor StateColor,
    TimeSpan TotalDuration,
    TimeSpan Elapsed,
    TimeSpan Remaining,
    bool ShowSecondBorder,
    double SecondBorderRatio,
    int? LastStartedMinutes,
    bool CompletedThisTick);
