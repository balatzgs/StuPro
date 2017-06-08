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

namespace Camera.net
{

    public class RGBCamera
    {
        private readonly string SAMPLE_XML_FILE = @"../../../Data/SamplesConfig.xml";
        private Context context;
        private ScriptNode scriptNode;
        private ImageGenerator rgbGen;
        private DepthGenerator depthGen;
        private MainWindow mainWindow;
        private DepthCamera depthCamera;

        public RGBCamera(DepthCamera depthCamera){
            this.context = Context.CreateFromXmlFile(SAMPLE_XML_FILE, out scriptNode);
            this.rgbGen = context.FindExistingNode(NodeType.Image) as ImageGenerator;
            this.depthGen = context.FindExistingNode(NodeType.Depth) as DepthGenerator;
            this.depthCamera = depthCamera;
            //calibration of rgb and depth
            this.depthGen.AlternativeViewpointCapability.SetViewpoint(rgbGen);
            if (this.rgbGen == null)
            {
                throw new Exception("Viewer must have a depth node!");
            }
        }

        public void setMainWindow(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        public unsafe BitmapData getData(BitmapData data)
        {
            ImageMetaData rgbMD = new ImageMetaData();
            try
            {
                this.context.WaitOneUpdateAll(this.rgbGen);
            }
            catch (Exception)
            {
            }

            this.rgbGen.GetMetaData(rgbMD);
             
                byte* imstp = (byte*) rgbMD.ImageMapPtr.ToPointer();
                byte* pDest = (byte*)data.Scan0.ToPointer();
                for (int y = 0; y < rgbMD.YRes; ++y)
                {
                    for (int x = 0; x < rgbMD.XRes; x++, pDest += 3, imstp += 3)
                    {
                        pDest[0] = imstp[2];
                        pDest[1] = imstp[1];
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
