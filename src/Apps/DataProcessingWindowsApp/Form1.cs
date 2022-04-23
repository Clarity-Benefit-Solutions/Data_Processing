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
            this.SubscribeToUnhandledExceptions();
            this.InitializeComponent();

            this.Closed += this.Form1_Closed;

            this.listLogs.AutoGenerateColumns = true;
            this.listLogs.AutoSize = true;
            this.listLogs.DataSource = this._bindingSource1;

            //
            this.SubscribeToEvents();

            //
            if (Vars.Environment != "TEST")
            {
                this.cmdClearAll.Visible = false;
                this.cmdCopyTestFiles.Visible = false;
                this.cmdOpenAccessDB.Visible = false;
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
            currentDomain.UnhandledException += this.HandleUnhandledException;
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

            this.HandleOnFileLogOperationCallback(null, logItem, null);
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

            this.HandleOnFileLogOperationCallback(sender, logItem, null);
        }

        private void HandleOnFileLogOperationCallback(object sender, LogFields logItem, Exception ex)
        {
            if (this.Visible)
            {
                if (this.listLogs.InvokeRequired)
                {
                    this.listLogs.Invoke(
                        (Action) (() =>
                            {
                                this._bindingSource1.Add(logItem);
                                this._bindingSource1.MoveLast();

                                if (ex != null)
                                {
                                    ShowThreadExceptionDialog("Unhandled Error", ex);
                                }
                            }
                        )
                    );
                }
                else
                {
                    // thread - safe equivalent 
                    this._bindingSource1.Add(logItem);
                    this._bindingSource1.MoveLast();

                    if (ex != null)
                    {
                        ShowThreadExceptionDialog("Unhandled Error", ex);
                    }
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
            DbUtils.eventOnLogFileOperationCallback += this.HandleOnFileLogOperationCallback;
        }

     

        private async void cmdProcessIncomingFiles_Click(object sender, EventArgs e)
        {
            this.cmdProcessIncomingFiles.Enabled = false;

            try
            {
                await IncomingFileProcessing.ProcessAll();
            }
            catch (Exception ex)
            {
                this.HandleOnFileLogOperationCallback(sender,
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
                this.cmdProcessIncomingFiles.Enabled = true;
            }
        }

        private async void cmdRetrieveFtpErrorLogs_Click(object sender, EventArgs e)
        {
            this.cmdRetrieveFtpErrorLogs.Enabled = false;

            try
            {
                await AlegeusErrorLog.ProcessAll();
            }
            catch (Exception ex)
            {
                this.HandleOnFileLogOperationCallback(sender,
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
                this.cmdRetrieveFtpErrorLogs.Enabled = true;
            }
        }

        private void cmdClearLog_Click(object sender, EventArgs e)
        {
            this._bindingSource1.Clear();
        }

        private void cmdClearAll_Click(object sender, EventArgs e)
        {
            this.cmdClearAll.Enabled = false;
            try
            {
                var errorLog = new AlegeusErrorLog();
                errorLog.USERVERYCAUTIOUSLY_ClearAllTables();
            }
            catch (Exception ex)
            {
                this.HandleOnFileLogOperationCallback(sender,
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
                this.cmdClearAll.Enabled = true;
            }
        }

        private void cmdOpenAccessDB_Click(object sender, EventArgs e)
        {
            this.cmdOpenAccessDB.Enabled = false;

            try
            {
                var directoryPath = Vars.GetProcessBaseDir();
                Process.Start($"{directoryPath}/../../../_MsAccessFiles/AlegeusErrorLogSystemv4v_Control-New.accdb");
            }
            catch (Exception ex)
            {
                this.HandleOnFileLogOperationCallback(sender,
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
                this.cmdOpenAccessDB.Enabled = true;
            }
        }

        private void cmdCopyTestFiles_Click(object sender, EventArgs e)
        {
            //
            this.cmdCopyTestFiles.Enabled = false;

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
                this.HandleOnFileLogOperationCallback(sender,
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
                this.cmdCopyTestFiles.Enabled = true;
            }
        }

        private async void cmdDoALL_Click(object sender, EventArgs e)
        {
            this.cmdDoALL.Enabled = false;

            try
            {
                var eventArgs = EventArgs.Empty;
                if (this.cmdClearAll.Visible && this.cmdClearAll.Enabled)
                {
                    this.cmdClearAll_Click(this, eventArgs);
                }

                if (this.cmdCopyTestFiles.Visible && this.cmdCopyTestFiles.Enabled)
                {
                    this.cmdCopyTestFiles_Click(this, eventArgs);
                }

                this.cmdClearLog_Click(this, eventArgs);
                await IncomingFileProcessing.ProcessAll();
                await AlegeusErrorLog.ProcessAll();
                if (this.cmdOpenAccessDB.Visible && this.cmdOpenAccessDB.Enabled)
                {
                    this.cmdOpenAccessDB_Click(this, eventArgs);
                }
            }
            catch (Exception ex)
            {
                this.HandleOnFileLogOperationCallback(sender,
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
                this.cmdDoALL.Enabled = true;
            }
        }
    }

}