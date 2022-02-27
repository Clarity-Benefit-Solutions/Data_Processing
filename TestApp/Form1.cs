using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using CoreUtils;
using CoreUtils.Classes;
using DataProcessing;

namespace TestApp
{

    


    public partial class Form1 : Form
    {
        private BindingSource bindingSource1 = new BindingSource();
        
        public Form1()
        {
            InitializeComponent();

            this.listLogs.AutoGenerateColumns = true;
            this.listLogs.AutoSize = true;
            this.listLogs.DataSource = bindingSource1;

            SubscribeToEvents();
            
        }

        private void HandleOnLogOperationCallback(object sender, MessageLogParams logParams)
        {
            //string logText = $"{DateTime.Now.ToString(CultureInfo.InvariantCulture)} - {logParams}";
            //listLogs.Items.Add(logText);
            //listLogs.TopIndex = listLogs.Items.Count - 1;
            //listLogs.Invalidate();
            //listLogs.Update();
            //listLogs.Refresh();
            //Application.DoEvents();
            //Application.DoEvents();
            //Application.DoEvents();
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

            this.bindingSource1.Add(logItem);
            this.bindingSource1.MoveLast();
            

            listLogs.Invalidate();
            listLogs.Update();
            listLogs.Refresh();
            Application.DoEvents();
            Application.DoEvents();
            Application.DoEvents();
        }

        public void SubscribeToEvents()
        {
            // Subscribe to the event
            DbUtils.EventOnLogOperationCallback += HandleOnLogOperationCallback;

            // Subscribe to the event
            DbUtils.eventOnLogFileOperationCallback += HandleOnFileLogOperationCallback;
        }

        private void cmdProcessCobraFiles_Click(object sender, EventArgs e)
        {
            cmdProcessCobraFiles.Enabled = false;
            //
            CobraDataProcessing.MoveAndProcessCobraFtpFiles();
            //
            cmdProcessCobraFiles.Enabled = true;
        }


        private void cmdProcessAlegeusFiles_Click(object sender, EventArgs e)
        {
            cmdProcessAlegeusFiles.Enabled = false;
            //
            AlegeusDataProcessing.ProcessAllFiles();
            //
            cmdProcessAlegeusFiles.Enabled = true;
        }

        private void cmdRetrieveFtpErrorLogs_Click(object sender, EventArgs e)
        {
            cmdRetrieveFtpErrorLogs.Enabled = false;
            //
            AlegeusErrorLog.RetrieveErrorLogs();
            //
            cmdRetrieveFtpErrorLogs.Enabled = true;
        }

        private void cmdClearLog_Click(object sender, EventArgs e)
        {
            bindingSource1.Clear();
        }

        private void cmdClearAll_Click(object sender, EventArgs e)
        {
            AlegeusErrorLog.USERVERYCAUTIOUSLY_ClearAllTables();
        }

        private void cmdOpenAccessDB_Click(object sender, EventArgs e)
        {
            var directoryPath = Utils.GetExeBaseDir();
            Process.Start($"{directoryPath}/../../../_MsAccessFiles/AlegeusErrorLogSystemv4v_Control-New.accdb");
        }

        private void cmdCopyTestFiles_Click(object sender, EventArgs e)
        {
            var directoryPath = Utils.GetExeBaseDir();
            Process.Start($"{directoryPath}/../../../__LocalTestDirsAndFiles/copy_Alegeus_mbi+res_to_export_ftp.bat");
            Process.Start(
                $"{directoryPath}/../../../__LocalTestDirsAndFiles/copy_Alegeus_source_files_to_import_ftp.bat");
            Process.Start(
                $"{directoryPath}/../../../__LocalTestDirsAndFiles/copy_COBRA_source_files_to_import_ftp.bat");
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