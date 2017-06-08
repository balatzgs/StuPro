
﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using OpenNI;
using System.Threading;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;


namespace Camera.net
{
    public partial class MainWindow : Form
    {

        private DepthCamera depthCam;
        private RGBCamera rgbCam;
        private Thread renderThread;
        private bool shouldRun;
        private Bitmap bitmap;
        private int startTime = 0;
        private Boolean showRGB;
        private Boolean showDepth;
        private Boolean showThermo;

       /// <summary>
       /// test
       /// </summary>
        public MainWindow(DepthCamera depthCam,RGBCamera rgbCam)
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
            this.rgbCam = rgbCam;
            this.bitmap = new Bitmap(320,240, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            this.shouldRun = true;
         
            //this.depthThread = new Thread(depthRenderThread);
            //this.depthThread.Start();

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
                    this.picturePanel.Size.Height);
            }
        }

        //protected override void OnPaintBackground(PaintEventArgs pevent)
        //{
        //    //Don't allow the background to paint
        //}

        /// <summary>
        /// end  Threads
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(CancelEventArgs e)
        {
            this.shouldRun = false;
            if (showDepth || showRGB)
            {
                this.renderThread.Join();
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
        
        private unsafe void depthRenderThread()
        {

            while (this.shouldRun)
            {
                lock (this)
                {
                    Rectangle rect = new Rectangle(0, 0, this.bitmap.Width, this.bitmap.Height);
                    BitmapData data = this.bitmap.LockBits(rect, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                    data = this.depthCam.getData(data);
                    this.bitmap.UnlockBits(data);
                    this.bitmap.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipX);
                }

               this.Invalidate();
               
            }
        }
        private unsafe void rgbRenderThread()
        {

            while (this.shouldRun)
            {
                lock (this)
                {
                    Rectangle rect = new Rectangle(0, 0, this.bitmap.Width, this.bitmap.Height);
                    BitmapData data = this.bitmap.LockBits(rect, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                    data = this.rgbCam.getData(data);
                    this.depthCam.getDistanceForRGB(data);
                    this.bitmap.UnlockBits(data);
                    this.bitmap.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipX);
                }

                this.Invalidate();

            }
        }

        
       
        public void labelUpdate(ushort distance)
        {
            int currentTime = Environment.TickCount & Int32.MaxValue;
            //if Konstrukt soegt für weniger schwankungen bei der Distanz anzeige
            //wird nur 200ms aktuallisiert
            if (currentTime-startTime >200){
                startTime = currentTime;
                double distanceInMeter = (double) distance / 1000;
            if (this.depthLabel.InvokeRequired)
            {
                this.depthLabel.BeginInvoke((MethodInvoker)delegate()
              { this.depthLabel.Text = Math.Round(distanceInMeter, 2).ToString() + " m"; ;});
            }
            else
            {
                this.depthLabel.Text = Math.Round(distanceInMeter, 2).ToString() + " m";
            }
            }   
        }

        private void tiefenbildToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (showRGB)
            {
                showRGB = false;
                this.shouldRun = false;
                this.renderThread.Join();
                shouldRun = true;
            }
            if (!showDepth)
            {
            this.renderThread = new Thread(depthRenderThread);
            this.renderThread.Start();
            showDepth = true;
            }
            
        }

        private void rGBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (showDepth)
            {
                showDepth = false;
                this.shouldRun = false;
                this.renderThread.Join();
                shouldRun = true;
            }
            if (!showRGB)
            {
                this.renderThread = new Thread(rgbRenderThread);
                this.renderThread.Start();
                showRGB = true;
            }
        }


        private void depthLabel_Resize(object sender, EventArgs e)
        {
            this.depthLabel.Anchor = System.Windows.Forms.AnchorStyles.Right;
        }

      //private void button1_Click(object sender, EventArgs e)
      //  {
      //      Boolean toggle=false;
      //      if (toggle = true)
      //      {
      //          GoFullscreen(false);
      //          toggle = false;
      //      }
      //      else
      //      {
      //          GoFullscreen(true);
      //          toggle = true; }
      //  }

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


    

        


    }
}
