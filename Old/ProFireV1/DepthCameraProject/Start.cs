using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace Camera.net
{
    static class Start
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            DateTime start;
            DateTime end;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
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
                RGBCamera rgbCam = new RGBCamera();
                MainWindow mainWindow = new MainWindow(depthCam, rgbCam);
                depthCam.setMainWindow(mainWindow);
                rgbCam.setMainWindow(mainWindow);
                Application.Run(mainWindow);

            }
            catch (System.Exception ex)
            {
                ex.ToString();
                // MessageBox.Show("Error: " + ex.Message, "DepthCamera", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show("Error: " + "Tiefenbild Kamera nicht angeschlossen", "TiefenSensor", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

