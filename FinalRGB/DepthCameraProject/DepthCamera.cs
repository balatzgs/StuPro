
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
using System.Linq;

namespace Camera.net
{

    public class DepthCamera
    {

        private readonly string SAMPLE_XML_FILE = @"SamplesConfig.xml";
        private Context context;
        private ScriptNode scriptNode;
        private DepthGenerator depth;
        private ImageGenerator rgbImage;
        private int[] histogram;
        private MainWindow mainWindow;
        private int[,] posX_BeforeTransformed;
        private int[,] posY_BeforeTransformed;
        private int depthOfMidRGB;
        // rotations Vektor
        //rotation Matrix
        //double x1r = 0.999953035145459;
        //double x2r = -0.00370103092875861;
        //double x3r = -0.00895711300859757;
        //double y1r = 0.00425451687122014;
        //double y2r = 0.998030611241891;
        //double y3r = 0.000625843279929575; //0.0625843279929575;
        //double z1r = 0.00870784643737582;
        //double z2r = -0.0626194969175093;
        //double z3r = 0.997999484977934;
        double x1r = 1.0;
        double x2r = 0.0;
        double x3r = 0.0;
        double y1r = 0.0;
        double y2r = 1.0;
        double y3r = 0.0;
        double z1r = 0.0;
        double z2r = 0.0;
        double z3r = 1.0;

        // translation Vektor
        double xTrans = 27.3009721742182;
        double yTrans = 49.4186755906782;
        double zTrans = 26.0094219765227;


        //focalpoint von WaermeCamera
        double fx = 554.70427736082;
        double fy = 556.8066856654182;
        //principalpoint von waermecamera
        double cx = 194.949595761411;
        double cy = 152.782323849735;

        // focallength depthCam
        double fx2 = 265.438177175451 * 2;
        double fy2 = 264.815761475803 * 2;
        // principalpoint depthCam
        double cx2 = 163.373277701732 * 2;
        double cy2 = 121.886735406953 * 2;



        /** 
         * Konstruktor
         * 
         */
        public DepthCamera()
        {
            this.context = Context.CreateFromXmlFile(SAMPLE_XML_FILE, out scriptNode);
            this.depth = context.FindExistingNode(NodeType.Depth) as DepthGenerator;
            this.rgbImage = context.FindExistingNode(NodeType.Image) as ImageGenerator;
            this.depth.AlternativeViewpointCapability.SetViewpoint(rgbImage);
            if (this.depth == null)
            {
                throw new Exception("Viewer must have a depth node!");
            }
            if (this.rgbImage == null)
            {
                throw new Exception("Viewer must have a depth node!");
            }

            this.histogram = new int[this.depth.DeviceMaxDepth];
            //MapOutputMode mapMode = new MapOutputMode();
            //mapMode.XRes = 640;
            //mapMode.YRes = 480;
            //mapMode.FPS = 30;
            //depth.MapOutputMode = mapMode;
            //rgbImage.MapOutputMode = mapMode;
            posX_BeforeTransformed = new int[382,288];
            posY_BeforeTransformed = new int[382,288];
        }

        /**
         * Zugriff auf DistanceLabelUpdate
         * TODO: Eventuell ändern/ besser machen
         */
        public void setMainWindow(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        /**
        * 
        */
        private unsafe ushort[,] depthToArray()
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

            //muss unbedingt danach ansosnten depthmd.xres oder yres keinen wert
            ushort[,] depthArray = new ushort[depthMD.XRes, depthMD.YRes];

            ushort* pDepth1 = (ushort*)depthMD.DepthMapPtr.ToPointer();

            for (int y = 0; y < depthMD.YRes; ++y)
            {
                for (int x = 0; x < depthMD.XRes; ++x, ++pDepth1)
                {
                    depthArray[x, y] = Convert.ToUInt16(*pDepth1);
                }
            }

            return depthArray;
        }

        /**
         * Histogramm berechnen für das kleine Bild
         * 
         */
        private unsafe void CalcHist382(ushort[,] depthArray)
        {
            // Histogramm auf null setzen
            for (int i = 0; i < this.histogram.Length; ++i)
            {
                this.histogram[i] = 0;
            }


            int points = 0;

            // Rgb-Wert aussuchen: Weit-> Hoher Wert ; Nah -> niederiger Wert
            for (int y = 0; y < 288; ++y)
            {
                for (int x = 0; x < 382; ++x)
                {
                    ushort depthVal = depthArray[x, y];
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

        /**
         * Holt Bitmapdaten für das kleine Tiefenbild 382x
         */
        public unsafe BitmapData getData382(BitmapData data)
        {

            //calculate histogramm[] which has depthValue for each imagepoint
            ushort[,] depthArray = dilation382(transform(depthToArray()));
            //ushort[,] depthArray = dilation640(dilation382(transform(depthToArray())));

            CalcHist382(depthArray);

            //pointer for depthMap

            for (int y = 0; y < 288; ++y)
            {

                byte* pDest = (byte*)data.Scan0.ToPointer() + y * data.Stride;

                for (int x = 0; x < 382; x++, pDest += 3)
                {

                        byte pixel = (byte)this.histogram[depthArray[x, y]];

                        pDest[0] = pixel; //blau
                        pDest[1] = pixel; //gruen
                        pDest[2] = pixel; //rot


                        if ((y == (288 / 2) && x >= (382 / 2) - 5 && x <= (382 / 2) + 5)
                            || (x == (382 / 2) && y >= (288 / 2) - 5 && y <= (288 / 2) + 5))
                        {
                            pDest[0] = 0;
                            pDest[1] = 0;
                            pDest[2] = 255;

                        }

                }
            }
            mainWindow.distanceLabelUpdate(depthArray[191, 144]);
            return data;
        }

        /**
 * Füllt die Lücken, die nach der Kalibrierung entstehen
 */
        public unsafe ushort[,] dilation382(ushort[,] array)
        {
            ushort[,] newArray = array;
            for (int p = 0; p < 1; p++)
            {
                int[] minValue;

                for (int y = 1; y < 287; ++y)
                {
                    for (int x = 1; x < 381; ++x)
                    {
                        if (newArray[x, y] == 0)
                        {
                            minValue = new int[8];
                            ushort min = 9999;
                            ushort myResult = 0;

                            int sum = 0;
                            minValue[0] = newArray[x - 1, y - 1];
                            minValue[1] = newArray[x, y - 1];
                            minValue[2] = newArray[x + 1, y - 1];

                            minValue[3] = newArray[x - 1, y];
                            minValue[4] = newArray[x + 1, y];

                            minValue[5] = newArray[x - 1, y + 1];
                            minValue[6] = newArray[x, y + 1];
                            minValue[7] = newArray[x + 1, y + 1];

                            //0 1 2
                            //3 $ 4
                            //5 6 7
                            foreach (ushort value in minValue)
                            {
                                if (value == 0)
                                {
                                    sum++;
                                }
                                if (value < min && value != 0)
                                {
                                    min = value;
                                }

                            }

                            int gap = 60;

                            //vertikal,horizontal,diagonal
                            if (Math.Abs(minValue[3] - minValue[4]) < gap && minValue[3] != 0)
                            {
                                myResult = (ushort)Math.Min(minValue[3], minValue[4]);
                            }
                            else if (Math.Abs(minValue[1] - minValue[6]) < gap && minValue[1] != 0)
                            {
                                myResult = (ushort)Math.Min(minValue[1], minValue[6]);
                            }
                            else if (Math.Abs(minValue[0] - minValue[7]) < gap && minValue[0] != 0)
                            {
                                myResult = (ushort)Math.Min(minValue[0], minValue[7]);
                            }
                            else if (Math.Abs(minValue[2] - minValue[5]) < gap && minValue[1] != 0)
                            {
                                myResult = (ushort)Math.Min(minValue[2], minValue[5]);
                            }
                            //..........................................................................
                            else if (Math.Abs(minValue[0] - minValue[1]) < gap && Math.Abs(minValue[0] - minValue[2]) < gap &&
                                Math.Abs(minValue[0] - minValue[7]) < 10 && minValue[0] != 0)
                            {
                                myResult = (ushort)((minValue[0] + minValue[1] + minValue[2] + minValue[7]) / 4);
                            }
                            else if (Math.Abs(minValue[5] - minValue[6]) < gap && Math.Abs(minValue[5] - minValue[7]) < gap &&
                                Math.Abs(minValue[5] - minValue[4]) < gap && minValue[5] != 0)
                            {
                                myResult = (ushort)((minValue[5] + minValue[6] + minValue[7] + minValue[7]) / 4);
                            }
                            else if (Math.Abs(minValue[0] - minValue[3]) < gap &&
                               Math.Abs(minValue[0] - minValue[3]) < 20 && Math.Abs(minValue[0] - minValue[4]) < gap && minValue[0] != 0)
                            {
                                myResult = (ushort)((minValue[0] + minValue[3] + minValue[4]) / 3);
                            }
                            newArray[x, y] = myResult;

                            if (newArray[x, y] > 9999)
                            {
                                newArray[x, y] = 0;
                            }
                        }
                    }
                }

            }
            return array;
        }

        /**
         * Histogramm für das große Bild 
         */
        private unsafe void CalcHist640(ushort[,] depthArray)
        {
            // reset
            for (int i = 0; i < this.histogram.Length; ++i)
            {
                this.histogram[i] = 0;
            }


            int points = 0;

            for (int y = 0; y < 480; ++y)
            {
                for (int x = 0; x < 640; ++x)
                {
                    ushort depthVal = depthArray[x, y];
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

        /**
         * Holt Bitmapdaten für das große Tiefenbild 640x480
         */
        public unsafe BitmapData getData640(BitmapData data)
        {
            //for dilatation
            //ushort[,] depthArray = dilation640(depthToArray());
            ushort[,] depthArray = depthToArray();

            CalcHist640(depthArray);

            //pointer for depthMap

            for (int y = 0; y < 640; ++y)
            {

                byte* pDest = (byte*)data.Scan0.ToPointer() + y * data.Stride;

                for (int x = 0; x < 480; ++x, pDest += 3)
                {

                    byte pixel = (byte)this.histogram[depthArray[x, y]];


                    pDest[0] = pixel;
                    pDest[1] = pixel;
                    pDest[2] = pixel;

                    //if ((y == (480 / 2) && x >= (640 / 2) - 5 && x <= (640 / 2) + 5)
                    //        || (x == (640 / 2) && y >= (480 / 2) - 5 && y <= (480 / 2) + 5))
                    //{
                    //    pDest[0] = 0;
                    //    pDest[1] = 0;
                    //    pDest[2] = 255;

                    //}
                    //if (x == 640 / 2 && y == 480 / 2)
                    //{
                    //    mainWindow.distanceLabelUpdate(depthArray[x, y]);
                    //}


                }
            }
            return data;
        }

        /*
 * Ausfüllen des Kalibrierten Bildes für das große bild
 */
        public unsafe ushort[,] dilation640(ushort[,] array)
        {
            for (int y = 1; y < 287; ++y)
            {
                for (int x = 1; x < 381; ++x)
                {
                    if (array[x, y] == 0)
                    {

                        /*
                         * 0 1 0
                         * 1 2 1
                         * 0 1 0
                         * 
                         * Mit dieser Struktur werden die Werte der zentralen Stelle(2) gefüllt
                         * beginnend von links im Uhrzeigersinn
                         * 
                         */
                        array[x, y] = Convert.ToUInt16((array[x, y - 1] + array[x + 1, y] + array[x, y + 1] + array[x - 1, y]) / 4);
                        if (array[x, y] > 1000)
                        {
                            array[x, y] = 0;
                        }
                    }
                }
            }
            return array;
        }

        /**
         * Distanz für den Mittelunkt
         */
        public unsafe void getDistanceCentrePoint()
        {
            ushort[,] depthArray = dilation382(transform(depthToArray()));
            mainWindow.distanceLabelUpdate(depthArray[191, 144]);
        }

        /**
         * Kalibrierung
         * */
        private int[] calibrateImagePoint(double xReal, double yReal, double zReal)
        {
            double xRealCamera2 = x1r * xReal + x2r * yReal + x3r * zReal + xTrans;
            double yRealCamera2 = y1r * xReal + y2r * yReal + y3r * zReal + yTrans;
            double zRealCamera2 = z1r * xReal + z2r * yReal + z3r * zReal + zTrans;

            double xS = xRealCamera2 / zRealCamera2;
            double yS = yRealCamera2 / zRealCamera2;

            double u = (fx * xS) + cx;
            double v = (fy * yS) + cy;

            int[] result = new int[2];
            result[0] = (int)Math.Round(u);
            result[1] = (int)Math.Round(v);

            return result;
        }

        /**
         * Transformaton
         */
        private ushort[,] transform(ushort[,] oldArray)
        {

            // neue Array initialisieren
            ushort[,] newArray = new ushort[382, 288];

            // Berechnung in das neue Bild

          for (int y = 70; y < 370; ++y)
            {
                for (int x = 100; x < 510; ++x)
                {
                    ushort depthInMeter = oldArray[x, y];

                    if (depthInMeter > 600)
                    {
                        double zDist = depthInMeter;
                        double x3D = (double)(x - cx2) * zDist / fx2;
                        double y3D = (double)(y - cy2) * zDist / fy2;
                        double z3D = zDist;
                        int[] result = calibrateImagePoint(x3D, y3D, z3D);

                        //super Verschiebefunktion für werte zwischen 600 und 1500
                        if (depthInMeter > 600 && depthInMeter < 1500)
                        {
                            result[1] = (int)(result[1] + 0.0000000366 * Math.Pow(depthInMeter, 3) - 0.00018901 * Math.Pow(depthInMeter, 2) + 0.3248141277 * depthInMeter - 194.9221714206);
                        }
                        if (depthInMeter > 3500)
                        {
                            result[1] = (int)(result[1] + 15);
                        }

                        
                        // Verhindert OutOfBounds von newArray, nur PUnkte die in dem Array liegen bekommen Tiefenwerte
                        if (!((result[0]) > 381 || (result[0]) < 0
                            
                            || (result[1]) > 287 || (result[1]) < 0))
                        {
                            newArray[result[0], result[1]] = depthInMeter;
                            posX_BeforeTransformed[result[0],result[1]] = x;
                            posY_BeforeTransformed[result[0],result[1]] = y;
                            if (result[0] == 191 && result[1] == 144)
                            {
                                mainWindow.distanceLabelUpdate(depthInMeter);
                            }
                         
                        }

                    }
                }
            }
            return newArray;
        }
        public void transformForRGB()
        {
            posX_BeforeTransformed = new int[382,288];
            posY_BeforeTransformed = new int[382,288];
            transform(depthToArray());
           
        }
        public int[,] getPosX_transformed()
        {
            return posX_BeforeTransformed;
        }
        public int[,] getPosY_transformed()
        {
            return posY_BeforeTransformed;
        }

        byte LoByte(Int16 val) { return BitConverter.GetBytes(val)[0]; }
        byte HiByte(Int16 val) { return BitConverter.GetBytes(val)[1]; }
        byte clip(Int32 val) { return (byte)((val <= 255) ? ((val > 0) ? val : 0) : 255); }

        public unsafe BitmapData getOverlapedBitmap(BitmapData data, short[] values)
        {
            ushort[,] depthArray = dilation382(transform(depthToArray()));
            CalcHist382(depthArray);
            bool isPixelRGB = true;
            byte* pDest = (byte*)data.Scan0.ToPointer();
            int a = 0;
            int b = 0;
            bool c = false;
            int stride_diff = data.Stride - 382 * 3 * 2;

            for (Int32 dst = 0, src = 0, y = 0; y < 288; y++, dst += stride_diff)
            {
                for (Int32 x = 0; x < 2 * 382; x++, pDest += 3)
                {
                    
                    if (isPixelRGB && !c)
                    {
                        byte pixel = (byte)this.histogram[depthArray[a, b]];
                        pDest[0] = pixel;
                        pDest[1] = pixel;
                        pDest[2] = pixel;
                        isPixelRGB = false;
                        ++a;
                        if (a > 381)
                        {
                            a = 0;
                        }
                    }
                    else
                    {
                        Int32 C = (Int32)LoByte(values[src]) - 16;
                        Int32 D = (Int32)HiByte(values[src - (src % 2)]) - 128;
                        Int32 E = (Int32)HiByte(values[src - (src % 2) + 1]) - 128;
                        pDest[dst] = clip((298 * C + 516 * D + 128) >> 8);
                        pDest[dst + 1] = clip((298 * C - 100 * D - 208 * E + 128) >> 8);
                        pDest[dst + 2] = clip((298 * C + 409 * E + 128) >> 8);
                        src++;
                        isPixelRGB = true;
                    }
                    if ((y == (288 / 2) && x >= 382 - 5 && x <= 382 + 5)
                          || (x == 382 && y >= (288 / 2) - 5 && y <= (288 / 2) + 5))
                    {
                        pDest[0] = 0;
                        pDest[1] = 0;
                        pDest[2] = 255;                       
                    }
                   
                }
              
                a = 0;
                b++;
                isPixelRGB = !isPixelRGB;
                if (b > 287)
                {
                    b = 0;
                    c = true;
                }
            }
            mainWindow.distanceLabelUpdate(depthArray[191, 144]);
            return data;
        }
    }
}