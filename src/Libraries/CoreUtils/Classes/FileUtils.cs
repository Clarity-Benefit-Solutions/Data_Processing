using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ExcelDataReader;
using ExcelDataReader.Exceptions;
using Sylvan;

namespace CoreUtils.Classes
{

    public delegate void SingleFileCallback(string filePath1, string filePath2, string fileContents);

    public delegate bool ImportThisLineCallback(string filePath1, int rowNo, string line);

    public delegate void OnErrorCallback(string arg1, string arg2, Exception ex);


    public class FileUtils
    {
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

        public static bool IgnoreFile(string srcFilePath)
        {
            var srcFileInfo = new FileInfo(srcFilePath);
            var fileName = srcFileInfo.Name;

            if (fileName.StartsWith("."))
            {
                return true;
            }

            return false;
        }

        public static void WriteToFile(string filePath, string text, OnErrorCallback onErrorCallback)
        {
            try
            {
                //
                EnsurePathExists(filePath);

                using var writer = new StreamWriter(filePath, false);
                writer.WriteLine(text);

                // close
                if (writer != null)
                {
                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                {
                    onErrorCallback(filePath, text, ex);
                }
                else
                {
                    throw;
                }
            }
        }

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
                {
                    IterateDirectory(sourceDirectory, iterateType, subDirsAlso, fileMasks, fileCallback,
                        onErrorCallback);
                }
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                {
                    onErrorCallback(sourceDirectories.Join(","), fileMasks.Join(","), ex);
                }
                else
                {
                    throw;
                }
            }
        }

        public static void IterateDirectory(string directory, DirectoryIterateType iterateType, bool subDirsAlso,
            string[] fileMasks, SingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            if (fileMasks == null || fileMasks.Length == 0)
            {
                fileMasks = new[] { "*.*" };
            }

            try
            {
                // iterate for each fileMask
                foreach (var fileMask in fileMasks)
                {
                    IterateDirectory(directory, iterateType, subDirsAlso, fileMask, fileCallback, onErrorCallback);
                }
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                {
                    onErrorCallback(directory, fileMasks.Join(","), ex);
                }
                else
                {
                    throw;
                }
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
                if (dirInfo.Exists == false)
                {
                    Directory.CreateDirectory(directory);
                }

                // get all files in dir
                string[] files = { };

                if (iterateType == DirectoryIterateType.Files)
                {
                    files = Directory.GetFiles(directory, fileMask,
                        subDirsAlso ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly
                    );
                }
                else if (iterateType == DirectoryIterateType.Directories)
                {
                    files = Directory.GetDirectories(directory, fileMask,
                        subDirsAlso ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly
                    );
                }

                // callback for each file
                foreach (var file in files)
                {
                    try
                    {
                        if (IgnoreFile(file))
                        {
                            // dont call back
                        }
                        else
                        {
                            fileCallback(file, "", "");
                        }
                    }
                    catch (Exception ex)
                    {
                        // callback for complete
                        if (onErrorCallback != null)
                        {
                            onErrorCallback(directory, file, ex);
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                {
                    onErrorCallback(directory, directory, ex);
                }
                else
                {
                    throw;
                }
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
                {
                    DoMultipleFilesOperation(FileOperation.Move, sourceDirectory, subDirsAlso, fileMasks, destDirectory,
                        destFileName, destFileExt, fileCallback, onErrorCallback);
                }
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                {
                    onErrorCallback(sourceDirectories.Join(","), fileMasks.Join(","), ex);
                }
                else
                {
                    throw;
                }
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
                {
                    onErrorCallback(sourceDirectory, fileMask, ex);
                }
                else
                {
                    throw;
                }
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
                {
                    DoMultipleFilesOperation(FileOperation.Copy, sourceDirectory, subDirsAlso, fileMasks, destDirectory,
                        destFileName, destFileExt, fileCallback, onErrorCallback);
                }
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                {
                    onErrorCallback(sourceDirectories.Join(","), fileMasks.Join(","), ex);
                }
                else
                {
                    throw;
                }
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
                {
                    onErrorCallback(sourceDirectory, fileMasks.Join(","), ex);
                }
                else
                {
                    throw;
                }
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
                {
                    onErrorCallback(sourceDirectory, fileMask, ex);
                }
                else
                {
                    throw;
                }
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
                {
                    DoMultipleFilesOperation(FileOperation.Delete, sourceDirectory, subDirsAlso, fileMasks, "", "", "",
                        fileCallback, onErrorCallback);
                }
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                {
                    onErrorCallback(sourceDirectories.Join(","), fileMasks.Join(","), ex);
                }
                else
                {
                    throw;
                }
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
                {
                    onErrorCallback(sourceDirectory, fileMasks.Join(","), ex);
                }
                else
                {
                    throw;
                }
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
                {
                    onErrorCallback(sourceDirectory, fileMask, ex);
                }
                else
                {
                    throw;
                }
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
                {
                    onErrorCallback(sourceFilePath, destFilePath, ex);
                }
                else
                {
                    throw;
                }
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
                {
                    onErrorCallback(sourceFilePath, destFilePath, ex);
                }
                else
                {
                    throw;
                }
            }
        }

        public static string FixPath(string filePath, bool makeLowerCase = false)
        {
            // do not fix path is starts with // i.e. is a UNC path
            if (filePath.StartsWith("\\\\"))
            {
                return filePath;
            }

            filePath = filePath.Replace("\\", "/");
            filePath = filePath.Replace("//", "/");
            // remove last slash
            if (filePath.Length > 1 && Utils.Right(filePath, 1) == "/")
            {
                filePath = filePath.Remove(filePath.Length - 1, 1);
            }

            if (makeLowerCase)
            {
                filePath = filePath.ToLower();
            }

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
            {
                newName = Regex.Replace(newName, replaceNamePattern, replaceNameString);
            }

            if (Utils.IsBlank(newExt))
            {
                newExt = sourceFileInfo.Extension;
            }

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

            if (Utils.IsBlank(newExt))
            {
                newExt = sourceFileInfo.Extension;
            }

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
                {
                    onErrorCallback(sourceFilePath, "", ex);
                }
                else
                {
                    throw;
                }
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
                {
                    onErrorCallback(sourceFilePath, "", ex);
                }
                else
                {
                    throw;
                }
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
                {
                    onErrorCallback(sourceFilePath, "", ex);
                }
                else
                {
                    throw;
                }
            }
        }


        private static void DoMultipleFilesOperation(FileOperation fileOperation, string sourceDirectory,
            bool subDirsAlso, string[] fileMasks, string destDirectory, string destFileName, string destFileExt,
            SingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            if (fileMasks == null || fileMasks.Length == 0)
            {
                fileMasks = new[] { "*.*" };
            }

            try
            {
                // iterate for each fileMask
                foreach (var fileMask in fileMasks)
                {
                    DoMultipleFilesOperation(fileOperation, sourceDirectory, subDirsAlso, fileMask, destDirectory,
                        destFileName, destFileExt, fileCallback,
                        onErrorCallback);
                }
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                {
                    onErrorCallback(sourceDirectory, fileMasks.Join(","), ex);
                }
                else
                {
                    throw;
                }
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
                    (directory, file, ex) =>
                    {
                        if (onErrorCallback == null)
                        {
                            throw ex;
                        }

                        onErrorCallback(directory, file, ex);
                    }
                );
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                {
                    onErrorCallback(sourceDirectory, fileMask, ex);
                }
                else
                {
                    throw;
                }
            }
        }

        public static void DoSingleFileOperation(FileOperation fileOperation, string sourceFilePath,
            string destFilePath, SingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            //
            try
            {
                sourceFilePath = FileUtils.FixPath(sourceFilePath);
                destFilePath = FileUtils.FixPath(destFilePath);

                var srcFileInfo = new FileInfo(sourceFilePath);
                if (!Utils.IsBlank(destFilePath))
                {
                    var destFileDirInfo = new DirectoryInfo(Path.GetDirectoryName(destFilePath) ?? string.Empty);
                    if (!destFileDirInfo.Exists)
                    {
                        destFileDirInfo.Create();
                    }
                }

                var fileContents = "";

                // validate
                if (!srcFileInfo.Exists)
                {
                    if (fileOperation == FileOperation.DeleteIfExists)
                    {
                        return;
                    }

                    var message =
                        $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : Source File: {sourceFilePath} does not exist";
                    throw new Exception(message);
                }

                // do operation
                if (fileOperation == FileOperation.Move)
                {
                    if (FixPath(destFilePath) != FixPath(sourceFilePath))
                    {
                        var destFile = new FileInfo(destFilePath);
                        if (destFile.Exists)
                        {
                            destFile.Delete();
                            fileContents = "";
                        }

                        srcFileInfo.MoveTo(destFilePath);
                    }
                }
                else if (fileOperation == FileOperation.Copy)
                {
                    if (FixPath(destFilePath) != FixPath(sourceFilePath))
                    {
                        srcFileInfo.CopyTo(destFilePath, true);
                        fileContents = "";
                    }
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
                if (fileCallback != null)
                {
                    fileCallback(sourceFilePath, destFilePath, fileContents);
                }
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                {
                    onErrorCallback(sourceFilePath, destFilePath, ex);
                }
                else
                {
                    throw;
                }
            }
        }

        public static string GetFlatFileContents(string srcFilePath, int firstNLinesOnly = 0)
        {
            var srcFileInfo = new FileInfo(srcFilePath);

            // validate
            if (!srcFileInfo.Exists)
            {
                var message =
                    $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : Source File: {srcFilePath} does not exist";
                throw new Exception(message);
            }

            if (FileUtils.IsExcelFile(srcFilePath))
            {
                var csvFilePath = Path.GetTempFileName() + ".csv";


                FileUtils.ConvertExcelFileToCsv(srcFilePath, csvFilePath,
                    "",
                    null,
                    null);

                srcFilePath = csvFilePath;
            }

            var contents = "";
            // read each line and insert
            using var inputFile = new StreamReader(srcFilePath);
            string line;
            var rowNo = 0;
            while ((line = inputFile.ReadLine()!) != null)
            {
                rowNo++;
                contents += line;

                if (firstNLinesOnly != 0 && rowNo >= firstNLinesOnly)
                {
                    return contents;
                }
            }

            return contents;
        }

        #endregion

        #region FileConversions

        public static void ConvertAllExcelFilesToCsv(string sourceDir, bool subDirsAlso, string destDir,
            DbConnection dbConn, FileOperationLogParams fileLogParams, SingleFileCallback singleFileCallback,
            OnErrorCallback onErrorCallback)
        {
            IterateDirectory(
                sourceDir, DirectoryIterateType.Files, subDirsAlso, "*.xls*"
                , /*fileCallBack*/ (foundFile, dummy, dummy2) =>
                {

                } /*end fileCallBack*/
                , /*onErrorCallback*/ // at end 
                (directory, file, ex) =>
                {
                    DbUtils.LogError(directory, file, ex, fileLogParams);
                } /*end onErrorCallback*/
            ); //FileUtils.IterateDirectory;
        } //end method

        public static Boolean IsExcelFile(string srcFilePath)
        {
            var fileExt = Path.GetExtension(srcFilePath);
            if (fileExt == ".xlsx" || fileExt == ".xls")
            {
                return true;
            }

            return false;
        }
        public static void ConvertExcelFileToCsv(string sourceFilePath, string destFilePath, string password,
            SingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            try
            {
                using (var stream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var config = new ExcelReaderConfiguration { Password = password };
                    IExcelDataReader reader = null;
                    if (sourceFilePath.EndsWith(".xls"))
                    {
                        reader = ExcelReaderFactory.CreateBinaryReader(stream);
                    }
                    else if (sourceFilePath.EndsWith(".xlsx"))
                    {
                        reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                    }

                    if (reader == null)
                    {
                        return;
                    }

                    var ds = reader.AsDataSet(new ExcelDataSetConfiguration
                    {
                        ConfigureDataTable = tableReader => new ExcelDataTableConfiguration
                        {
                            UseHeaderRow = false,
                        },
                    });

                    var csvContent = string.Empty;
                    var rowNo = 0;
                    while (rowNo < ds.Tables[0].Rows.Count)
                    {
                        List<string> arr = new List<string>();
                        for (var i = 0; i < ds.Tables[0].Columns.Count; i++)
                        {
                            var fieldValue = ds.Tables[0].Rows[rowNo][i];
                            string strFieldValue;
                            if (fieldValue.GetType().Name == "DateTime")
                            {
                                strFieldValue = Utils.ToIsoDateTimeString((DateTime)fieldValue);
                            }
                            else
                            {
                                strFieldValue = fieldValue.ToString() ?? "";
                            }

                            arr.Add(strFieldValue);
                        }

                        rowNo++;
                        var line = string.Join(",", arr) + "\n";
                        csvContent += line;
                    }

                    //
                    EnsurePathExists(destFilePath);
                    var csv = new StreamWriter(destFilePath, false);
                    csv.Write(csvContent);
                    csv.Close();
                }

                // callback for complete
                if (fileCallback != null)
                {
                    fileCallback(sourceFilePath, destFilePath, "");
                }
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                {
                    onErrorCallback(sourceFilePath, sourceFilePath, ex);
                }
                else
                {
                    throw;
                }
            }
        }

        public static void ConvertExcelFileToCsv(string sourceFilePath, string destFilePath, string[] passwords,
            SingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            Boolean success = false;

            // add empty password if missing
            List<string> passwords2 = new List<string> { };
            if (!passwords.Contains(""))
            {
                passwords2.Add("");
            }
            passwords2.AddRange(passwords);

            // try each password
            foreach (var password in passwords2)
            {
                try
                {
                    ConvertExcelFileToCsv(sourceFilePath, destFilePath, password, fileCallback, null);
                    success = true;
                    return;
                }
                catch (InvalidPasswordException ex)
                {
                    continue;
                }
                catch (Exception ex)
                {
                    if (onErrorCallback != null)
                    {
                        onErrorCallback(sourceFilePath, sourceFilePath, ex);
                        return;
                    }
                    else
                    {
                        throw ;
                    }
                }
            }

            if (!success)
            {
                // throw error
                var ex = new InvalidPasswordException($"Invalid password: Could Not Open Excel File : {sourceFilePath}");
                if (onErrorCallback != null)
                {
                    onErrorCallback(sourceFilePath, sourceFilePath, ex);
                }
                else
                {
                    throw ex;
                }
            }
        }

        #endregion
    }

}
