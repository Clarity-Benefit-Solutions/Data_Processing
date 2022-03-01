using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using CoreUtils.Classes;
using StackExchange.Profiling;

namespace TestApp
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Thread.CurrentThread.Name = "Main";

            // profiling
#if PROFILE
            var profiler = MiniProfiler.StartNew("TestApp");
#endif

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());

            //
#if PROFILE
            Console.WriteLine(profiler.RenderPlainText());
            Console.WriteLine(profiler.GetTimingHierarchy().ToString());
#endif
        }
    }
}