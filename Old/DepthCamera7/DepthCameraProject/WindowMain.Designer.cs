﻿namespace Camera.net
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
            this.picturePanel = new System.Windows.Forms.Panel();
            this.rightDisplayPanel = new System.Windows.Forms.Panel();
            this.depthLabel = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.videomodusToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.rGBToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tiefenbildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.wärmebildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.rightDisplayPanel.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // picturePanel
            // 
            this.picturePanel.Location = new System.Drawing.Point(0, 30);
            this.picturePanel.Name = "picturePanel";
            this.picturePanel.Size = new System.Drawing.Size(System.Windows.Forms.SystemInformation.VirtualScreen.Width-150, 768);
            this.picturePanel.TabIndex = 0;
            this.picturePanel.Visible = false;
            // 
            // rightDisplayPanel
            // 
            this.rightDisplayPanel.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.rightDisplayPanel.BackColor = System.Drawing.Color.White;
            this.rightDisplayPanel.Controls.Add(this.depthLabel);
            this.rightDisplayPanel.Location = new System.Drawing.Point(950, 0);
            this.rightDisplayPanel.Name = "rightDisplayPanel";
            this.rightDisplayPanel.Size = new System.Drawing.Size(150, 768);
            this.rightDisplayPanel.TabIndex = 3;
            // 
            // depthLabel
            // 
            this.depthLabel.BackColor = System.Drawing.Color.White;
            this.depthLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 30F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.depthLabel.Location = new System.Drawing.Point(10, 302);
            this.depthLabel.Name = "depthLabel";
            this.depthLabel.Size = new System.Drawing.Size(150, 60);
            this.depthLabel.TabIndex = 1;
            this.depthLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.depthLabel.Resize += new System.EventHandler(this.depthLabel_Resize);
            
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.videomodusToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1100, 24);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // videomodusToolStripMenuItem
            // 
            this.videomodusToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.rGBToolStripMenuItem,
            this.tiefenbildToolStripMenuItem,
            this.wärmebildToolStripMenuItem});
            this.videomodusToolStripMenuItem.Name = "videomodusToolStripMenuItem";
            this.videomodusToolStripMenuItem.Size = new System.Drawing.Size(86, 20);
            this.videomodusToolStripMenuItem.Text = "Videomodus";
            // 
            // rGBToolStripMenuItem
            // 
            this.rGBToolStripMenuItem.Name = "rGBToolStripMenuItem";
            this.rGBToolStripMenuItem.Size = new System.Drawing.Size(132, 22);
            this.rGBToolStripMenuItem.Text = "RGB";
            this.rGBToolStripMenuItem.Click += new System.EventHandler(this.rGBToolStripMenuItem_Click);
            // 
            // tiefenbildToolStripMenuItem
            // 
            this.tiefenbildToolStripMenuItem.Name = "tiefenbildToolStripMenuItem";
            this.tiefenbildToolStripMenuItem.Size = new System.Drawing.Size(132, 22);
            this.tiefenbildToolStripMenuItem.Text = "Tiefenbild";
            this.tiefenbildToolStripMenuItem.Click += new System.EventHandler(this.tiefenbildToolStripMenuItem_Click);
            // 
            // wärmebildToolStripMenuItem
            // 
            this.wärmebildToolStripMenuItem.Name = "wärmebildToolStripMenuItem";
            this.wärmebildToolStripMenuItem.Size = new System.Drawing.Size(132, 22);
            this.wärmebildToolStripMenuItem.Text = "Wärmebild";
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(1100, 720);
            this.Controls.Add(this.rightDisplayPanel);
            this.Controls.Add(this.picturePanel);
            this.Controls.Add(this.menuStrip1);
            this.Name = "MainWindow";
            this.Text = "DepthSensorViewer";
            this.rightDisplayPanel.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

         }

         #endregion

         private System.Windows.Forms.Panel picturePanel;
         private System.Windows.Forms.Label depthLabel;
         private System.Windows.Forms.Panel panel1;
         private System.Windows.Forms.MenuStrip menuStrip1;
         private System.Windows.Forms.ToolStripMenuItem videomodusToolStripMenuItem;
         private System.Windows.Forms.ToolStripMenuItem rGBToolStripMenuItem;
         private System.Windows.Forms.ToolStripMenuItem tiefenbildToolStripMenuItem;
         private System.Windows.Forms.ToolStripMenuItem wärmebildToolStripMenuItem;
         private System.Windows.Forms.Panel rightDisplayPanel;
     }
 }

