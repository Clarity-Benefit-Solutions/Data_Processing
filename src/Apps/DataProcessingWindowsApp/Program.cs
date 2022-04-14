using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreUtils.Classes;
using DataProcessing;
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

            // write output to logfile
            StreamWriter swConsoleOut = new StreamWriter($"{Vars.GetProcessBaseDir()}/_Output.log", true);
            swConsoleOut.AutoFlush = true;
            Console.SetOut(swConsoleOut);

            // handle startup args for scheduled processing

            List<Task> tasks = new List<Task> { };
            string[] args = Environment.GetCommandLineArgs();

            Boolean showUI = true;
            foreach (string arg in args)
            {
                switch (arg.ToString().ToLower())
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

                    case @"processcobrafiles":
                        tasks.Add(CobraDataProcessing.ProcessAll());
                        break;

                    case @"processalegeusfiles":
                        tasks.Add(AlegeusDataProcessing.ProcessAll());
                        break;

                    case @"retrieveftperrorlogs":
                        tasks.Add(AlegeusErrorLog.ProcessAll());
                        break;

                    case @"copytestfiles":
                        tasks.Add(AlegeusDataProcessing.CopyTestFiles());
                        break;

                    case @"noui":
                        showUI = false;
                        break;

                    default:
                        break;
                }
            }

            if (tasks.Count > 0)
            {
                // init form so we can view the logs
                Form1 mainForm = new Form1();
                if (showUI)
                {
                    mainForm.Show();
                    Application.Run();

                }

                foreach (var task in tasks)
                {
                    for (int i = 0; i < 10; i++)
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