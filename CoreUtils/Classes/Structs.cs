using System;
using System.Data.Common;
using System.Runtime.InteropServices;

namespace CoreUtils.Classes
{
    
    
    public class MessageLogParams : EventArgs
    {
        public string Command = "";
        public DbConnection DbConnection = null;
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
            DbConnection = dbConnection;
            LogTableName = logTableName;
            ModuleName = moduleName;
            SubModuleName = subModuleName;
            StepType = stepType;
            StepName = stepName;
            Command = command;
        }

        public override string ToString()
        {
            return $"{LogTableName} - {ModuleName} - {SubModuleName} - {StepType} - {StepName} - {Command} ";
        }


        public MessageLogParams Clone()
        {
            var cloned = (MessageLogParams) MemberwiseClone();
            //
            return cloned;
        }

        public MessageLogParams SetSubModuleStepAndCommand(string subModuleName, string stepType, string stepName,
            string command)
        {
            //
            SubModuleName = subModuleName;
            StepType = stepType;
            StepName = stepName;
            Command = command;
            //
            //var cloned = (MessageLogParams)MemberwiseClone();
            //return cloned;
            return this;
        }

        public MessageLogParams SetStepAndCommand(string stepName, string command)
        {
            var cloned = (MessageLogParams) MemberwiseClone();
            //
            cloned.StepName = stepName;
            cloned.Command = command;
            //
            return cloned;
        }

        public MessageLogParams SetCommand(string command)
        {
            Command = command;
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
        public DbConnection DbConnection = null;

        public MessageLogParams DbMessageLogParams;
        public string FileId = "";
        public int FileLogId = 0;

        public int FileLogTaskId = 0;
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
            DbConnection = dbConnection;
            DbMessageLogParams = dbMessageLogParams;

            Platform = platform;
            LogTableName = logTableName;
            FileId = fileId;
            FolderName = folderName;
            TemplateType = templateType;
            IcType = icType;
            ToFtp = toFtp;
            Bencode = bencode;

            FileLogId = 0;
            FileLogTaskId = 0;
        }

        public FileOperationLogParams Clone()
        {
            var cloned = (FileOperationLogParams) MemberwiseClone();
            cloned.DbMessageLogParams = DbMessageLogParams;
            //
            return cloned;
        }


        public FileOperationLogParams ReInitIds()
        {
            //
            FileLogId = 0;
            FileLogTaskId = 0;
            FileId = "";
            //
            //var cloned = (MessageLogParams)MemberwiseClone();
            //return cloned;
            return this;
        }

        public MessageLogParams GetMessageLogParams()
        {
            //DbConnection dbConnection, string logTableName, string moduleName, string subModuleName,
            //string stepType, string stepName, string command
            return new MessageLogParams(DbConnection, "dbo.message_log", Platform,
                NewFileFullPath, ProcessingTask, ProcessingTaskOutcome, ProcessingTaskOutcomeDetails);
        }

        public FileOperationLogParams SetFileNames(string fileId, string originalFileName, string originalFullPath,
            string newFileName, string newFullPath, string processingTask, string processingTaskOutcome,
            string processingTaskOutcomeDetails)
        {
            //
            FileId = fileId;
            OriginalFileName = originalFileName;
            OriginalFullPath = originalFullPath;
            NewFileName = newFileName;
            NewFileFullPath = newFullPath;

            FolderName = "";
            Bencode = "";
            TemplateType = "";


            ProcessingTask = processingTask;
            ProcessingTaskOutcome = processingTaskOutcome;
            ProcessingTaskOutcomeDetails = processingTaskOutcomeDetails;
            //
            CalculateIds();
            //
            //var cloned = (FileOperationLogParams)MemberwiseClone();
            return this;
        }

        public FileOperationLogParams SetSourceFolderName(string folderName, string bencode = "", string icType = "",
            string templateType = "")
        {
            //
            FolderName = folderName;
            IcType = icType;
            TemplateType = templateType;
            Bencode = bencode;
            //
            OriginalFileName = folderName;
            OriginalFullPath = folderName;
            NewFileName = "";
            NewFileFullPath = "";
            //
            CalculateIds();
            //
            //var cloned = (FileOperationLogParams)MemberwiseClone();
            return this;
        }

        public FileOperationLogParams SetTaskOutcome(string processingTaskOutcome,
            string processingTaskOutcomeDetails)
        {
            //
            ProcessingTaskOutcome = processingTaskOutcome;
            ProcessingTaskOutcomeDetails = processingTaskOutcomeDetails;
            //
            CalculateIds();

            //var cloned = (FileOperationLogParams)MemberwiseClone();
            return this;
        }

        public void setOriginalFileUploadedOn(DateTime originalFileUploadedOn)
        {
            if (originalFileUploadedOn > DateTime.MinValue)
                OriginalFileUploadedOn = originalFileUploadedOn.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public void CalculateIds()
        {
            if (Utils.IsBlank(OriginalFileName) && Utils.IsBlank(NewFileName))
            {
                if (!Utils.IsBlank(FolderName))
                    OriginalFileName = FolderName;
                else if (!Utils.IsBlank(ProcessingTask))
                    OriginalFileName = ProcessingTask;
                else if (!Utils.IsBlank(Platform)) OriginalFileName = Platform;
            }

            var orgFileId = DbUtils.GetUniqueIdFromFileName(OriginalFileName);
            var newFileId = DbUtils.GetUniqueIdFromFileName(NewFileName);
            if (!Utils.IsBlank(newFileId))
            {
                FileId = newFileId;
                FileLogId = DbUtils.GetFileOperationFileLogId(NewFileName, this);
            }
            else if (!Utils.IsBlank(orgFileId))
            {
                FileId = orgFileId;
                FileLogId = DbUtils.GetFileOperationFileLogId(OriginalFileName, this);
            }
            else
            {
                // FileId = OriginalFileName;
                FileLogId = DbUtils.GetFileOperationFileLogId(OriginalFileName, this);
            }
        }


        public override string ToString()
        {
            return
                $"'{FileId}' -  '{ProcessingTask}' - '{OriginalFileName}' - '{NewFileName}' - '{ProcessingTaskOutcome}' - '{ProcessingTaskOutcomeDetails}'";
        }
    }
}