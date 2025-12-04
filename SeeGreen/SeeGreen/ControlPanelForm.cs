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

    public ControlPanelForm(Preferences prefs)
    {
        _prefs = prefs;
        InitializeComponent();

        // Initialize from prefs
        trackZoom.Minimum = 1;
        trackZoom.Maximum = 20;
        trackZoom.Value = Math.Clamp(_prefs.ZoomFactor, trackZoom.Minimum, trackZoom.Maximum);
        lblZoomValue.Text = $"{trackZoom.Value}x";

        chkFollow.Checked = _prefs.FollowCursor;
        chkSmoothing.Checked = _prefs.Smoothing;
        chkCrosshair.Checked = _prefs.Crosshair;

        chkCaptureCrosshair.Checked = _prefs.CaptureCrosshair;
        chkCaptureCrosshair.Enabled = chkCrosshair.Checked; // disable if crosshair not shown

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
            // Disable capture-crosshair when crosshair is hidden, but keep the checked state
            chkCaptureCrosshair.Enabled = chkCrosshair.Checked;
        };
        // Keep capture checkbox state; only its Enabled changes
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
        FormClosed += (s, e) => Application.Exit();
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

    private bool IsMagnifierOn() => btnToggleMagnifier.Text.StartsWith("Turn Off", StringComparison.Ordinal);
}