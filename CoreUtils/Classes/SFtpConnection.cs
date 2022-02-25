using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace CoreUtils.Classes
{
    public delegate void FtpSingleFileCallback(string file1, string file2, string fileContents);
    public delegate void FtpFileonErrorCallback(object arg1, object arg2, Exception ex);


    [Guid("E321976A-45C3-4BC5-BC0B-E4743213CABC")]
    [ComVisible(true)]
    public class SFtpConnection
    {
        private string _connectionString;
        private object _activeConnection;
        private string _host;
        private int _port;
        private string _userName;
        private string _userPassword;
        private string _privateKeyPath;
        private string _privateKeyPassPhrase;
        private string _rootPath;
        private ConnectionInfo _connectionInfo;

        // ReSharper disable once InconsistentNaming
        private SftpClient client;

        public SFtpConnection(string host, int port, string userName, string userPassword, string privateKeyPath = null, string privateKeyPassPhrase = null, string rootPath = "/")
        {
            this._host = host;
            this._port = port;
            this._userName = userName;
            this._userPassword = userPassword;
            this._privateKeyPath = privateKeyPath;
            this._privateKeyPassPhrase = privateKeyPassPhrase;
            this._rootPath = rootPath;

            PrivateKeyFile keyFile;
            PrivateKeyFile[] keyFiles = null;

            if (!Utils.IsBlank(privateKeyPath))
            {
                keyFile = new PrivateKeyFile(_privateKeyPath, _privateKeyPassPhrase);
                keyFiles = new[] { keyFile };
            }

            if (Utils.IsBlank(_privateKeyPath) || keyFiles?.Length == 0)
            {
                this._connectionInfo = new ConnectionInfo(_host, _port, userName,
                    new PasswordAuthenticationMethod(_userName, _userPassword)
                );
            }
            else
            {
                this._connectionInfo = new ConnectionInfo(_host, _port, userName,
                    new PasswordAuthenticationMethod(_userName, _userPassword),
                    new PrivateKeyAuthenticationMethod("rsa.key", keyFiles));
            }
        }

        private SftpClient EnsureConnection()
        {
            if (Utils.IsBlank(_host) || _port <= 0)
            {
                var message =
                    $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : {_host}:{_port} is invalid";
                throw new Exception(message);
            }

            if (client == null || !client.IsConnected)
            {
                var client1 = new SftpClient(this._connectionInfo);
                client1.Connect();
                //
                //
                this.client = client1;
            }


            //
            return this.client;
        }

        #region IOOperations

        public void IterateDirectory(string[] sourceDirectories, DirectoryIterateType iterateType,
            bool subDirsAlso, string[] fileMasks,
            FtpSingleFileCallback fileCallback,
            FtpFileonErrorCallback onErrorCallback)
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
            string[] fileMasks, FtpSingleFileCallback fileCallback, FtpFileonErrorCallback onErrorCallback)
        {
            if (fileMasks == null || fileMasks.Length == 0) fileMasks = new[] { "*.*" };

            // iterate for each fileMask
            foreach (var fileMask in fileMasks)
                IterateDirectory(directory, iterateType, subDirsAlso, fileMask, fileCallback,
                    onErrorCallback);

        }

        public void IterateDirectory(string directory, DirectoryIterateType iterateType, bool subDirsAlso,
            string fileMask, FtpSingleFileCallback fileCallback, FtpFileonErrorCallback onErrorCallback)
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
            this.EnsureConnection();
            //
            if (client.Exists(directory) == false)
            {
                client.CreateDirectory(directory);
            }
            // get all files in dir
            IEnumerable<SftpFile> ftpFiles = client.ListDirectory(directory);

            // ReSharper disable once PossibleMultipleEnumeration
            foreach (SftpFile ftpFile in ftpFiles)
            {
                // isDirectory?
                if (ftpFile.IsDirectory)
                {
                    if (iterateType == DirectoryIterateType.Directories && Utils.TextMatchesPattern(ftpFile.Name, fileMask))
                    {
                        fileCallback(ftpFile.FullName, null, null);
                    }

                    // iterate subDir if asked
                    if (subDirsAlso)
                    {
                        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                        this.IterateDirectory(ftpFile.FullName, iterateType, subDirsAlso, fileMask, fileCallback, null);
                    }
                }
                else if (ftpFile.IsSymbolicLink)
                {
                    // Console.WriteLine("Ignoring symbolic link {0}", ftpFile.FullName);
                }
                else if (ftpFile.IsRegularFile && ftpFile.Name != "." && ftpFile.Name != ".." && Utils.TextMatchesPattern(ftpFile.Name, fileMask))
                {
                    fileCallback(ftpFile.FullName, null, null);
                }
            }

        }

        public void CopyOrMoveFiles(FtpFileOperation fileOperation, string[] sourceDirectories, bool subDirsAlso, string[] fileMasks,
            string destDirectory, string destFileName, string destFileExt, FtpSingleFileCallback fileCallback,
            FtpFileonErrorCallback onErrorCallback)
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

        public void CopyOrMoveFiles(FtpFileOperation fileOperation, string sourceDirectory, bool subDirsAlso, string fileMask, string destDirectory,
            string destFileName, string destFileExt, FtpSingleFileCallback fileCallback,
            FtpFileonErrorCallback onErrorCallback)
        {
            DoMultipleFilesOperation(fileOperation, sourceDirectory, subDirsAlso, fileMask, destDirectory,
                destFileName, destFileExt, fileCallback, onErrorCallback);
        }

        public void CopyOrMoveFile(FtpFileOperation fileOperation, string sourceFilePath, string destFilePath, FtpSingleFileCallback fileCallback)
        {
            DoSingleFtpFileOperation(fileOperation, sourceFilePath, destFilePath, fileCallback);
        }


        public void DeleteFiles(string[] sourceDirectories, bool subDirsAlso, string[] fileMasks,
            FtpSingleFileCallback fileCallback, FtpFileonErrorCallback onErrorCallback)
        {
            if (sourceDirectories == null || sourceDirectories.Length == 0)
            {
                var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : sourceDirectories should be set";
                throw new Exception(message);
            }

            // iterate for each fileMask
            foreach (var sourceDirectory in sourceDirectories)
                DoMultipleFilesOperation(FtpFileOperation.DeleteRemoteFileIfExists, sourceDirectory, subDirsAlso, fileMasks, "", "", "",
                    fileCallback, onErrorCallback);

        }

        public void DeleteFiles(string sourceDirectory, bool subDirsAlso, string[] fileMasks,
            FtpSingleFileCallback fileCallback, FtpFileonErrorCallback onErrorCallback)
        {
            DoMultipleFilesOperation(FtpFileOperation.DeleteRemoteFileIfExists, sourceDirectory, subDirsAlso, fileMasks, "", "", "",
                fileCallback, onErrorCallback);
        }

        public void DeleteFiles(string sourceDirectory, bool subDirsAlso, string fileMask,
            FtpSingleFileCallback fileCallback, FtpFileonErrorCallback onErrorCallback)
        {
            DoMultipleFilesOperation(FtpFileOperation.DeleteRemoteFileIfExists, sourceDirectory, subDirsAlso, fileMask, "", "", "",
                fileCallback, onErrorCallback);
        }

        public void DeleteFileIfExists(string sourceFilePath, FtpSingleFileCallback fileCallback)
        {
            DoSingleFtpFileOperation(FtpFileOperation.DeleteRemoteFileIfExists, sourceFilePath, "", fileCallback);
        }

        public void ReadFile(string sourceFilePath, FtpSingleFileCallback fileCallback)
        {
            DoSingleFtpFileOperation(FtpFileOperation.ReadRemoteFile, sourceFilePath, "", fileCallback);
        }

        private void DoMultipleFilesOperation(FtpFileOperation fileOperation, string sourceDirectory,
            bool subDirsAlso, string[] fileMasks, string destDirectory, string destFileName, string destFileExt,
            FtpSingleFileCallback fileCallback, FtpFileonErrorCallback onErrorCallback)
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
            FtpSingleFileCallback fileCallback, FtpFileonErrorCallback onErrorCallback)
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
                    DoSingleFtpFileOperation(fileOperation, foundFile, destFullFilePath, fileCallback);
                },
                // at end 
                (arg1, arg2, ex) =>
                {
                    // callback for complete
                    if (onErrorCallback != null)
                    {
                        onErrorCallback(sourceDirectory, fileMask, ex);
                    }
                    else
                    {
                        throw ex;
                    }
                }
            );
        }

        public SftpFile GetRemoteFileInfo(string sourceFilePath)
        {
            //
            this.EnsureConnection();
            //

            return this.client.Get(sourceFilePath);
        }

        public void DoSingleFtpFileOperation(FtpFileOperation fileOperation, string sourceFilePath,
            string destFilePath, FtpSingleFileCallback fileCallback)
        {
            //
            this.EnsureConnection();
            //

            var fileContents = "";

            // if we are uploading, sourceFile is a local path
            if (fileOperation == FtpFileOperation.ReadRemoteFile)
            {
                var tempFilePath = Path.GetTempFileName();
                using var stream = File.Open(tempFilePath, FileMode.Open);
                //
                this.client.DownloadFile(sourceFilePath, stream, null);
                fileContents = File.ReadAllText(tempFilePath);
                //
                System.IO.File.Delete(tempFilePath);
            }

            else if (fileOperation == FtpFileOperation.DeleteRemoteFileIfExists)
            {
                this.client.DeleteFile(destFilePath);
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
                this.client.UploadFile(stream, destFilePath, null);

                if (fileOperation == FtpFileOperation.UploadAndDelete)
                {
                    srcFileInfo.Delete();
                }
            }
            else if (fileOperation == FtpFileOperation.Download || fileOperation == FtpFileOperation.DownloadAndDelete)
            {
                FileInfo fileInfo = new FileInfo(destFilePath);
                if (fileInfo.Exists)
                {
                    fileInfo.Delete();
                }
                using var stream = File.Open(destFilePath, FileMode.OpenOrCreate);
                this.client.DownloadFile(sourceFilePath, stream, null);

                if (fileOperation == FtpFileOperation.DownloadAndDelete)
                {
                    this.client.DeleteFile(sourceFilePath);
                }
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

    }
}