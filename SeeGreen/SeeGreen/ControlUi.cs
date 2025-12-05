using System.Runtime.InteropServices;
using Timer = System.Windows.Forms.Timer;

namespace SeeGreen;

public partial class ControlUi : Form
{
   private readonly Timer _captureTimer = new() { Interval = 30 }; // ~33 FPS
   private bool _magnifierActive = false;
   private int _zoomFactor = 2;
   private bool _followCursor = true;
   private bool _smoothing = true;
   private bool _showCrosshair = true;
   private bool _captureCrosshair = false;

   // Show "Magnifier Off" message only once per application run
   private bool _showOffMessageOnce = true;

   // Keep native icon handle to destroy it on close to prevent leaks (only when using runtime-generated icon)
   private IntPtr _iconHandle = IntPtr.Zero;

   private ControlPanelForm? _panel;
   private readonly Preferences _prefs = Preferences.Load();

   private Point _captureCenter;

   // Hotkey: Ctrl + Shift + M
   private const int HOTKEY_ID = 0xC0DE;
   private const uint MOD_CONTROL = 0x0002;
   private const uint MOD_SHIFT = 0x0004;
   private const uint WM_HOTKEY = 0x0312;
   private const uint VK_M = 0x4D;

   public ControlUi()
   {
      InitializeComponent();

      // Set runtime-generated icon(programmatic.ico) so it appears in title bar and taskbar
      //try
      //{
      //   var (icon, hIcon) = AppIcon.Create(32);
      //   Icon = icon;
      //   _iconHandle = hIcon;
      //}
      //catch
      //{
      //   // If generation fails, ignore and continue
      //}

      _zoomFactor = _prefs.ZoomFactor;
      _magnifierActive = _prefs.MagnifierActive;
      _followCursor = _prefs.FollowCursor;
      _smoothing = _prefs.Smoothing;
      _showCrosshair = _prefs.Crosshair;
      _captureCrosshair = _prefs.CaptureCrosshair;

      magPictureBox.BackColor = Color.Black;
      magPictureBox.SizeMode = PictureBoxSizeMode.StretchImage; // reliable fill rendering
      magPictureBox.BorderStyle = BorderStyle.None;
      magPictureBox.Cursor = Cursors.Cross;
      magPictureBox.Width = _prefs.MagnifierWidth;
      magPictureBox.Height = _prefs.MagnifierHeight;

      _captureCenter = Cursor.Position;

      _captureTimer.Tick += CaptureTimer_Tick;

      magPictureBox.Paint += MagPictureBox_Paint;
      magPictureBox.MouseDown += MagPictureBox_MouseDown;
      magPictureBox.MouseMove += MagPictureBox_MouseMove;
      magPictureBox.MouseUp += MagPictureBox_MouseUp;

      RegisterHotKey(Handle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, VK_M);

      _captureTimer.Enabled = _magnifierActive;

      ShowControlPanel();

      Text = "SeeGreen Magnifier View";
      FormBorderStyle = FormBorderStyle.Sizable;
      StartPosition = FormStartPosition.CenterScreen;
      ClientSize = new Size(magPictureBox.Width + 24, magPictureBox.Height + 24);
      magPictureBox.Location = new Point(12, 12);

      Resize += MagnifierForm_Resize;

      if (_magnifierActive)
      {
         CaptureTimer_Tick(this, EventArgs.Empty);
         _showOffMessageOnce = false; // if starting active, suppress off message for the rest of the session
      }
      else
      {
         magPictureBox.Invalidate();
      }
   }

   private void MagnifierForm_Resize(object? sender, EventArgs e)
   {
      const int padding = 12;
      var newWidth = Math.Max(100, ClientSize.Width - padding * 2);
      var newHeight = Math.Max(100, ClientSize.Height - padding * 2);

      magPictureBox.Location = new Point(padding, padding);
      magPictureBox.Size = new Size(newWidth, newHeight);

      _prefs.MagnifierWidth = magPictureBox.Width;
      _prefs.MagnifierHeight = magPictureBox.Height;
      _prefs.Save();

      if (_magnifierActive)
      {
         CaptureTimer_Tick(this, EventArgs.Empty);
      }
      else
      {
         // Update the off message scaling on resize
         magPictureBox.Invalidate();
      }
   }

   private void ShowControlPanel()
   {
      _panel = new ControlPanelForm(_prefs);
      _panel.ZoomChanged += (s, z) => { _zoomFactor = z; _prefs.ZoomFactor = z; _prefs.Save(); };
      _panel.ToggleMagnifierRequested += (s, on) => ToggleMagnifier(on);
      _panel.ScreenshotRequested += (s, e) => SaveScreenshot();
      _panel.FollowCursorChanged += (s, follow) =>
      {
         _followCursor = follow;
         _prefs.FollowCursor = follow;
         _prefs.Save();
         if (!_followCursor) _captureCenter = Cursor.Position;
      };
      _panel.SmoothingChanged += (s, smooth) => { _smoothing = smooth; _prefs.Smoothing = smooth; _prefs.Save(); };
      _panel.CrosshairChanged += (s, show) => { _showCrosshair = show; _prefs.Crosshair = show; _prefs.Save(); magPictureBox.Invalidate(); };
      _panel.CaptureCrosshairChanged += (s, capture) => { _captureCrosshair = capture; _prefs.CaptureCrosshair = capture; _prefs.Save(); };
      _panel.ResetRequested += (s, e) => ResetToDefaults();

      _panel.UpdateZoomLabel(_zoomFactor);
      _panel.UpdateFollowSmoothingCrosshair(_followCursor, _smoothing, _showCrosshair);
      _panel.SetMagnifierButtonState(_magnifierActive);

      _panel.FormClosed += (s, e) => Application.Exit();
      _panel.Show();
   }

   protected override void OnFormClosed(FormClosedEventArgs e)
   {
      base.OnFormClosed(e);
      _captureTimer.Stop();
      UnregisterHotKey(Handle, HOTKEY_ID);

      // Dispose magnified image
      var img = magPictureBox.Image;
      magPictureBox.Image = null;
      img?.Dispose();

      // Destroy native icon handle to avoid leaks if we created one
      if (_iconHandle != IntPtr.Zero)
      {
         AppIcon.DisposeIconHandle(_iconHandle);
         _iconHandle = IntPtr.Zero;
      }

      _prefs.MagnifierActive = _magnifierActive;
      _prefs.ZoomFactor = _zoomFactor;
      _prefs.FollowCursor = _followCursor;
      _prefs.Smoothing = _smoothing;
      _prefs.Crosshair = _showCrosshair;
      _prefs.MagnifierWidth = magPictureBox.Width;
      _prefs.MagnifierHeight = magPictureBox.Height;
      _prefs.Save();

      _panel?.Close();
   }

   protected override void WndProc(ref Message m)
   {
      base.WndProc(ref m);
      if (m.Msg == WM_HOTKEY && m.WParam == (IntPtr)HOTKEY_ID)
      {
         ToggleMagnifier(); // Ctrl+Shift+M toggles magnifier
      }
   }

   private void ToggleMagnifier(bool? active = null)
   {
      bool previouslyOn = _magnifierActive;

      _magnifierActive = active ?? !_magnifierActive;
      _captureTimer.Enabled = _magnifierActive;
      _panel?.SetMagnifierButtonState(_magnifierActive);

      _prefs.MagnifierActive = _magnifierActive;
      _prefs.Save();

      // Once the magnifier is turned on at least once, suppress the off message until restart
      if (!previouslyOn && _magnifierActive)
      {
         _showOffMessageOnce = false;
      }

      if (_magnifierActive)
      {
         CaptureTimer_Tick(this, EventArgs.Empty);
      }

      magPictureBox.Invalidate();
   }

   private void ResetToDefaults()
   {
      _prefs.ResetToDefaults();
      _prefs.Save();

      _zoomFactor = _prefs.ZoomFactor;
      _followCursor = _prefs.FollowCursor;
      _smoothing = _prefs.Smoothing;
      _showCrosshair = _prefs.Crosshair;
      _captureCrosshair = _prefs.CaptureCrosshair;

      _captureCenter = Cursor.Position;

      var desiredClient = new Size(_prefs.MagnifierWidth + 24, _prefs.MagnifierHeight + 24);
      ClientSize = desiredClient;

      if (_magnifierActive)
      {
         ToggleMagnifier(false);
      }
      else
      {
         _captureTimer.Enabled = false;
         _panel?.SetMagnifierButtonState(false);
      }

      _panel?.UpdateZoomLabel(_zoomFactor);
      _panel?.UpdateFollowSmoothingCrosshair(_followCursor, _smoothing, _showCrosshair);

      magPictureBox.Invalidate();
   }

   private void CaptureTimer_Tick(object? sender, EventArgs e)
   {
      if (!_magnifierActive) return;

      // Determine capture center
      Point sourceCenter = _followCursor ? Cursor.Position : _captureCenter;

      int srcWidth = Math.Max(16, magPictureBox.Width / _zoomFactor);
      int srcHeight = Math.Max(16, magPictureBox.Height / _zoomFactor);

      int srcLeft = sourceCenter.X - srcWidth / 2;
      int srcTop = sourceCenter.Y - srcHeight / 2;

      // Clamp to virtual screen to preserve size
      var vs = SystemInformation.VirtualScreen;
      srcLeft = Math.Min(srcLeft, vs.Right - srcWidth);
      srcTop = Math.Min(srcTop, vs.Bottom - srcHeight);
      srcLeft = Math.Max(srcLeft, vs.Left);
      srcTop = Math.Max(srcTop, vs.Top);

      var captureRect = new Rectangle(srcLeft, srcTop, srcWidth, srcHeight);

      try
      {
         using var srcBmp = new Bitmap(captureRect.Width, captureRect.Height);
         using (var g = Graphics.FromImage(srcBmp))
         {
            g.CopyFromScreen(captureRect.Location, Point.Empty, captureRect.Size);
         }

         var displayBmp = new Bitmap(magPictureBox.Width, magPictureBox.Height);
         using (var g = Graphics.FromImage(displayBmp))
         {
            if (_smoothing)
            {
               g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
               g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
               g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
               g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            }
            else
            {
               g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
               g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
               g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
               g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.AssumeLinear;
            }

            // Overdraw slightly to ensure full coverage, eliminating right/bottom black border
            var dest = new Rectangle(-1, -1, displayBmp.Width + 2, displayBmp.Height + 2);
            g.DrawImage(srcBmp, dest);
         }

         var oldImage = magPictureBox.Image;
         magPictureBox.Image = displayBmp;
         oldImage?.Dispose();
      }
      catch
      {
         // Keep previous image if capture fails
      }

      magPictureBox.Invalidate();
   }

   private void SaveScreenshot()
   {
      if (magPictureBox.Image is null)
      {
         MessageBox.Show("No magnified image available.", "Screenshot", MessageBoxButtons.OK, MessageBoxIcon.Information);
         return;
      }

      using var toSave = new Bitmap(magPictureBox.Image.Width, magPictureBox.Image.Height);
      using (var g = Graphics.FromImage(toSave))
      {
         g.DrawImage(magPictureBox.Image, new Rectangle(0, 0, toSave.Width, toSave.Height));
         if (_captureCrosshair && _showCrosshair)
         {
           using var pen = new Pen(Color.Lime, 1);
           int cx = toSave.Width / 2;
           int cy = toSave.Height / 2;
           g.DrawLine(pen, cx, 0, cx, toSave.Height);
           g.DrawLine(pen, 0, cy, toSave.Width, cy);
           using var circlePen = new Pen(Color.Lime, 1);
           g.DrawEllipse(circlePen, cx - 4, cy - 4, 8, 8);
         }
      }

      using var sfd = new SaveFileDialog
      {
         Filter = "PNG Image|*.png|JPEG Image|*.jpg;*.jpeg|BMP Image|*.bmp",
         FileName = $"Magnifier_{DateTime.Now:yyyyMMdd_HHmmss}.png",
         OverwritePrompt = true
      };

      if (sfd.ShowDialog(this) == DialogResult.OK)
      {
         var format = Path.GetExtension(sfd.FileName).ToLowerInvariant() switch
         {
            ".jpg" or ".jpeg" => System.Drawing.Imaging.ImageFormat.Jpeg,
            ".bmp" => System.Drawing.Imaging.ImageFormat.Bmp,
            _ => System.Drawing.Imaging.ImageFormat.Png
         };
         toSave.Save(sfd.FileName, format);
      }
   }

   private void MagPictureBox_Paint(object? sender, PaintEventArgs e)
   {
      var g = e.Graphics;

      // Show the "Magnifier Off" message only before the magnifier is ever turned on in this session
      if (!_magnifierActive && _showOffMessageOnce)
      {
         var client = magPictureBox.ClientRectangle;

         using (var bgBrush = new SolidBrush(Color.FromArgb(160, 32, 32, 32)))
         {
            g.FillRectangle(bgBrush, client);
         }

         string line1 = "Magnifier";
         string line2 = "Off";

         int pad = (int)Math.Max(6, Math.Min(client.Width, client.Height) * 0.04);
         var avail = Rectangle.Inflate(client, -pad, -pad);

         // Use StringFormat centered and no wrapping; we will fit by font and clamp width
         using var sf = new StringFormat(StringFormatFlags.NoClip)
         {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
         };

         // Binary search font size that fits width and height with controlled line spacing
         float minSize = 6f;
         float maxSize = 400f;
         float best = minSize;

         using var baseFont = new Font(FontFamily.GenericSansSerif, 12f, FontStyle.Bold, GraphicsUnit.Pixel);

         // Measure that respects very narrow widths by checking each line against avail.Width
         while (maxSize - minSize > 0.5f)
         {
            float mid = (minSize + maxSize) / 2f;
            using var testFont = new Font(FontFamily.GenericSansSerif, mid, FontStyle.Bold, GraphicsUnit.Pixel);

            var size1 = g.MeasureString(line1, testFont);
            var size2 = g.MeasureString(line2, testFont);

            // Controlled line spacing: 0.85 of the first line height
            float spacing = size1.Height * 0.85f;

            float totalH = size1.Height + spacing + size2.Height;
            float maxW = Math.Max(size1.Width, size2.Width);

            if (totalH <= avail.Height && maxW <= avail.Width)
            {
               best = mid;
               minSize = mid;
            }
            else
            {
               maxSize = mid;
            }
         }

         using var finalFont = new Font(FontFamily.GenericSansSerif, best, FontStyle.Bold, GraphicsUnit.Pixel);
         var sizeA = g.MeasureString(line1, finalFont);
         var sizeB = g.MeasureString(line2, finalFont);
         float spacingFinal = sizeA.Height * 0.85f;

         // Compute top-lefts to render centered, clamped within avail
         float totalHeight = sizeA.Height + spacingFinal + sizeB.Height;
         float startY = avail.Top + (avail.Height - totalHeight) / 2f;

         float x1 = avail.Left + (avail.Width - sizeA.Width) / 2f;
         float y1 = startY;

         float x2 = avail.Left + (avail.Width - sizeB.Width) / 2f;
         float y2 = startY + sizeA.Height + spacingFinal;

         // Clamp to ensure visibility in very tall/narrow windows
         x1 = Math.Max(avail.Left, Math.Min(x1, avail.Right - sizeA.Width));
         x2 = Math.Max(avail.Left, Math.Min(x2, avail.Right - sizeB.Width));
         y1 = Math.Max(avail.Top, Math.Min(y1, avail.Bottom - (sizeA.Height + sizeB.Height + spacingFinal)));
         y2 = Math.Max(avail.Top + sizeA.Height * 0.5f, Math.Min(y2, avail.Bottom - sizeB.Height));

         // Local background behind the text
         var textBounds = Rectangle.FromLTRB(
            (int)Math.Min(x1, x2) - pad,
            (int)y1 - pad,
            (int)Math.Max(x1 + sizeA.Width, x2 + sizeB.Width) + pad,
            (int)(y2 + sizeB.Height + pad)
         );
         using (var localBg = new SolidBrush(Color.FromArgb(200, 0, 0, 0)))
         {
            g.FillRectangle(localBg, textBounds);
         }

         using var textBrush = new SolidBrush(Color.Lime);
         g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
         g.DrawString(line1, finalFont, textBrush, new PointF(x1, y1));
         g.DrawString(line2, finalFont, textBrush, new PointF(x2, y2));

         // No crosshair when magnifier is off
         return;
      }

      // Draw crosshair over magnified image when enabled
      if (!_showCrosshair) return;

      using var pen = new Pen(Color.Lime, 1);
      int cx = magPictureBox.Width / 2;
      int cy = magPictureBox.Height / 2;
      g.DrawLine(pen, cx, 0, cx, magPictureBox.Height);
      g.DrawLine(pen, 0, cy, magPictureBox.Width, cy);

      using var centerPen = new Pen(Color.Lime, 1);
      g.DrawEllipse(centerPen, cx - 4, cy - 4, 8, 8);
   }

   // Drag-to-move capture region when not following cursor
   private bool _dragging = false;
   private Point _dragStartScreen;

   private void MagPictureBox_MouseDown(object? sender, MouseEventArgs e)
   {
      if (e.Button == MouseButtons.Left && !_followCursor)
      {
         _dragging = true;
         _dragStartScreen = Cursor.Position;
      }
   }

   private void MagPictureBox_MouseMove(object? sender, MouseEventArgs e)
   {
      if (_dragging && !_followCursor)
      {
         var current = Cursor.Position;
         var dx = current.X - _dragStartScreen.X;
         var dy = current.Y - _dragStartScreen.Y;
         _captureCenter = new Point(_captureCenter.X + dx, _captureCenter.Y + dy);
         _dragStartScreen = current; // incremental drag
      }
   }

   private void MagPictureBox_MouseUp(object? sender, MouseEventArgs e)
   {
      if (e.Button == MouseButtons.Left)
      {
         _dragging = false;
      }
   }

   [DllImport("user32.dll")]
   private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

   [DllImport("user32.dll")]
   private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}