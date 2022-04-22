using System;
using System.Data.Common;

namespace CoreUtils.Classes
{

    public class MessageLogParams : EventArgs
    {
        public string Command = "";
        public DbConnection DbConnection;
        public string LogTableName = "";
        public string ModuleName = "";
        public string Platform = "";
        public string StepName = "";
        public string StepType = "";
        public string SubModuleName = "";


        public MessageLogParams()
        {
        }

        public MessageLogParams(DbConnection dbConnection, string logTableName, string moduleName, string subModuleName,
            string stepType, string stepName, string command)
        {
            this.DbConnection = dbConnection;
            this.LogTableName = logTableName;
            this.ModuleName = moduleName;
            this.SubModuleName = subModuleName;
            this.StepType = stepType;
            this.StepName = stepName;
            this.Command = command;
        }

        public override string ToString()
        {
            return
                $"{this.LogTableName} - {this.ModuleName} - {this.SubModuleName} - {this.StepType} - {this.StepName} - {this.Command} ";
        }


        public MessageLogParams Clone()
        {
            var cloned = (MessageLogParams) this.MemberwiseClone();
            //
            return cloned;
        }

        public MessageLogParams SetSubModuleStepAndCommand(string subModuleName, string stepType, string stepName,
            string command)
        {
            //
            this.SubModuleName = subModuleName;
            this.StepType = stepType;
            this.StepName = stepName;
            this.Command = command;
            //
            //var cloned = (MessageLogParams)MemberwiseClone();
            //return cloned;
            return this;
        }

        public MessageLogParams SetStepAndCommand(string stepName, string command)
        {
            var cloned = (MessageLogParams) this.MemberwiseClone();
            //
            cloned.StepName = stepName;
            cloned.Command = command;
            //
            return cloned;
        }

        public MessageLogParams SetCommand(string command)
        {
            this.Command = command;
            //
            //
            //var cloned = (MessageLogParams)MemberwiseClone();
            //return cloned;
            return this;
        }
    }


    public class FileOperationLogParams : EventArgs
    {
        public string Bencode = "";
        public DbConnection DbConnection;

        public MessageLogParams DbMessageLogParams;
        public string FileId = "";
        public int FileLogId;

        public int FileLogTaskId;
        public string FolderName = "";
        public string IcType = "";
        public string LogTableName = "";
        public string NewFileFullPath = "";
        public string NewFileName = "";
        public string OriginalFileName = "";
        public string OriginalFileUploadedOn = "";
        public string OriginalFullPath = "";
        public string Platform = "";
        public string ProcessingTask = "";
        public string ProcessingTaskOutcome = "";
        public string ProcessingTaskOutcomeDetails = "";
        public string TemplateType = "";
        public string ToFtp = "";


        public FileOperationLogParams()
        {
        }


        public FileOperationLogParams(DbConnection dbConnection, MessageLogParams dbMessageLogParams, string platform,
            string logTableName, string fileId, string folderName, string templateType, string icType, string toFtp,
            string bencode)
        {
            this.DbConnection = dbConnection;
            this.DbMessageLogParams = dbMessageLogParams;

            this.Platform = platform;
            this.LogTableName = logTableName;
            this.FileId = fileId;
            this.FolderName = folderName;
            this.TemplateType = templateType;
            this.IcType = icType;
            this.ToFtp = toFtp;
            this.Bencode = bencode;

            this.FileLogId = 0;
            this.FileLogTaskId = 0;
        }

        public FileOperationLogParams Clone()
        {
            var cloned = (FileOperationLogParams) this.MemberwiseClone();
            cloned.DbMessageLogParams = this.DbMessageLogParams;
            //
            return cloned;
        }


        public FileOperationLogParams ReInitIds()
        {
            //
            this.FileLogId = 0;
            this.FileLogTaskId = 0;
            this.FileId = "";
            //
            //var cloned = (MessageLogParams)MemberwiseClone();
            //return cloned;
            return this;
        }

        public MessageLogParams GetMessageLogParams()
        {
            //DbConnection dbConnection, string logTableName, string moduleName, string subModuleName,
            //string stepType, string stepName, string command
            return new MessageLogParams(this.DbConnection, "dbo.message_log", this.Platform, this.NewFileFullPath,
                this.ProcessingTask, this.ProcessingTaskOutcome, this.ProcessingTaskOutcomeDetails);
        }

        public FileOperationLogParams SetFileNames(string fileId, string originalFileName, string originalFullPath,
            string newFileName, string newFullPath, string processingTask, string processingTaskOutcome,
            string processingTaskOutcomeDetails)
        {
            //
            this.FileId = fileId;
            this.OriginalFileName = originalFileName;
            this.OriginalFullPath = originalFullPath;
            this.NewFileName = newFileName;
            this.NewFileFullPath = newFullPath;

            this.FolderName = "";
            this.Bencode = "";
            this.TemplateType = "";

            this.ProcessingTask = processingTask;
            this.ProcessingTaskOutcome = processingTaskOutcome;
            this.ProcessingTaskOutcomeDetails = processingTaskOutcomeDetails;
            //
            this.CalculateIds();
            //
            //var cloned = (FileOperationLogParams)MemberwiseClone();
            return this;
        }

        public FileOperationLogParams SetSourceFolderName(string folderName, string bencode = "", string icType = "",
            string templateType = "")
        {
            //
            this.FolderName = folderName;
            this.IcType = icType;
            this.TemplateType = templateType;
            this.Bencode = bencode;
            //
            this.OriginalFileName = folderName;
            this.OriginalFullPath = folderName;
            this.NewFileName = "";
            this.NewFileFullPath = "";
            //
            this.CalculateIds();
            //
            //var cloned = (FileOperationLogParams)MemberwiseClone();
            return this;
        }

        public FileOperationLogParams SetTaskOutcome(string processingTaskOutcome,
            string processingTaskOutcomeDetails)
        {
            //
            this.ProcessingTaskOutcome = processingTaskOutcome;
            this.ProcessingTaskOutcomeDetails = processingTaskOutcomeDetails;
            //
            this.CalculateIds();

            //var cloned = (FileOperationLogParams)MemberwiseClone();
            return this;
        }

        public void setOriginalFileUploadedOn(DateTime originalFileUploadedOn)
        {
            // add if > then sql server min date
            if (originalFileUploadedOn > Utils.ToDate("1753-01-01"))
            {
                this.OriginalFileUploadedOn = originalFileUploadedOn.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                this.OriginalFileUploadedOn = "";
            }
        }

        public void CalculateIds()
        {
            if (Utils.IsBlank(this.OriginalFileName) && Utils.IsBlank(this.NewFileName))
            {
                if (!Utils.IsBlank(this.FolderName))
                {
                    this.OriginalFileName = this.FolderName;
                }
                else if (!Utils.IsBlank(this.ProcessingTask))
                {
                    this.OriginalFileName = this.ProcessingTask;
                }
                else if (!Utils.IsBlank(this.Platform))
                {
                    this.OriginalFileName = this.Platform;
                }
            }

            var orgFileId = Utils.GetUniqueIdFromFileName(this.OriginalFileName);
            var newFileId = Utils.GetUniqueIdFromFileName(this.NewFileName);
            if (!Utils.IsBlank(newFileId))
            {
                this.FileId = newFileId;
                this.FileLogId = DbUtils.GetFileOperationFileLogId(this.NewFileName, this);
            }
            else if (!Utils.IsBlank(orgFileId))
            {
                this.FileId = orgFileId;
                this.FileLogId = DbUtils.GetFileOperationFileLogId(this.OriginalFileName, this);
            }
            else
            {
                // FileId = OriginalFileName;
                this.FileLogId = DbUtils.GetFileOperationFileLogId(this.OriginalFileName, this);
            }
        }


        public override string ToString()
        {
            return
                $"'{this.FileId}' - '{this.ProcessingTask}' - '{this.OriginalFileName}' - '{this.NewFileName}' - '{this.ProcessingTaskOutcome}' - '{this.ProcessingTaskOutcomeDetails}'";
        }
    }

}