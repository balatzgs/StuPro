﻿
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

        private readonly string SAMPLE_XML_FILE = @"../../../Data/SamplesConfig.xml";
        private Context context;
        private ScriptNode scriptNode;
        private DepthGenerator depth;
        private ImageGenerator rgbImage;
        private int[] histogram;
        private MainWindow mainWindow;

        // rotations Vektor
        //rotation Matrix
        double x1r = 0.999953035145459;
        double x2r = -0.00370103092875861;
        double x3r = -0.00895711300859757;
        double y1r = 0.00425451687122014;
        double y2r = 0.998030611241891;
        double y3r = 0.000625843279929575; //0.0625843279929575;
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
        //double fx = 566.797339802501;
        //double fy = 580.760036485533;
        double fx = 554.70427736082;
        double fy = 556.8066856654182;
        //principalpoint von waermecamera
        double cx = 194.949595761411;
        double cy = 152.782323849735;

        // focallength depthCam
        double fx2 = 265.438177175451*2;
        double fy2 = 264.815761475803*2;
        // principalpoint depthCam
        double cx2 = 163.373277701732*2;
        double cy2 = 121.886735406953*2;

        //radial distortion von waerme camera
        //double k1 = -0.182651997215065;
        //double k2 = -0.0708124075849761;
        //double k3 = 2.87950761399306;
        //tangential distortion von waerme camera
        //double p1 = 0.0;
        //double p2 = 0.0;
        // float
        //double skew = 0;


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
            MapOutputMode mapMode = this.depth.MapOutputMode;
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
            //ushort[,] depthArray = dilation382(transform(depthToArray()));
            ushort[,] depthArray = transform(depthToArray());
            CalcHist382(depthArray);

            //pointer for depthMap

            for (int y = 0; y < 288; ++y)
            {

                byte* pDest = (byte*)data.Scan0.ToPointer() + y * data.Stride;

                for (int x = 0; x < 382; x++, pDest += 3)
                {



                    if (depthArray[x, y] > 500)
                    {
                        byte pixel = (byte)this.histogram[depthArray[x, y]];

                        pDest[0] = pixel; //blau
                        pDest[1] = pixel; //gruen
                        pDest[2] = 0; //rot


                        if ((y == (288 / 2) && x >= (382 / 2) - 1 && x <= (382 / 2) + 1)
                            || (x == (382 / 2) && y >= (288 / 2) - 1 && y <= (288 / 2) + 1))
                        {
                            pDest[0] = 0;
                            pDest[1] = 0;
                            pDest[2] = 255;

                        }
                    }
                }
            }
            mainWindow.distanceLabelUpdate(depthArray[191, 144]);
            return data;
        }

        /**
 * Füllt die Lücken, die nach der Kalibrierung entstehen
 * TODO: Noch Relativ unperformant
 */
        public unsafe ushort[,] dilation382(ushort[,] array)
        {
            ushort[,] newArray = array;
            for (int p = 0; p < 3; p++)
            {
                int[] minValue;

                for (int y = 4; y < 283; ++y)
                {
                    for (int x = 4; x < 377; ++x)
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

                             //sprung 4 pixel horizontal
                            else if (Math.Abs(array[x - 4, y] - array[x + 4, y]) < gap && array[x - 2, y] != 0)
                            {
                                myResult = (ushort)Math.Min(array[x - 4, y], array[x + 4, y]);
                            }
                            //sprung 4 pixel vertikal
                            else if (Math.Abs(array[x, y - 4] - array[x, y + 4]) < gap && array[x, y + 4] != 0)
                            {
                                myResult = (ushort)Math.Min(array[x, y - 4], array[x, y + 4]);
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
            //ushort[,] depthArray = dilation640(depthToArray());
            ushort[,] depthArray = depthToArray();

            CalcHist640(depthArray);

            //pointer for depthMap

            for (int y = 0; y < 480; ++y)
            {

                byte* pDest = (byte*)data.Scan0.ToPointer() + y * data.Stride;

                for (int x = 0; x < 640; ++x, pDest += 3)
                {

                    byte pixel = (byte)this.histogram[depthArray[x, y]];


                    pDest[0] = pixel;
                    pDest[1] = pixel;
                    pDest[2] = 0;


                    if (x == 640 / 2 && y == 480 / 2)
                    {
                        pDest[0] = 0;
                        pDest[1] = 0;
                        pDest[2] = 255;
                        mainWindow.distanceLabelUpdate(depthArray[x, y]);
                    }


                }
            }
            return data;
        }

        /*
 * Ausfüllen des Kalibrierten Bildes für das große bild
 */
        public unsafe ushort[,] dilation640(ushort[,] array)
        {
            //ushort[,] newArray = array;
            //for (int p = 0; p < 1; p++)
            //{
            //    int[] minValue;

            //    for (int y = 4; y < 475; ++y)
            //    {
            //        for (int x = 4; x < 635; ++x)
            //        {
            //            if (newArray[x, y] == 0)
            //            {
            //                minValue = new int[8];
            //                minValue[0] = newArray[x - 1, y - 1];
            //                minValue[1] = newArray[x, y - 1];
            //                minValue[2] = newArray[x + 1, y - 1];

            //                minValue[3] = newArray[x - 1, y];
            //                minValue[4] = newArray[x + 1, y];

            //                minValue[5] = newArray[x - 1, y + 1];
            //                minValue[6] = newArray[x, y + 1];
            //                minValue[7] = newArray[x + 1, y + 1];

            //                //0 1 2
            //                //3 $ 4
            //                //5 6 7

            //                int numEqualElements;
            //                int maxNumOfEqualElements = 0;
            //                int currentMinValue = 0;
            //                for (int i = 0; i < 8; i++)
            //                {
            //                    numEqualElements = 0;
            //                    if (minValue[i] != 0)
            //                    {
            //                        foreach (ushort value in minValue)
            //                        {
            //                            if (Math.Abs(minValue[i] - value) < 50)
            //                            {
            //                                numEqualElements++;
            //                            }
            //                        }
            //                        //holt Wert der am haeufigsten im arrayBereich vorkommt in currentAverage 
            //                        // 0 pruefung war oben also 0 kann hier nicht vorkommen
            //                        if (numEqualElements > maxNumOfEqualElements)
            //                        {
            //                            maxNumOfEqualElements = numEqualElements;
            //                            currentMinValue = minValue[i];
            //                        }
            //                    }
            //                }
            //                //Evtl auch auf 4 setzen dadurch wird evtl mehr ausgefuellt
            //                if (maxNumOfEqualElements > 4)
            //                {
            //                    newArray[x, y] = (ushort)currentMinValue;
            //                }
            //            }
            //        }
            //    }
            //}
            //return newArray;

            /*
             * For-Schleife wird nur 318*238 durchlaufen, damit beim Vergleich mit Nachbartellen das Index nicht überläuft
             */

            for (int y = 1; y < 479; ++y)
            {
                for (int x = 1; x < 639; ++x)
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

            //double r2 = xS * xS + yS * yS;

            //double xSS = (1 + k1 * r2 + k2 * r2 * r2 + k3 * r2 * r2 * r2) * xS + 2 * p1 * xS * yS + p2 * (r2 + 2 * xS * xS);
            //double ySS = (1 + k1 * r2 + k2 * r2 * r2 + k3 * r2 * r2 * r2) * yS + p1 * (r2 + 2 * yS * yS) + 2 * p2 * yS * xS;

            //double u = fx * (xSS + skew * ySS) + cx;
            //double v = fy * ySS + cy;

            double u = (fx * xS) + cx;
            double v = (fy * yS) + cy;

            int[] result = new int[2];
            result[0] = (int)Math.Round(u);
            result[1] = (int)Math.Round(v);

            //result[0] = (int)u;
            //result[1] = (int)v;

            return result;
        }

        /**
         * Transformaton
         */
        private ushort[,] transform(ushort[,] oldArray)
        {
            xTrans = 27.3009721742182;
            yTrans = 49.4186755906782;
            zTrans = 26.0094219765227;
        
            // neue Array initialisieren
            ushort[,] newArray = new ushort[382,288];
            // Berechnung in das neue Bild
            for (int y = 70; y < 370; ++y)
            {
                for (int x = 100; x < 510; ++x)
                {
                    ushort depthInMeter = oldArray[x, y];
                  
                    if (depthInMeter > 800)
                    {
                        if (depthInMeter < 1500)
                        {
                            xTrans = 29.3009721742182;
                            yTrans = 0.4186755906782;
                            zTrans = 26.0094219765227;

                        }

                        // Brauchen wir wenn wir mit dem Tiefenbild kalibrieren
                        //Point3D depth3D = new Point3D(x, y, depthInMeter);
                        //Point3D real3D = depth.ConvertProjectiveToRealWorld(depth3D);

                        //int[] result = calibrateImagePoint(real3D.X, -real3D.Y, real3D.Z);
                        
                        double zDist = depthInMeter;
                        double x3D = (double)(x - cx2) * zDist / fx2;
                        double y3D = (double)(y - cy2) * zDist / fy2;
                        double z3D = zDist;
                        int[] result = calibrateImagePoint(x3D, y3D, z3D);
                        
 
                        // Für Kalibrierung mit Tiefenbild

                        // Verhindert OutOfBounds von newArray, nur PUnkte die in dem Array liegen bekommen Tiefenwerte
                        if (!((result[0]) > 381 || (result[0]) < 0
                            || (result[1]  )> 287 || (result[1] ) < 0))
                        {
                            newArray[result[0] , result[1] ] = depthInMeter;
                        }
                       
                    }
                }
            }

            return newArray;
        }

        /**
         * Überlagerung 
         */
        public unsafe BitmapData getDataOverlap(BitmapData data)
        {

            //calculate histogramm[] which has depthValue for each imagepoint
            ushort[,] depthArray = dilation382(transform(depthToArray()));
            CalcHist382(depthArray);

            //Zeichne bei Konturen Linien ein
            for (int y = 1; y < 287; ++y)
            {

                byte* pDest = (byte*)data.Scan0.ToPointer() + y * data.Stride;

                for (int x = 1; x < 381; x++, pDest += 3)
                {
                    if (depthArray[x, y] > 1000)
                    {
                        byte pixel = (byte)this.histogram[depthArray[x, y]];
                        if (x % 2 == 0 || y % 2 == 0)
                        {
                            pDest[0] = pixel;
                            pDest[1] = 0;
                            pDest[2] = 0;
                        }

                        /*
                         * Kantendetektion einfach mal drin lassen
                        */
                        //matrix = new int[9];

                        //int sum = 0;
                        //matrix[0] = depthArray[x - 1, y - 1];
                        //matrix[1] = depthArray[x, y - 1];
                        //matrix[2] = depthArray[x + 1, y - 1];

                        //matrix[3] = depthArray[x - 1, y];
                        //matrix[4] = depthArray[x, y];
                        //matrix[5] = depthArray[x + 1, y];

                        //matrix[6] = depthArray[x - 1, y + 1];
                        //matrix[7] = depthArray[x, y + 1];
                        //matrix[8] = depthArray[x + 1, y + 1];

                        //// aussehen Matrix $ ist jetztiger Punkt
                        //// 0 1 2
                        //// 3 $ 4
                        //// 5 6 7
                        //int gap = 100;
                        //int gap2 = 100;
                        //if (depthArray[x, y] > 1000)
                        //{
                        //    //gap = 100;
                        //}
                        //else
                        //{
                        //    //gap = 80;
                        //}
                        //foreach (ushort value in matrix)
                        //{
                        //    if (value == 0)
                        //    {
                        //        sum++;
                        //    }

                        //}
                        //if (depthArray[x, y] == 0)
                        //{
                        //    sum--;
                        //}

                        //if (sum < 3)
                        //{

                        //}


                        //else if (Math.Abs(matrix[0] - matrix[4]) > gap && matrix[4] != 0)
                        //{
                        //    pDest[0] = 255;
                        //    pDest[1] = 255;
                        //    pDest[2] = 255;
                        //    pDest[4] = 255;
                        //    pDest[5] = 255;
                        //    pDest[6] = 255;

                        //}
                        //else if (Math.Abs(matrix[1] - matrix[4]) > gap && matrix[4] != 0)
                        //{
                        //    pDest[0] = 255;
                        //    pDest[1] = 255;
                        //    pDest[2] = 255;
                        //    pDest[4] = 255;
                        //    pDest[5] = 255;
                        //    pDest[6] = 255;
                        //}
                        //else if (Math.Abs(matrix[2] - matrix[4]) > gap && matrix[4] != 0)
                        //{
                        //    pDest[0] = 255;
                        //    pDest[1] = 255;
                        //    pDest[2] = 255;
                        //    pDest[4] = 255;
                        //    pDest[5] = 255;
                        //    pDest[6] = 255;
                        //}
                        //else if (Math.Abs(matrix[5] - matrix[4]) > gap && matrix[4] != 0)
                        //{
                        //    pDest[0] = 255;
                        //    pDest[1] = 255;
                        //    pDest[2] = 255;
                        //    pDest[4] = 255;
                        //    pDest[5] = 255;
                        //    pDest[6] = 255;

                        //}

                        //else if (Math.Abs(matrix[6] - matrix[4]) > gap && matrix[4] != 0)
                        //{
                        //    pDest[0] = 255;
                        //    pDest[1] = 255;
                        //    pDest[2] = 255;
                        //    pDest[4] = 255;
                        //    pDest[5] = 255;
                        //    pDest[6] = 255;
                        //}
                        //else if (Math.Abs(matrix[7] - matrix[4]) > gap && matrix[4] != 0)
                        //{
                        //    pDest[0] = 255;
                        //    pDest[1] = 255;
                        //    pDest[2] = 255;
                        //    pDest[4] = 255;
                        //    pDest[5] = 255;
                        //    pDest[6] = 255;
                        //}
                        //else if (Math.Abs(matrix[8] - matrix[4]) > gap && matrix[4] != 0)
                        //{
                        //    pDest[0] = 255;
                        //    pDest[1] = 255;
                        //    pDest[2] = 255;
                        //    pDest[4] = 255;
                        //    pDest[5] = 255;
                        //    pDest[6] = 255;
                        //}
                    }
                }
            }
            mainWindow.distanceLabelUpdate(depthArray[191, 144]);
            return data;
        }

        byte LoByte(Int16 val) { return BitConverter.GetBytes(val)[0]; }
        byte HiByte(Int16 val) { return BitConverter.GetBytes(val)[1]; }
        byte clip(Int32 val) { return (byte)((val <= 255) ? ((val > 0) ? val : 0) : 255); }

        public unsafe BitmapData getOverlapedBitmap(BitmapData data, short[] values)
        {
            ushort[,] depthArray = transform(depthToArray());
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
                }
                a = 0;
                b++;
                //isPixelRGB = !isPixelRGB;
                if (b > 287)
                {
                    b = 0;
                    c = true;
                }
            }
            return data;
        }

        

    }
}