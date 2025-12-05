namespace SeeGreen;

public partial class ControlPanelForm : Form
{
   public event EventHandler<int>? ZoomChanged;
   public event EventHandler<bool>? ToggleMagnifierRequested;
   public event EventHandler? ScreenshotRequested;
   public event EventHandler<bool>? FollowCursorChanged;
   public event EventHandler<bool>? SmoothingChanged;
   public event EventHandler<bool>? CrosshairChanged;
   public event EventHandler<bool>? CaptureCrosshairChanged;
   public event EventHandler? ResetRequested;

   private readonly Preferences _prefs;

   private IntPtr _iconHandle = IntPtr.Zero;

   private MenuStrip? _menuStrip;
   private ToolStripMenuItem? _helpMenu;
   private ToolStripMenuItem? _aboutItem;

   public ControlPanelForm(Preferences prefs)
   {
      _prefs = prefs;
      InitializeComponent();

      EnsureAboutMenu();

      try
      {
         var (icon, hIcon) = AppIcon.Create(32);
         Icon = icon;
         _iconHandle = hIcon;
      }
      catch { }

      // Initialize from prefs
      trackZoom.Minimum = 1;
      trackZoom.Maximum = 20;
      trackZoom.Value = Math.Clamp(_prefs.ZoomFactor, trackZoom.Minimum, trackZoom.Maximum);
      lblZoomValue.Text = $"{trackZoom.Value}x";

      chkFollow.Checked = _prefs.FollowCursor;
      chkSmoothing.Checked = _prefs.Smoothing;
      chkCrosshair.Checked = _prefs.Crosshair;

      chkCaptureCrosshair.Checked = _prefs.CaptureCrosshair;
      chkCaptureCrosshair.Enabled = chkCrosshair.Checked;

      btnToggleMagnifier.Text = _prefs.MagnifierActive ? "Turn Off Magnifier (Ctrl+Shift+M)" : "Turn On Magnifier (Ctrl+Shift+M)";

      // Wire events
      trackZoom.ValueChanged += (s, e) =>
      {
         lblZoomValue.Text = $"{trackZoom.Value}x";
         ZoomChanged?.Invoke(this, trackZoom.Value);
      };
      chkFollow.CheckedChanged += (s, e) => FollowCursorChanged?.Invoke(this, chkFollow.Checked);
      chkSmoothing.CheckedChanged += (s, e) => SmoothingChanged?.Invoke(this, chkSmoothing.Checked);
      chkCrosshair.CheckedChanged += (s, e) =>
      {
         CrosshairChanged?.Invoke(this, chkCrosshair.Checked);
         chkCaptureCrosshair.Enabled = chkCrosshair.Checked;
      };
      chkCaptureCrosshair.CheckedChanged += (s, e) => CaptureCrosshairChanged?.Invoke(this, chkCaptureCrosshair.Checked);
      btnToggleMagnifier.Click += (s, e) => ToggleMagnifierRequested?.Invoke(this, !IsMagnifierOn());
      btnScreenshot.Click += (s, e) => ScreenshotRequested?.Invoke(this, EventArgs.Empty);
      btnReset.Click += (s, e) => ResetRequested?.Invoke(this, EventArgs.Empty);

      // Restore position
      StartPosition = FormStartPosition.Manual;
      Left = _prefs.PanelLeft;
      Top = _prefs.PanelTop;

      Move += (s, e) =>
      {
         _prefs.PanelLeft = Left;
         _prefs.PanelTop = Top;
         _prefs.Save();
      };

      // Closing the control panel exits the app
      FormClosed += (s, e) =>
      {
         AppIcon.DisposeIconHandle(_iconHandle);
         Application.Exit();
      };
   }

   public void SetMagnifierButtonState(bool on)
   {
      btnToggleMagnifier.Text = on ? "Turn Off Magnifier (Ctrl+Shift+M)" : "Turn On Magnifier (Ctrl+Shift+M)";
   }

   public void UpdateZoomLabel(int zoom)
   {
      trackZoom.Value = Math.Clamp(zoom, trackZoom.Minimum, trackZoom.Maximum);
      lblZoomValue.Text = $"{trackZoom.Value}x";
   }

   public void UpdateFollowSmoothingCrosshair(bool follow, bool smoothing, bool crosshair)
   {
      chkFollow.Checked = follow;
      chkSmoothing.Checked = smoothing;
      chkCrosshair.Checked = crosshair;
      chkCaptureCrosshair.Enabled = crosshair;
   }

   private void EnsureAboutMenu()
   {
      SuspendLayout();

      _menuStrip = Controls.OfType<MenuStrip>().FirstOrDefault();
      if (_menuStrip is null)
      {
         _menuStrip = new MenuStrip
         {
            Dock = DockStyle.Top,
            GripStyle = ToolStripGripStyle.Hidden,
            AutoSize = true,
            Padding = new Padding(0),
            RenderMode = ToolStripRenderMode.ManagerRenderMode,
            // Use standard control color to match form background and avoid gradients
            BackColor = SystemColors.Control,
            ForeColor = SystemColors.ControlText
         };

         // Flat renderer: no border and solid background (no gradient)
         _menuStrip.Renderer = new FlatToolStripRenderer();

         MainMenuStrip = _menuStrip;
         Controls.Add(_menuStrip);
         Controls.SetChildIndex(_menuStrip, 0);
      }
      else
      {
         _menuStrip.Dock = DockStyle.Top;
         _menuStrip.Padding = new Padding(0);
         _menuStrip.RenderMode = ToolStripRenderMode.ManagerRenderMode;
         _menuStrip.BackColor = SystemColors.Control;
         _menuStrip.ForeColor = SystemColors.ControlText;
         _menuStrip.Renderer = new FlatToolStripRenderer();
         MainMenuStrip = _menuStrip;
         Controls.SetChildIndex(_menuStrip, 0);
      }

      _helpMenu = _menuStrip.Items.OfType<ToolStripMenuItem>().FirstOrDefault(i => i.Text == "Help");
      if (_helpMenu is null)
      {
         _helpMenu = new ToolStripMenuItem("Help");
         _menuStrip.Items.Add(_helpMenu);
      }

      _aboutItem = _helpMenu.DropDownItems.OfType<ToolStripMenuItem>().FirstOrDefault(i => i.Text == "About SeeGreen");
      if (_aboutItem is null)
      {
         _aboutItem = new ToolStripMenuItem("About SeeGreen");
         _aboutItem.Click += (s, e) =>
         {
            using var dlg = new AboutForm();
            dlg.ShowDialog(this);
         };
         _helpMenu.DropDownItems.Add(_aboutItem);
      }

      ResumeLayout(performLayout: true);
   }

   private bool IsMagnifierOn()
   {
      return btnToggleMagnifier.Text.StartsWith("Turn Off", StringComparison.OrdinalIgnoreCase);
   }
}

// Flat renderer: solid background and no border for the main MenuStrip,
// but draws a subtle border around ToolStripDropDown (submenu).
internal sealed class FlatToolStripRenderer : ToolStripProfessionalRenderer
{
   protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
   {
      // Draw border only for dropdown menus; keep main menu strip border suppressed
      if (e.ToolStrip is ToolStripDropDown dropDown)
      {
         using var pen = new Pen(SystemColors.ControlDark);
         var r = new Rectangle(Point.Empty, dropDown.Size);
         r.Width -= 1;
         r.Height -= 1;
         e.Graphics.DrawRectangle(pen, r);
      }
      // else: no border for top MenuStrip
   }

   protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
   {
      using var b = new SolidBrush(e.ToolStrip.BackColor);
      e.Graphics.FillRectangle(b, e.AffectedBounds);
   }

   protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
   {
      // Solid highlight without gradients
      var bg = e.Item.Selected ? SystemColors.Highlight : e.ToolStrip.BackColor;
      using var b = new SolidBrush(bg);
      e.Graphics.FillRectangle(b, new Rectangle(Point.Empty, e.Item.Bounds.Size));
      e.Item.ForeColor = e.Item.Selected ? SystemColors.HighlightText : SystemColors.ControlText;
   }
}