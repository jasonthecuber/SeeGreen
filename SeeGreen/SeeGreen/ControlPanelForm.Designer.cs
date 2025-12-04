namespace SeeGreen;

partial class ControlPanelForm
{
   private System.ComponentModel.IContainer components = null;
   private TrackBar trackZoom;
   private Label lblZoom;
   private Label lblZoomValue;
   private CheckBox chkFollow;
   private CheckBox chkSmoothing;
   private CheckBox chkCrosshair;
   private CheckBox chkCaptureCrosshair;
   private Button btnToggleMagnifier;
   private Button btnScreenshot;
   private Button btnReset;

   protected override void Dispose(bool disposing)
   {
      if (disposing)
      {
         components?.Dispose();
      }
      base.Dispose(disposing);
   }

   private void InitializeComponent()
   {
      components = new System.ComponentModel.Container();
      Text = "Magnifier Controls";
      FormBorderStyle = FormBorderStyle.SizableToolWindow;
      MinimumSize = new Size(360, 240);
      MaximizeBox = false;
      StartPosition = FormStartPosition.Manual;

      lblZoom = new Label { Text = "Zoom:", Left = 12, Top = 12, AutoSize = true };
      trackZoom = new TrackBar
      {
         Left = 12,
         Top = 32,
         Width = 260,
         TickStyle = TickStyle.BottomRight,
         Minimum = 1,
         Maximum = 20,
         Value = 2
      };
      lblZoomValue = new Label
      {
         Text = "2x",
         Left = trackZoom.Left + trackZoom.Width + 8,
         Top = trackZoom.Top + 4,
         AutoSize = true
      };

      chkFollow = new CheckBox { Text = "Follow Cursor", Left = 12, Top = trackZoom.Bottom + 12, AutoSize = true };
      chkSmoothing = new CheckBox { Text = "Smoothing (HighQuality)", Left = 12, Top = chkFollow.Bottom + 6, AutoSize = true };
      chkCrosshair = new CheckBox { Text = "Show Crosshair", Left = 12, Top = chkSmoothing.Bottom + 6, AutoSize = true };
      chkCaptureCrosshair = new CheckBox { Text = "Capture Crosshair", Left = chkCrosshair.Right + 12, Top = chkCrosshair.Top, AutoSize = true };

      btnToggleMagnifier = new Button { Text = "Turn On Magnifier (Ctrl+Shift+M)", Left = 12, Top = chkCrosshair.Bottom + 16, Width = 320 };
      btnScreenshot = new Button { Text = "Screenshot", Left = 12, Top = btnToggleMagnifier.Bottom + 8, Width = 150 };
      btnReset = new Button { Text = "Reset to Defaults", Left = btnScreenshot.Right + 8, Top = btnToggleMagnifier.Bottom + 8, Width = 160 };

      var bottom = btnReset.Bottom + 12;
      ClientSize = new Size(360, bottom);

      Controls.Add(lblZoom);
      Controls.Add(trackZoom);
      Controls.Add(lblZoomValue);
      Controls.Add(chkFollow);
      Controls.Add(chkSmoothing);
      Controls.Add(chkCrosshair);
      Controls.Add(chkCaptureCrosshair);
      Controls.Add(btnToggleMagnifier);
      Controls.Add(btnScreenshot);
      Controls.Add(btnReset);
   }
}