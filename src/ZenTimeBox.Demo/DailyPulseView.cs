using System.Drawing;
using System.Drawing.Drawing2D;

namespace ZenTimeBox.Demo;

internal sealed class DailyPulseView : Control
{
    private DailyPulseSnapshot snapshot = new(DateOnly.FromDateTime(DateTime.Now), DateTime.Now.Hour, new bool[24], 0);
    private ThemeMode themeMode = ThemeMode.Auto;
    private readonly ThemeResolver themeResolver = new();

    public DailyPulseView()
    {
        DoubleBuffered = true;
        Margin = Padding.Empty;
        Padding = Padding.Empty;
        MinimumSize = new Size(228, 48);
        Size = MinimumSize;
        TabStop = false;
    }

    public void UpdateSnapshot(DailyPulseSnapshot nextSnapshot, ThemeMode nextThemeMode)
    {
        snapshot = nextSnapshot;
        themeMode = nextThemeMode;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        bool dark = IsDarkMode();
        Color back = dark ? Color.FromArgb(32, 35, 40) : Color.FromArgb(248, 249, 251);
        Color border = dark ? Color.FromArgb(61, 68, 76) : Color.FromArgb(220, 224, 230);
        Color title = dark ? Color.FromArgb(246, 248, 252) : Color.FromArgb(23, 27, 33);
        Color muted = dark ? Color.FromArgb(160, 168, 178) : Color.FromArgb(102, 111, 123);
        Color inactive = dark ? Color.FromArgb(64, 70, 78) : Color.FromArgb(218, 223, 230);
        Color active = dark ? Color.FromArgb(95, 211, 137) : Color.FromArgb(21, 139, 74);
        Color current = dark ? Color.FromArgb(255, 229, 128) : Color.FromArgb(177, 108, 0);

        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(SystemColors.Menu);

        Rectangle card = new(6, 5, Width - 12, Height - 10);
        using GraphicsPath cardPath = RoundedRectangle(card, 6);
        using SolidBrush backBrush = new(back);
        using Pen borderPen = new(border);
        graphics.FillPath(backBrush, cardPath);
        graphics.DrawPath(borderPen, cardPath);

        using Font titleFont = new(Font.FontFamily, 8.5f, FontStyle.Bold);
        using Font metaFont = new(Font.FontFamily, 7.2f, FontStyle.Regular);
        using SolidBrush titleBrush = new(title);
        using SolidBrush mutedBrush = new(muted);

        graphics.DrawString("Today", titleFont, titleBrush, card.Left + 10, card.Top + 6);
        string count = $"{snapshot.CompletionCount} done";
        SizeF countSize = graphics.MeasureString(count, metaFont);
        graphics.DrawString(count, metaFont, mutedBrush, card.Right - countSize.Width - 10, card.Top + 7);

        int dotSize = 5;
        int gap = 3;
        int totalWidth = (24 * dotSize) + (23 * gap);
        int startX = card.Left + Math.Max(10, (card.Width - totalWidth) / 2);
        int y = card.Top + 29;

        for (int hour = 0; hour < 24; hour++)
        {
            Rectangle dot = new(startX + hour * (dotSize + gap), y, dotSize, dotSize);
            Color dotColor = snapshot.CompletedHours.ElementAtOrDefault(hour) ? active : inactive;
            using SolidBrush dotBrush = new(dotColor);
            graphics.FillRectangle(dotBrush, dot);

            if (hour == snapshot.CurrentHour)
            {
                using Pen currentPen = new(current);
                graphics.DrawRectangle(currentPen, Rectangle.Inflate(dot, 1, 1));
            }
        }
    }

    private bool IsDarkMode()
    {
        ThemePalette palette = themeResolver.Resolve(themeMode, StateColor.Focus);
        return palette.Foreground.GetBrightness() > palette.Outline.GetBrightness();
    }

    private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        GraphicsPath path = new();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
