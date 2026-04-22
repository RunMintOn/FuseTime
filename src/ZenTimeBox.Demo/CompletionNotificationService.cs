namespace ZenTimeBox.Demo;

internal sealed class CompletionNotificationService
{
    public void ShowCompleted(NotifyIcon notifyIcon)
    {
        notifyIcon.BalloonTipTitle = "ZenTimeBox";
        notifyIcon.BalloonTipText = "Time box complete.";
        notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
        notifyIcon.ShowBalloonTip(5000);
    }
}
