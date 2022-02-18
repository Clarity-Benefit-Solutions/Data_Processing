using System;
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
        public Form1()
        {
            InitializeComponent();
            SubscribeToEvents();
        }

        private void HandleOnLogOperationCallback(object sender, MessageLogParams logParams)
        {
            //string logText = $"{DateTime.Now.ToString(CultureInfo.InvariantCulture)} - {logParams}";
            //listLogs.Items.Add(logText);
            //listLogs.TopIndex = listLogs.Items.Count - 1;
        }

        private void HandleOnFileLogOperationCallback(object sender, FileOperationLogParams logParams)
        {
            var logText = $"{DateTime.Now.ToString(CultureInfo.InvariantCulture)} - {logParams}";
            listLogs.Items.Add(logText);
            listLogs.TopIndex = listLogs.Items.Count - 1;
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
            this.listLogs.Items.Clear();
        }

        private void cmdClearAll_Click(object sender, EventArgs e)
        {
            AlegeusErrorLog.USERVERYCAUTIOUSLY_ClearAllTables();
        }

        private void cmdOpenAccessDB_Click(object sender, EventArgs e)
        {
            Process.Start(
                "C:/___Clarity/clarity_dev/r1_cl_sqldb/_NewApps/_MsAccessFiles/AlegeusErrorLogSystemv4v_Control-New.accdb");
        }



        private void cmdCopyTestFiles_Click(object sender, EventArgs e)
        {
            Process.Start("C:/SQLDBTEST/___TestFiles/copy_Alegeus_mbi+res_to_export_ftp.bat");
            Process.Start("C:/SQLDBTEST/___TestFiles/copy_Alegeus_source_files_to_import_ftp.bat");
            Process.Start("C:/SQLDBTEST/___TestFiles/copy_COBRA_source_files_to_import_ftp.bat");

        }

        private void cmdDoALL_Click(object sender, EventArgs e)
        {
            var eventArgs = EventArgs.Empty;
            this.cmdCopyTestFiles_Click(this, eventArgs);
            this.cmdClearLog_Click(this, eventArgs);
            this.cmdClearAll_Click(this, eventArgs);
            this.cmdProcessCobraFiles_Click(this, eventArgs);
            this.cmdProcessAlegeusFiles_Click(this, eventArgs);
            this.cmdRetrieveFtpErrorLogs_Click(this, eventArgs);
            this.cmdOpenAccessDB_Click(this, eventArgs);
        }
    }
}