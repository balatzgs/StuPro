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

namespace Camera.net
{
   public class ThermalCamera
    {
        private DepthCamera depthCam;
        private ThermalCamera thermalCam;
        private RGBCamera rgbCam;
        private Bitmap bitmap;
        private Boolean showFusedImage =false;
        private Boolean showDepth = false;
        private Boolean showThermo = false;
        private Boolean showRGB = false;
 
#if _WIN64
        const Int32 OS = 64;
#else
        const Int32 OS = 32;
#endif
        public static readonly Int32 S_OK = 0;
        public static readonly Int32 S_FALSE = 1;

        Int32 FrameWidth, FrameHeight, FrameDepth, FrameSize;
        public IPC2 ipc;
        bool ipcInitialized = false, frameInitialized = false;
        bool Connected = false;
        bool Colors = false;

        byte[] rgbValues;
        Int16[] Values;

        bool Painted = false;

        private MainWindow mainWindow;


        public void setMainWindow(MainWindow mainWindow,DepthCamera depthCam,RGBCamera rgbCam)
        {
            this.mainWindow = mainWindow;
            this.depthCam = depthCam;
            this.rgbCam = rgbCam;
        }
        public void setShowFusedImage(Boolean value)
        {
            this.showFusedImage = value;
        }
        public void setShowThermalImage(Boolean value)
        {
            this.showThermo = value;
        }
        public void setShowDepthImage(Boolean value)
        {
            this.showDepth = value;
        }
        public void setShowRGBImage(Boolean value)
        {
            this.showRGB = value;
        }

        public Boolean isShowFusedImage()
        {
            return showFusedImage;
        }
        public Boolean isShowThermalImage()
        {
            return showThermo;
        }
        public Boolean isShowDepthImage()
        {
            return showDepth;
        }
        public Boolean isShowRGBImage()
        {
            return showRGB;
        }


        public void thermoRenderStart()
        {
            Init(382, 288, 2);
            ipc = new IPC2(1);
#if POLLING 
	        Application.Idle +=  new EventHandler(this.Application_Idle);
#endif
            this.mainWindow.enableTimer2();
        }
        public void closeThermoRender()
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
                        if ((y == (FrameHeight / 2) && x >= (FrameWidth / 2) - 3 && x <= (FrameWidth / 2) + 3)
                           || (x == (FrameWidth / 2) && y >= (FrameHeight / 2) - 3 && y <= (FrameHeight / 2) + 3))
                        {
                            rgbValues[dst] = 0;
                            rgbValues[dst + 1] = 0;
                            rgbValues[dst + 2] = 255;
                        }
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
                        if ((y == (FrameHeight / 2) && x >= (FrameWidth / 2) - 2 && x <= (FrameWidth / 2) + 2)
                           || (x == (FrameWidth / 2) && y >= (FrameHeight / 2) - 2 && y <= (FrameHeight / 2) + 2))
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
            this.mainWindow.enableTimer1();
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
            this.mainWindow.drawBlackScreen();
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

            if (showThermo || showFusedImage)
            {

                for (Int32 x = 0; x < FrameSize; x++)
                    Values[x] = Marshal.ReadInt16(data, x * 2);
                //timer1_tick welches Painted Flag setzt schein benoetigt zu sein
                //falls ohne Painted haengt sich das Bild manchmal auf 
               
                    if (showFusedImage)
                    {
                        if (!Painted)
                        {
                        this.bitmap = new Bitmap(382 * 2, 288, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        Rectangle rect = new Rectangle(0, 0, this.bitmap.Width, this.bitmap.Height);
                        BitmapData dataThermo = this.bitmap.LockBits(rect, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                        dataThermo = this.depthCam.getOverlapedBitmap(dataThermo, Values);
                        this.bitmap.UnlockBits(dataThermo);
                        this.mainWindow.reenderImage(bitmap);
                        Painted = true;
                        }
                    }
                    else if(showThermo)
                    {
                        if (!Painted)
                        {
                            this.bitmap = new Bitmap(382, 288, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                            GetBitmap(bitmap, Values);
                            this.depthCam.getDistanceCentrePoint();
                            this.mainWindow.reenderImage(bitmap);
                            Painted = true;
                        }
                   }
                   


            }
            else if(showDepth)
            {
                //hier eventuell spaeter if(!Painted) wegmachen dann lauft alles vielleicht schneller
                // darf nicht weggemacht ansonsten funktioneir templabel nicht mehr
                if (!Painted)
                {
                    this.bitmap = new Bitmap(382, 288, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    //this.bitmap = new Bitmap(640, 480, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    Rectangle rect = new Rectangle(0, 0, this.bitmap.Width, this.bitmap.Height);
                    BitmapData dataDepth = this.bitmap.LockBits(rect, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    dataDepth = this.depthCam.getData382(dataDepth);
                    this.bitmap.UnlockBits(dataDepth);
                    this.mainWindow.reenderImage(bitmap);
                    Painted = true;
                }
            }
            else if (showRGB)
            {
                if (!Painted)
                {
                    this.bitmap = new Bitmap(320,240, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    //this.bitmap = new Bitmap(640, 480, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    Rectangle rect = new Rectangle(0, 0, this.bitmap.Width, this.bitmap.Height);
                    BitmapData dataRGB = this.bitmap.LockBits(rect, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    dataRGB = this.rgbCam.getData(dataRGB);
                    this.bitmap.UnlockBits(dataRGB);
                    this.mainWindow.reenderImage(bitmap);
                    Painted = true;
                }
            }
           

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
        public void timer1_tick()
        {
            MainTimer100ms();
        }

        public void timer2_tick()
        {
            if (!ipcInitialized || !Connected) InitIPC();
        }

    }
}
