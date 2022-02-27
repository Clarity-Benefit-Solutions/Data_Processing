using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace CoreUtils.Classes
{
    public delegate void FtpSingleFileCallback(string file1, string file2, string fileContents);


    [Guid("E321976A-45C3-4BC5-BC0B-E4743213CABC")]
    [ComVisible(true)]
    public class SFtpConnection
    {
        // ReSharper disable once InconsistentNaming
        private SftpClient client;

        public SFtpConnection(string host, int port, string userName, string userPassword, string privateKeyPath = null,
            string privateKeyPassPhrase = null, string rootPath = "/")
        {
            Host = host;
            Port = port;
            UserName = userName;
            UserPassword = userPassword;
            PrivateKeyPath = privateKeyPath;
            PrivateKeyPassPhrase = privateKeyPassPhrase;
            RootPath = rootPath;

            PrivateKeyFile[] keyFiles = null;

            if (!Utils.IsBlank(privateKeyPath))
            {
                var keyFile = new PrivateKeyFile(PrivateKeyPath, PrivateKeyPassPhrase);
                keyFiles = new[] { keyFile };
            }

            if (Utils.IsBlank(PrivateKeyPath) || keyFiles?.Length == 0)
                ConnInfo = new ConnectionInfo(Host, Port, userName,
                    new PasswordAuthenticationMethod(UserName, UserPassword)
                );
            else
                ConnInfo = new ConnectionInfo(Host, Port, userName,
                    new PasswordAuthenticationMethod(UserName, UserPassword),
                    new PrivateKeyAuthenticationMethod("rsa.key", keyFiles));
        }

        public string ConnectionString { get; }
        public object ActiveConnection { get; }
        public string Host { get; }
        public int Port { get; }
        public string UserName { get; }
        public string UserPassword { get; }
        public string PrivateKeyPath { get; }
        public string PrivateKeyPassPhrase { get; }
        public string RootPath { get; }
        public ConnectionInfo ConnInfo { get; }

        private SftpClient EnsureConnection()
        {
            if (Utils.IsBlank(Host) || Port <= 0)
            {
                var message =
                    $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : {Host}:{Port} is invalid";
                throw new Exception(message);
            }

            if (client == null || !client.IsConnected)
            {
                var client1 = new SftpClient(ConnInfo);
                client1.Connect();
                //
                //
                client = client1;
            }


            //
            return client;
        }

        #region IOOperations

        public void IterateDirectory(string[] sourceDirectories, DirectoryIterateType iterateType,
            bool subDirsAlso, string[] fileMasks,
            FtpSingleFileCallback fileCallback,
            OnErrorCallback onErrorCallback)
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

        public void IterateDirectory(string directory, DirectoryIterateType iterateType, bool subDirsAlso,
            string[] fileMasks, FtpSingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            if (fileMasks == null || fileMasks.Length == 0) fileMasks = new[] { "*.*" };

            // iterate for each fileMask
            foreach (var fileMask in fileMasks)
                IterateDirectory(directory, iterateType, subDirsAlso, fileMask, fileCallback,
                    onErrorCallback);
        }

        public void IterateDirectory(string directory, DirectoryIterateType iterateType, bool subDirsAlso,
            string fileMask, FtpSingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
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
                //if (onErrorCallback == null)
                //{
                //    string message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : onErrorCallback should be set";
                //    throw new Exception(message);
                //}

                // check dir exists
                EnsureConnection();
                //
                if (client.Exists(directory) == false) client.CreateDirectory(directory);
                // get all files in dir
                var ftpFiles = client.ListDirectory(directory);

                // ReSharper disable once PossibleMultipleEnumeration
                foreach (var ftpFile in ftpFiles)
                    // isDirectory?
                    if (ftpFile.IsDirectory)
                    {
                        if (iterateType == DirectoryIterateType.Directories &&
                            Utils.TextMatchesPattern(ftpFile.Name, fileMask))
                            fileCallback(ftpFile.FullName, null, null);

                        // iterate subDir if asked
                        if (subDirsAlso)
                            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                            IterateDirectory(ftpFile.FullName, iterateType, subDirsAlso, fileMask, fileCallback, null);
                    }
                    else if (ftpFile.IsSymbolicLink)
                    {
                        // Console.WriteLine("Ignoring symbolic link {0}", ftpFile.FullName);
                    }
                    else if (ftpFile.IsRegularFile && ftpFile.Name != "." && ftpFile.Name != ".." &&
                             Utils.TextMatchesPattern(ftpFile.Name, fileMask))
                    {
                        if (ftpFile.Name.StartsWith("."))
                        {
                            continue;
                        }
                        else
                        {
                            fileCallback(ftpFile.FullName, null, null);
                        }
                    }
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

        public void CopyOrMoveFiles(FtpFileOperation fileOperation, string[] sourceDirectories, bool subDirsAlso,
            string[] fileMasks,
            string destDirectory, string destFileName, string destFileExt, FtpSingleFileCallback fileCallback,
            OnErrorCallback onErrorCallback)
        {
            if (sourceDirectories == null || sourceDirectories.Length == 0)
            {
                var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : sourceDirectories should be set";
                throw new Exception(message);
            }

            // iterate for each fileMask
            foreach (var sourceDirectory in sourceDirectories)
                DoMultipleFilesOperation(fileOperation, sourceDirectory, subDirsAlso, fileMasks, destDirectory,
                    destFileName, destFileExt, fileCallback, onErrorCallback);
        }

        public void CopyOrMoveFiles(FtpFileOperation fileOperation, string sourceDirectory, bool subDirsAlso,
            string fileMask, string destDirectory,
            string destFileName, string destFileExt, FtpSingleFileCallback fileCallback,
            OnErrorCallback onErrorCallback)
        {
            DoMultipleFilesOperation(fileOperation, sourceDirectory, subDirsAlso, fileMask, destDirectory,
                destFileName, destFileExt, fileCallback, onErrorCallback);
        }

        public void CopyOrMoveFile(FtpFileOperation fileOperation, string sourceFilePath, string destFilePath,
            FtpSingleFileCallback fileCallback,
            OnErrorCallback onErrorCallback)
        {
            DoSingleFtpFileOperation(fileOperation, sourceFilePath, destFilePath, fileCallback, onErrorCallback);
        }


        public void DeleteFiles(string[] sourceDirectories, bool subDirsAlso, string[] fileMasks,
            FtpSingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
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
                    DoMultipleFilesOperation(FtpFileOperation.DeleteRemoteFileIfExists, sourceDirectory, subDirsAlso,
                        fileMasks, "", "", "",
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

        public void DeleteFiles(string sourceDirectory, bool subDirsAlso, string[] fileMasks,
            FtpSingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            DoMultipleFilesOperation(FtpFileOperation.DeleteRemoteFileIfExists, sourceDirectory, subDirsAlso, fileMasks,
                "", "", "",
                fileCallback, onErrorCallback);
        }

        public void DeleteFiles(string sourceDirectory, bool subDirsAlso, string fileMask,
            FtpSingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            DoMultipleFilesOperation(FtpFileOperation.DeleteRemoteFileIfExists, sourceDirectory, subDirsAlso, fileMask,
                "", "", "",
                fileCallback, onErrorCallback);
        }

        public void DeleteFileIfExists(string sourceFilePath, FtpSingleFileCallback fileCallback,
            OnErrorCallback onErrorCallback)
        {
            DoSingleFtpFileOperation(FtpFileOperation.DeleteRemoteFileIfExists, sourceFilePath, "", fileCallback,
                onErrorCallback);
        }

        public void ReadFile(string sourceFilePath, FtpSingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            DoSingleFtpFileOperation(FtpFileOperation.ReadRemoteFile, sourceFilePath, "", fileCallback,
                onErrorCallback);
        }

        private void DoMultipleFilesOperation(FtpFileOperation fileOperation, string sourceDirectory,
            bool subDirsAlso, string[] fileMasks, string destDirectory, string destFileName, string destFileExt,
            FtpSingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            if (fileMasks == null || fileMasks.Length == 0) fileMasks = new[] { "*.*" };

            // iterate for each fileMask
            foreach (var fileMask in fileMasks)
                DoMultipleFilesOperation(fileOperation, sourceDirectory, subDirsAlso, fileMask, destDirectory,
                    destFileName, destFileExt, fileCallback,
                    onErrorCallback);
        }

        private void DoMultipleFilesOperation(FtpFileOperation fileOperation, string sourceDirectory,
            bool subDirsAlso, string fileMask, string destDirectory, string destFileName, string destFileExt,
            FtpSingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            IterateDirectory(sourceDirectory, DirectoryIterateType.Files, subDirsAlso, fileMask,
                // handle each found file
                (foundFile, dummy, dummy2) =>
                {
                    // 
                    var destFileNameOnly =
                        Path.GetFileNameWithoutExtension(Utils.IsBlank(destFileName) ? foundFile : destFileName);

                    var destFileExtOnly = Utils.IsBlank(destFileExt) ? Path.GetExtension(foundFile) : destFileExt;
                    var destFullFilePath = $"{destDirectory}/{destFileNameOnly}{destFileExtOnly}";
                    //
                    DoSingleFtpFileOperation(fileOperation, foundFile, destFullFilePath, fileCallback, onErrorCallback);
                },
                // at end 
                (arg1, arg2, ex) =>
                {
                    if (onErrorCallback == null) throw ex;
                    onErrorCallback(arg1, arg2, ex);
                }
            );
        }

        public SftpFile GetRemoteFileInfo(string sourceFilePath)
        {
            //
            EnsureConnection();
            //

            return client.Get(sourceFilePath);
        }

        public void DoSingleFtpFileOperation(FtpFileOperation fileOperation, string sourceFilePath,
            string destFilePath, FtpSingleFileCallback fileCallback,
            OnErrorCallback onErrorCallback)
        {
            try
            {
                //
                EnsureConnection();
                //

                var fileContents = "";

                // if we are uploading, sourceFile is a local path
                if (fileOperation == FtpFileOperation.ReadRemoteFile)
                {
                    var tempFilePath = Path.GetTempFileName();
                    using var stream = File.Open(tempFilePath, FileMode.Open);
                    //
                    client.DownloadFile(sourceFilePath, stream, null);
                    fileContents = File.ReadAllText(tempFilePath);
                    //
                    File.Delete(tempFilePath);
                }

                else if (fileOperation == FtpFileOperation.DeleteRemoteFileIfExists)
                {
                    client.DeleteFile(destFilePath);
                }
                else if (fileOperation == FtpFileOperation.Upload || fileOperation == FtpFileOperation.UploadAndDelete)
                {
                    var srcFileInfo = new FileInfo(sourceFilePath);
                    if (!srcFileInfo.Exists)
                    {
                        var message =
                            $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : Source File: {sourceFilePath} does not exist";
                        throw new Exception(message);
                    }

                    using var stream = File.Open(sourceFilePath, FileMode.Open);
                    client.UploadFile(stream, destFilePath, null);

                    if (fileOperation == FtpFileOperation.UploadAndDelete) srcFileInfo.Delete();
                }
                else if (fileOperation == FtpFileOperation.Download ||
                         fileOperation == FtpFileOperation.DownloadAndDelete)
                {
                    var fileInfo = new FileInfo(destFilePath);
                    if (fileInfo.Exists) fileInfo.Delete();
                    using var stream = File.Open(destFilePath, FileMode.OpenOrCreate);
                    client.DownloadFile(sourceFilePath, stream, null);

                    if (fileOperation == FtpFileOperation.DownloadAndDelete) client.DeleteFile(sourceFilePath);
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

        #endregion
    }
}