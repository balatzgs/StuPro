
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
using System.Threading.Tasks;
using System.IO;

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
        private unsafe void CalcHist(ushort[,] array)
        {

            // reset
            for (int i = 0; i < this.histogram.Length; ++i)
            {
                this.histogram[i] = 0;
            }




            int points = 0;

            for (int y = 0; y < 240; ++y)
            {
                for (int x = 0; x < 320; ++x)
                {
                    ushort depthVal = array[x, y];
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

            //DepthMetaData depthMD = new DepthMetaData();

            //try
            //{
            //    this.context.WaitOneUpdateAll(this.depth);
            //}
            //catch (Exception)
            //{
            //}

            //this.depth.GetMetaData(depthMD);



            //calculate histogramm[] which has depthValue for each imagepoint
            //CalcHist(dilation(calculateMiddleValue()));
            //ushort[,] middleValueArray = dilation(calculateMiddleValue());
            ushort[,] middleValueArray = calculateMiddleValue();
            CalcHist(middleValueArray);
            //pointer for depthMap
            ushort* pDepth = (ushort*)this.depth.DepthMapPtr.ToPointer();


            //draw depth bitMap
            for (int y = 0; y < 240; ++y)
            {
                //pDest pointer for colorvalues at specific imagepoint
                //1 imagepoint = 3 values(r,g,b) -> 1 imagepoint has 3 colorvalues 
                //data.Stride = 3*xRes (xRes=360)
                byte* pDest = (byte*)data.Scan0.ToPointer() + y * data.Stride;

                for (int x = 0; x < 320; ++x, ++pDepth, pDest += 3)
                {
                    //if ((y == 240 / 2) && (x == 320 / 2))
                    //{
                    //    mainWindow.updateDistanceLabel(*pDepth);
                        
                    //}

         


                    if (*pDepth> 1100 || *pDepth == 0)
                    {
                        byte pixel = (byte)this.histogram[*pDepth];
                        pDest[0] = 255;
                        pDest[1] = 255;
                        pDest[2] = 255;
                    }
                    else
                    {
                        pDest[0] = 0;
                        pDest[1] = 0;
                        pDest[2] = 0;
                    }
                    //byte pixel = (byte)this.histogram[*pDepth];
                    //pDest[0] = pixel;
                    //pDest[1] = pixel;
                    //pDest[2] = pixel;


                }
            }
            return data;
        }


        public unsafe void getDistance()
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
            mainWindow.updateDistanceLabel(*(pDepth + midpoint));


        }


        /*
         * Methode zur Berechnung des Tiefenmittelwertes aus 3 verschiedenen Bildern
         * 
         */
        public unsafe ushort[,] calculateMiddleValue()
        {

            // Tiefenbildobjekte erzeugen
            DepthMetaData depthMD1 = new DepthMetaData();
            DepthMetaData depthMD2 = new DepthMetaData();
            DepthMetaData depthMD3 = new DepthMetaData();

            ushort[,] depthMD_middleValue = new ushort[320, 240];


            // 1. Tiefenbild updaten
            try
            {
                this.context.WaitOneUpdateAll(this.depth);
            }
            catch (Exception)
            {
            }

            //Tiefendaten des 1. Bildes auslesen und speichern
            this.depth.GetMetaData(depthMD1);
            // Zugriff auf Tiefendaten der Pixel mittels Pointer 1
            ushort* pDepth1 = (ushort*)depthMD1.DepthMapPtr.ToPointer();

            //// 2. Tiefenbild updaten
            //try
            //{
            //    this.context.WaitOneUpdateAll(this.depth);
            //}
            //catch (Exception)
            //{
            //}

            ////Tiefendaten des 2. Bildes auslesen und speichern
            //this.depth.GetMetaData(depthMD2);
            //// Zugriff auf Tiefendaten der Pixel mittels Pointer 2
            //ushort* pDepth2 = (ushort*)depthMD2.DepthMapPtr.ToPointer();

            //// 3. Tiefenbild updaten
            //try
            //{
            //    this.context.WaitOneUpdateAll(this.depth);
            //}
            //catch (Exception)
            //{
            //}
            ////Tiefendaten des 3. Bildes auslesen und speichern
            //this.depth.GetMetaData(depthMD3);

            //// Zugriff auf Tiefendaten der Pixel mittels Pointer 3
            //ushort* pDepth3 = (ushort*)depthMD3.DepthMapPtr.ToPointer();



            // Durch alle drei Tiefenobjete iterieren und daraus Mittelwert berechnen
            for (int y = 0; y < 240; ++y)
            {
                //for (int x = 0; x < 320; ++x, ++pDepth1, ++pDepth2, ++pDepth3)
                //{
                    for (int x = 0; x < 320; ++x, ++pDepth1)
                {
                    ////Mittelwert berechenen wobei beim Auftreten von zwei Nullwerten kein Mittelwert genommen wird
                    //if ((*pDepth1 + *pDepth2 + *pDepth3) == *pDepth1 || (*pDepth1 + *pDepth2 + *pDepth3) == *pDepth2 || (*pDepth1 + *pDepth2 + *pDepth3) == *pDepth3)
                    //{
                    //    //depthMD_middleValue[x, y] = Math.Max(Math.Max(Convert.ToUInt16(*pDepth1), Convert.ToUInt16(*pDepth2)), Convert.ToUInt16(*pDepth3));
                    //    depthMD_middleValue[x, y] = Convert.ToUInt16((*pDepth1 + *pDepth2 + *pDepth3));
                    //}
                    //else
                    //{
                    //    //depthMD_middleValue[x, y] = Convert.ToUInt16((*pDepth1 + *pDepth2 + *pDepth3) / 3);
                    //    depthMD_middleValue[x, y] = Math.Max(Math.Max(Convert.ToUInt16(*pDepth1), Convert.ToUInt16(*pDepth2)), Convert.ToUInt16(*pDepth3));
                    //}

                    depthMD_middleValue[x, y] = Convert.ToUInt16(*pDepth1);
                }
            }





            return depthMD_middleValue;
        }


        /*
         * Methode zur Durchführung der Dilatation
         * Hierbei wird das Array durchlaufen und alle Nullstellen(schwarze Punkte) mit Tiefenwerten 
         * der Nachbarstellen gefüllt
         * 
         */
        public unsafe ushort[,] dilation(ushort[,] array)
        {



            /*
             * For-Schleife wird nur 318*238 durchlaufen, damit beim Vergleich mit Nachbartellen das Index nicht überläuft
             */

            for (int y = 1; y < 239; ++y)
            {
                for (int x = 1; x < 319; ++x)
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
                        array[x, y] = Convert.ToUInt16((array[x, y- 1] + array[x + 1, y] + array[x, y + 1] + array[x - 1, y]) / 3);
                        array[x, y] = 1000;
                    



                    }
                }
            }
            return array;
        }

    }


}
