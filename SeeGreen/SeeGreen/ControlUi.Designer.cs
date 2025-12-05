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
      magPictureBox = new PictureBox();
      ((System.ComponentModel.ISupportInitialize)magPictureBox).BeginInit();
      SuspendLayout();
      // 
      // magPictureBox
      // 
      magPictureBox.Location = new Point(0, 0);
      magPictureBox.Name = "magPictureBox";
      magPictureBox.Size = new Size(100, 50);
      magPictureBox.TabIndex = 0;
      magPictureBox.TabStop = false;
      // 
      // ControlUi
      // 
      ClientSize = new Size(284, 261);
      Controls.Add(magPictureBox);
      MinimumSize = new Size(300, 200);
      Name = "ControlUi";
      StartPosition = FormStartPosition.CenterScreen;
      Text = "SeeGreen Magnifier View";
      ((System.ComponentModel.ISupportInitialize)magPictureBox).EndInit();
      ResumeLayout(false);
   }
}