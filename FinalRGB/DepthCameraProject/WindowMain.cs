
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using OpenNI;
using System.Threading;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;


namespace Camera.net
{
    public partial class MainWindow : Form
    {

        private DepthCamera depthCam;
        private ThermalCamera thermalCam;
        private RGBCamera rgbCam;
        private Bitmap bitmap;
        private int startTime = 0;
        


        //..................................................

        /// <summary>
        /// test
        /// </summary>
        public MainWindow(DepthCamera depthCam, ThermalCamera thermalCam,RGBCamera rgbCam)
        {
            InitializeComponent();
            GoFullscreen(true);
            //prevent flickering 
            this.SetStyle(
             ControlStyles.AllPaintingInWmPaint |
             ControlStyles.UserPaint |
             ControlStyles.DoubleBuffer,
             true);
            this.depthCam = depthCam;
            this.thermalCam = thermalCam;
            this.rgbCam =rgbCam;
            this.bitmap = new Bitmap(382, 288, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            this.Activate();
            

        }

        private void WindowMain_Load(object sender, EventArgs e)
        {
            this.TopMost = true;
            this.Activate();
            this.BringToFront();
            this.TopMost = false;
            thermalCam.thermoRenderStart();
            thermalCam.setShowFusedImage(true);
        }

        /// <summary>
        /// Draws bitmap on panel
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            lock (this)
            {
                e.Graphics.DrawImage(this.bitmap,
                    this.picturePanel.Location.X,
                    this.picturePanel.Location.Y,
                    this.picturePanel.Size.Width,
                    this.picturePanel.Size.Height

                    );
            }

        }
        //public static Process prozess = new Process();

        /// <summary>
        /// end  Threads
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(CancelEventArgs e)
        {

            if (thermalCam.isShowThermalImage()|| thermalCam.isShowDepthImage()||thermalCam.isShowRGBImage())
            {
                this.thermalCam.closeThermoRender();
            }
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C netsh wlan stop hostednetwork";
            //startInfo.RedirectStandardOutput = true;
            process.StartInfo = startInfo;
            process.Start();
            Program.blackscreen.Close();
            

            base.OnClosing(e);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            //Keychar 27 escape
            if (e.KeyChar == 27)
            {
                Close();
            }
            base.OnKeyPress(e);
        }

        public double distanceLabelUpdate(ushort distance)
        {
            double distanceInMeter = 0;
            int currentTime = Environment.TickCount & Int32.MaxValue;
            //if Konstrukt sorgt für weniger schwankungen bei der Distanz anzeige
            //wird nur alle 200ms aktuallisiert
            if (currentTime - startTime > 200)
            {
                startTime = currentTime;
                distanceInMeter = (double)distance / 1000;
                if (this.depthLabel.InvokeRequired)
                {
                    this.depthLabel.BeginInvoke((MethodInvoker)delegate ()
                  {
                      distanceInMeter = Math.Round(distanceInMeter, 2);
                      if (distance < 500)
                      {
                          this.depthLabel.Text = "NA";
                      }else{
                      this.depthLabel.Text = Math.Round(distanceInMeter, 2).ToString() + " m";
                      }
                      ;
                      

                  });
                }
                else
                {
                    if (distance < 500)
                    {
                        this.depthLabel.Text = "NA";
                    }
                    else
                    {
                        this.depthLabel.Text = Math.Round(distanceInMeter, 2).ToString() + " m";
                    }
                }
            }
            return Math.Round(distanceInMeter, 2);
        }


        private  DateTime startMode;
        private DateTime endMode;
        private void switchImage()
        {
            //intitialisiere falls noch kein Modus an ist
            if (!(thermalCam.isShowDepthImage() || thermalCam.isShowThermalImage() || thermalCam.isShowFusedImage()||thermalCam.isShowRGBImage()))
            {
                startMode = DateTime.Now;
               
                using (StreamWriter file = new StreamWriter(@"UseLog.txt", true))
                {       
                    file.WriteLine(DateTime.Today.ToString("d") + " " + startMode.ToString("h:mm:ss tt") + " Mode 2 start");
                }
                thermalCam.setShowRGBImage(true);
            }
            else if (thermalCam.isShowRGBImage())
            {
                endMode = startMode;
                startMode = DateTime.Now;
                TimeSpan? variable = startMode - endMode;

                using (StreamWriter file = new StreamWriter(@"UseLog.txt", true))
                {
                    file.WriteLine(DateTime.Today.ToString("d") + " " + startMode.ToString("h:mm:ss tt") + " Mode 1 stop" + " Duration:" + variable.Value.TotalSeconds);
                }

                using (StreamWriter file = new StreamWriter(@"UseLog.txt", true))
                {
                    file.WriteLine(DateTime.Today.ToString("d") + " " + startMode.ToString("h:mm:ss tt") + " Mode 2 start");
                }
                thermalCam.setShowFusedImage(true);
                thermalCam.setShowDepthImage(false);
                thermalCam.setShowRGBImage(false);
            }
            else if (thermalCam.isShowDepthImage())
            {
                endMode = startMode;
                startMode = DateTime.Now;
                TimeSpan? variable = startMode - endMode;
                
                using (StreamWriter file = new StreamWriter(@"UseLog.txt", true))
                {
                    file.WriteLine(DateTime.Today.ToString("d") + " " + startMode.ToString("h:mm:ss tt") + " Mode 2 stop" + " Duration:" + variable.Value.TotalSeconds);
                }

                using (StreamWriter file = new StreamWriter(@"UseLog.txt", true))
                {
                    file.WriteLine(DateTime.Today.ToString("d") + " " + startMode.ToString("h:mm:ss tt") + " Mode 3 start");
                }
                thermalCam.setShowDepthImage(false);
                thermalCam.setShowThermalImage(true);
                thermalCam.setShowFusedImage(false);
            }
            else if (thermalCam.isShowThermalImage())
            {
                endMode = startMode;
                startMode = DateTime.Now;
                TimeSpan? variable = startMode - endMode;
               
                using (StreamWriter file = new StreamWriter(@"UseLog.txt", true))
                {
                    file.WriteLine(DateTime.Today.ToString("d") + " " + startMode.ToString("h:mm:ss tt") + " Mode 3 stop" + " Duration:" + variable.Value.TotalSeconds);
                }

                using (StreamWriter file = new StreamWriter(@"UseLog.txt", true))
                {
                    file.WriteLine(DateTime.Today.ToString("d") + " " + startMode.ToString("h:mm:ss tt") + " Mode 4 start");
                }
                thermalCam.setShowThermalImage(false);
                thermalCam.setShowDepthImage(false);
                thermalCam.setShowRGBImage(true);
            }
            else if (thermalCam.isShowFusedImage())
            {
                endMode = startMode;
                startMode = DateTime.Now;
                TimeSpan? variable = startMode - endMode;

                using (StreamWriter file = new StreamWriter(@"UseLog.txt", true))
                {
                    file.WriteLine(DateTime.Today.ToString("d") + " " + startMode.ToString("h:mm:ss tt") + " Mode 4 stop" + " Duration:" + variable.Value.TotalSeconds);
                }

                using (StreamWriter file = new StreamWriter(@"UseLog.txt", true))
                {
                    file.WriteLine(DateTime.Today.ToString("d") + " " + startMode.ToString("h:mm:ss tt") + " Mode 1 start");
                }
                thermalCam.setShowDepthImage(true);
                thermalCam.setShowFusedImage(false);
            }
        }

        private void MainWindow_MouseClick(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    switchImage();
                    break;

                case MouseButtons.Right:
                    Program.toggle();
                    break;

            }

        }

        private void GoFullscreen(bool fullscreen)
        {
            if (fullscreen)
            {
                this.WindowState = FormWindowState.Normal;
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                this.Bounds = Screen.PrimaryScreen.Bounds;
            }
            else
            {
                this.WindowState = FormWindowState.Maximized;
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            }

        }

       



        //Checkt alle 200 sekunden ob noch verbunden mit PiConnect
        // wenn nicht wird neu versucht zu verbinden
        //dieser timer2 wird nur aktiv wenn thermoRenderStart() ausgefuehrt wurde
        private void timer2_Tick(object sender, EventArgs e)
        {
            this.thermalCam.timer2_tick();
            
        }


        /// <summary>
        /// Setzt alle 100ms Painted auf falsch irgendwie UNBEDINGT BENOETIGT
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            this.thermalCam.timer1_tick();
        }

        public void enableTimer1() {
            timer1.Enabled = true;
        }
        public void enableTimer2()
        {
            timer2.Enabled = true;

        }
        //HIER........................................................................................
        public void reenderImage(Bitmap bitmap)
        {
            this.bitmap = bitmap;
            tempLabel.Text = String.Format("{0:##0.0}°C", IPC2.GetTempTarget(0));
            // hier kann die bitmap abgespeicher werden
            // die bitmap die hier reinkommt wird auch im program gezeichnet
            //this.bitmap.Save(C/ richtiger pfad...);
            this.Invalidate();
        }

        public void drawBlackScreen()
        {
            Graphics g = Graphics.FromImage(bitmap);
            g.FillRectangle(new SolidBrush(Color.Black), 0, 0, bitmap.Width, bitmap.Height);
            this.Invalidate();
        }
        
        
    }
}
