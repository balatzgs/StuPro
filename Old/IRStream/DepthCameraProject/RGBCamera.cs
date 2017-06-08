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
/// ToDo Zeichnen algorithmus verbessern Kreuz kommplett nach Ende des erzeugten Bildes Zeichnen
namespace Camera.net
{

    public class RGBCamera
    {
        private readonly string SAMPLE_XML_FILE = @"../../../Data/SamplesConfig.xml";
        private Context context;
        private ScriptNode scriptNode;
        private IRGenerator rgbImage;
        private MainWindow mainWindow;
        private DepthGenerator depthGen;

        public RGBCamera()
        {
            this.context = Context.CreateFromXmlFile(SAMPLE_XML_FILE, out scriptNode);
            this.rgbImage = context.FindExistingNode(NodeType.IR) as IRGenerator;
            //this.depthGen = context.FindExistingNode(NodeType.Depth) as DepthGenerator;
            //this.depthGen.AlternativeViewpointCapability.SetViewpoint(rgbImage);
            //if (this.rgbImage == null)
            //{
            //    throw new Exception("Viewer must have a depth node!");
            //}
        }

        public void setMainWindow(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        public unsafe BitmapData getData(BitmapData data)
        {
           IRMetaData rgbMD = new IRMetaData();
            try
            {
                this.context.WaitOneUpdateAll(this.rgbImage);
            }
            catch (Exception)
            {
            }

            this.rgbImage.GetMetaData(rgbMD);

            byte* imstp = (byte*)rgbMD.IRMapPtr.ToPointer();
            byte* pDest = (byte*)data.Scan0.ToPointer();
            for (int y = 0; y < rgbMD.YRes; ++y)
            {
                for (int x = 0; x < rgbMD.XRes; x++, pDest += 3, imstp+=2)
                {
                    pDest[0] = imstp[0];
                    pDest[1] = imstp[0];
                    pDest[2] = imstp[0];

                    //rotes Kruez
                    if ((y == (rgbMD.YRes / 2)) && (x >= (rgbMD.XRes / 2) - 5) && (x <= (rgbMD.XRes / 2) + 5)
                           || (x == (rgbMD.XRes / 2) && y >= (rgbMD.YRes / 2) - 5 && y <= (rgbMD.YRes / 2) + 5))
                    {
                        pDest[0] = 0;
                        pDest[1] = 0;
                        pDest[2] = 255;

                    }
                }
            }




            return data;
        }

    }
}
