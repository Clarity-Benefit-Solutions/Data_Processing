using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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

        public Form1()
        {
            InitializeComponent();

            this.listLogs.AutoGenerateColumns = true;
            this.listLogs.AutoSize = true;
            this.listLogs.DataSource = _bindingSource1;

            //
            SubscribeToEvents();

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


            if (listLogs.InvokeRequired)
            {
                listLogs.Invoke(
                    (Action)(() =>
                   {
                       _bindingSource1.Add(logItem);
                       _bindingSource1.MoveLast();
                   }
                )
                    );
            }
            else
            {
                // thread - safe equivalent 
                _bindingSource1.Add(logItem);
                _bindingSource1.MoveLast();
            }
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
            //
            await Task.Factory.StartNew
            (
                () =>
                {
                    Thread.CurrentThread.Name = "ProcessCobraFiles";
                    CobraDataProcessing cobraProcessing = new CobraDataProcessing();
                    cobraProcessing.MoveAndProcessCobraFtpFiles();
                }
            );

            //
            cmdProcessCobraFiles.Enabled = true;
        }


        private async void cmdProcessAlegeusFiles_Click(object sender, EventArgs e)
        {
            cmdProcessAlegeusFiles.Enabled = false;
            //
            await Task.Factory.StartNew
            (
                () =>
                {
                    AlegeusDataProcessing dataProcessing = new AlegeusDataProcessing();
                    dataProcessing.ProcessAllFiles();
                }
            );

            //
            cmdProcessAlegeusFiles.Enabled = true;
        }

        private async void cmdRetrieveFtpErrorLogs_Click(object sender, EventArgs e)
        {
            cmdRetrieveFtpErrorLogs.Enabled = false;
            //
            await Task.Factory.StartNew
            (
                () =>
              {
                  AlegeusErrorLog errorLog = new AlegeusErrorLog();
                  errorLog.RetrieveErrorLogs();
              }
                );


            //
            cmdRetrieveFtpErrorLogs.Enabled = true;
        }

        private async void cmdClearLog_Click(object sender, EventArgs e)
        {
            _bindingSource1.Clear();
        }

        private async void cmdClearAll_Click(object sender, EventArgs e)
        {
            //
            await Task.Factory.StartNew
            (
                () =>
               {
                   AlegeusErrorLog errorLog = new AlegeusErrorLog();
                   errorLog.USERVERYCAUTIOUSLY_ClearAllTables();
               }
            );

        }

        private async void cmdOpenAccessDB_Click(object sender, EventArgs e)
        {
            var directoryPath = Utils.GetExeBaseDir();
            Process.Start($"{directoryPath}/../../../_MsAccessFiles/AlegeusErrorLogSystemv4v_Control-New.accdb");
        }

        private async void cmdCopyTestFiles_Click(object sender, EventArgs e)
        {  //
            await Task.Factory.StartNew
            (
                () =>
              {
                  var directoryPath = Utils.GetExeBaseDir();
                  Process.Start($"{directoryPath}/../../../__LocalTestDirsAndFiles/copy_Alegeus_mbi+res_to_export_ftp.bat");
                  Process.Start(
                      $"{directoryPath}/../../../__LocalTestDirsAndFiles/copy_Alegeus_source_files_to_import_ftp.bat");
                  Process.Start(
                      $"{directoryPath}/../../../__LocalTestDirsAndFiles/copy_COBRA_source_files_to_import_ftp.bat");
              }
            );

        }

        private void cmdDoALL_Click(object sender, EventArgs e)
        {
            var eventArgs = EventArgs.Empty;
            cmdCopyTestFiles_Click(this, eventArgs);
            cmdClearLog_Click(this, eventArgs);
            cmdClearAll_Click(this, eventArgs);
            cmdProcessCobraFiles_Click(this, eventArgs);
            cmdProcessAlegeusFiles_Click(this, eventArgs);
            cmdRetrieveFtpErrorLogs_Click(this, eventArgs);
            cmdOpenAccessDB_Click(this, eventArgs);
        }
    }

    public class LogFields
    {
        public string LogTime { get; } = "";
        public string FileId { get; } = "";
        public string Task { get; } = "";
        public string Status { get; } = "";
        public string FileName { get; } = "";
        public string OutcomeDetails { get; } = "";

        public LogFields(string logTime, string fileId, string task, string status, string fileName, string outcomeDetails)
        {
            LogTime = logTime;
            FileId = fileId;
            Task = task;
            Status = status;
            FileName = fileName;
            OutcomeDetails = outcomeDetails;
        }
    }
}