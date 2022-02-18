using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using ExcelDataReader;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;


namespace CoreUtils.Classes
{
    public delegate void SingleFileCallback(string filePath1, string filePath2, string fileContents);

    public delegate void FileActionCompleteCallback();


    [Guid("EAA4976A-45C3-4BC5-BC0B-E474F4C3CABC")]
    [ComVisible(true)]
    public class FileUtils
    {
        #region IOOperations

        public static void IterateDirectory(string[] sourceDirectories, DirectoryIterateType iterateType,
            bool subDirsAlso, string[] fileMasks, SingleFileCallback fileCallback,
            FileActionCompleteCallback actionCompleteCallback)
        {
            if (sourceDirectories == null || sourceDirectories.Length == 0)
            {
                var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : sourceDirectories should be set";
                throw new Exception(message);
            }

            // iterate for each fileMask
            foreach (var sourceDirectory in sourceDirectories)
                IterateDirectory(sourceDirectory, iterateType, subDirsAlso, fileMasks, fileCallback,
                    null /*we dont want to trigger complete till all masks are processed*/);

            // callback for complete
            if (actionCompleteCallback != null) actionCompleteCallback();
        }

        public static void IterateDirectory(string directory, DirectoryIterateType iterateType, bool subDirsAlso,
            string[] fileMasks, SingleFileCallback fileCallback, FileActionCompleteCallback actionCompleteCallback)
        {
            if (fileMasks == null || fileMasks.Length == 0) fileMasks = new[] { "*.*" };

            // iterate for each fileMask
            foreach (var fileMask in fileMasks)
                IterateDirectory(directory, iterateType, subDirsAlso, fileMask, fileCallback,
                    null /*we don't want to trigger complete till all masks are processed*/);

            // callback for complete
            if (actionCompleteCallback != null) actionCompleteCallback();
        }

        public static void IterateDirectory(string directory, DirectoryIterateType iterateType, bool subDirsAlso,
            string fileMask, SingleFileCallback fileCallback, FileActionCompleteCallback actionCompleteCallback)
        {
            // validate
            if (Utils.IsBlank(directory))
            {
                var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : directory should be set";
                throw new Exception(message);
            }

            if (Utils.IsBlank(fileMask))
            {
                var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : fileMask should be set";
                throw new Exception(message);
            }

            if (fileCallback == null)
            {
                var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : fileCallback should be set";
                throw new Exception(message);
            }
            //if (actionCompleteCallback == null)
            //{
            //    string message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : actionCompleteCallback should be set";
            //    throw new Exception(message);
            //}

            // check dir exists
            var dirInfo = new DirectoryInfo(directory);
            if (dirInfo.Exists == false) Directory.CreateDirectory(directory);

            // get all files in dir
            string[] files = { };

            if (iterateType == DirectoryIterateType.Files)
                files = Directory.GetFiles(directory, fileMask,
                    subDirsAlso ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly
                );
            else if (iterateType == DirectoryIterateType.Directories)
                files = Directory.GetDirectories(directory, fileMask,
                    subDirsAlso ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly
                );


            // callback for each file
            foreach (var file in files) fileCallback(file, "", "");

            // callback for complete
            if (actionCompleteCallback != null) actionCompleteCallback();
        }

        public static void MoveFiles(string[] sourceDirectories, bool subDirsAlso, string[] fileMasks,
            string destDirectory, string destFileName, string destFileExt, SingleFileCallback fileCallback,
            FileActionCompleteCallback actionCompleteCallback)
        {
            if (sourceDirectories == null || sourceDirectories.Length == 0)
            {
                var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : sourceDirectories should be set";
                throw new Exception(message);
            }


            // iterate for each fileMask
            foreach (var sourceDirectory in sourceDirectories)
                DoMultipleFilesOperation(FileOperation.Move, sourceDirectory, subDirsAlso, fileMasks, destDirectory,
                    destFileName, destFileExt, fileCallback, actionCompleteCallback);

            // callback for complete
            if (actionCompleteCallback != null) actionCompleteCallback();
        }

        public static void MoveFiles(string sourceDirectory, bool subDirsAlso, string fileMask, string destDirectory,
            string destFileName, string destFileExt, SingleFileCallback fileCallback,
            FileActionCompleteCallback actionCompleteCallback)
        {
            DoMultipleFilesOperation(FileOperation.Move, sourceDirectory, subDirsAlso, fileMask, destDirectory,
                destFileName, destFileExt, fileCallback, actionCompleteCallback);
        }

        public static void CopyFiles(string[] sourceDirectories, bool subDirsAlso, string[] fileMasks,
            string destDirectory,
            string destFileName, string destFileExt, SingleFileCallback fileCallback,
            FileActionCompleteCallback actionCompleteCallback)
        {
            if (sourceDirectories == null || sourceDirectories.Length == 0)
            {
                var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : sourceDirectories should be set";
                throw new Exception(message);
            }

            // iterate for each fileMask
            foreach (var sourceDirectory in sourceDirectories)
                DoMultipleFilesOperation(FileOperation.Copy, sourceDirectory, subDirsAlso, fileMasks, destDirectory,
                    destFileName, destFileExt, fileCallback, actionCompleteCallback);

            // callback for complete
            if (actionCompleteCallback != null) actionCompleteCallback();
        }

        public static void CopyFiles(string sourceDirectory, bool subDirsAlso, string[] fileMasks, string destDirectory,
            string destFileName, string destFileExt, SingleFileCallback fileCallback,
            FileActionCompleteCallback actionCompleteCallback)
        {
            DoMultipleFilesOperation(FileOperation.Copy, sourceDirectory, subDirsAlso, fileMasks, destDirectory,
                destFileName, destFileExt, fileCallback, actionCompleteCallback);
        }

        public static void CopyFiles(string sourceDirectory, bool subDirsAlso, string fileMask, string destDirectory,
            string destFileName, string destFileExt, SingleFileCallback fileCallback,
            FileActionCompleteCallback actionCompleteCallback)
        {
            DoMultipleFilesOperation(FileOperation.Copy, sourceDirectory, subDirsAlso, fileMask, destDirectory,
                destFileName, destFileExt, fileCallback, actionCompleteCallback);
        }

        public static void DeleteFiles(string[] sourceDirectories, bool subDirsAlso, string[] fileMasks,
            SingleFileCallback fileCallback, FileActionCompleteCallback actionCompleteCallback)
        {
            if (sourceDirectories == null || sourceDirectories.Length == 0)
            {
                var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : sourceDirectories should be set";
                throw new Exception(message);
            }

            // iterate for each fileMask
            foreach (var sourceDirectory in sourceDirectories)
                DoMultipleFilesOperation(FileOperation.Delete, sourceDirectory, subDirsAlso, fileMasks, "", "", "",
                    fileCallback, actionCompleteCallback);

            // callback for complete
            if (actionCompleteCallback != null) actionCompleteCallback();
        }

        public static void DeleteFiles(string sourceDirectory, bool subDirsAlso, string[] fileMasks,
            SingleFileCallback fileCallback, FileActionCompleteCallback actionCompleteCallback)
        {
            DoMultipleFilesOperation(FileOperation.Delete, sourceDirectory, subDirsAlso, fileMasks, "", "", "",
                fileCallback, actionCompleteCallback);
        }

        public static void DeleteFiles(string sourceDirectory, bool subDirsAlso, string fileMask,
            SingleFileCallback fileCallback, FileActionCompleteCallback actionCompleteCallback)
        {
            DoMultipleFilesOperation(FileOperation.Delete, sourceDirectory, subDirsAlso, fileMask, "", "", "",
                fileCallback, actionCompleteCallback);
        }

        public static void MoveFile(string sourceFilePath, string destFilePath, SingleFileCallback fileCallback)
        {
            DoSingleFileOperation(FileOperation.Move, sourceFilePath, destFilePath, fileCallback);
        }

        public static void CopyFile(string sourceFilePath, string destFilePath, SingleFileCallback fileCallback)
        {
            DoSingleFileOperation(FileOperation.Copy, sourceFilePath, destFilePath, fileCallback);
        }

        public static string FixPath(string filePath, bool makeLowerCase = false)
        {
            filePath = filePath.Replace("\\", "/");
            if (makeLowerCase) filePath = filePath.ToLower();
            return filePath;
        }


        public static string GetDestFilePath(string sourceFilePath, string destDir, string replaceNamePattern,
            string replaceNameString, string newExt)
        {
            var sourceFileInfo = new FileInfo(sourceFilePath);
            if (!sourceFileInfo.Exists)
            {
                var message =
                    $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : Source File: {sourceFilePath} does not exist";
                throw new Exception(message);
            }

            //
            var newName = Path.GetFileNameWithoutExtension(sourceFileInfo.Name);
            if (!Utils.IsBlank(replaceNamePattern))
                newName = Regex.Replace(newName, replaceNamePattern, replaceNameString);

            if (Utils.IsBlank(newExt)) newExt = sourceFileInfo.Extension;

            var destFilePath = $"{destDir}/{newName}{newExt}";
            return destFilePath;
        }

        public static string GetDestFilePath(string sourceFilePath, string newExt)
        {
            var sourceFileInfo = new FileInfo(sourceFilePath);
            if (!sourceFileInfo.Exists)
            {
                var message =
                    $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : Source File: {sourceFilePath} does not exist";
                throw new Exception(message);
            }

            //
            var newName = Path.GetFileNameWithoutExtension(sourceFileInfo.Name);

            if (Utils.IsBlank(newExt)) newExt = sourceFileInfo.Extension;

            var destFilePath = $"{sourceFileInfo.Directory}/{newName}{newExt}";
            return destFilePath;
        }


        public static void DeleteFile(string sourceFilePath, SingleFileCallback fileCallback)
        {
            DoSingleFileOperation(FileOperation.Delete, sourceFilePath, "", fileCallback);
        }

        public static void DeleteFileIfExists(string sourceFilePath, SingleFileCallback fileCallback)
        {
            DoSingleFileOperation(FileOperation.DeleteIfExists, sourceFilePath, "", fileCallback);
        }

        public static void ReadFile(string sourceFilePath, SingleFileCallback fileCallback)
        {
            DoSingleFileOperation(FileOperation.Read, sourceFilePath, "", fileCallback);
        }


        private static void DoMultipleFilesOperation(FileOperation fileOperation, string sourceDirectory,
            bool subDirsAlso, string[] fileMasks, string destDirectory, string destFileName, string destFileExt,
            SingleFileCallback fileCallback, FileActionCompleteCallback actionCompleteCallback)
        {
            if (fileMasks == null || fileMasks.Length == 0) fileMasks = new[] { "*.*" };

            // iterate for each fileMask
            foreach (var fileMask in fileMasks)
                DoMultipleFilesOperation(fileOperation, sourceDirectory, subDirsAlso, fileMask, destDirectory,
                    destFileName, destFileExt, fileCallback,
                    null /*we don't want to trigger complete till all masks are processed*/);

            // callback for complete
            if (actionCompleteCallback != null) actionCompleteCallback();
        }

        private static void DoMultipleFilesOperation(FileOperation fileOperation, string sourceDirectory,
            bool subDirsAlso, string fileMask, string destDirectory, string destFileName, string destFileExt,
            SingleFileCallback fileCallback, FileActionCompleteCallback actionCompleteCallback)
        {
            IterateDirectory(sourceDirectory, DirectoryIterateType.Files, subDirsAlso, fileMask,
                // handle each found file
                (foundFile, dummy, dummy2) =>
                {
                    // 
                    var fileInfo = new FileInfo(foundFile);
                    var destFileNameOnly =
                        Path.GetFileNameWithoutExtension(Utils.IsBlank(destFileName) ? fileInfo.Name : destFileName);
                    var destFileExtOnly = Utils.IsBlank(destFileExt) ? fileInfo.Extension : destFileExt;
                    var destFullFilePath = $"{destDirectory}/{destFileNameOnly}{destFileExtOnly}";
                    //
                    DoSingleFileOperation(fileOperation, foundFile, destFullFilePath, fileCallback);
                },
                // at end 
                () =>
                {
                    // callback for complete
                    if (actionCompleteCallback != null) actionCompleteCallback();
                }
            );
        }

        public static void DoSingleFileOperation(FileOperation fileOperation, string sourceFilePath,
            string destFilePath, SingleFileCallback fileCallback)
        {
            //
            var srcFileInfo = new FileInfo(sourceFilePath);
            if (!Utils.IsBlank(destFilePath))
            {
                var destFileDirInfo = new DirectoryInfo(Path.GetDirectoryName(destFilePath) ?? string.Empty);
                if (!destFileDirInfo.Exists) destFileDirInfo.Create();
            }

            var fileContents = "";

            // validate
            if (!srcFileInfo.Exists)
            {
                if (fileOperation == FileOperation.DeleteIfExists) return;

                var message =
                    $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : Source File: {sourceFilePath} does not exist";
                throw new Exception(message);
            }

            // do operation
            if (fileOperation == FileOperation.Move)
            {
                var destFile = new FileInfo(destFilePath);
                if (destFile.Exists)
                {
                    destFile.Delete();
                    fileContents = "";
                }

                srcFileInfo.MoveTo(destFilePath);
            }
            else if (fileOperation == FileOperation.Copy)
            {
                srcFileInfo.CopyTo(destFilePath, true);
                fileContents = "";
            }
            else if (fileOperation == FileOperation.Delete || fileOperation == FileOperation.DeleteIfExists)
            {
                srcFileInfo.Delete();
                destFilePath = "";
                fileContents = "";
            }
            else if (fileOperation == FileOperation.Read)
            {
                fileContents = File.ReadAllText(sourceFilePath);
                destFilePath = "";
            }
            else
            {
                var message =
                    $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : fileOperation : {fileOperation} is invalid";
                throw new Exception(message);
            }

            // callback for complete
            if (fileCallback != null) fileCallback(sourceFilePath, destFilePath, fileContents);
        }

        #endregion

        #region FileConversions

        public static void ConvertAllExcelFilesToCsv(string sourceDir, bool subDirsAlso, string destDir,
            DbConnection dbConn, FileOperationLogParams fileLogParams)
        {
            IterateDirectory(
                sourceDir, DirectoryIterateType.Files, subDirsAlso, "*.xls*"
                , /*filecallback*/ (foundFile, dummy, dummy2) =>
                {
                    var csvFilePath = GetDestFilePath(foundFile, destDir, "", "", ".csv");
                    //
                    ConvertExcelFileToCsv(foundFile, csvFilePath,
                        (srcFilePath, destFilePath, dummy4) =>
                        {
                            // add to fileLog
                            fileLogParams.SetFileNames("", Path.GetFileName(srcFilePath), srcFilePath,
                                Path.GetFileName(destFilePath), destFilePath, "CreateHeaders-ConvertExcelFile",
                                "Success", "Converted Excel File to Csv");
                            DbUtils.LogFileOperation(fileLogParams);
                        });
                } /*end filecallback*/
                , /*actionCompleteCallback*/ () => { } /*end actionCompleteCallback*/
            ); //FileUtils.IterateDirectory;
        } //end method

        public static void ConvertExcelFileToCsv(string sourceFilePath, string destFilePath,
            SingleFileCallback fileCallback)
        {
            using (var stream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                IExcelDataReader reader = null;
                if (sourceFilePath.EndsWith(".xls"))
                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                else if (sourceFilePath.EndsWith(".xlsx")) reader = ExcelReaderFactory.CreateOpenXmlReader(stream);

                if (reader == null) return;

                var ds = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = tableReader => new ExcelDataTableConfiguration
                    {
                        UseHeaderRow = false
                    }
                });

                var csvContent = string.Empty;
                var rowNo = 0;
                while (rowNo < ds.Tables[0].Rows.Count)
                {
                    var arr = new List<string>();
                    for (var i = 0; i < ds.Tables[0].Columns.Count; i++)
                        arr.Add(ds.Tables[0].Rows[rowNo][i].ToString());
                    rowNo++;
                    csvContent += string.Join(",", arr) + "\n";
                }

                var csv = new StreamWriter(destFilePath, false);
                csv.Write(csvContent);
                csv.Close();
            }

            // callback for complete
            if (fileCallback != null) fileCallback(sourceFilePath, destFilePath, "");
        }

        #endregion

       
    }
}