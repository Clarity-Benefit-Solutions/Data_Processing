using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Threading;
using CoreUtils;
using CoreUtils.Classes;
using DataProcessing;

namespace TestApp
{




    public partial class Form1 : Form
    {
        private Dispatcher _uiDispatcher = Dispatcher.CurrentDispatcher;
        private BindingSource _bindingSource1 = new BindingSource();
        Vars vars = new Vars();


        public Form1()
        {
            SubscribeToUnhandledExceptions();
            InitializeComponent();

            this.Closed += new System.EventHandler(this.Form1_Closed);

            this.listLogs.AutoGenerateColumns = true;
            this.listLogs.AutoSize = true;
            this.listLogs.DataSource = _bindingSource1;

            //
            SubscribeToEvents();

            //
            if (Vars.Environment != "TEST")
            {
                this.cmdClearAll.Enabled = false;
            }

        }

        private void Form1_Closed(object sender, EventArgs e)
        {
            Application.Exit();
            Environment.Exit(1);
        }

        private void SubscribeToUnhandledExceptions()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(HandleUnhandledException);
        }


        private void HandleUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            var logItem = new LogFields(
                DateTime.Now.ToString(CultureInfo.InvariantCulture),
                "",
               "Unhandled Exception",
               $"Unhandled Exception", "",
                $"Unhandled Exception: {e.ToString()}"
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
                if (listLogs.InvokeRequired)
                {
                    listLogs.Invoke(
                        (Action)(() =>
                        {
                            _bindingSource1.Add(logItem);
                            _bindingSource1.MoveLast();

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
                    _bindingSource1.Add(logItem);
                    _bindingSource1.MoveLast();

                    if (ex != null)
                    {
                        ShowThreadExceptionDialog("Unhandled Error", ex);
                    }
                }
            }
            else
            {
                Console.WriteLine(logItem.ToString());
            }

        }

        public static DialogResult ShowThreadExceptionDialog(string title, Exception e)
        {
            string errorMsg = "An application error occurred. Please contact the adminstrator " +
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

                this.HandleOnFileLogOperationCallback(sender,
                    new LogFields(DateTime.Now.ToString(CultureInfo.InvariantCulture),
                        "",
                       "Unhandled Exception",
                       $"Unhandled Exception", "",
                        $"Unhandled Exception: {ex.ToString()}"
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

                this.HandleOnFileLogOperationCallback(sender,
                    new LogFields(DateTime.Now.ToString(CultureInfo.InvariantCulture),
                        "",
                       "Unhandled Exception",
                       $"Unhandled Exception", "",
                        $"Unhandled Exception: {ex.ToString()}"
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
                await DataProcessing.AlegeusErrorLog.ProcessAll();
            }
            catch (Exception ex)
            {

                this.HandleOnFileLogOperationCallback(sender,
                    new LogFields(DateTime.Now.ToString(CultureInfo.InvariantCulture),
                        "",
                       "Unhandled Exception",
                       $"Unhandled Exception", "",
                        $"Unhandled Exception: {ex.ToString()}"
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
                AlegeusErrorLog errorLog = new AlegeusErrorLog();
                errorLog.USERVERYCAUTIOUSLY_ClearAllTables();
            }
            catch (Exception ex)
            {

                this.HandleOnFileLogOperationCallback(sender,
                    new LogFields(DateTime.Now.ToString(CultureInfo.InvariantCulture),
                        "",
                       "Unhandled Exception",
                       $"Unhandled Exception", "",
                        $"Unhandled Exception: {ex.ToString()}"
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

                this.HandleOnFileLogOperationCallback(sender,
                    new LogFields(DateTime.Now.ToString(CultureInfo.InvariantCulture),
                        "",
                       "Unhandled Exception",
                       $"Unhandled Exception", "",
                        $"Unhandled Exception: {ex.ToString()}"
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
        {  //
            cmdCopyTestFiles.Enabled = false;

            try
            {
                var directoryPath = Vars.GetProcessBaseDir();
                Process.Start($"{directoryPath}/../../../__LocalTestDirsAndFiles/copy_Alegeus_mbi+res_to_export_ftp.bat");
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
                       $"Unhandled Exception", "",
                        $"Unhandled Exception: {ex.ToString()}"
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
                cmdCopyTestFiles_Click(this, eventArgs);
                cmdClearLog_Click(this, eventArgs);
                if (Vars.Environment != "TEST")
                {
                    this.cmdClearAll.Enabled = false;
                }
                await CobraDataProcessing.ProcessAll();
                await AlegeusDataProcessing.ProcessAll();
                await AlegeusErrorLog.ProcessAll();
                cmdOpenAccessDB_Click(this, eventArgs);
            }
            catch (Exception ex)
            {

                this.HandleOnFileLogOperationCallback(sender,
                    new LogFields(DateTime.Now.ToString(CultureInfo.InvariantCulture),
                        "",
                       "Unhandled Exception",
                       $"Unhandled Exception", "",
                        $"Unhandled Exception: {ex.ToString()}"
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