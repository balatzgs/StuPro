//#define POLLING

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace IPC2
{
    public partial class FormMain : Form
    {
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
		Bitmap bmp;
        bool Connected = false;
        bool Colors = false;

		byte[] rgbValues;
		Int16[] Values;

		Int32 MainTimerDivider;
        bool Painted = false;

        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
        	Text += String.Format(" (Rel. {0} (x{1}))", Version, OS);

	        Init(160, 120, 2);
            ipc = new IPC2(1);
#if POLLING 
	        Application.Idle +=  new EventHandler(this.Application_Idle);
#endif
            timer2.Enabled = true;
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
#if POLLING 
	        Application.Idle -=  new EventHandler(this.Application_Idle);
#endif
	        ReleaseIPC();

        }

        void InitIPC() 
        {
	        Int64 hr;
            if ((ipc != null) && !ipcInitialized)
	         {
		        hr = IPC2.Init(0, "");
        		
		        if(hr != S_OK)
		        {
			        ipcInitialized = frameInitialized = false;
		        }
		        else
		        {
#if !POLLING 
                    ipc.OnServerStopped = new IPC2.delOnServerStopped(OnServerStopped);
                    IPC2.SetCallback_OnServerStopped(0, ipc.OnServerStopped);

                    ipc.OnFrameInit = new IPC2.delOnFrameInit(OnFrameInit);
                    Int32 u = IPC2.SetCallback_OnFrameInit(0, ipc.OnFrameInit);

                    ipc.OnNewFrameEx = new IPC2.delOnNewFrameEx(OnNewFrameEx);
                    IPC2.SetCallback_OnNewFrameEx(0, ipc.OnNewFrameEx);

                    ipc.OnInitCompleted = new IPC2.delOnInitCompleted(OnInitCompleted);
                    IPC2.SetCallback_OnInitCompleted(0, ipc.OnInitCompleted);
#endif
			        hr = IPC2.Run(0);
			        ipcInitialized = (hr == S_OK);
		        }
                //label1.Text = (hr != S_OK) ? "NOT CONNECTED" : "OK";

	         }
        }

        private void ReleaseIPC() 
        {
	        Connected = false;
	        if((ipc != null) && ipcInitialized)
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
	        System.Drawing.Imaging.BitmapData bmpData = Bmp.LockBits( rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, Bmp.PixelFormat );
	        stride_diff = bmpData.Stride - FrameWidth*3;

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
                    }
            }
            else
            {
                Int16 mn, mx;
                GetBitmap_Limits(values, out mn, out mx);
                double Fact = 255.0 / (mx - mn);

                for (Int32 dst = 0, src = 0, y = 0; y < FrameHeight; y++, dst += stride_diff)
                    for (Int32 x = 0; x < FrameWidth; x++, src++, dst += 3)
                        rgbValues[dst] = rgbValues[dst + 1] = rgbValues[dst + 2] = (byte)Math.Min(Math.Max((Int32)(Fact * (values[src] - mn)), 0), 255);
            }

	        // Copy the RGB values back to the bitmap
	        System.Runtime.InteropServices.Marshal.Copy( rgbValues, 0, ptr, rgbValues.Length );

	        // Unlock the bits.
	        Bmp.UnlockBits( bmpData );
        }

        void GetBitmap_Limits(Int16[] Values, out Int16 min, out Int16 max)
        {
	        Int32 y;
	        double Sum, Mean, Variance;
            min = Int16.MinValue;
            max = Int16.MaxValue;
	        if(Values == null) return;

	        Sum = 0;
	        for (y=0; y < FrameSize; y++ ) 
		        Sum += Values[y];
	        Mean = (double)Sum / FrameSize;
	        Sum = 0;
	        for (y=0; y < FrameSize; y++ ) 
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

        Int64 MainTimer500ms()
        {
            if (Connected)
            {
                //labelTempTarget.Text = String.Format("Target-Temp: {0:##0.0}�C", IPC2.GetTempTarget(0));
            }
	        return S_OK;
        }

        void Init(Int32 frameWidth, Int32 frameHeight, Int32 frameDepth)
        {
	        FrameWidth = frameWidth;
	        FrameHeight = frameHeight;
	        FrameSize = FrameWidth * FrameHeight;
	        FrameDepth = frameDepth;
            timer1.Enabled = true;
            bmp = new Bitmap( FrameWidth , FrameHeight , System.Drawing.Imaging.PixelFormat.Format24bppRgb );
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits( rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat );
            Int32 stride = bmpData.Stride;
            bmp.UnlockBits( bmpData );
            rgbValues = new Byte[stride * FrameHeight];
            Values = new Int16[FrameSize];
            pictureBox.Size = new System.Drawing.Size(FrameWidth, FrameHeight);
            UpdateSize();
	        frameInitialized = true;
        }

        void UpdateSize()
        {
	        Size = new System.Drawing.Size(pictureBox.Right + 20, Math.Max(0, pictureBox.Bottom) + 50);
        }

        Int32 OnServerStopped(Int32 reason)
        {
	        ReleaseIPC();
	        Graphics g = Graphics.FromImage(bmp);
	        g.FillRectangle(new SolidBrush(Color.Black), 0, 0, bmp.Width, bmp.Height);
	        pictureBox.Invalidate();
	        return 0;
        }

        Int32 OnFrameInit(Int32 frameWidth, Int32 frameHeight, Int32 frameDepth)
        {
	        Init(frameWidth, frameHeight, frameDepth);
	        return 0;
        } 

        // will work with Imager.exe release > 2.0 only:
        Int32 OnNewFrameEx(IntPtr data, IntPtr Metadata)
        {
            if (!frameInitialized)
                return S_FALSE;
            return NewFrame(data, (IPC2.FrameMetadata)Marshal.PtrToStructure(Metadata, typeof(IPC2.FrameMetadata)));
        }

        Int32 NewFrame(IntPtr data, IPC2.FrameMetadata Metadata)
        {

            for (Int32 x = 0; x < FrameSize; x++)
                Values[x] = Marshal.ReadInt16(data, x * 2);
            if (!Painted)
            {
                GetBitmap(bmp, Values);
                pictureBox.Invalidate();
                Painted = true;
            }

            return 0;
        }

        Int32 OnInitCompleted()
        {
            Colors = ((TIPCMode)IPC2.GetIPCMode(0) == TIPCMode.Colors);
	        Connected = true;
	        UpdateSize();
	        return S_OK;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (!ipcInitialized || !Connected) InitIPC();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Painted = false;
            MainTimerDivider++;
            MainTimer100ms();
            if ((MainTimerDivider % 5) == 0) MainTimer500ms();
        }
        private void pictureBox_Paint(object sender, PaintEventArgs e)
        {
               e.Graphics.DrawImage(bmp, 0, 0);
        }

        private void show_Temp_Click(object sender, PaintEventArgs e)
        {

        }

        private void pictureBox_Click(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (!show_temp.Visible)
                {
                    show_temp.BackColor = Color.Transparent;
                    show_temp.Visible = true;
                }
                else
                {
                    show_temp.Visible = false;
                }
            } else if (e.Button == MouseButtons.Right)
            {
                flag_Renew.Visible = true;
                flag_Renew.Text = String.Format("Renew ({0})", IPC2.RenewFlag(0) ? "Success" : "Failed");
                timer3.Enabled = true;
                timer3.Start();
            }
        }

        private void pictureBox_Click(object sender, EventArgs e)
        {

        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }

        private void flag_Renew_Click(object sender, EventArgs e)
        {

        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            timer3.Stop();
            flag_Renew.Visible = false;
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        private void flagToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void tempToolStripMenuItem_Click(object sender, EventArgs e)
        {
          
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

    }
}




