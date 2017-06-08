namespace IPC2
{
    partial class FormMain
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.timer2 = new System.Windows.Forms.Timer(this.components);
            this.show_temp = new System.Windows.Forms.Label();
            this.flag_Renew = new System.Windows.Forms.Label();
            this.timer3 = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox
            // 
            this.pictureBox.Location = new System.Drawing.Point(1, 1);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(160, 120);
            this.pictureBox.TabIndex = 84;
            this.pictureBox.TabStop = false;
            this.pictureBox.Click += new System.EventHandler(this.pictureBox_Click);
            this.pictureBox.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBox_Paint);
            this.pictureBox.MouseClick += new System.Windows.Forms.MouseEventHandler(this.pictureBox_Click);
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
            // show_temp
            // 
            this.show_temp.AutoSize = true;
            this.show_temp.Location = new System.Drawing.Point(115, 9);
            this.show_temp.Name = "show_temp";
            this.show_temp.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.show_temp.Size = new System.Drawing.Size(37, 13);
            this.show_temp.TabIndex = 88;
            this.show_temp.Text = "Temp:";
            this.show_temp.Visible = false;
            this.show_temp.Click += new System.EventHandler(this.label1_Click);
            // 
            // flag_Renew
            // 
            this.flag_Renew.AutoSize = true;
            this.flag_Renew.Location = new System.Drawing.Point(12, 100);
            this.flag_Renew.Name = "flag_Renew";
            this.flag_Renew.Size = new System.Drawing.Size(27, 13);
            this.flag_Renew.TabIndex = 89;
            this.flag_Renew.Text = "Flag";
            this.flag_Renew.Visible = false;
            this.flag_Renew.Click += new System.EventHandler(this.flag_Renew_Click);
            // 
            // timer3
            // 
            this.timer3.Interval = 300;
            this.timer3.Tick += new System.EventHandler(this.timer3_Tick);
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(162, 122);
            this.Controls.Add(this.flag_Renew);
            this.Controls.Add(this.show_temp);
            this.Controls.Add(this.pictureBox);
            this.Name = "FormMain";
            this.Text = "Thermal Imager";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormMain_FormClosing);
            this.Load += new System.EventHandler(this.FormMain_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Timer timer2;
        private System.Windows.Forms.Label show_temp;
        private System.Windows.Forms.Label flag_Renew;
        private System.Windows.Forms.Timer timer3;
    }
}

