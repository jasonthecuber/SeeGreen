using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SeeGreen;

public static class AppIconExporter
{
    // Export multi-size ICO composed from independently rendered PNGs to preserve quality.
    public static void ExportMultiSizeIco(string icoPath, int[] sizes = null!)
    {
        sizes ??= new[] { 16, 24, 32, 48, 64, 128, 256 };

        // Render each size as a native bitmap and encode as PNG
        var pngBlobs = new List<(int size, byte[] png)>();
        foreach (var size in sizes)
        {
            using var bmp = RenderIconBitmap(size);
            using var ms = new MemoryStream();
            var encoder = GetPngEncoder();
            var paramsEnc = new EncoderParameters(1);
            paramsEnc.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.ColorDepth, 32L);
            bmp.Save(ms, encoder, paramsEnc);
            pngBlobs.Add((size, ms.ToArray()));
        }

        // Build ICO file with PNG-compressed images (Vista+)
        using var fs = new FileStream(icoPath, FileMode.Create, FileAccess.Write);
        WritePngIco(fs, pngBlobs);
    }

    // Export individual PNGs (optional, for inspection)
    public static void ExportPngs(string folder, int[] sizes = null!)
    {
        Directory.CreateDirectory(folder);
        sizes ??= new[] { 16, 24, 32, 48, 64, 128, 256 };
        foreach (var size in sizes)
        {
            using var bmp = RenderIconBitmap(size);
            var path = Path.Combine(folder, $"AppMagnifier_{size}.png");
            var encoder = GetPngEncoder();
            var paramsEnc = new EncoderParameters(1);
            paramsEnc.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.ColorDepth, 32L);
            bmp.Save(path, encoder, paramsEnc);
        }
    }

    // Render at target size with high-quality settings and 96 DPI
    private static Bitmap RenderIconBitmap(int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        bmp.SetResolution(96, 96);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        // Draw using the same logic as AppIcon.Create but targeting 'size'
        DrawMagnifierIcon(g, size);
        return bmp;
    }

    // Reuses the AppIcon drawing but targets Graphics at the specified size
    private static void DrawMagnifierIcon(Graphics g, int size)
    {
        // Keep this in sync with AppIcon.Create’s drawing for consistent visuals
        var margin = (int)Math.Round(size * 0.1);
        var screenRect = new Rectangle(margin, margin, size - margin * 2, size - margin * 2);

        int radius = (int)(size * 0.12);
        using (var screenBorder = new Pen(Color.FromArgb(220, 180, 200, 210), Math.Max(1f, size * 0.05f)))
        using (var screenFill = new SolidBrush(Color.FromArgb(255, 25, 25, 30)))
        {
            g.FillRoundedRect(screenFill, screenRect, radius);

            var gridPadding = (int)Math.Round(size * 0.06);
            var gridRect = Rectangle.Inflate(screenRect, -gridPadding, -gridPadding);

            int cols = 3;
            int rows = 2;
            int cellW = gridRect.Width / cols;
            int cellH = gridRect.Height / rows;

            Color[] palette =
            {
                Color.FromArgb(255, 76, 175, 80),
                Color.FromArgb(255, 33, 150, 243),
                Color.FromArgb(255, 255, 193, 7),
                Color.FromArgb(255, 244, 67, 54),
                Color.FromArgb(255, 156, 39, 176),
                Color.FromArgb(255, 0, 188, 212)
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

                    using var cellFill = new System.Drawing.Drawing2D.LinearGradientBrush(
                        cell,
                        palette[pi % palette.Length],
                        ControlPaint.Light(palette[pi % palette.Length]),
                        System.Drawing.Drawing2D.LinearGradientMode.Vertical);
                    g.FillRectangle(cellFill, cell);
                    pi++;
                }
            }

            g.DrawRoundedRect(screenBorder, screenRect, radius);
        }

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

        float lensRadius = size * 0.26f;
        var lensCenter = new PointF(zoomRect.Right - lensRadius * 0.65f, zoomRect.Bottom - lensRadius * 0.65f);
        var lensRect = new RectangleF(lensCenter.X - lensRadius, lensCenter.Y - lensRadius, lensRadius * 2, lensRadius * 2);

        using var lensFill2 = new SolidBrush(Color.FromArgb(240, 245, 255));
        using var lensOutline = new Pen(Color.FromArgb(40, 40, 40), Math.Max(1f, size * 0.06f));
        using var lensHighlight = new Pen(Color.FromArgb(120, 255, 255, 255), Math.Max(1f, size * 0.03f));

        g.FillEllipse(lensFill2, lensRect);
        g.DrawEllipse(lensOutline, lensRect);

        var arcRect = new RectangleF(lensRect.X + lensRect.Width * 0.12f, lensRect.Y + lensRect.Height * 0.12f, lensRect.Width * 0.76f, lensRect.Height * 0.76f);
        g.DrawArc(lensHighlight, arcRect, 210, 80);

        var handleLen = size * 0.38f;
        var handleWidth = Math.Max(2f, size * 0.09f);
        var handleStart = new PointF(lensCenter.X + lensRadius * 0.6f, lensCenter.Y + lensRadius * 0.6f);
        var handleEnd = new PointF(handleStart.X + handleLen * 0.75f, handleStart.Y + handleLen * 0.75f);

        using var handlePen = new Pen(Color.FromArgb(40, 40, 40), handleWidth)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round
        };
        g.DrawLine(handlePen, handleStart, handleEnd);

        using var crossPen = new Pen(Color.Lime, Math.Max(1f, size * 0.06f));
        int cx = zoomRect.Left + zoomRect.Width / 2;
        int cy = zoomRect.Top + zoomRect.Height / 2;
        g.DrawLine(crossPen, cx, zoomRect.Top + (int)(size * 0.02f), cx, zoomRect.Bottom - (int)(size * 0.02f));
        g.DrawLine(crossPen, zoomRect.Left + (int)(size * 0.02f), cy, zoomRect.Right - (int)(size * 0.02f), cy);
    }

    private static ImageCodecInfo GetPngEncoder()
    {
        return ImageCodecInfo.GetImageEncoders().First(e => e.MimeType == "image/png");
    }

    // ICO writer with PNG-compressed entries (required for sizes >= 256)
    private static void WritePngIco(Stream output, List<(int size, byte[] png)> images)
    {
        using var bw = new BinaryWriter(output, System.Text.Encoding.ASCII, leaveOpen: true);

        // ICONDIR (header)
        bw.Write((ushort)0);              // Reserved
        bw.Write((ushort)1);              // Type: 1 = icon
        bw.Write((ushort)images.Count);   // Image count

        // Placeholder for ICONDIRENTRY array
        long entriesPos = bw.BaseStream.Position;
        int entrySize = 16; // 16 bytes each
        bw.BaseStream.Position += images.Count * entrySize;

        var imageOffsets = new List<int>();
        var imageSizes = new List<int>();

        // Write PNG blobs and record offsets/sizes
        foreach (var img in images)
        {
            int offset = (int)bw.BaseStream.Position;
            bw.Write(img.png);
            int size = img.png.Length;

            imageOffsets.Add(offset);
            imageSizes.Add(size);
        }

        // Write ICONDIRENTRY with width/height (0 means 256 in ICO)
        bw.BaseStream.Position = entriesPos;
        for (int i = 0; i < images.Count; i++)
        {
            int w = images[i].size;
            int h = images[i].size;

            bw.Write((byte)(w == 256 ? 0 : w)); // width
            bw.Write((byte)(h == 256 ? 0 : h)); // height
            bw.Write((byte)0);                  // color count (0 = no palette)
            bw.Write((byte)0);                  // reserved
            bw.Write((ushort)1);                // planes
            bw.Write((ushort)32);               // bit count
            bw.Write(imageSizes[i]);            // bytes in resource
            bw.Write(imageOffsets[i]);          // offset
        }
    }
}