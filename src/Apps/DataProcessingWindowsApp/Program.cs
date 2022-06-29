using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DataProcessing;

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
            Application.ThreadException += UIThreadException;

            //            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string filePath = $"{Vars.GetProcessExeDir()}/_Output_{DateTime.Now.ToString("yyyy-MM-dd HH-mm")}.log";
try
            {
                // write output to logfile
                var swConsoleOut = new StreamWriter(filePath, true);
                swConsoleOut.AutoFlush = true;
                Console.SetOut(swConsoleOut);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could Not Log to File {filePath} as {ex.ToString()}");
            }


            // handle startup args for scheduled processing

            List<Task> tasks = new List<Task>();
            var args = Environment.GetCommandLineArgs();

            var showUI = true;
            foreach (var arg in args)
            {
                switch (arg.ToLower())
                {
                    case @"test":
                        Vars.Environment = "TEST";
                        break;

                    case @"prod":
                        Vars.Environment = "PROD";
                        break;

                    case @"usevpntoconnecttoportal":
                        Vars.UseVPNToConnectToPortal = true;
                        break;

                    case @"processparticipantenrollmentfiles":
                        tasks.Add(MiscFileProcessing.ProcessAll());
                        break;

                    case @"processincomingfiles":
                        tasks.Add(IncomingFileProcessing.ProcessAll());
                        break;

                    case @"retrieveftperrorlogs":
                        tasks.Add(AlegeusErrorLog.ProcessAll());
                        break;

                    case @"copytestfiles":
                        tasks.Add(IncomingFileProcessing.CopyTestFiles());
                        break;

                    case @"noui":
                        showUI = false;
                        break;
                }
            }

            if (tasks.Count > 0)
            {
                // init form so we can view the logs
                var mainForm = new Form1();
                if (showUI)
                {
                    mainForm.Show();
                    Application.Run();
                }

                foreach (var task in tasks)
                {
                    for (var i = 0; i < 10; i++)
                    {
                        Application.DoEvents();
                    }

                    task.Wait();
                }
            }
            else
            {
                Application.Run(new Form1());
            }

            //
#if PROFILE
            Console.WriteLine(profiler.RenderPlainText());
            Console.WriteLine(profiler.GetTimingHierarchy().ToString());
#endif
        }


        public static void UIThreadException(object sender, ThreadExceptionEventArgs t)
        {
            var result = DialogResult.Cancel;
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
            {
                Application.Exit();
            }
        }
    }

}