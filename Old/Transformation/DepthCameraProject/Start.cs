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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                DepthCamera depthCam = new DepthCamera();
                RGBCamera rgbCam = new RGBCamera();
                MainWindow mainWindow = new MainWindow(depthCam,rgbCam);
                depthCam.setMainWindow(mainWindow);
                rgbCam.setMainWindow(mainWindow);
                Application.Run(mainWindow);
            }
            catch (System.Exception ex)
            {
               // MessageBox.Show("Error: " + ex.Message, "DepthCamera", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show("Error: " + "Tiefenbild Kamera nicht angeschlossen", "TiefenSensor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

