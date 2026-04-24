using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace ZenTimeBox.Demo;

internal sealed class TrayIconRenderer : IDisposable
{
    private readonly ThemeResolver themeResolver = new();

    public Icon Render(DemoRenderRequest request)
    {
        return CreateIcon(request);
    }

    public void Dispose()
    {
        // No cached icons to release.
    }

    private Icon CreateIcon(DemoRenderRequest request)
    {
        ThemePalette palette = themeResolver.Resolve(request.ThemeMode, request.StateColor);
        using Bitmap bitmap = new(request.IconSize.Width, request.IconSize.Height);
        bitmap.MakeTransparent();

        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        graphics.Clear(palette.TransparentColor);
        if (palette.Background.A > 0)
        {
            using SolidBrush backgroundBrush = new(palette.Background);
            graphics.FillRectangle(backgroundBrush, 0, 0, request.IconSize.Width, request.IconSize.Height);
        }

        Rectangle contentBounds = new(
            x: 0,
            y: 0,
            width: request.IconSize.Width,
            height: request.IconSize.Height);

        if (request.VisualMode == TrayIconVisualMode.Logo)
        {
            DrawLogo(graphics, contentBounds, palette);
        }
        else if (!string.IsNullOrWhiteSpace(request.DisplayText))
        {
            if (request.ShowSecondBorder)
            {
                DrawSecondBorder(graphics, contentBounds, request.SecondBorderRatio, palette);
            }

            DrawText(graphics, contentBounds, request, palette);
        }

        IntPtr iconHandle = bitmap.GetHicon();
        try
        {
            return (Icon)Icon.FromHandle(iconHandle).Clone();
        }
        finally
        {
            DestroyIcon(iconHandle);
        }
    }

    private static void DrawText(Graphics graphics, Rectangle bounds, DemoRenderRequest request, ThemePalette palette)
    {
        if (!int.TryParse(request.DisplayText, out int number))
        {
            return;
        }

        Rectangle textBounds = request.ShowSecondBorder
            ? Rectangle.Inflate(bounds, -1, -1)
            : bounds;

        DigitSchemes.Get(request.SchemeId).Draw(graphics, textBounds, number, palette);
    }

    private static void DrawSecondBorder(Graphics graphics, Rectangle bounds, double secondBorderRatio, ThemePalette palette)
    {
        List<Point> borderPath = BuildBorderPath(bounds);
        if (borderPath.Count == 0)
        {
            return;
        }

        int activeCount = Math.Clamp((int)Math.Ceiling(secondBorderRatio * borderPath.Count), 0, borderPath.Count);
        using SolidBrush trackBrush = new(palette.BorderTrack);
        using SolidBrush progressBrush = new(palette.BorderProgress);

        for (int index = 0; index < borderPath.Count; index++)
        {
            Point pixel = borderPath[index];
            graphics.FillRectangle(trackBrush, pixel.X, pixel.Y, 1, 1);

            if (index < activeCount)
            {
                graphics.FillRectangle(progressBrush, pixel.X, pixel.Y, 1, 1);
            }
        }
    }

    private static List<Point> BuildBorderPath(Rectangle bounds)
    {
        List<Point> path = [];
        int left = bounds.Left;
        int top = bounds.Top;
        int right = bounds.Right - 1;
        int bottom = bounds.Bottom - 1;
        int topMid = left + ((right - left) / 2);

        if (right <= left || bottom <= top)
        {
            return path;
        }

        for (int x = topMid; x >= left; x--)
        {
            path.Add(new Point(x, top));
        }

        for (int y = top + 1; y <= bottom; y++)
        {
            path.Add(new Point(left, y));
        }

        for (int x = left + 1; x <= right; x++)
        {
            path.Add(new Point(x, bottom));
        }

        for (int y = bottom - 1; y >= top; y--)
        {
            path.Add(new Point(right, y));
        }

        for (int x = right - 1; x > topMid; x--)
        {
            path.Add(new Point(x, top));
        }

        return path;
    }

    private static void DrawLogo(Graphics graphics, Rectangle bounds, ThemePalette palette)
    {
        int inset = Math.Max(1, bounds.Width / 8);
        Rectangle circleBounds = Rectangle.Inflate(bounds, -inset, -inset);

        using Pen ringPen = new(palette.Foreground, Math.Max(2f, bounds.Width / 6f));
        using Pen handPen = new(palette.Foreground, Math.Max(1.5f, bounds.Width / 7f))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };
        using SolidBrush centerBrush = new(palette.Foreground);

        graphics.DrawEllipse(ringPen, circleBounds);

        Point center = new(circleBounds.Left + (circleBounds.Width / 2), circleBounds.Top + (circleBounds.Height / 2));
        Point hourHand = new(center.X, center.Y - (circleBounds.Height / 4));
        Point minuteHand = new(center.X + (circleBounds.Width / 5), center.Y + 1);

        graphics.DrawLine(handPen, center, hourHand);
        graphics.DrawLine(handPen, center, minuteHand);
        graphics.FillEllipse(centerBrush, center.X - 1, center.Y - 1, 3, 3);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);
}
