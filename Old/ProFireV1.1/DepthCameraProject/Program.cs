using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


    public static class Program
    {
        public static int procId = 0;
        public static Process blackscreen;


        public static void toggle()
        {
            try
            {
                var pId = procId;
                blackscreen.Kill();
                blackscreen.StartInfo.Arguments = "-c";
                blackscreen.Start();
            }
            catch (InvalidOperationException)
            {
                blackscreen.StartInfo.Arguments = "-e";
                blackscreen.Start();
            }
            catch (Exception ex)
            {
                blackscreen.StartInfo.Arguments = "-e";
                blackscreen.Start();
            }
        }
    }
