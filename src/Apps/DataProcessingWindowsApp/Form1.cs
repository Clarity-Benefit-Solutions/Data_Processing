using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Threading;
using CoreUtils;
using CoreUtils.Classes;
using DataProcessing;

namespace TestApp
{

    public partial class Form1 : Form
    {
        private readonly BindingSource _bindingSource1 = new BindingSource();
        private Dispatcher _uiDispatcher = Dispatcher.CurrentDispatcher;

        private Vars vars = new Vars();


        public Form1()
        {
            SubscribeToUnhandledExceptions();
            InitializeComponent();

            Closed += Form1_Closed;

            listLogs.AutoGenerateColumns = true;
            listLogs.AutoSize = true;
            listLogs.DataSource = _bindingSource1;

            //
            SubscribeToEvents();

            //
            if (Vars.Environment != "TEST")
            {
                cmdClearAll.Visible = false;
                cmdCopyTestFiles.Visible = false;
                cmdOpenAccessDB.Visible = false;
            }
        }

        private void Form1_Closed(object sender, EventArgs e)
        {
            Application.Exit();
            Environment.Exit(1);
        }

        private void SubscribeToUnhandledExceptions()
        {
            var currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += HandleUnhandledException;
        }


        private void HandleUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            var e = (Exception) args.ExceptionObject;
            var logItem = new LogFields(
                DateTime.Now.ToString(CultureInfo.InvariantCulture),
                "",
                "Unhandled Exception",
                "Unhandled Exception", "",
                $"Unhandled Exception: {e}"
            );

            HandleOnFileLogOperationCallback(null, logItem, null);
        }

        private void HandleOnFileLogOperationCallback(object sender, FileOperationLogParams logParams)
        {
            var logItem = new LogFields(
                DateTime.Now.ToString(CultureInfo.InvariantCulture),
                logParams.FileId,
                logParams.ProcessingTask,
                logParams.ProcessingTaskOutcome,
                logParams.OriginalFileName,
                logParams.ProcessingTaskOutcomeDetails
            );

            HandleOnFileLogOperationCallback(sender, logItem, null);
        }

        private void HandleOnFileLogOperationCallback(object sender, LogFields logItem, Exception ex)
        {
            if (Visible)
            {
                if (listLogs.InvokeRequired)
                {
                    listLogs.Invoke(
                        (Action) (() =>
                            {
                                _bindingSource1.Add(logItem);
                                _bindingSource1.MoveLast();

                                if (ex != null) ShowThreadExceptionDialog("Unhandled Error", ex);
                            }
                        )
                    );
                }
                else
                {
                    // thread - safe equivalent 
                    _bindingSource1.Add(logItem);
                    _bindingSource1.MoveLast();

                    if (ex != null) ShowThreadExceptionDialog("Unhandled Error", ex);
                }
            }

            Console.Out.WriteLine(logItem.ToString());
        }

        public static DialogResult ShowThreadExceptionDialog(string title, Exception e)
        {
            var errorMsg = "An application error occurred. Please contact the adminstrator " +
                           "with the following information:\n\n";
            errorMsg = errorMsg + e.Message + "\n\nStack Trace:\n" + e.StackTrace;
            return MessageBox.Show(errorMsg, title, MessageBoxButtons.AbortRetryIgnore,
                MessageBoxIcon.Stop);
        }

        public void SubscribeToEvents()
        {
            //// Subscribe to the event
            //DbUtils.EventOnLogOperationCallback += HandleOnLogOperationCallback;

            // Subscribe to the event
            DbUtils.eventOnLogFileOperationCallback += HandleOnFileLogOperationCallback;
        }

        private async void cmdProcessCobraFiles_Click(object sender, EventArgs e)
        {
            cmdProcessCobraFiles.Enabled = false;

            try
            {
                await CobraDataProcessing.ProcessAll();
            }
            catch (Exception ex)
            {
                HandleOnFileLogOperationCallback(sender,
                    new LogFields(DateTime.Now.ToString(CultureInfo.InvariantCulture),
                        "",
                        "Unhandled Exception",
                        "Unhandled Exception", "",
                        $"Unhandled Exception: {ex}"
                    ),
                    ex
                );
            }
            finally
            {
                cmdProcessCobraFiles.Enabled = true;
            }
        }


        private async void cmdProcessAlegeusFiles_Click(object sender, EventArgs e)
        {
            cmdProcessAlegeusFiles.Enabled = false;

            try
            {
                await AlegeusDataProcessing.ProcessAll();
            }
            catch (Exception ex)
            {
                HandleOnFileLogOperationCallback(sender,
                    new LogFields(DateTime.Now.ToString(CultureInfo.InvariantCulture),
                        "",
                        "Unhandled Exception",
                        "Unhandled Exception", "",
                        $"Unhandled Exception: {ex}"
                    ),
                    ex
                );
            }
            finally
            {
                cmdProcessAlegeusFiles.Enabled = true;
            }
        }

        private async void cmdRetrieveFtpErrorLogs_Click(object sender, EventArgs e)
        {
            cmdRetrieveFtpErrorLogs.Enabled = false;

            try
            {
                await AlegeusErrorLog.ProcessAll();
            }
            catch (Exception ex)
            {
                HandleOnFileLogOperationCallback(sender,
                    new LogFields(DateTime.Now.ToString(CultureInfo.InvariantCulture),
                        "",
                        "Unhandled Exception",
                        "Unhandled Exception", "",
                        $"Unhandled Exception: {ex}"
                    ),
                    ex
                );
            }
            finally
            {
                cmdRetrieveFtpErrorLogs.Enabled = true;
            }
        }

        private void cmdClearLog_Click(object sender, EventArgs e)
        {
            _bindingSource1.Clear();
        }

        private void cmdClearAll_Click(object sender, EventArgs e)
        {
            cmdClearAll.Enabled = false;
            try
            {
                var errorLog = new AlegeusErrorLog();
                errorLog.USERVERYCAUTIOUSLY_ClearAllTables();
            }
            catch (Exception ex)
            {
                HandleOnFileLogOperationCallback(sender,
                    new LogFields(DateTime.Now.ToString(CultureInfo.InvariantCulture),
                        "",
                        "Unhandled Exception",
                        "Unhandled Exception", "",
                        $"Unhandled Exception: {ex}"
                    ),
                    ex
                );
            }
            finally
            {
                cmdClearAll.Enabled = true;
            }
        }

        private void cmdOpenAccessDB_Click(object sender, EventArgs e)
        {
            cmdOpenAccessDB.Enabled = false;

            try
            {
                var directoryPath = Vars.GetProcessBaseDir();
                Process.Start($"{directoryPath}/../../../_MsAccessFiles/AlegeusErrorLogSystemv4v_Control-New.accdb");
            }
            catch (Exception ex)
            {
                HandleOnFileLogOperationCallback(sender,
                    new LogFields(DateTime.Now.ToString(CultureInfo.InvariantCulture),
                        "",
                        "Unhandled Exception",
                        "Unhandled Exception", "",
                        $"Unhandled Exception: {ex}"
                    ),
                    ex
                );
            }
            finally
            {
                cmdOpenAccessDB.Enabled = true;
            }
        }

        private void cmdCopyTestFiles_Click(object sender, EventArgs e)
        {
            //
            cmdCopyTestFiles.Enabled = false;

            try
            {
                var directoryPath = Vars.GetProcessBaseDir();
                Process.Start(
                    $"{directoryPath}/../../../__LocalTestDirsAndFiles/copy_Alegeus_mbi+res_to_export_ftp.bat");
                Process.Start(
                    $"{directoryPath}/../../../__LocalTestDirsAndFiles/copy_Alegeus_source_files_to_import_ftp.bat");
                Process.Start(
                    $"{directoryPath}/../../../__LocalTestDirsAndFiles/copy_COBRA_source_files_to_import_ftp.bat");
            }
            catch (Exception ex)
            {
                HandleOnFileLogOperationCallback(sender,
                    new LogFields(DateTime.Now.ToString(CultureInfo.InvariantCulture),
                        "",
                        "Unhandled Exception",
                        "Unhandled Exception", "",
                        $"Unhandled Exception: {ex}"
                    ),
                    ex
                );
            }
            finally
            {
                cmdCopyTestFiles.Enabled = true;
            }
        }

        private async void cmdDoALL_Click(object sender, EventArgs e)
        {
            cmdDoALL.Enabled = false;

            try
            {
                var eventArgs = EventArgs.Empty;
                if (cmdClearAll.Visible && cmdClearAll.Enabled) cmdClearAll_Click(this, eventArgs);
                if (cmdCopyTestFiles.Visible && cmdCopyTestFiles.Enabled) cmdCopyTestFiles_Click(this, eventArgs);
                cmdClearLog_Click(this, eventArgs);
                await CobraDataProcessing.ProcessAll();
                await AlegeusDataProcessing.ProcessAll();
                await AlegeusErrorLog.ProcessAll();
                if (cmdOpenAccessDB.Visible && cmdOpenAccessDB.Enabled) cmdOpenAccessDB_Click(this, eventArgs);
            }
            catch (Exception ex)
            {
                HandleOnFileLogOperationCallback(sender,
                    new LogFields(DateTime.Now.ToString(CultureInfo.InvariantCulture),
                        "",
                        "Unhandled Exception",
                        "Unhandled Exception", "",
                        $"Unhandled Exception: {ex}"
                    ),
                    ex
                );
            }
            finally
            {
                cmdDoALL.Enabled = true;
            }
        }
    }

}