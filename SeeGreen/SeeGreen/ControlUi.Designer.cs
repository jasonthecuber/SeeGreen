namespace SeeGreen;

partial class ControlUi
{
   private System.ComponentModel.IContainer components = null;
   private PictureBox magPictureBox;

   protected override void Dispose(bool disposing)
   {
      if (disposing)
      {
         components?.Dispose();
         var img = magPictureBox?.Image;
         if (magPictureBox != null) magPictureBox.Image = null;
         img?.Dispose();
      }
      base.Dispose(disposing);
   }

   private void InitializeComponent()
   {
      components = new System.ComponentModel.Container();
      Text = "SeeGreen Magnifier View";
      FormBorderStyle = FormBorderStyle.Sizable;
      StartPosition = FormStartPosition.CenterScreen;
      MinimumSize = new Size(300, 200);

      magPictureBox = new PictureBox
      {
         Width = 600,
         Height = 400,
         BorderStyle = BorderStyle.None, // remove border to eliminate black edge
         Anchor = AnchorStyles.Top | AnchorStyles.Left
      };

      Controls.Add(magPictureBox);
   }
}