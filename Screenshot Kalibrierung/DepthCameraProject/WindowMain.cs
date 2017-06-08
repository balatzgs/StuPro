
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


/// Bei Kamera Wechsel Kameras richtig beenden/starten falls es Probleme
/// Exception für nicht gefundene Kameras einfügen
/// ThermorCamera methoden auslagern in neue Klasse 
/// weiterhin rotes Kreuz zeichen effizienter machen
namespace Camera.net
{
    public partial class MainWindow : Form
    {

        private DepthCamera depthCam;
        private RGBCamera rgbCam;
        private Thread renderThread;
        private bool shouldRun;
        private Bitmap bitmap;
        private Bitmap bitmapToDraw;
        private Bitmap bitmap2ToDraw;
        private int startTime = 0;
        private Boolean showRGB = false;
        private Boolean showDepth;
        private Boolean showThermo;
        //........................................
        //Thermo Zeugs
#if _WIN64
        const Int32 OS = 64;
#else
        const Int32 OS = 32;
#endif
        public static readonly Int32 S_OK = 0;
        public static readonly Int32 S_FALSE = 1;

        public System.Version Version
        {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version; }
        }

        Int32 FrameWidth, FrameHeight, FrameDepth, FrameSize;
        public IPC2 ipc;
        bool ipcInitialized = false, frameInitialized = false;
        bool Connected = false;
        bool Colors = false;

        byte[] rgbValues;
        Int16[] Values;

        Int32 MainTimerDivider;
        bool Painted = false;

        int imageNr;

        //..................................................

        /// <summary>
        /// test
        /// </summary>
        public MainWindow(DepthCamera depthCam, RGBCamera rgbCam)
        {
            InitializeComponent();
            //GoFullscreen(false);
            //prevent flickering 
            this.SetStyle(
             ControlStyles.AllPaintingInWmPaint |
             ControlStyles.UserPaint |
             ControlStyles.DoubleBuffer,
             true);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(0, 0);
            this.depthCam = depthCam;
            this.rgbCam = rgbCam;
            this.bitmap = new Bitmap(320, 240, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            this.bitmap2ToDraw = new Bitmap(320, 240, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            this.shouldRun = true;
            thermoRenderStart();


            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo("C://Users//Kim//Documents//MATLAB//rightCamera");
            imageNr = dir.GetFiles().Length;

            if (imageNr == 0)
            {
                imageNr = 1;
            }


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
                e.Graphics.DrawImage(this.bitmap2ToDraw,
                    this.picturePanel.Location.X,
                    this.picturePanel.Location.Y,
                    this.picturePanel.Size.Width,
                    this.picturePanel.Size.Height

                    );
                e.Graphics.DrawImage(this.bitmap,
                    this.picturePanel2.Location.X,
                    this.picturePanel2.Location.Y,
                    this.picturePanel2.Size.Width,
                    this.picturePanel2.Size.Height

                    );
            }


        }



        /// <summary>
        /// end  Threads
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(CancelEventArgs e)
        {

            if (showThermo || showDepth)
            {
                closeThermoRender();
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
            if (e.KeyChar == (char)Keys.Space)
            {
                if (imageNr < 10)
                {
                    
                    bitmap2ToDraw.Save("C://Users//Kim//Documents//MATLAB//rightCamera//" + "right0" + imageNr + ".png");
                    bitmap.Save("C://Users//Kim//Documents//MATLAB//leftCamera//" + "left0" + imageNr + ".png");
                }
                else
                {
                    bitmap2ToDraw.Save("C://Users//Kim//Documents//MATLAB//rightCamera//" + "right" + imageNr + ".png");
                    bitmap.Save("C://Users//Kim//Documents//MATLAB//leftCamera//" + "left" + imageNr + ".png");
                }
                //using (var image = Image.FromFile(@"C://Users//Kim//Desktop//temp//" + "thermo" + imageNr + ".png"))
                //using (var newImage = ScaleImage(image, 640, 480))
                //{
                //    newImage.Save(@"C://Users//Kim//Desktop//rightCamera//" + "thermo" + imageNr + ".png", ImageFormat.Png);
                //}
                
                imageNr++;
                
            }
               base.OnKeyPress(e);
        }
        public static Image ScaleImage(Image image, int maxWidth, int maxHeight)
        {
            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            var newImage = new Bitmap(640,480);

            using (var graphics = Graphics.FromImage(newImage))
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);

            return newImage;
        }



        public double updateDistanceLabel(ushort distance)
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
                    this.depthLabel.BeginInvoke((MethodInvoker)delegate()
                  {
                      distanceInMeter = Math.Round(distanceInMeter, 2);
                      this.depthLabel.Text = Math.Round(distanceInMeter, 2).ToString() + " m"; ;});
                }
                else
                {
                    this.depthLabel.Text = Math.Round(distanceInMeter, 2).ToString() + " m";
                }
            }
            return Math.Round(distanceInMeter, 2);
        }

        public void updateTempLabe(String temperature)
        {
            if (this.tempLabel.InvokeRequired)
            {
                this.tempLabel.BeginInvoke((MethodInvoker)delegate()
                {
                    this.tempLabel.Text = temperature; ;
                });
            }
            else
            {
                this.tempLabel.Text = temperature;
            }
        }

        private void switchImage()
        {
            //intitialisiere falls noch kein Modus an ist
            if (!(showDepth || showThermo))
            {
                thermoRenderStart();
                showDepth = true;
            }
            else if (showDepth)
            {

                showDepth = false;
                showThermo = true;
            }
            else if (showThermo)
            {
                showThermo = false;
                showDepth = true;
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
        private void thermoRenderStart()
        {
            //was macht die dritte zahl 2 als frameDepth ?
            Init(320, 240, 2);
            ipc = new IPC2(1);
#if POLLING 
	        Application.Idle +=  new EventHandler(this.Application_Idle);
#endif
            timer2.Enabled = true;
        }
        private void closeThermoRender()
        {
#if POLLING
	        Application.Idle -=  new EventHandler(this.Application_Idle);
#endif
            ReleaseIPC();

        }
        /// <summary>
        /// Methode laeuft die ganze Zeit im Hintergrund falls sie gestartet wurde zustaendig fuer alles von 
        /// der Waermebild
        /// </summary>
        void InitIPC()
        {
            Int64 hr;
            if ((ipc != null) && !ipcInitialized)
            {
                hr = IPC2.Init(0, "");
                // int S_OK ist die Zahl 0
                //wahrscheinlich gibt IPC2.Init(0, "") den Wert 0 wieder wenn erfolgreich mit PiConnect verbunden
                if (hr != S_OK)
                {
                    ipcInitialized = frameInitialized = false;
                }
                //Prueft nach welcher ereignis eingetreten ist
                //Reihenfolge scheint keine Rolle zu spielen
                else
                {
#if !POLLING
                    //Verbindung zu PiConnect verloren ? rufe OnServerStopped() welches Bildbereich schwarz zeichnet
                    ipc.OnServerStopped = new IPC2.delOnServerStopped(OnServerStopped);
                    IPC2.SetCallback_OnServerStopped(0, ipc.OnServerStopped);

                    //Bereitet einfach nur das zu zeichnende Bitmap vor ? initailisiert alle Werte in Init()
                    ipc.OnFrameInit = new IPC2.delOnFrameInit(OnFrameInit);
                    Int32 u = IPC2.SetCallback_OnFrameInit(0, ipc.OnFrameInit);

                    //Falls neues Waermebild vorhanden Zeichne es mit OnNewFrameEx
                    ipc.OnNewFrameEx = new IPC2.delOnNewFrameEx(OnNewFrameEx);
                    IPC2.SetCallback_OnNewFrameEx(0, ipc.OnNewFrameEx);

                    //Sagt was gemacht werden soll falls die ganzen Variablen/Arrays die fuer das Zeichnen benoetigt 
                    //werden fertig in onFrameInit() initialisiert wurden. Hier wurde frueher einfach nur noch Farbmodus eingeschaltet
                    //habe es aber in OnFrameInit() direkt reingemacht
                    //evtl. brauchen wir es später
                    ipc.OnInitCompleted = new IPC2.delOnInitCompleted(OnInitCompleted);
                    IPC2.SetCallback_OnInitCompleted(0, ipc.OnInitCompleted);

#endif
                    hr = IPC2.Run(0);
                    ipcInitialized = (hr == S_OK);

                }
            }
        }

        private void ReleaseIPC()
        {
            Connected = false;
            if ((ipc != null) && ipcInitialized)
            {
                IPC2.Release(0);
                ipcInitialized = false;
            }
        }

        byte LoByte(Int16 val) { return BitConverter.GetBytes(val)[0]; }
        byte HiByte(Int16 val) { return BitConverter.GetBytes(val)[1]; }
        byte clip(Int32 val) { return (byte)((val <= 255) ? ((val > 0) ? val : 0) : 255); }


        void GetBitmap(Bitmap Bmp, Int16[] values)
        {
            Int32 stride_diff;
            // Lock the bitmap's bits.  
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, Bmp.Width, Bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData = Bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, Bmp.PixelFormat);
            stride_diff = bmpData.Stride - FrameWidth * 3;

            //this.tempLabel.Text = Bmp.Height.ToString();

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            if (Colors)
            {
                for (Int32 dst = 0, src = 0, y = 0; y < FrameHeight; y++, dst += stride_diff)
                    for (Int32 x = 0; x < FrameWidth; x++, src++, dst += 3)
                    {
                        Int32 C = (Int32)LoByte(values[src]) - 16;
                        Int32 D = (Int32)HiByte(values[src - (src % 2)]) - 128;
                        Int32 E = (Int32)HiByte(values[src - (src % 2) + 1]) - 128;
                        rgbValues[dst] = clip((298 * C + 516 * D + 128) >> 8);
                        rgbValues[dst + 1] = clip((298 * C - 100 * D - 208 * E + 128) >> 8);
                        rgbValues[dst + 2] = clip((298 * C + 409 * E + 128) >> 8);

                        //rotes Kreuz
                        //if ((y == (FrameHeight / 2) && x >= (FrameWidth / 2) - 5 && x <= (FrameWidth / 2) + 5)
                        //   || (x == (FrameWidth / 2) && y >= (FrameHeight / 2) - 5 && y <= (FrameHeight / 2) + 5))
                        //{
                        //    rgbValues[dst] = 0;
                        //    rgbValues[dst + 1] = 0;
                        //    rgbValues[dst + 2] = 255;
                        //}
                    }
            }
            else
            {
                Int16 mn, mx;
                GetBitmap_Limits(values, out mn, out mx);
                double Fact = 255.0 / (mx - mn);

                for (Int32 dst = 0, src = 0, y = 0; y < FrameHeight; y++, dst += stride_diff)
                    for (Int32 x = 0; x < FrameWidth; x++, src++, dst += 3)
                    {
                        rgbValues[dst] = rgbValues[dst + 1] = rgbValues[dst + 2] = (byte)Math.Min(Math.Max((Int32)(Fact * (values[src] - mn)), 0), 255);

                        //rotes Kreuz
                        if ((y == (FrameHeight / 2) && x >= (FrameWidth / 2) - 5 && x <= (FrameWidth / 2) + 5)
                           || (x == (FrameWidth / 2) && y >= (FrameHeight / 2) - 5 && y <= (FrameHeight / 2) + 5))
                        {
                            rgbValues[dst] = 0;
                            rgbValues[dst + 1] = 0;
                            rgbValues[dst + 2] = 255;
                        }
                    }
            }


            // Copy the RGB values back to the bitmap
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, rgbValues.Length);

            // Unlock the bits.
            Bmp.UnlockBits(bmpData);
        }

        void GetBitmap_Limits(Int16[] Values, out Int16 min, out Int16 max)
        {
            Int32 y;
            double Sum, Mean, Variance;
            min = Int16.MinValue;
            max = Int16.MaxValue;
            if (Values == null) return;

            Sum = 0;
            for (y = 0; y < FrameSize; y++)
                Sum += Values[y];
            Mean = (double)Sum / FrameSize;
            Sum = 0;
            for (y = 0; y < FrameSize; y++)
                Sum += (Mean - Values[y]) * (Mean - Values[y]);
            Variance = Sum / FrameSize;
            Variance = Math.Sqrt(Variance);
            Variance *= 3;  // 3 Sigma
            min = (Int16)(Mean - Variance);
            max = (Int16)(Mean + Variance);
        }

        void Application_Idle(Object sender, EventArgs e)
        {
#if POLLING 
            if(Connected && frameInitialized)
            {
                Int32 Size = FrameWidth * FrameHeight * FrameDepth;
                IntPtr Buffer = Marshal.AllocHGlobal(Size);
                IPC2.FrameMetadata Metadata;
                for (Int32 x = 0; x < FrameSize; x++)
                    Marshal.WriteInt16(Buffer, x * 2, (Int16)x);
                if (IPC2.GetFrameQueue(0) > 0)
                    if (IPC2.GetFrame(0, 0, Buffer, (UInt32)Size, out Metadata) == S_OK)
                        NewFrame(Buffer, Metadata);
                Marshal.FreeHGlobal(Buffer);
            }
#endif
        }

        Int64 MainTimer100ms()
        {
            Painted = false;
#if POLLING
	        if(ipcInitialized)
	        {
                IPC2.IPCState State = IPC2.GetIPCState(0, true);
                if ((State & IPC2.IPCState.ServerStopped) != 0)
			        OnServerStopped(0);
                if (!Connected && ((State & IPC2.IPCState.InitCompleted) != 0))
			        OnInitCompleted();
                if ((State & IPC2.IPCState.FrameInit) != 0)
		        {
			        Int32 frameWidth, frameHeight, frameDepth;
                    Int32 a = IPC2.GetFrameConfig(0, out frameWidth, out frameHeight, out frameDepth);
			        if(a == S_OK)
				        Init(frameWidth, frameHeight, frameDepth);
		        }
		        if(Connected && ((State & IPC2.IPCState.FileCmdReady) != 0))
		        {
			        string Filename = IPC2.GetPathOfStoredFile(0);
                    if(Filename != null)
			            OnFileCommandReady(Filename);
		        }
	        }
#endif
            return S_OK;
        }

        /// <summary>
        /// Aktualisiert alle 500ms die Temperatur anzeige
        /// </summary>
        /// <returns></returns>
        Int64 MainTimer500ms()
        {
            if (Connected)
            {
                tempLabel.Text = String.Format("Target-Temp: {0:##0.0}°C", IPC2.GetTempTarget(0));
            }
            return S_OK;
        }

        /// <summary>
        /// Initialisiert/resettet alle Variablen/Arrays die fuer das Zeichnen benoetigt werden
        /// </summary>
        /// <param name="frameWidth"></param>
        /// <param name="frameHeight"></param>
        /// <param name="frameDepth"></param>
        void Init(Int32 frameWidth, Int32 frameHeight, Int32 frameDepth)
        {
            FrameWidth = frameWidth;
            FrameHeight = frameHeight;
            FrameSize = FrameWidth * FrameHeight;
            FrameDepth = frameDepth;
            timer1.Enabled = true;
            bitmap = new Bitmap(FrameWidth, FrameHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
            System.Drawing.Imaging.BitmapData bmpData = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);
            Int32 stride = bmpData.Stride;
            bitmap.UnlockBits(bmpData);
            rgbValues = new Byte[stride * FrameHeight];
            Values = new Int16[FrameSize];
            frameInitialized = true;
        }

        /// <summary>
        /// Falls Verbindung zu PiConnect verloren geht soll Zeichenfläche Schwarz werden
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        Int32 OnServerStopped(Int32 reason)
        {
            ReleaseIPC();
            Graphics g = Graphics.FromImage(bitmap);
            g.FillRectangle(new SolidBrush(Color.Black), 0, 0, bitmap.Width, bitmap.Height);
            this.Invalidate();
            return 0;
        }

        Int32 OnFrameInit(Int32 frameWidth, Int32 frameHeight, Int32 frameDepth)
        {
            Init(frameWidth, frameHeight, frameDepth);
            Colors = ((TIPCMode)IPC2.GetIPCMode(0) == TIPCMode.Colors);
            return 0;
        }

        /// <summary>
        /// will work with Imager.exe release > 2.0 only:
        /// Falls neues Waermebild verfuegbar wird NewFrame() welches das BitmapBild zeichnet aufgerufen
        /// </summary>
        /// <param name="data"></param>
        /// <param name="Metadata"></param>
        /// <returns></returns>
        Int32 OnNewFrameEx(IntPtr data, IntPtr Metadata)
        {
            //Falls benoetigte Variablen/Arrays noch nicht initialisiert werden
            //breche ab und warte bis in InitIPC() das Ereignis OnFrameInit() ausgefuehrt wurde
            if (!frameInitialized)
                return S_FALSE;
            return NewFrame(data, (IPC2.FrameMetadata)Marshal.PtrToStructure(Metadata, typeof(IPC2.FrameMetadata)));
        }

        /// <summary>
        /// Zeichnet das Waermebild
        /// </summary>
        /// <param name="data"></param>
        /// <param name="Metadata"></param>
        /// <returns></returns>
        Int32 NewFrame(IntPtr data, IPC2.FrameMetadata Metadata)
        {

          

                for (Int32 x = 0; x < FrameSize; x++)
                    Values[x] = Marshal.ReadInt16(data, x * 2);
                //timer1_tick welches Painted Flag setzt schein benoetigt zu sein
                //falls ohne Painted Flaf haengt sich das Bild manchmal auf 
               
                    this.bitmap = new Bitmap(382, 288, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    GetBitmap(bitmap, Values);

                    //aktualisiere Distanz + Temperaturanzeige
                    tempLabel.Text = String.Format("{0:##0.0}°C", IPC2.GetTempTarget(0));
                    depthCam.getDistance();

                    //Folgende Zeilen zeichen Waermebildmap von 382,288 zu 320,240
                    //bitmapToDraw = new Bitmap(320, 240);
                    //using (Graphics g = Graphics.FromImage((Image)bitmapToDraw))
                    //{
                    //    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    //    g.DrawImage(bitmap, 0, 0, 320, 240);

                    //}


             
                
                    this.bitmap2ToDraw = new Bitmap(320, 240, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    Rectangle rect = new Rectangle(0, 0, this.bitmap2ToDraw.Width, this.bitmap2ToDraw.Height);
                    BitmapData dataDepth = this.bitmap2ToDraw.LockBits(rect, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    //dataDepth = this.depthCam.getData(dataDepth);
                    dataDepth = this.rgbCam.getData(dataDepth);
                    this.bitmap2ToDraw.UnlockBits(dataDepth);
                    this.bitmap2ToDraw.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipX);

                    //aktualisiere temp anzeige,distanz wird in depthCam.getData(dataDepth) bereits geholt
                    //tempLabel.Text = String.Format("{0:##0.0}°C", IPC2.GetTempTarget(0));

                    this.Invalidate();
                    Painted = true;
                
            
            return 0;
        }
        /// <summary>
        /// nicht noetig zurzeit 
        /// </summary>
        /// <returns></returns>
        Int32 OnInitCompleted()
        {
            //Colors = ((TIPCMode)IPC2.GetIPCMode(0) == TIPCMode.Colors);
            //Connected = true;
            return S_OK;
        }

        //Checkt alle 200 sekunden ob noch verbunden mit PiConnect
        // wenn nicht wird neu versucht zu verbinden
        //dieser timer2 wird nur aktiv wenn thermoRenderStart() ausgefuehrt wurde
        private void timer2_Tick(object sender, EventArgs e)
        {
            if (!ipcInitialized || !Connected) InitIPC();
        }


        /// <summary>
        /// Setzt alle 100ms Painted auf falsch 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            Painted = false;
            MainTimerDivider++;
            MainTimer100ms();
            if ((MainTimerDivider % 5) == 0) MainTimer500ms();
        }


        private void MainWindow_MouseClick(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    if (!(showRGB || showDepth || showThermo))
                    {
                        using (StreamWriter file = new StreamWriter(@"UseLog.txt", true))
                        {
                            file.WriteLine(DateTime.Today.ToString("d") + " " + DateTime.Now.ToString("h:mm:ss tt") + " Mode 2");
                        }
                        //depthLabel.Text = "";
                        //tempLabel.Text = String.Format("T: {0:##0.0}°C", IPC2.GetTempTarget(0));
                        //depthLabel.Refresh();
                        //tempLabel.Refresh();

                        //switchtToDepthImage();

                        //switchImage();

                        //thermoRenderStart();

                    }
                    //else if (showRGB)
                    //{
                    //    using (StreamWriter file = new StreamWriter(@"UseLog.txt", true))
                    //    {
                    //        file.WriteLine(DateTime.Today.ToString("d") + " " + DateTime.Now.ToString("h:mm:ss tt") + " Mode 2");
                    //    }

                    //    tempLabel.Text = String.Format("T: {0:##0.0}°C", IPC2.GetTempTarget(0));
                    //    depthLabel.Refresh();
                    //    tempLabel.Refresh();
                    //    switchtToDepthImage();
                    //}
                    else if (showDepth)
                    {
                        using (StreamWriter file = new StreamWriter(@"UseLog.txt", true))
                        {
                            file.WriteLine(DateTime.Today.ToString("d") + " " + DateTime.Now.ToString("h:mm:ss tt") + " Mode 3");
                        }
                        //depthLabel.Text = "";
                        //tempLabel.Text = String.Format("T: {0:##0.0}°C", IPC2.GetTempTarget(0));
                        //depthLabel.Refresh();
                        //tempLabel.Refresh();

                        //switchToThermoImage();

                        switchImage();

                    }
                    else if (showThermo)
                    {
                        using (StreamWriter file = new StreamWriter(@"UseLog.txt", true))
                        {
                            file.WriteLine(DateTime.Today.ToString("d") + " " + DateTime.Now.ToString("h:mm:ss tt") + " Mode 2");
                        }
                        //depthLabel.Text = "";
                        //tempLabel.Text = String.Format("T: {0:##0.0}°C", IPC2.GetTempTarget(0));
                        //depthLabel.Refresh();
                        //tempLabel.Refresh();

                        //switchtToDepthImage();

                        switchImage();

                    }
                    break;
                /*
            case MouseButtons.Right:
                if (!(showRGB || showDepth || showThermo))
                {
                    using (StreamWriter file = new StreamWriter(@"UseLog.txt", true))
                    {
                        file.WriteLine(DateTime.Today.ToString("d") + " " + DateTime.Now.ToString("h:mm:ss tt") + " Mode 3");
                    }
                    //tempLabel.Text = String.Format("T: {0:##0.0}°C", IPC2.GetTempTarget(0));
                    //depthLabel.Text = "";
                    //switchToThermoImage();
                    //depthLabel.Refresh();
                    tempLabel.Refresh();
                }
                else if (showThermo)
                {
                    using (StreamWriter file = new StreamWriter(@"UseLog.txt", true))
                    {
                        file.WriteLine(DateTime.Today.ToString("d") + " " + DateTime.Now.ToString("h:mm:ss tt") + " Mode 2");
                    }
                    //tempLabel.Text = String.Format("T: {0:##0.0}°C", IPC2.GetTempTarget(0));
                    //depthLabel.Text = "";
                    switchtToDepthImage();
                    //depthLabel.Refresh();
                    //tempLabel.Refresh();
                }
                else if (showDepth)
                {
                    using (StreamWriter file = new StreamWriter(@"UseLog.txt", true))
                    {
                        file.WriteLine(DateTime.Today.ToString("d") + " " + DateTime.Now.ToString("h:mm:ss tt") + " Mode 3");
                    }
                    //tempLabel.Text = String.Format("T: {0:##0.0}°C", IPC2.GetTempTarget(0));
                    //depthLabel.Text = "";
                    switchToThermoImage();
                    //depthLabel.Refresh();
                    //tempLabel.Refresh();
                }
                //else if (showRGB)
                //{
                //    depthLabel.Text = depthLabel.Text = "";
                //    switchToThermoImage();

                //}

                break;
                 */
            }

        }
    }
}
