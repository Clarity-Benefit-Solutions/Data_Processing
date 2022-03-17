using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using ExcelDataReader;

namespace CoreUtils.Classes
{
    public delegate void SingleFileCallback(string filePath1, string filePath2, string fileContents);

    public delegate void OnErrorCallback(string arg1, string arg2, Exception ex);


    
    
    public class FileUtils
    {
        #region IOOperations

        public static void IterateDirectory(string[] sourceDirectories, DirectoryIterateType iterateType,
            bool subDirsAlso, string[] fileMasks, SingleFileCallback fileCallback,
            OnErrorCallback onErrorCallback)
        {
            try
            {
                if (sourceDirectories == null || sourceDirectories.Length == 0)
                {
                    var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : sourceDirectories should be set";
                    throw new Exception(message);
                }

                // iterate for each fileMask
                foreach (var sourceDirectory in sourceDirectories)
                    IterateDirectory(sourceDirectory, iterateType, subDirsAlso, fileMasks, fileCallback,
                        onErrorCallback);
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                    onErrorCallback(sourceDirectories.Join(","), fileMasks.Join(","), ex);
                else
                    throw;
            }
        }

        public static void IterateDirectory(string directory, DirectoryIterateType iterateType, bool subDirsAlso,
            string[] fileMasks, SingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            if (fileMasks == null || fileMasks.Length == 0) fileMasks = new[] { "*.*" };

            try
            {
                // iterate for each fileMask
                foreach (var fileMask in fileMasks)
                    IterateDirectory(directory, iterateType, subDirsAlso, fileMask, fileCallback, onErrorCallback);
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                    onErrorCallback(directory, fileMasks.Join(","), ex);
                else
                    throw;
            }
        }

        public static void IterateDirectory(string directory, DirectoryIterateType iterateType, bool subDirsAlso,
            string fileMask, SingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            try
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
                foreach (var file in files)
                {
                    if (IgnoreFile(file))
                    {
                        continue;
                    }
                    else
                    {
                        fileCallback(file, "", "");
                    }
                };
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                    onErrorCallback(directory, fileMask, ex);
                else
                    throw;
            }
        }

        public static void MoveFiles(string[] sourceDirectories, bool subDirsAlso, string[] fileMasks,
            string destDirectory, string destFileName, string destFileExt, SingleFileCallback fileCallback,
            OnErrorCallback onErrorCallback)
        {
            try
            {
                if (sourceDirectories == null || sourceDirectories.Length == 0)
                {
                    var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : sourceDirectories should be set";
                    throw new Exception(message);
                }


                // iterate for each fileMask
                foreach (var sourceDirectory in sourceDirectories)
                    DoMultipleFilesOperation(FileOperation.Move, sourceDirectory, subDirsAlso, fileMasks, destDirectory,
                        destFileName, destFileExt, fileCallback, onErrorCallback);
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                    onErrorCallback(sourceDirectories.Join(","), fileMasks.Join(","), ex);
                else
                    throw;
            }
        }

        public static void MoveFiles(string sourceDirectory, bool subDirsAlso, string fileMask, string destDirectory,
            string destFileName, string destFileExt, SingleFileCallback fileCallback,
            OnErrorCallback onErrorCallback)
        {
            try
            {
                DoMultipleFilesOperation(FileOperation.Move, sourceDirectory, subDirsAlso, fileMask, destDirectory,
                    destFileName, destFileExt, fileCallback, onErrorCallback);
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                    onErrorCallback(sourceDirectory, fileMask, ex);
                else
                    throw;
            }
        }

        public static void CopyFiles(string[] sourceDirectories, bool subDirsAlso, string[] fileMasks,
            string destDirectory,
            string destFileName, string destFileExt, SingleFileCallback fileCallback,
            OnErrorCallback onErrorCallback)
        {
            try
            {
                if (sourceDirectories == null || sourceDirectories.Length == 0)
                {
                    var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : sourceDirectories should be set";
                    throw new Exception(message);
                }

                // iterate for each fileMask
                foreach (var sourceDirectory in sourceDirectories)
                    DoMultipleFilesOperation(FileOperation.Copy, sourceDirectory, subDirsAlso, fileMasks, destDirectory,
                        destFileName, destFileExt, fileCallback, onErrorCallback);
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                    onErrorCallback(sourceDirectories.Join(","), fileMasks.Join(","), ex);
                else
                    throw;
            }
        }

        public static void CopyFiles(string sourceDirectory, bool subDirsAlso, string[] fileMasks, string destDirectory,
            string destFileName, string destFileExt, SingleFileCallback fileCallback,
            OnErrorCallback onErrorCallback)
        {
            try
            {
                DoMultipleFilesOperation(FileOperation.Copy, sourceDirectory, subDirsAlso, fileMasks, destDirectory,
                    destFileName, destFileExt, fileCallback, onErrorCallback);
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                    onErrorCallback(sourceDirectory, fileMasks.Join(","), ex);
                else
                    throw;
            }
        }

        public static void CopyFiles(string sourceDirectory, bool subDirsAlso, string fileMask, string destDirectory,
            string destFileName, string destFileExt, SingleFileCallback fileCallback,
            OnErrorCallback onErrorCallback)
        {
            try
            {
                DoMultipleFilesOperation(FileOperation.Copy, sourceDirectory, subDirsAlso, fileMask, destDirectory,
                    destFileName, destFileExt, fileCallback, onErrorCallback);
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                    onErrorCallback(sourceDirectory, fileMask, ex);
                else
                    throw;
            }
        }

        public static void DeleteFiles(string[] sourceDirectories, bool subDirsAlso, string[] fileMasks,
            SingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            try
            {
                if (sourceDirectories == null || sourceDirectories.Length == 0)
                {
                    var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : sourceDirectories should be set";
                    throw new Exception(message);
                }

                // iterate for each fileMask
                foreach (var sourceDirectory in sourceDirectories)
                    DoMultipleFilesOperation(FileOperation.Delete, sourceDirectory, subDirsAlso, fileMasks, "", "", "",
                        fileCallback, onErrorCallback);
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                    onErrorCallback(sourceDirectories.Join(","), fileMasks.Join(","), ex);
                else
                    throw;
            }
        }

        public static void DeleteFiles(string sourceDirectory, bool subDirsAlso, string[] fileMasks,
            SingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            try
            {
                DoMultipleFilesOperation(FileOperation.Delete, sourceDirectory, subDirsAlso, fileMasks, "", "", "",
                    fileCallback, onErrorCallback);
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                    onErrorCallback(sourceDirectory, fileMasks.Join(","), ex);
                else
                    throw;
            }
        }

        public static void DeleteFiles(string sourceDirectory, bool subDirsAlso, string fileMask,
            SingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            try
            {
                DoMultipleFilesOperation(FileOperation.Delete, sourceDirectory, subDirsAlso, fileMask, "", "", "",
                    fileCallback, onErrorCallback);
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                    onErrorCallback(sourceDirectory, fileMask, ex);
                else
                    throw;
            }
        }

        public static void MoveFile(string sourceFilePath, string destFilePath, SingleFileCallback fileCallback,
            OnErrorCallback onErrorCallback)
        {
            try
            {
                DoSingleFileOperation(FileOperation.Move, sourceFilePath, destFilePath, fileCallback, onErrorCallback);
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                    onErrorCallback(sourceFilePath, destFilePath, ex);
                else
                    throw;
            }
        }

        public static void CopyFile(string sourceFilePath, string destFilePath, SingleFileCallback fileCallback,
            OnErrorCallback onErrorCallback)
        {
            try
            {
                DoSingleFileOperation(FileOperation.Copy, sourceFilePath, destFilePath, fileCallback, onErrorCallback);
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                    onErrorCallback(sourceFilePath, destFilePath, ex);
                else
                    throw;
            }
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


        public static void DeleteFile(string sourceFilePath, SingleFileCallback fileCallback,
            OnErrorCallback onErrorCallback)
        {
            try
            {
                DoSingleFileOperation(FileOperation.Delete, sourceFilePath, "", fileCallback, onErrorCallback);
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                    onErrorCallback(sourceFilePath, "", ex);
                else
                    throw;
            }
        }

        public static void DeleteFileIfExists(string sourceFilePath, SingleFileCallback fileCallback,
            OnErrorCallback onErrorCallback)
        {
            try
            {
                DoSingleFileOperation(FileOperation.DeleteIfExists, sourceFilePath, "", fileCallback, onErrorCallback);
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                    onErrorCallback(sourceFilePath, "", ex);
                else
                    throw;
            }
        }

        public static void ReadFile(string sourceFilePath, SingleFileCallback fileCallback,
            OnErrorCallback onErrorCallback)
        {
            try
            {
                DoSingleFileOperation(FileOperation.Read, sourceFilePath, "", fileCallback, onErrorCallback);
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                    onErrorCallback(sourceFilePath, "", ex);
                else
                    throw;
            }
        }


        private static void DoMultipleFilesOperation(FileOperation fileOperation, string sourceDirectory,
            bool subDirsAlso, string[] fileMasks, string destDirectory, string destFileName, string destFileExt,
            SingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            if (fileMasks == null || fileMasks.Length == 0) fileMasks = new[] { "*.*" };
            try
            {
                // iterate for each fileMask
                foreach (var fileMask in fileMasks)
                    DoMultipleFilesOperation(fileOperation, sourceDirectory, subDirsAlso, fileMask, destDirectory,
                        destFileName, destFileExt, fileCallback,
                        onErrorCallback);
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                    onErrorCallback(sourceDirectory, fileMasks.Join(","), ex);
                else
                    throw;
            }
        }

        private static void DoMultipleFilesOperation(FileOperation fileOperation, string sourceDirectory,
            bool subDirsAlso, string fileMask, string destDirectory, string destFileName, string destFileExt,
            SingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            try
            {
                IterateDirectory(sourceDirectory, DirectoryIterateType.Files, subDirsAlso, fileMask,
                    // handle each found file
                    (foundFile, dummy, dummy2) =>
                    {
                        // 
                        var fileInfo = new FileInfo(foundFile);
                        var destFileNameOnly =
                            Path.GetFileNameWithoutExtension(Utils.IsBlank(destFileName)
                                ? fileInfo.Name
                                : destFileName);
                        var destFileExtOnly = Utils.IsBlank(destFileExt) ? fileInfo.Extension : destFileExt;
                        var destFullFilePath = $"{destDirectory}/{destFileNameOnly}{destFileExtOnly}";
                        //
                        DoSingleFileOperation(fileOperation, foundFile, destFullFilePath, fileCallback,
                            onErrorCallback);
                    },
                    // at end 
                    (arg1, arg2, ex) =>
                    {
                        if (onErrorCallback == null) throw ex;
                        onErrorCallback(arg1, arg2, ex);
                    }
                );
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                    onErrorCallback(sourceDirectory, fileMask, ex);
                else
                    throw;
            }
        }

        public static void DoSingleFileOperation(FileOperation fileOperation, string sourceFilePath,
            string destFilePath, SingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            //
            try
            {
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
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                    onErrorCallback(sourceFilePath, destFilePath, ex);
                else
                    throw;
            }
        }

        public static string GetFlatFileContents(string sourceFilePath, int firstNLinesOnly = 0)
        {
            var srcFileInfo = new FileInfo(sourceFilePath);

            // validate
            if (!srcFileInfo.Exists)
            {
                var message =
                    $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : Source File: {sourceFilePath} does not exist";
                throw new Exception(message);
            }

            var contents = "";
            // read each line and insert
            using var inputFile = new StreamReader(sourceFilePath);
            string line;
            var rowNo = 0;
            while ((line = inputFile.ReadLine()!) != null)
            {
                rowNo++;
                contents += line;

                if (firstNLinesOnly != 0 && rowNo >= firstNLinesOnly) return contents;
            }

            return contents;
        }

        #endregion

        #region FileConversions

        public static void ConvertAllExcelFilesToCsv(string sourceDir, bool subDirsAlso, string destDir,
            DbConnection dbConn, FileOperationLogParams fileLogParams, OnErrorCallback onErrorCallback)
        {
            IterateDirectory(
                sourceDir, DirectoryIterateType.Files, subDirsAlso, "*.xls*"
                , /*fileCallBack*/ (foundFile, dummy, dummy2) =>
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
                        },
                        onErrorCallback);
                } /*end fileCallBack*/
                , /*onErrorCallback*/ // at end 
                (arg1, arg2, ex) => { DbUtils.LogError(arg1, arg2, ex, fileLogParams); } /*end onErrorCallback*/
            ); //FileUtils.IterateDirectory;
        } //end method

        public static void ConvertExcelFileToCsv(string sourceFilePath, string destFilePath,
            SingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            try
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

                    //
                    FileUtils.EnsurePathExists(destFilePath);
                    var csv = new StreamWriter(destFilePath, false);
                    csv.Write(csvContent);
                    csv.Close();
                }

                // callback for complete
                if (fileCallback != null) fileCallback(sourceFilePath, destFilePath, "");
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                    onErrorCallback(sourceFilePath, destFilePath, ex);
                else
                    throw;
            }
        }

        #endregion

        public static void EnsurePathExists(string fullFilePath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullFilePath)!);
            }
            catch (Exception)
            {
                //ignore
            }
        }

        public static Boolean IgnoreFile(string srcFilePath)
        {
            FileInfo srcFileInfo = new FileInfo(srcFilePath);
            string fileName = srcFileInfo.Name;

            if (fileName.StartsWith("."))
            {
                return true;
            }

            return false;
        }
    }
}