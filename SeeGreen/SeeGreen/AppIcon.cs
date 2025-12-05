using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace SeeGreen;

public static class AppIcon
{
    // Create a high-quality icon bitmap and convert to Icon
    // Returns both Icon and the native HICON handle so the caller can destroy it to prevent leaks.
    public static (Icon icon, IntPtr hIcon) Create(int size = 32)
    {
        size = Math.Clamp(size, 16, 64); // reasonable desktop icon sizes
        using var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            // Draw a colorful "screen" tile (grid of colored rectangles)
            var margin = (int)Math.Round(size * 0.1);
            var screenRect = new Rectangle(margin, margin, size - margin * 2, size - margin * 2);

            int radius = (int)(size * 0.12);
            using (var screenBorder = new Pen(Color.FromArgb(220, 180, 200, 210), Math.Max(1f, size * 0.05f)))
            {
                g.FillRoundedRect(new SolidBrush(Color.FromArgb(255, 25, 25, 30)), screenRect, radius);

                // Colorful grid inside screen to suggest a display
                var gridPadding = (int)Math.Round(size * 0.06);
                var gridRect = Rectangle.Inflate(screenRect, -gridPadding, -gridPadding);

                int cols = 3;
                int rows = 2;
                int cellW = gridRect.Width / cols;
                int cellH = gridRect.Height / rows;

                Color[] palette =
                {
                    Color.FromArgb(255, 76, 175, 80),   // green
                    Color.FromArgb(255, 33, 150, 243),  // blue
                    Color.FromArgb(255, 255, 193, 7),   // amber
                    Color.FromArgb(255, 244, 67, 54),   // red
                    Color.FromArgb(255, 156, 39, 176),  // purple
                    Color.FromArgb(255, 0, 188, 212)    // teal
                };

                int pi = 0;
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        var cell = new Rectangle(
                            gridRect.Left + c * cellW,
                            gridRect.Top + r * cellH,
                            c == cols - 1 ? gridRect.Right - (gridRect.Left + c * cellW) : cellW,
                            r == rows - 1 ? gridRect.Bottom - (gridRect.Top + r * cellH) : cellH);

                        using var cellFill = new LinearGradientBrush(
                            cell,
                            palette[pi % palette.Length],
                            ControlPaint.Light(palette[pi % palette.Length]),
                            LinearGradientMode.Vertical);
                        g.FillRectangle(cellFill, cell);
                        pi++;
                    }
                }

                // Outer rounded border on screen
                g.DrawRoundedRect(screenBorder, screenRect, radius);
            }

            // "Zoomed" subregion (slightly lighter square)
            var zoomRectSize = (int)Math.Round(size * 0.42);
            var zoomRect = new Rectangle(
                screenRect.Left + screenRect.Width / 2 - zoomRectSize / 2,
                screenRect.Top + screenRect.Height / 2 - zoomRectSize / 2,
                zoomRectSize, zoomRectSize);

            using (var zoomFill = new SolidBrush(Color.FromArgb(220, 235, 245, 255)))
            using (var zoomBorder = new Pen(Color.FromArgb(255, 30, 120, 210), Math.Max(1f, size * 0.04f)))
            {
                g.FillRectangle(zoomFill, zoomRect);
                g.DrawRectangle(zoomBorder, zoomRect);
            }

            // Magnifying glass: lens and handle
            float lensRadius = size * 0.26f;
            var lensCenter = new PointF(zoomRect.Right - lensRadius * 0.65f, zoomRect.Bottom - lensRadius * 0.65f);
            var lensRect = new RectangleF(lensCenter.X - lensRadius, lensCenter.Y - lensRadius, lensRadius * 2, lensRadius * 2);

            using var lensFill = new SolidBrush(Color.FromArgb(240, 245, 255));
            using var lensOutline = new Pen(Color.FromArgb(40, 40, 40), Math.Max(1f, size * 0.06f));
            using var lensHighlight = new Pen(Color.FromArgb(120, 255, 255, 255), Math.Max(1f, size * 0.03f));

            g.FillEllipse(lensFill, lensRect);
            g.DrawEllipse(lensOutline, lensRect);

            // Highlight arc on lens
            var arcRect = new RectangleF(lensRect.X + lensRect.Width * 0.12f, lensRect.Y + lensRect.Height * 0.12f, lensRect.Width * 0.76f, lensRect.Height * 0.76f);
            g.DrawArc(lensHighlight, arcRect, 210, 80);

            // Handle
            var handleLen = size * 0.38f;
            var handleWidth = Math.Max(2f, size * 0.09f);
            var handleStart = new PointF(lensCenter.X + lensRadius * 0.6f, lensCenter.Y + lensRadius * 0.6f);
            var handleEnd = new PointF(handleStart.X + handleLen * 0.75f, handleStart.Y + handleLen * 0.75f);

            using var handlePen = new Pen(Color.FromArgb(40, 40, 40), handleWidth)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            g.DrawLine(handlePen, handleStart, handleEnd);

            // Lime crosshair inside zoomRect for recognizability at small sizes
            using var crossPen = new Pen(Color.Lime, Math.Max(1f, size * 0.06f));
            int cx = zoomRect.Left + zoomRect.Width / 2;
            int cy = zoomRect.Top + zoomRect.Height / 2;
            g.DrawLine(crossPen, cx, zoomRect.Top + (int)(size * 0.02f), cx, zoomRect.Bottom - (int)(size * 0.02f));
            g.DrawLine(crossPen, zoomRect.Left + (int)(size * 0.02f), cy, zoomRect.Right - (int)(size * 0.02f), cy);
        }

        // Convert to Icon and return its handle so caller can DestroyIcon when done.
        var hIcon = bmp.GetHicon();
        var icon = Icon.FromHandle(hIcon);
        return (icon, hIcon);
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    // Helpers for rounded rectangles
    private static GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        var arc = new Rectangle(rect.Left, rect.Top, d, d);
        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - d;
        path.AddArc(arc, 270, 90);
        arc.Y = rect.Bottom - d;
        path.AddArc(arc, 0, 90);
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void FillRoundedRect(this Graphics g, Brush brush, Rectangle rect, int radius)
    {
        using var path = RoundedRect(rect, radius);
        g.FillPath(brush, path);
    }

    public static void DrawRoundedRect(this Graphics g, Pen pen, Rectangle rect, int radius)
    {
        using var path = RoundedRect(rect, radius);
        g.DrawPath(pen, path);
    }

    // Convenience to safely dispose icon created from handle
    public static void DisposeIconHandle(IntPtr hIcon)
    {
        if (hIcon != IntPtr.Zero)
        {
            DestroyIcon(hIcon);
        }
    }
}