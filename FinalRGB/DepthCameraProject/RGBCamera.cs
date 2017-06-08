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
        private readonly string SAMPLE_XML_FILE = @"SamplesConfig.xml";
        private Context context;
        private ScriptNode scriptNode;
        private ImageGenerator rgbImage;
        private MainWindow mainWindow;
        private DepthGenerator depthGen;
        private DepthCamera depthCam;
        private int[,] posX_transformed;
        private int[,] posY_transformed;

        public RGBCamera(DepthCamera depthCam)
        {
            this.context = Context.CreateFromXmlFile(SAMPLE_XML_FILE, out scriptNode);
            this.rgbImage = context.FindExistingNode(NodeType.Image) as ImageGenerator;
            this.depthGen = context.FindExistingNode(NodeType.Depth) as DepthGenerator;
            this.depthGen.AlternativeViewpointCapability.SetViewpoint(rgbImage);
            //MapOutputMode mapMode = new MapOutputMode();
            //mapMode.XRes = 640;
            //mapMode.YRes = 480;
            //mapMode.FPS = 30;
            //rgbImage.MapOutputMode = mapMode;
            this.depthCam = depthCam;
            if (this.rgbImage == null)
            {
                throw new Exception("Viewer must have a depth node!");
            }
        }

        public void setMainWindow(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        public unsafe BitmapData getDataKalib(BitmapData data)
        {
            ImageMetaData rgbMD = new ImageMetaData();
            try
            {
                this.context.WaitOneUpdateAll(this.rgbImage);
            }
            catch (Exception)
            {
            }

            this.rgbImage.GetMetaData(rgbMD);
            this.depthCam.transformForRGB();
            posX_transformed = depthCam.getPosX_transformed();
            posY_transformed = depthCam.getPosY_transformed();

            byte* imstp = (byte*)rgbMD.ImageMapPtr.ToPointer();
            byte* pDest = (byte*)data.Scan0.ToPointer();
            for (int y = 0; y < 288; ++y)
            {
               pDest = (byte*)data.Scan0.ToPointer() + y * data.Stride;
                for (int x = 0; x <382; x++, pDest += 3)
                {
                    pDest[0] = *(imstp + posX_transformed[x, y] * 3 + posY_transformed[x, y] * 3 * 640 + 2);
                    pDest[1] = *(imstp + posX_transformed[x, y] * 3 + posY_transformed[x, y] * 3 * 640 + 1);
                    pDest[2] = *(imstp + posX_transformed[x, y] * 3 + posY_transformed[x, y] * 3 * 640);

                        if ((y == (288 / 2) && x >= (382 / 2) - 5 && x <= (382 / 2) + 5)
                            || (x == (382 / 2) && y >= (288 / 2) - 5 && y <= (288 / 2) + 5))
                        {
                            pDest[0] = 0;
                            pDest[1] = 0;
                            pDest[2] = 255;

                        }
                    //if (x == 381)
                    //{
                    //    for (int i = 382; i < 640; i++)
                    //    {
                    //        //imstp += 3;
                    //        pDest += 3;
                    //        pDest[0] = 0;
                    //        pDest[1] = 0;
                    //        pDest[2] = 0;
                    //    }
                    //}
                }
            }
            return data;
        }

        public unsafe BitmapData getData(BitmapData data)
        {
            ImageMetaData rgbMD = new ImageMetaData();
            try
            {
                this.context.WaitOneUpdateAll(this.rgbImage);
            }
            catch (Exception)
            {
            }

            this.rgbImage.GetMetaData(rgbMD);

            byte* imstp = (byte*)rgbMD.ImageMapPtr.ToPointer();
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


 //for (int y = 0; y < rgbMD.YRes; ++y)
 //           {
 //               for (int x = 0; x < rgbMD.XRes; x++, pDest += 3, imstp += 3)
 //               {
 //                   pDest[0] = imstp[2];
 //                   pDest[1] = imstp[1];
 //                   pDest[2] = imstp[0];

 //                   //rotes Kruez
 //                   if ((y == (rgbMD.YRes / 2)) && (x >= (rgbMD.XRes / 2) - 5) && (x <= (rgbMD.XRes / 2) + 5)
 //                          || (x == (rgbMD.XRes / 2) && y >= (rgbMD.YRes / 2) - 5 && y <= (rgbMD.YRes / 2) + 5))
 //                   {
 //                       pDest[0] = 0;
 //                       pDest[1] = 0;
 //                       pDest[2] = 255;

 //                   }
 //               }
 //           }