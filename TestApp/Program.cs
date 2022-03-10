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

            // Add the event handler for handling UI thread exceptions to the event.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += new ThreadExceptionEventHandler(UIThreadException);

            //            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());

            //
#if PROFILE
            Console.WriteLine(profiler.RenderPlainText());
            Console.WriteLine(profiler.GetTimingHierarchy().ToString());
#endif
        }

      

        public static void UIThreadException(object sender, ThreadExceptionEventArgs t)
        {
            DialogResult result = DialogResult.Cancel;
            try
            {
                result = Form1.ShowThreadExceptionDialog("Unhandled Error", t.Exception);
            }
            catch
            {
                try
                {
                    MessageBox.Show("Fatal Windows Forms Error",
                        "Fatal Windows Forms Error", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Stop);
                }
                finally
                {
                    Application.Exit();
                }
            }

            // Exits the program when the user clicks Abort.
            if (result == DialogResult.Abort)
                Application.Exit();
        }
    }
}