

namespace Camera.net
{
    partial class MainWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.timer2 = new System.Windows.Forms.Timer(this.components);
            this.picturePanel = new System.Windows.Forms.Panel();
            this.rightInfoPanel = new System.Windows.Forms.Panel();
            this.tempLabel = new System.Windows.Forms.Label();
            this.depthLabel = new System.Windows.Forms.Label();
            this.rightInfoPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // timer2
            // 
            this.timer2.Interval = 200;
            this.timer2.Tick += new System.EventHandler(this.timer2_Tick);
            // 
            // picturePanel
            // 
            this.picturePanel.Location = new System.Drawing.Point(0, 3);
            this.picturePanel.Name = "picturePanel";
            // Bei Fehler in WindowForm die -150 bei System.Windows.Forms.SystemInformation.VirtualScreen.Width-150
            // wegmachen, Teil des Bildes wird aber vom rightInfoPanel weggeschnitte( welches 150 breit ist)
            //this.picturePanel.Size = new System.Drawing.Size(System.Windows.Forms.SystemInformation.VirtualScreen.Width - 150, System.Windows.Forms.SystemInformation.VirtualScreen.Height);
            this.picturePanel.Size = new System.Drawing.Size(640, 480);
            this.picturePanel.TabIndex = 0;
            this.picturePanel.Visible = false;
            // 
            // rightDisplayPanel
            // 
            this.rightInfoPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rightInfoPanel.BackColor = System.Drawing.Color.White;
            this.rightInfoPanel.Controls.Add(this.tempLabel);
            this.rightInfoPanel.Controls.Add(this.depthLabel);
            this.rightInfoPanel.Location = new System.Drawing.Point(950, 0);
            this.rightInfoPanel.Name = "rightDisplayPanel";
            this.rightInfoPanel.Size = new System.Drawing.Size(150, 795);
            this.rightInfoPanel.TabIndex = 3;
            // 
            // tempLabel
            // 
            this.tempLabel.BackColor = System.Drawing.Color.White;
            this.tempLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tempLabel.Location = new System.Drawing.Point(0, 242);
            this.tempLabel.Name = "tempLabel";
            this.tempLabel.Size = new System.Drawing.Size(150, 60);
            this.tempLabel.TabIndex = 3;
            this.tempLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // depthLabel
            // 
            this.depthLabel.BackColor = System.Drawing.Color.White;
            this.depthLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 30F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.depthLabel.Location = new System.Drawing.Point(0, 302);
            this.depthLabel.Name = "depthLabel";
            this.depthLabel.Size = new System.Drawing.Size(150, 60);
            this.depthLabel.TabIndex = 1;
            this.depthLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(1100, 720);
            this.Controls.Add(this.rightInfoPanel);
            this.Controls.Add(this.picturePanel);
            this.Name = "MainWindow";
            this.Text = "DepthSensorViewer";
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.MainWindow_MouseClick);
            this.rightInfoPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel picturePanel;
        private System.Windows.Forms.Label depthLabel;
        private System.Windows.Forms.Panel rightInfoPanel;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Timer timer2;
        private System.Windows.Forms.Label tempLabel;
    }
}

