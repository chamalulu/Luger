namespace WinForms
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.HorizontalSplitContainer = new System.Windows.Forms.SplitContainer();
            this.InputSplitContainer = new System.Windows.Forms.SplitContainer();
            this.OutputSplitContainer = new System.Windows.Forms.SplitContainer();
            this.ImageGroupBox = new System.Windows.Forms.GroupBox();
            this.SceneGroupBox = new System.Windows.Forms.GroupBox();
            this.ImagePictureBox = new System.Windows.Forms.PictureBox();
            this.ControlGroupBox = new System.Windows.Forms.GroupBox();
            this.ResolutionLabel = new System.Windows.Forms.Label();
            this.ResolutionComboBox = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.HorizontalSplitContainer)).BeginInit();
            this.HorizontalSplitContainer.Panel1.SuspendLayout();
            this.HorizontalSplitContainer.Panel2.SuspendLayout();
            this.HorizontalSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.InputSplitContainer)).BeginInit();
            this.InputSplitContainer.Panel1.SuspendLayout();
            this.InputSplitContainer.Panel2.SuspendLayout();
            this.InputSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.OutputSplitContainer)).BeginInit();
            this.OutputSplitContainer.Panel1.SuspendLayout();
            this.OutputSplitContainer.Panel2.SuspendLayout();
            this.OutputSplitContainer.SuspendLayout();
            this.ImageGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ImagePictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // HorizontalSplitContainer
            // 
            this.HorizontalSplitContainer.Cursor = System.Windows.Forms.Cursors.HSplit;
            this.HorizontalSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.HorizontalSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.HorizontalSplitContainer.Name = "HorizontalSplitContainer";
            this.HorizontalSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // HorizontalSplitContainer.Panel1
            // 
            this.HorizontalSplitContainer.Panel1.Controls.Add(this.InputSplitContainer);
            // 
            // HorizontalSplitContainer.Panel2
            // 
            this.HorizontalSplitContainer.Panel2.Controls.Add(this.OutputSplitContainer);
            this.HorizontalSplitContainer.Size = new System.Drawing.Size(800, 450);
            this.HorizontalSplitContainer.SplitterDistance = 171;
            this.HorizontalSplitContainer.TabIndex = 0;
            // 
            // InputSplitContainer
            // 
            this.InputSplitContainer.Cursor = System.Windows.Forms.Cursors.VSplit;
            this.InputSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.InputSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.InputSplitContainer.Name = "InputSplitContainer";
            // 
            // InputSplitContainer.Panel1
            // 
            this.InputSplitContainer.Panel1.Controls.Add(this.ImageGroupBox);
            // 
            // InputSplitContainer.Panel2
            // 
            this.InputSplitContainer.Panel2.Controls.Add(this.SceneGroupBox);
            this.InputSplitContainer.Size = new System.Drawing.Size(800, 171);
            this.InputSplitContainer.SplitterDistance = 195;
            this.InputSplitContainer.TabIndex = 0;
            // 
            // OutputSplitContainer
            // 
            this.OutputSplitContainer.Cursor = System.Windows.Forms.Cursors.VSplit;
            this.OutputSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.OutputSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.OutputSplitContainer.Name = "OutputSplitContainer";
            // 
            // OutputSplitContainer.Panel1
            // 
            this.OutputSplitContainer.Panel1.Controls.Add(this.ControlGroupBox);
            // 
            // OutputSplitContainer.Panel2
            // 
            this.OutputSplitContainer.Panel2.Controls.Add(this.ImagePictureBox);
            this.OutputSplitContainer.Size = new System.Drawing.Size(800, 275);
            this.OutputSplitContainer.SplitterDistance = 420;
            this.OutputSplitContainer.TabIndex = 0;
            // 
            // ImageGroupBox
            // 
            this.ImageGroupBox.Controls.Add(this.ResolutionComboBox);
            this.ImageGroupBox.Controls.Add(this.ResolutionLabel);
            this.ImageGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ImageGroupBox.Location = new System.Drawing.Point(0, 0);
            this.ImageGroupBox.Name = "ImageGroupBox";
            this.ImageGroupBox.Size = new System.Drawing.Size(195, 171);
            this.ImageGroupBox.TabIndex = 0;
            this.ImageGroupBox.TabStop = false;
            this.ImageGroupBox.Text = "Image";
            // 
            // SceneGroupBox
            // 
            this.SceneGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SceneGroupBox.Location = new System.Drawing.Point(0, 0);
            this.SceneGroupBox.Name = "SceneGroupBox";
            this.SceneGroupBox.Size = new System.Drawing.Size(601, 171);
            this.SceneGroupBox.TabIndex = 0;
            this.SceneGroupBox.TabStop = false;
            this.SceneGroupBox.Text = "Scene";
            // 
            // ImagePictureBox
            // 
            this.ImagePictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ImagePictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ImagePictureBox.Location = new System.Drawing.Point(0, 0);
            this.ImagePictureBox.Name = "ImagePictureBox";
            this.ImagePictureBox.Size = new System.Drawing.Size(376, 275);
            this.ImagePictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.ImagePictureBox.TabIndex = 1;
            this.ImagePictureBox.TabStop = false;
            // 
            // ControlGroupBox
            // 
            this.ControlGroupBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ControlGroupBox.Location = new System.Drawing.Point(0, 0);
            this.ControlGroupBox.Name = "ControlGroupBox";
            this.ControlGroupBox.Size = new System.Drawing.Size(420, 275);
            this.ControlGroupBox.TabIndex = 0;
            this.ControlGroupBox.TabStop = false;
            this.ControlGroupBox.Text = "Control";
            // 
            // ResolutionLabel
            // 
            this.ResolutionLabel.AutoSize = true;
            this.ResolutionLabel.Location = new System.Drawing.Point(12, 19);
            this.ResolutionLabel.Name = "ResolutionLabel";
            this.ResolutionLabel.Size = new System.Drawing.Size(66, 15);
            this.ResolutionLabel.TabIndex = 0;
            this.ResolutionLabel.Text = "Resolution:";
            this.ResolutionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // ResolutionComboBox
            // 
            this.ResolutionComboBox.FormattingEnabled = true;
            this.ResolutionComboBox.Items.AddRange(new object[] {
            "3840 x 2160",
            "3440 x 1440",
            "1980 x 1080",
            "320 x 240"});
            this.ResolutionComboBox.Location = new System.Drawing.Point(84, 16);
            this.ResolutionComboBox.Name = "ResolutionComboBox";
            this.ResolutionComboBox.Size = new System.Drawing.Size(95, 23);
            this.ResolutionComboBox.TabIndex = 1;
            this.ResolutionComboBox.Text = "3840 x 2160";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.HorizontalSplitContainer);
            this.Name = "MainForm";
            this.Text = "RenderSandbox";
            this.HorizontalSplitContainer.Panel1.ResumeLayout(false);
            this.HorizontalSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.HorizontalSplitContainer)).EndInit();
            this.HorizontalSplitContainer.ResumeLayout(false);
            this.InputSplitContainer.Panel1.ResumeLayout(false);
            this.InputSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.InputSplitContainer)).EndInit();
            this.InputSplitContainer.ResumeLayout(false);
            this.OutputSplitContainer.Panel1.ResumeLayout(false);
            this.OutputSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.OutputSplitContainer)).EndInit();
            this.OutputSplitContainer.ResumeLayout(false);
            this.ImageGroupBox.ResumeLayout(false);
            this.ImageGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ImagePictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private SplitContainer HorizontalSplitContainer;
        private SplitContainer InputSplitContainer;
        private GroupBox ImageGroupBox;
        private ComboBox ResolutionComboBox;
        private Label ResolutionLabel;
        private GroupBox SceneGroupBox;
        private SplitContainer OutputSplitContainer;
        private GroupBox ControlGroupBox;
        private PictureBox ImagePictureBox;
    }
}
