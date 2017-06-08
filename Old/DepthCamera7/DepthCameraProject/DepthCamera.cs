
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
        private RGBCamera rgbCam;

        private int[] histogram;
        private MainWindow mainWindow;

        /// <summary>
        /// test
        /// </summary>
        public DepthCamera(RGBCamera rgbCam)
        {
            // InitializeComponent();
            //Context? SkriptNode? 

            this.context = Context.CreateFromXmlFile(SAMPLE_XML_FILE, out scriptNode);
            this.depth = context.FindExistingNode(NodeType.Depth) as DepthGenerator;
            this.rgbCam=rgbCam;
            if (this.depth == null)
            {
                throw new Exception("Viewer must have a depth node!");
            }

            this.histogram = new int[this.depth.DeviceMaxDepth];

            MapOutputMode mapMode = this.depth.MapOutputMode;



        }

        public void setMainWindow(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }



        /// <summary>
        /// maybe not needed ?
        /// </summary>
        /// <param name="depthMD"></param>
        private unsafe void CalcHist(DepthMetaData depthMD)
        {
            
                
            // reset
            for (int i = 0; i < this.histogram.Length; ++i)
            {
                this.histogram[i] = 0;
            }

            ushort* pDepth = (ushort*)depthMD.DepthMapPtr.ToPointer();

            int points = 0;
            for (int y = 0; y < depthMD.YRes; ++y)
            {
                for (int x = 0; x < depthMD.XRes; ++x, ++pDepth)
                {
                    ushort depthVal = *pDepth;
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

            CalcHist(depthMD);

            ushort* pDepth = (ushort*)this.depth.DepthMapPtr.ToPointer();
            
            bool isPixelRGB = true;
            
            //RGB Data
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
            
            
            // set pixels
            //hier wird gezeichnet pDest[0] = blau, pdest[1] = gruen,pdest[3] = rot
            for (int y = 0; y < 2*depthMD.YRes; ++y)
            {
                byte* pDest2 = (byte*)data.Scan0.ToPointer() + y * data.Stride;
                for (int x = 0; x < depthMD.XRes; ++x, ++pDepth, pDest2 += 3)
                {
                    if ((y == 2*depthMD.YRes / 2) && (x == 2*depthMD.XRes / 2))
                    {
                        mainWindow.labelUpdate(*pDepth);

                    }
                    byte pixel = (byte)this.histogram[*pDepth];
                    
                    if(isPixelRGB)
                    {
                    
                        pDest2[0] = pixel;
                        pDest2[1] = pixel;
                        pDest2[2] = pixel;
                        isPixelRGB=true;
                    }else
                    {
                        pDest[0] = imstp[2];
                        pDest[1] = imstp[1];
                        pDest[2] = imstp[0];
                        pdest += 3
                        imstp += 3
                        isPixelRGB=false;
                    }


                    //rotes Kruez
                    if ((y == (depthMD.YRes / 2) && x >= (depthMD.XRes / 2) - 5 && x <= (depthMD.XRes / 2) + 5)
                        || (x == (depthMD.XRes / 2) && y >= (depthMD.YRes / 2) - 5 && y <= (depthMD.YRes / 2) + 5))
                    {
                        pDest[0] = 0;
                        pDest[1] = 0;
                        pDest[2] = 255;

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

            CalcHist(depthMD);

            ushort* pDepth = (ushort*)this.depth.DepthMapPtr.ToPointer();

            // set pixels
            //hier wird gezeichnet pDest[0] = blau, pdest[1] = gruen,pdest[3] = rot
            for (int y = 0; y < depthMD.YRes; ++y)
            {
                byte* pDest = (byte*)data.Scan0.ToPointer() + y * data.Stride;
                for (int x = 0; x < depthMD.XRes; ++x, ++pDepth, pDest += 3)
                {
                    if ((y == depthMD.YRes / 2) && (x == depthMD.XRes / 2))
                    {
                        mainWindow.labelUpdate(*pDepth);

                    }

                }
            }
        }


        /*
          * Methode zur Berechnung des Tiefenmittelwertes aus 3 verschiedenen Bildern
          * 
          */
        public unsafe DepthMetaData calculateMiddleValue()
        {

            // Tiefenbildobjekte erzeugen
            DepthMetaData depthMD1 = new DepthMetaData();
            DepthMetaData depthMD2 = new DepthMetaData();
            DepthMetaData depthMD3 = new DepthMetaData();
            DepthMetaData depthMD_middleValue = new DepthMetaData();


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

            // 2. Tiefenbild updaten
            try
            {
                this.context.WaitOneUpdateAll(this.depth);
            }
            catch (Exception)
            {
            }

            //Tiefendaten des 2. Bildes auslesen und speichern
            this.depth.GetMetaData(depthMD2);
            // Zugriff auf Tiefendaten der Pixel mittels Pointer 2
            ushort* pDepth2 = (ushort*)depthMD2.DepthMapPtr.ToPointer();

            // 3. Tiefenbild updaten
            try
            {
                this.context.WaitOneUpdateAll(this.depth);
            }
            catch (Exception)
            {
            }
            //Tiefendaten des 3. Bildes auslesen und speichern
            this.depth.GetMetaData(depthMD3);

            // Zugriff auf Tiefendaten der Pixel mittels Pointer 3
            ushort* pDepth3 = (ushort*)depthMD3.DepthMapPtr.ToPointer();

            int* pDepth4 = (int*)depthMD_middleValue.DepthMapPtr.ToPointer();


            // Durch alle drei Tiefenobjete iterieren und daraus Mittelwert berechnen
            for (int y = 0; y < depthMD1.YRes; ++y)
            {
                for (int x = 0; x < depthMD1.XRes; ++x, ++pDepth1, ++pDepth2, ++pDepth3, ++pDepth4)
                {

                    //Mittelwert berechenen wobei beim Auftreten von zwei Nullwerten kein Mittelwert genommen wird
                    if ((*pDepth1 + *pDepth2 + *pDepth3) == *pDepth1 || (*pDepth1 + *pDepth2 + *pDepth3) == *pDepth2 || (*pDepth1 + *pDepth2 + *pDepth3) == *pDepth3)
                    {
                        *pDepth4 = (*pDepth1 + *pDepth2 + *pDepth3);

                    }
                    else
                    {
                        *pDepth4 = (*pDepth1 + *pDepth2 + *pDepth3) / 3;
                    }


                }
            }

            return depthMD_middleValue;
        }


    }
}
