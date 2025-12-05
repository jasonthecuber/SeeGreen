namespace SeeGreen;

public sealed class AboutForm : Form
{
   private readonly LinkLabel _link;
   private readonly Label _title;
   private readonly Label _version;
   private readonly Label _desc;
   private readonly Label _dev;
   private readonly Button _ok;

   public AboutForm()
   {
      Text = "About SeeGreen";
      FormBorderStyle = FormBorderStyle.FixedDialog;
      MaximizeBox = false;
      MinimizeBox = false;
      ShowInTaskbar = false;
      StartPosition = FormStartPosition.CenterParent;
      Padding = new Padding(12);
      AutoScaleMode = AutoScaleMode.Font;
      ClientSize = new Size(440, 280);

      // Title
      _title = new Label
      {
         AutoSize = false,
         Font = new Font(Font.FontFamily, 12f, FontStyle.Bold),
         Text = "SeeGreen Magnifier",
         TextAlign = ContentAlignment.MiddleLeft, // left-aligned
         Size = new Size(ClientSize.Width - 24, 28)
      };

      // Version from assembly
      var asm = System.Reflection.Assembly.GetExecutingAssembly();
      var version = asm.GetName().Version?.ToString() ?? "1.0.0";
      var infoVer = asm.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
                       .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
                       .FirstOrDefault()?.InformationalVersion;
      var versionText = infoVer ?? version;

      _version = new Label
      {
         AutoSize = false,
         Text = $"Version: {versionText}",
         TextAlign = ContentAlignment.MiddleLeft, // left-aligned
         Size = new Size(ClientSize.Width - 24, 20)
      };

      // Description
      _desc = new Label
      {
         AutoSize = false,
         Size = new Size(ClientSize.Width - 24, 80),
         TextAlign = ContentAlignment.MiddleLeft, // left-aligned
         Text = "SeeGreen is a screen magnifier that lets you zoom into areas of your screen, " +
                "follow the mouse cursor, take screenshots, and overlay a green crosshair for precise focusing. " +
                "Smoothing and other preferences can be customized and saved between sessions."
      };

      // Link
      _link = new LinkLabel
      {
         AutoSize = false,
         Size = new Size(ClientSize.Width - 24, 22),
         Text = "SeeGreen on GitHub",
         TextAlign = ContentAlignment.MiddleLeft, // left-aligned
         LinkColor = Color.RoyalBlue
      };
      _link.Links.Clear();
      var url = "https://github.com/jasonthecuber/SeeGreen";
      _link.Links.Add(0, _link.Text.Length, url);
      _link.LinkClicked += (s, e) =>
      {
         try
         {
            var target = e.Link.LinkData?.ToString();
            if (!string.IsNullOrWhiteSpace(target))
            {
               System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
               {
                  FileName = target,
                  UseShellExecute = true
               });
            }
         }
         catch (Exception ex)
         {
            MessageBox.Show(this, $"Failed to open link.\n\n{ex.Message}", "Open Link", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      };

      // Developer info
      _dev = new Label
      {
         AutoSize = false,
         Size = new Size(ClientSize.Width - 24, 20),
         TextAlign = ContentAlignment.MiddleLeft, // left-aligned
         Text = "Developer: Jason Green"
      };

      // OK button
      _ok = new Button
      {
         Text = "OK",
         DialogResult = DialogResult.OK,
         Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
         Size = new Size(88, 28),
         Location = new Point(ClientSize.Width - 12 - 88, ClientSize.Height - 12 - 28)
      };

      AcceptButton = _ok;

      // Add controls first to compute layout
      Controls.Add(_title);
      Controls.Add(_version);
      Controls.Add(_desc);
      Controls.Add(_link);
      Controls.Add(_dev);
      Controls.Add(_ok);

      // Vertically center the stacked content (title, version, description, link, developer)
      // Compute combined height including gaps
      const int gap = 8;
      var totalHeight =
         _title.Height +
         gap +
         _version.Height +
         gap +
         _desc.Height +
         gap +
         _link.Height +
         gap +
         _dev.Height;

      // available vertical area excludes padding and OK button area
      var availableBottom = ClientSize.Height - Padding.Bottom - (_ok.Height + 12);
      var availableTop = Padding.Top + 12;
      var availableHeight = availableBottom - availableTop;

      var startY = availableTop + Math.Max(0, (availableHeight - totalHeight) / 2);

      // Position left using Padding.Left
      var left = Padding.Left;

      _title.Location = new Point(left, startY);
      _version.Location = new Point(left, _title.Bottom + gap);
      _desc.Location = new Point(left, _version.Bottom + gap);
      _link.Location = new Point(left, _desc.Bottom + gap);
      _dev.Location = new Point(left, _link.Bottom + gap);
   }
}