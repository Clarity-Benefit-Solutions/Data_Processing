using System.ComponentModel;
using System.Windows.Forms;

namespace TestApp
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.cmdProcessCobraFiles = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.listLogs = new System.Windows.Forms.ListBox();
            this.cmdProcessAlegeusFiles = new System.Windows.Forms.Button();
            this.cmdRetrieveFtpErrorLogs = new System.Windows.Forms.Button();
            this.cmdClearLog = new System.Windows.Forms.Button();
            this.cmdClearAll = new System.Windows.Forms.Button();
            this.cmdOpenAccessDB = new System.Windows.Forms.Button();
            this.cmdDoALL = new System.Windows.Forms.Button();
            this.cmdCopyTestFiles = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // cmdProcessCobraFiles
            // 
            this.cmdProcessCobraFiles.Location = new System.Drawing.Point(268, 21);
            this.cmdProcessCobraFiles.Name = "cmdProcessCobraFiles";
            this.cmdProcessCobraFiles.Size = new System.Drawing.Size(150, 36);
            this.cmdProcessCobraFiles.TabIndex = 0;
            this.cmdProcessCobraFiles.Text = "ProcessCobraFiles";
            this.cmdProcessCobraFiles.UseVisualStyleBackColor = true;
            this.cmdProcessCobraFiles.Click += new System.EventHandler(this.cmdProcessCobraFiles_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 82);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(39, 17);
            this.label1.TabIndex = 2;
            this.label1.Text = "Logs";
            // 
            // listLogs
            // 
            this.listLogs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listLogs.FormattingEnabled = true;
            this.listLogs.ItemHeight = 16;
            this.listLogs.Location = new System.Drawing.Point(23, 102);
            this.listLogs.Name = "listLogs";
            this.listLogs.Size = new System.Drawing.Size(1395, 740);
            this.listLogs.TabIndex = 3;
            // 
            // cmdProcessAlegeusFiles
            // 
            this.cmdProcessAlegeusFiles.Location = new System.Drawing.Point(441, 21);
            this.cmdProcessAlegeusFiles.Name = "cmdProcessAlegeusFiles";
            this.cmdProcessAlegeusFiles.Size = new System.Drawing.Size(150, 36);
            this.cmdProcessAlegeusFiles.TabIndex = 4;
            this.cmdProcessAlegeusFiles.Text = "ProcessAlegeusFiles";
            this.cmdProcessAlegeusFiles.UseVisualStyleBackColor = true;
            this.cmdProcessAlegeusFiles.Click += new System.EventHandler(this.cmdProcessAlegeusFiles_Click);
            // 
            // cmdRetrieveFtpErrorLogs
            // 
            this.cmdRetrieveFtpErrorLogs.Location = new System.Drawing.Point(625, 21);
            this.cmdRetrieveFtpErrorLogs.Name = "cmdRetrieveFtpErrorLogs";
            this.cmdRetrieveFtpErrorLogs.Size = new System.Drawing.Size(150, 36);
            this.cmdRetrieveFtpErrorLogs.TabIndex = 5;
            this.cmdRetrieveFtpErrorLogs.Text = "RetrieveFtpErrorLogs";
            this.cmdRetrieveFtpErrorLogs.UseVisualStyleBackColor = true;
            this.cmdRetrieveFtpErrorLogs.Click += new System.EventHandler(this.cmdRetrieveFtpErrorLogs_Click);
            // 
            // cmdClearLog
            // 
            this.cmdClearLog.Location = new System.Drawing.Point(1268, 60);
            this.cmdClearLog.Name = "cmdClearLog";
            this.cmdClearLog.Size = new System.Drawing.Size(150, 36);
            this.cmdClearLog.TabIndex = 6;
            this.cmdClearLog.Text = "ClearLog";
            this.cmdClearLog.UseVisualStyleBackColor = true;
            this.cmdClearLog.Click += new System.EventHandler(this.cmdClearLog_Click);
            // 
            // cmdClearAll
            // 
            this.cmdClearAll.Location = new System.Drawing.Point(1089, 12);
            this.cmdClearAll.Name = "cmdClearAll";
            this.cmdClearAll.Size = new System.Drawing.Size(150, 36);
            this.cmdClearAll.TabIndex = 7;
            this.cmdClearAll.Text = "Clear All Tables";
            this.cmdClearAll.UseVisualStyleBackColor = true;
            this.cmdClearAll.Click += new System.EventHandler(this.cmdClearAll_Click);
            // 
            // cmdOpenAccessDB
            // 
            this.cmdOpenAccessDB.Location = new System.Drawing.Point(802, 21);
            this.cmdOpenAccessDB.Name = "cmdOpenAccessDB";
            this.cmdOpenAccessDB.Size = new System.Drawing.Size(150, 36);
            this.cmdOpenAccessDB.TabIndex = 8;
            this.cmdOpenAccessDB.Text = "Open Access UI";
            this.cmdOpenAccessDB.UseVisualStyleBackColor = true;
            this.cmdOpenAccessDB.Click += new System.EventHandler(this.cmdOpenAccessDB_Click);
            // 
            // cmdDoALL
            // 
            this.cmdDoALL.Location = new System.Drawing.Point(1250, 12);
            this.cmdDoALL.Name = "cmdDoALL";
            this.cmdDoALL.Size = new System.Drawing.Size(150, 36);
            this.cmdDoALL.TabIndex = 9;
            this.cmdDoALL.Text = "Do ALL";
            this.cmdDoALL.UseVisualStyleBackColor = true;
            this.cmdDoALL.Click += new System.EventHandler(this.cmdDoALL_Click);
            // 
            // cmdCopyTestFiles
            // 
            this.cmdCopyTestFiles.Location = new System.Drawing.Point(23, 21);
            this.cmdCopyTestFiles.Name = "cmdCopyTestFiles";
            this.cmdCopyTestFiles.Size = new System.Drawing.Size(150, 36);
            this.cmdCopyTestFiles.TabIndex = 10;
            this.cmdCopyTestFiles.Text = "CopyTestFiles";
            this.cmdCopyTestFiles.UseVisualStyleBackColor = true;
            this.cmdCopyTestFiles.Click += new System.EventHandler(this.cmdCopyTestFiles_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1440, 861);
            this.Controls.Add(this.cmdCopyTestFiles);
            this.Controls.Add(this.cmdDoALL);
            this.Controls.Add(this.cmdOpenAccessDB);
            this.Controls.Add(this.cmdClearAll);
            this.Controls.Add(this.cmdClearLog);
            this.Controls.Add(this.cmdRetrieveFtpErrorLogs);
            this.Controls.Add(this.cmdProcessAlegeusFiles);
            this.Controls.Add(this.listLogs);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmdProcessCobraFiles);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Button cmdProcessCobraFiles;
        private Label label1;
        private ListBox listLogs;
        private Button cmdProcessAlegeusFiles;
        private Button cmdRetrieveFtpErrorLogs;
        private Button cmdClearLog;
        private Button cmdClearAll;
        private Button cmdOpenAccessDB;
        private Button cmdDoALL;
        private Button cmdCopyTestFiles;
    }
}

