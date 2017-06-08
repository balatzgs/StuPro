using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Threading;



namespace Camera.net
{

    public static class Start
    {
      
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Blackscreen für die Brille
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //Program.blackscreen = new Process();
            //Program.blackscreen.StartInfo.FileName = "blackscreen.exe";
            //Program.blackscreen.StartInfo.CreateNoWindow = true;
            //Program.blackscreen.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            //Program.blackscreen.StartInfo.Arguments = "-c";
            //Program.blackscreen.Start();
            //try
            //{
            //    Program.procId = Program.blackscreen.Id;
            //}
            //catch (InvalidOperationException)
            //{
            //}
            //catch (Exception ex)
            //{
            //}


            DateTime start;
            DateTime end;
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            start = DateTime.Now;
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"UseLog.txt", true))
            {
                file.WriteLine();
                file.WriteLine("-----------------------------------------------------------------------------------------------");
                file.WriteLine("//Start (Mode1 = no image; Mode2 = Tiefenbild; Mode3 = Wärmebild;)");
                file.WriteLine("Application start at: " + start.ToString("d") + " " + DateTime.Now.ToString("h:mm:ss tt"));
                file.WriteLine();
                file.WriteLine(DateTime.Today.ToString("d") + " " + DateTime.Now.ToString("h:mm:ss tt") + " Mode 1 (Idle) ");
            }

            try
            {
                DepthCamera depthCam = new DepthCamera();
                //RGBCamera rgbCam = new RGBCamera();
                ThermalCamera thermalCam = new ThermalCamera();
                MainWindow mainWindow = new MainWindow(depthCam,thermalCam);
                thermalCam.setMainWindow(mainWindow,depthCam);
                depthCam.setMainWindow(mainWindow);
                //rgbCam.setMainWindow(mainWindow);
                Application.Run(mainWindow);

            }
            catch (System.Exception ex)
            {
                ex.ToString();
                MessageBox.Show(ex.ToString(), "DepthCamera", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //MessageBox.Show("Error: " + "Tiefenbild Kamera nicht angeschlossen", "TiefenSensor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            end = DateTime.Now;
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"UseLog.txt", true))
            {
                file.WriteLine();
                file.WriteLine("Application close: " + end.ToString("d") + " " + DateTime.Now.ToString("h:mm:ss tt"));
                file.WriteLine("//End");
                file.WriteLine();
                file.WriteLine("//Eval:");
                file.WriteLine("Total Duration in seconds:" + Math.Round((end - start).TotalSeconds, 0));
                file.WriteLine("Total Duration in minutes:" + Math.Round((end - start).TotalMinutes, 2));
                file.WriteLine("-----------------------------------------------------------------------------------------------");

            }


        }
    }
}

