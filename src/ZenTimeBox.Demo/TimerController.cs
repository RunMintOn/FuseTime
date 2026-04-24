namespace ZenTimeBox.Demo;

internal sealed class TimerController
{
    private TimerSession? activeSession;
    private int? lastStartedMinutes;

    public void Start(int durationMinutes, DateTimeOffset now)
    {
        lastStartedMinutes = durationMinutes;
        activeSession = new TimerSession(
            DurationMinutes: durationMinutes,
            StartedAtUtc: now,
            EndAtUtc: now.AddMinutes(durationMinutes),
            CompletionRecorded: false);
    }

    public void Stop()
    {
        activeSession = null;
    }

    public bool Reset(DateTimeOffset now)
    {
        if (lastStartedMinutes is not int minutes)
        {
            return false;
        }

        Start(minutes, now);
        return true;
    }

    public TimerSnapshot GetSnapshot(DateTimeOffset now)
    {
        if (activeSession is null)
        {
            return new TimerSnapshot(
                Phase: TimerPhase.Idle,
                DisplayText: null,
                VisualMode: TrayIconVisualMode.Logo,
                StateColor: StateColor.Focus,
                TotalDuration: TimeSpan.Zero,
                Elapsed: TimeSpan.Zero,
                Remaining: TimeSpan.Zero,
                Overtime: TimeSpan.Zero,
                ShowSecondBorder: false,
                SecondBorderRatio: 0d,
                LastStartedMinutes: lastStartedMinutes,
                CompletedThisTick: false);
        }

        TimeSpan totalDuration = activeSession.EndAtUtc - activeSession.StartedAtUtc;
        TimeSpan remaining = activeSession.EndAtUtc - now;
        if (remaining <= TimeSpan.Zero)
        {
            TimeSpan overtime = now - activeSession.EndAtUtc;
            bool completedThisTick = !activeSession.CompletionRecorded;
            if (completedThisTick)
            {
                activeSession = activeSession with { CompletionRecorded = true };
            }

            int overtimeMinutes = Math.Max(1, (int)Math.Ceiling(overtime.TotalMinutes));
            return new TimerSnapshot(
                Phase: TimerPhase.Overtime,
                DisplayText: $"-{overtimeMinutes}",
                VisualMode: TrayIconVisualMode.Text,
                StateColor: StateColor.Overtime,
                TotalDuration: totalDuration,
                Elapsed: totalDuration + overtime,
                Remaining: TimeSpan.Zero,
                Overtime: overtime,
                ShowSecondBorder: false,
                SecondBorderRatio: 0d,
                LastStartedMinutes: lastStartedMinutes,
                CompletedThisTick: completedThisTick);
        }

        TimeSpan elapsed = totalDuration - remaining;
        bool isFinalMinute = remaining <= TimeSpan.FromMinutes(1);
        string displayText = isFinalMinute
            ? Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds)).ToString("00")
            : Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes)).ToString();

        StateColor stateColor = remaining <= TimeSpan.FromMinutes(1)
            ? StateColor.Critical
            : remaining <= TimeSpan.FromMinutes(5)
                ? StateColor.Warn
                : StateColor.Focus;

        bool showSecondBorder = !isFinalMinute;
        double secondBorderRatio = showSecondBorder
            ? GetMinuteCycleRatio(remaining)
            : 0d;

        return new TimerSnapshot(
            Phase: TimerPhase.Running,
            DisplayText: displayText,
            VisualMode: TrayIconVisualMode.Text,
            StateColor: stateColor,
            TotalDuration: totalDuration,
            Elapsed: elapsed,
            Remaining: remaining,
            Overtime: TimeSpan.Zero,
            ShowSecondBorder: showSecondBorder,
            SecondBorderRatio: secondBorderRatio,
            LastStartedMinutes: lastStartedMinutes,
            CompletedThisTick: false);
    }

    private static double GetMinuteCycleRatio(TimeSpan remaining)
    {
        double totalSeconds = Math.Max(0d, remaining.TotalSeconds);
        double secondsWithinMinute = totalSeconds % 60d;

        if (secondsWithinMinute <= double.Epsilon)
        {
            return 1d;
        }

        return Math.Clamp(secondsWithinMinute / 60d, 0d, 1d);
    }
}
