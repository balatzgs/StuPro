
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

    public class DepthCamera
    {

        private readonly string SAMPLE_XML_FILE = @"../../../Data/SamplesConfig.xml";

        private Context context;
        private ScriptNode scriptNode;
        private DepthGenerator depth;

        private int[] histogram;
        private MainWindow mainWindow;

        /// <summary>
        /// test
        /// </summary>
        public DepthCamera()
        {
            this.context = Context.CreateFromXmlFile(SAMPLE_XML_FILE, out scriptNode);
            this.depth = context.FindExistingNode(NodeType.Depth) as DepthGenerator;
            if (this.depth == null)
            {
                throw new Exception("Viewer must have a depth node!");
            }
            this.histogram = new int[this.depth.DeviceMaxDepth];
            MapOutputMode mapMode = this.depth.MapOutputMode;

            //            MessageBox.Show(depthMD.XRes.ToString(), "Some title",
            //MessageBoxButtons.OK, MessageBoxIcon.Error);  
        }

        public void setMainWindow(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }


        /// <summary>
        /// assign each depth value a "colorvalue" 
        /// </summary>
        /// <param name="depthMD"></param>
        private unsafe void CalcHist(ushort[,] depthArray)
        {
            // reset
            for (int i = 0; i < this.histogram.Length; ++i)
            {
                this.histogram[i] = 0;
            }


            int points = 0;
            for (int y = 0; y <288; ++y)
            {
                for (int x = 0; x < 382; ++x)
                {
                    ushort depthVal = depthArray[x,y];
                    if (depthVal != 0)
                    {
                        this.histogram[depthVal]++;
                        points++;

                    }
                }
            }


            for (int i = 1; i < this.histogram.Length; i++)
            {
                this.histogram[i] += this.histogram[i - 1];
            }

            if (points > 0)
            {
                for (int i = 1; i < this.histogram.Length; i++)
                {
                    this.histogram[i] = (int)(256 * (1.0f - (this.histogram[i] / (float)points)));
                }
            }
        }
        public unsafe BitmapData getData(BitmapData data)
        {
            DepthMetaData depthMD = new DepthMetaData();

            try
            {
                this.context.WaitOneUpdateAll(this.depth);
            }
            catch (Exception)
            {
            }

            this.depth.GetMetaData(depthMD);


            //calculate histogramm[] which has depthValue for each imagepoint

            ushort[,] depthArray = depthToArray();
            CalcHist(depthArray);

            //pointer for depthMap

            for (int y = 0; y < depthMD.YRes; ++y)
            {

                byte* pDest = (byte*)data.Scan0.ToPointer() + y * data.Stride;

                for (int x = 0; x < depthMD.XRes; ++x,  pDest += 3)
                {

                    byte pixel = (byte)this.histogram[depthArray[x,y]];
                    pDest[0] = pixel;
                    pDest[1] = pixel;
                    pDest[2] = pixel;
               
          
                    if (x == 160 && y == 120)
                    {
                        pDest[0] = 0;
                        pDest[1] = 0;
                        pDest[2] = 255;

                    }
                    if ((y == depthMD.YRes / 2) && (x == depthMD.XRes / 2))
                    {
                        mainWindow.distanceLabelUpdate(depthArray[x,y]);
                    }

                }
            }
            return data;
        }


        public unsafe void getDistanceForRGB(BitmapData data)
        {
            DepthMetaData depthMD = new DepthMetaData();

            try
            {
                this.context.WaitOneUpdateAll(this.depth);
            }
            catch (Exception)
            {
            }

            this.depth.GetMetaData(depthMD);
            //CalcHist(depthMD);

            ushort* pDepth = (ushort*)this.depth.DepthMapPtr.ToPointer();
            //*(pDepth) gets the value from pointer pDepth
            //*(pDepth+position) gets the value from pointer pDepth which has moved to "position"
            int midpoint = (depthMD.YRes / 2) * (depthMD.XRes) - 160;
            mainWindow.distanceLabelUpdate(*(pDepth + midpoint));



        }

        public unsafe BitmapData getTest(BitmapData data)
        {


            //focallength depthCam
            float fx = 264.9284f;
            float fy = 262.2864f;
            //principalpoint depthCam
            float cx = 161.7026f;
            float cy = 0;
            float zDist=670f;
            float x3D = (160 - cx) * zDist / fx;
            float y3D = (120 - cy) * zDist / fy;
            float z3D = zDist;


            ////ushort a = *pDepth;
            //Point3D depth3D = new Point3D(160, 120, 502);
            //Point3D real3D = depth.ConvertProjectiveToRealWorld(depth3D);
            //float realValue = real3D.X;
            ////mainWindow.updateTempLabel(realValue.ToString());
            //mainWindow.updateTempLabel(real3D.X.ToString() + "," + real3D.Y.ToString() + "," + real3D.Z.ToString());
            //int[] result;
            //result = calculateNewPoint(x3D, y3D, z3D);
            //result = calculateNewPoint(0, 0, 833);
            //mainWindow.updateTempLabel("ha");
            //mainWindow.updateTempLabel(result[0].ToString() + ", " + result[1].ToString());
            //mainWindow.updateTempLabel(x3D.ToString() + ", " + y3D.ToString());

            //draw depth bitMap
            for (int y = 0; y < 288; ++y)
            {
                byte* pDest = (byte*)data.Scan0.ToPointer() + y * data.Stride;

                for (int x = 0; x < 382; ++x, pDest += 3)
                {

                    //if (x == 173 && y == 126)
                    //{
                    //    pDest[0] = 255;
                    //    pDest[1] = 0;
                    //    pDest[2] = 0;
                    //}
                    if (x == 160 && y == 120)
                    {
                        pDest[0] = 0;
                        pDest[1] = 0;
                        pDest[2] = 255;

                    }

                }
            }
            return data;
        }

        //rotation Matrix
        double x1r = 0.999953035145459;
        double x2r = -0.00370103092875861;
        double x3r = -0.00895711300859757;
        double y1r = 0.00425451687122014;
        double y2r = 0.998030611241891;
        double y3r = 0.00625843279929575; //0.0625843279929575;
        double z1r = 0.00870784643737582;
        double z2r = -0.0626194969175093;
        double z3r = 0.997999484977934;
        //double x1r = 1.0;
        //double x2r = 0.0;
        //double x3r = 0.0;
        //double y1r = 0.0;
        //double y2r = 1.0;
        //double y3r = 0.0;
        //double z1r = 0.0;
        //double z2r = 0.0;
        //double z3r = 1.0;

        // translation Vektor
        double xTrans = 27.3009721742182;
        double yTrans = 49.4186755906782;
        double zTrans = 26.0094219765227;
        //focalpoint von WaermeCamera
        double fx = 566.797339802501;
        double fy = 580.760036485533;
        //principalpoint von waermecamera
        double cx = 194.949595761411;
        double cy = 152.782323849735;
        //radial distortion von waerme camera
        double k1 = -0.182651997215065;
        double k2= -0.0708124075849761;
        double k3 = 2.87950761399306;
        //tangential distortion von waerme camera
        double p1 = 0.0;
        double p2 = 0.0;

        // float
        double skew = 0;

        private int[] calculateNewPoint(double xReal, double yReal, double zReal)
        {
            double xRealCamera2 = x1r * xReal + x2r * yReal + x3r * zReal + xTrans;
            double yRealCamera2 = y1r * xReal + y2r * yReal + y3r * zReal + yTrans;
            double zRealCamera2 = z1r * xReal + z2r * yReal + z3r * zReal + zTrans;

            double xS = xRealCamera2 / zRealCamera2;
            double yS = yRealCamera2 / zRealCamera2;

            //double r2 = xS * xS + yS * yS;

            //double xSS = (1 + k1 * r2 + k2 * r2 * r2 + k3 * r2 * r2 * r2) * xS + 2 * p1 * xS * yS + p2 * (r2 + 2 * xS * xS);
            //double ySS = (1 + k1 * r2 + k2 * r2 * r2 + k3 * r2 * r2 * r2) * yS + p1 * (r2 + 2 * yS * yS) + 2 * p2 * yS * xS;

            //double u = fx * (xSS + skew * ySS) + cx;
            //double v = fy * ySS + cy;

            double u = fx * xS + cx;
            double v = fy * yS + cy;

            int[] result = new int[2];
            result[0] = (int)Math.Round(u);
            result[1] = (int)Math.Round(v);

            //result[0] = (int)u;
            //result[1] = (int)v;

            return result;
        }


        public unsafe BitmapData getData2(BitmapData data)
        {
            DepthMetaData depthMD = new DepthMetaData();

            try
            {
                this.context.WaitOneUpdateAll(this.depth);
            }
            catch (Exception)
            {
            }

            this.depth.GetMetaData(depthMD);


            //calculate histogramm[] which has depthValue for each imagepoint
            ushort[,] depthArray = dilation(transform(depthToArray()));
            CalcHist(depthArray);



            //pointer for depthMap

            for (int y = 0; y<288; ++y)
            {

                byte* pDest = (byte*)data.Scan0.ToPointer() + y * data.Stride;

                for (int x =0; x <382; x++, pDest += 3)
                {

                    byte pixel = (byte)this.histogram[depthArray[x,y]];
                    pDest[0] = pixel;
                    pDest[1] = pixel;
                    pDest[2] = pixel;

                    //if (x == 160 && y == 120)
                    //{
                    //    pDest[0] = 0;
                    //    pDest[1] = 0;
                    //    pDest[2] = 255;
                    //    //int[] result;
                    //    //Point3D depth3D = new Point3D(160, 120, depthArray[x,y]);
                    //    //Point3D real3D = depth.ConvertProjectiveToRealWorld(depth3D);
                    //    //result = calculateNewPoint(real3D.X, real3D.Y,real3D.Z);
                    //    //mainWindow.updateTempLabel(result[0].ToString() + ", " + result[1].ToString());

                    //}
                    //rotes Kruez
                    if ((y == (288 / 2) && x >= (382 / 2) - 3 && x <= (382 / 2) + 3)
                        || (x == (382 / 2) && y >= (288 / 2) - 3 && y <= (288 / 2) + 3))
                    {
                        pDest[0] = 0;
                        pDest[1] = 0;
                        pDest[2] = 255;

                    }
                    

               

                }
            }
            return data;
        }


        private unsafe ushort[,] depthToArray()
        {

            ushort[,] depthArray = new ushort[320, 240];
            DepthMetaData depthMD = new DepthMetaData();

            try
            {
                this.context.WaitOneUpdateAll(this.depth);
            }
            catch (Exception)
            {
            }

            this.depth.GetMetaData(depthMD);
            ushort* pDepth1 = (ushort*)depthMD.DepthMapPtr.ToPointer();
            for (int y = 0; y < 240; ++y)
            {
                for (int x = 0; x < 320; ++x, ++pDepth1)
                {
                    depthArray[x, y] = Convert.ToUInt16(*pDepth1);
                }
            }

            return depthArray;
        }

        private ushort[,] transform(ushort[,] oldArray)
        {
            //neue Array initialisieren
            ushort[,] newArray = new ushort[382, 288];
            for (int y = 0; y < 288; ++y)
            {
                for (int x = 0; x < 382; ++x)
                {
                    newArray[x, y] = 0; 
                }
            }
            //...
            for (int y = 0; y < 240; ++y)
            {
                for (int x = 0; x < 320; ++x)
                {
                   ushort depthInMeter = oldArray[x,y];

                   //Point3D depth3D = new Point3D(x, y, depthInMeter);
                   //Point3D real3D = depth.ConvertProjectiveToRealWorld(depth3D);

                   // focallength depthCam
                   double fx = 265.438177175451;
                   double fy = 264.815761475803;
                   // principalpoint depthCam
                   double cx = 163.373277701732;
                   double cy = 121.886735406953;

                   double zDist = depthInMeter;
                   double x3D = (x - cx) * zDist / fx;
                   double y3D = (y - cy) * zDist / fy;
                   double z3D = zDist;
                   int[] result = calculateNewPoint(x3D, y3D, z3D);

                   //int[] result = calculateNewPoint(real3D.X, real3D.Y, real3D.Z);

                   if (result[0] > 381 || result[0] < 0||result[1] > 287|| result[1]<0)
                   {

                   }
                   else
                   {
                       newArray[result[0], result[1]] = depthInMeter;
                   }
                }
            }

           


            return newArray;
        }

        public unsafe ushort[,] dilation(ushort[,] array)
        {



            /*
             * For-Schleife wird nur 318*238 durchlaufen, damit beim Vergleich mit Nachbartellen das Index nicht überläuft
             */

            //for (int y = 1; y < 239; ++y)
            //{
            //    for (int x = 1; x < 319; ++x)
            //    {
            //        if (array[x, y] == 0)
            //        {

                        /*
                         * 0 1 0
                         * 1 2 1
                         * 0 1 0
                         * 
                         * Mit dieser Struktur werden die Werte der zentralen Stelle(2) gefüllt
                         * beginnend von links im Uhrzeigersinn
                         * 
                         */
                        //array[x, y] = Convert.ToUInt16((array[x, y - 1] + array[x + 1, y] + array[x, y + 1] + array[x - 1, y]) / 3);

                        //ushort value = Math.Max(array[x, y - 1], array[x, y+1]);
                        //value =  Math.Max(value, array[x, y + 1]);
                        //value =  Math.Max(value, array[x - 1, y]);
                        //array[x, y] = Convert.ToUInt16(value);
                        //array[x, y] = 1000;
                //    }
                //}
            //}
            return array;
        }
    }


}
