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
            this.label1 = new System.Windows.Forms.Label();
            this.cmdProcessIncomingFiles = new System.Windows.Forms.Button();
            this.cmdRetrieveFtpErrorLogs = new System.Windows.Forms.Button();
            this.cmdClearLog = new System.Windows.Forms.Button();
            this.cmdClearAll = new System.Windows.Forms.Button();
            this.cmdOpenAccessDB = new System.Windows.Forms.Button();
            this.cmdDoALL = new System.Windows.Forms.Button();
            this.cmdCopyTestFiles = new System.Windows.Forms.Button();
            this.listLogs = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.listLogs)).BeginInit();
            this.SuspendLayout();
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
            // cmdProcessIncomingFiles
            // 
            this.cmdProcessIncomingFiles.Location = new System.Drawing.Point(267, 12);
            this.cmdProcessIncomingFiles.Name = "cmdProcessIncomingFiles";
            this.cmdProcessIncomingFiles.Size = new System.Drawing.Size(169, 36);
            this.cmdProcessIncomingFiles.TabIndex = 4;
            this.cmdProcessIncomingFiles.Text = "ProcessAllIncomingFiles";
            this.cmdProcessIncomingFiles.UseVisualStyleBackColor = true;
            this.cmdProcessIncomingFiles.Click += new System.EventHandler(this.cmdProcessIncomingFiles_Click);
            // 
            // cmdRetrieveFtpErrorLogs
            // 
            this.cmdRetrieveFtpErrorLogs.Location = new System.Drawing.Point(511, 12);
            this.cmdRetrieveFtpErrorLogs.Name = "cmdRetrieveFtpErrorLogs";
            this.cmdRetrieveFtpErrorLogs.Size = new System.Drawing.Size(169, 36);
            this.cmdRetrieveFtpErrorLogs.TabIndex = 5;
            this.cmdRetrieveFtpErrorLogs.Text = "RetrieveFtpErrorLogs";
            this.cmdRetrieveFtpErrorLogs.UseVisualStyleBackColor = true;
            this.cmdRetrieveFtpErrorLogs.Click += new System.EventHandler(this.cmdRetrieveFtpErrorLogs_Click);
            // 
            // cmdClearLog
            // 
            this.cmdClearLog.Location = new System.Drawing.Point(1247, 60);
            this.cmdClearLog.Name = "cmdClearLog";
            this.cmdClearLog.Size = new System.Drawing.Size(150, 36);
            this.cmdClearLog.TabIndex = 6;
            this.cmdClearLog.Text = "ClearLog";
            this.cmdClearLog.UseVisualStyleBackColor = true;
            this.cmdClearLog.Click += new System.EventHandler(this.cmdClearLog_Click);
            // 
            // cmdClearAll
            // 
            this.cmdClearAll.Location = new System.Drawing.Point(999, 12);
            this.cmdClearAll.Name = "cmdClearAll";
            this.cmdClearAll.Size = new System.Drawing.Size(169, 36);
            this.cmdClearAll.TabIndex = 7;
            this.cmdClearAll.Text = "Clear All Tables";
            this.cmdClearAll.UseVisualStyleBackColor = true;
            this.cmdClearAll.Click += new System.EventHandler(this.cmdClearAll_Click);
            // 
            // cmdOpenAccessDB
            // 
            this.cmdOpenAccessDB.Location = new System.Drawing.Point(755, 12);
            this.cmdOpenAccessDB.Name = "cmdOpenAccessDB";
            this.cmdOpenAccessDB.Size = new System.Drawing.Size(169, 36);
            this.cmdOpenAccessDB.TabIndex = 8;
            this.cmdOpenAccessDB.Text = "Open Access UI";
            this.cmdOpenAccessDB.UseVisualStyleBackColor = true;
            this.cmdOpenAccessDB.Click += new System.EventHandler(this.cmdOpenAccessDB_Click);
            // 
            // cmdDoALL
            // 
            this.cmdDoALL.Location = new System.Drawing.Point(1243, 12);
            this.cmdDoALL.Name = "cmdDoALL";
            this.cmdDoALL.Size = new System.Drawing.Size(169, 36);
            this.cmdDoALL.TabIndex = 9;
            this.cmdDoALL.Text = "Do ALL";
            this.cmdDoALL.UseVisualStyleBackColor = true;
            this.cmdDoALL.Click += new System.EventHandler(this.cmdDoALL_Click);
            // 
            // cmdCopyTestFiles
            // 
            this.cmdCopyTestFiles.Location = new System.Drawing.Point(23, 12);
            this.cmdCopyTestFiles.Name = "cmdCopyTestFiles";
            this.cmdCopyTestFiles.Size = new System.Drawing.Size(169, 36);
            this.cmdCopyTestFiles.TabIndex = 10;
            this.cmdCopyTestFiles.Text = "CopyTestFiles";
            this.cmdCopyTestFiles.UseVisualStyleBackColor = true;
            this.cmdCopyTestFiles.Click += new System.EventHandler(this.cmdCopyTestFiles_Click);
            // 
            // listLogs
            // 
            this.listLogs.AllowUserToAddRows = false;
            this.listLogs.AllowUserToDeleteRows = false;
            this.listLogs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listLogs.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.listLogs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.listLogs.Location = new System.Drawing.Point(23, 116);
            this.listLogs.Name = "listLogs";
            this.listLogs.RowHeadersWidth = 51;
            this.listLogs.RowTemplate.Height = 24;
            this.listLogs.Size = new System.Drawing.Size(1405, 733);
            this.listLogs.TabIndex = 12;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1440, 861);
            this.Controls.Add(this.listLogs);
            this.Controls.Add(this.cmdCopyTestFiles);
            this.Controls.Add(this.cmdDoALL);
            this.Controls.Add(this.cmdOpenAccessDB);
            this.Controls.Add(this.cmdClearAll);
            this.Controls.Add(this.cmdClearLog);
            this.Controls.Add(this.cmdRetrieveFtpErrorLogs);
            this.Controls.Add(this.cmdProcessIncomingFiles);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)(this.listLogs)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private Label label1;
        private Button cmdProcessIncomingFiles;
        private Button cmdRetrieveFtpErrorLogs;
        private Button cmdClearLog;
        private Button cmdClearAll;
        private Button cmdOpenAccessDB;
        private Button cmdDoALL;
        private Button cmdCopyTestFiles;
        private DataGridView listLogs;
    }
}

