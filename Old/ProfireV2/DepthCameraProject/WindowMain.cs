
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

///HAUPTKONTROLLKLASSE
///
/// Wichtig nur 
/// 1. switchImage() -> sagt der ThermalCamera Klasse welches Bildmodus wir uns befinden (showDepth,showThermo,showFused) 
/// 2. reenderImage(Bitmap bitmap) -> Wird in der ThermalCamer Klasse aufgerufen wenn Bitmap darin fertig berechnet wird
///                                   reenderimage(Bitmap bitmap) zeichnet dann einfach diese Bitmap.
///                                   
/// Zurzeit bestimmt ThermalCamera Klasse Methode NewFrame() wann reenderimage() aufgerufen wird
/// damit wird auch bestimmt wann gezeichnet wird, diese Aufgabe sollte aber später eigt die MainWindow Klasse erfuellen
/// 
/// Muss/Sollte noch geänder werden.
/// 
/// 
/// 
/// TODO Bei Kamera Wechsel Kameras richtig beenden/starten falls es Probleme
/// Exception für nicht gefundene Kameras einfügen
/// ThermorCamera methoden auslagern in neue Klasse 
/// weiterhin rotes Kreuz zeichen effizienter machen
namespace Camera.net
{
    public partial class MainWindow : Form
    {

        private DepthCamera depthCam;
        private ThermalCamera thermalCam;
        private Bitmap bitmap;
        private int startTime = 0;
        


        //..................................................

        /// <summary>
        /// test
        /// </summary>
        public MainWindow(DepthCamera depthCam, ThermalCamera thermalCam)
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
            this.bitmap = new Bitmap(382, 288, System.Drawing.Imaging.PixelFormat.Format24bppRgb);



        }

        private void WindowMain_Load(object sender, EventArgs e)
        {
            thermalCam.thermoRenderStart();
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

        /// <summary>
        /// end  Threads
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(CancelEventArgs e)
        {

            if (thermalCam.showThermo || thermalCam.showDepth)
            {
                this.thermalCam.closeThermoRender();
            }
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
            if (!(thermalCam.showDepth || thermalCam.showThermo || thermalCam.showFusedImage))
            {
                startMode = DateTime.Now;
               
                using (StreamWriter file = new StreamWriter(@"UseLog.txt", true))
                {       
                    file.WriteLine(DateTime.Today.ToString("d") + " " + startMode.ToString("h:mm:ss tt") + " Mode 2 start");
                }
                //this.thermalCam.thermoRenderStart();
                thermalCam.showDepth = true;
            }
            else if (thermalCam.showDepth)
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
                thermalCam.showDepth = false;
                thermalCam.showThermo = true;
                thermalCam.showFusedImage = false;
            }
            else if (thermalCam.showThermo)
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
                    file.WriteLine(DateTime.Today.ToString("d") + " " + startMode.ToString("h:mm:ss tt") + " Mode 2 start");
                }
                thermalCam.showThermo = false;
                thermalCam.showDepth = false;
                //showDepth = true;
                thermalCam.showFusedImage = true;
            }
            else if (thermalCam.showFusedImage)
            {
                thermalCam.showThermo = false;
                thermalCam.showDepth = true;
                thermalCam.showFusedImage = false;
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
                    //Program.toggle();
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

        ///................................Thermo Zeugs..................................................
       


        //.........................

        //Checkt alle 200 sekunden ob noch verbunden mit PiConnect
        // wenn nicht wird neu versucht zu verbinden
        //dieser timer2 wird nur aktiv wenn thermoRenderStart() ausgefuehrt wurde
        private void timer2_Tick(object sender, EventArgs e)
        {
            this.thermalCam.timer2_tick();
            
        }


        /// <summary>
        /// Setzt alle 100ms Painted auf falsch 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            this.thermalCam.timer1_tick();
        }
        //..................................................................................

        public void enableTimer1() {
            timer1.Enabled = true;
        }
        public void enableTimer2()
        {
            timer2.Enabled = true;

        }
        public void reenderImage(Bitmap bitmap)
        {
            this.bitmap = bitmap;
            tempLabel.Text = String.Format("{0:##0.0}°C", IPC2.GetTempTarget(0));
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
