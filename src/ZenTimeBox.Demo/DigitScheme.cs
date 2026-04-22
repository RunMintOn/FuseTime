using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace ZenTimeBox.Demo;

internal interface IDigitScheme
{
    DemoSchemeId Id { get; }

    string DisplayName { get; }

    void Draw(Graphics graphics, Rectangle bounds, int number, ThemePalette palette);
}

internal static class DigitSchemes
{
    public static IReadOnlyList<IDigitScheme> All { get; } =
    [
        new FontDigitScheme(DemoSchemeId.SegoeBold, "Segoe Bold", "Segoe UI", FontStyle.Bold, singleScale: 1.15f, doubleScale: 0.90f, yOffset: -0.04f),
        new FontDigitScheme(DemoSchemeId.TahomaBold, "Tahoma Bold", "Tahoma", FontStyle.Bold, singleScale: 1.14f, doubleScale: 0.92f, yOffset: -0.05f),
    ];

    public static IDigitScheme Get(DemoSchemeId id) => All.First(scheme => scheme.Id == id);
}

internal sealed class FontDigitScheme(
    DemoSchemeId id,
    string displayName,
    string familyName,
    FontStyle fontStyle,
    float singleScale,
    float doubleScale,
    float yOffset) : IDigitScheme
{
    public DemoSchemeId Id => id;

    public string DisplayName => displayName;

    public void Draw(Graphics graphics, Rectangle bounds, int number, ThemePalette palette)
    {
        string text = number.ToString();
        bool singleDigit = text.Length == 1;
        float fontSize = bounds.Height * (singleDigit ? singleScale : doubleScale);

        using Font font = new(familyName, fontSize, fontStyle, GraphicsUnit.Pixel);
        using GraphicsPath outlinePath = new();

        StringFormat format = StringFormat.GenericTypographic;
        format.Alignment = StringAlignment.Center;
        format.LineAlignment = StringAlignment.Center;

        PointF textOrigin = new(bounds.Left + bounds.Width / 2f, bounds.Top + bounds.Height * (0.5f + yOffset));
        float emSize = graphics.DpiY * font.SizeInPoints / 72f;
        outlinePath.AddString(text, font.FontFamily, (int)font.Style, emSize, textOrigin, format);

        RectangleF stringBounds = outlinePath.GetBounds();
        using Matrix translate = new();
        translate.Translate(
            bounds.Left + (bounds.Width - stringBounds.Width) / 2f - stringBounds.Left,
            bounds.Top + (bounds.Height - stringBounds.Height) / 2f - stringBounds.Top + (bounds.Height * yOffset));
        outlinePath.Transform(translate);

        using Pen pen = new(palette.Outline, Math.Max(1f, bounds.Width / 10f)) { LineJoin = LineJoin.Round };
        using SolidBrush brush = new(palette.Foreground);
        graphics.DrawPath(pen, outlinePath);
        graphics.FillPath(brush, outlinePath);
    }
}
