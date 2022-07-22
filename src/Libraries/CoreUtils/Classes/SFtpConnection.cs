using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace CoreUtils.Classes
{
    public delegate void FtpSingleFileCallback(string file1, string file2, SftpFile fileInfo, string fileContents);

    public class SFtpConnection
    {
        // ReSharper disable once InconsistentNaming
        private SftpClient client;

        public SFtpConnection(string host, int port, string userName, string userPassword, string privateKeyPath = null,
            string privateKeyPassPhrase = null, string rootPath = "/")
        {
            this.Host = host;
            this.Port = port;
            this.UserName = userName;
            this.UserPassword = userPassword;
            this.PrivateKeyPath = privateKeyPath;
            this.PrivateKeyPassPhrase = privateKeyPassPhrase;
            this.RootPath = rootPath;

            PrivateKeyFile[] keyFiles = null;

            if (!Utils.IsBlank(privateKeyPath))
            {
                var keyFile = new PrivateKeyFile(this.PrivateKeyPath, this.PrivateKeyPassPhrase);
                keyFiles = new[] { keyFile };
            }

            if (Utils.IsBlank(this.PrivateKeyPath) || keyFiles?.Length == 0)
            {
                this.ConnInfo = new ConnectionInfo(this.Host, this.Port, userName,
                    new PasswordAuthenticationMethod(this.UserName, this.UserPassword)
                );
            }
            else
            {
                this.ConnInfo = new ConnectionInfo(this.Host, this.Port, userName,
                    new PasswordAuthenticationMethod(this.UserName, this.UserPassword),
                    new PrivateKeyAuthenticationMethod("rsa.key", keyFiles));
            }
        }

        public object ActiveConnection { get; }
        public string ConnectionString { get; }
        public ConnectionInfo ConnInfo { get; }
        public string Host { get; }
        public int Port { get; }
        public string PrivateKeyPassPhrase { get; }
        public string PrivateKeyPath { get; }
        public string RootPath { get; }
        public string UserName { get; }
        public string UserPassword { get; }

        private SftpClient EnsureConnection()
        {
            if (Utils.IsBlank(this.Host) || this.Port <= 0)
            {
                var message =
                    $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : {this.Host}:{this.Port} is invalid";
                throw new Exception(message);
            }

            if (this.client == null || !this.client.IsConnected)
            {
                var client1 = new SftpClient(this.ConnInfo);
                client1.Connect();
                //
                //
                this.client = client1;
            }

            //
            return this.client;
        }

        #region IOOperations

        public void CopyOrMoveFile(FtpFileOperation fileOperation, string sourceFilePath, string destFilePath, SftpFile file,
            FtpSingleFileCallback fileCallback,
            OnErrorCallback onErrorCallback)
        {
            this.DoSingleFtpFileOperation(fileOperation, sourceFilePath, destFilePath, file, fileCallback, onErrorCallback);
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
            {
                this.DoMultipleFilesOperation(fileOperation, sourceDirectory, subDirsAlso, fileMasks, destDirectory,
                    destFileName, destFileExt, fileCallback, onErrorCallback);
            }
        }

        public void CopyOrMoveFiles(FtpFileOperation fileOperation, string sourceDirectory, bool subDirsAlso,
            string fileMask, string destDirectory,
            string destFileName, string destFileExt, FtpSingleFileCallback fileCallback,
            OnErrorCallback onErrorCallback)
        {
            this.DoMultipleFilesOperation(fileOperation, sourceDirectory, subDirsAlso, fileMask, destDirectory,
                destFileName, destFileExt, fileCallback, onErrorCallback);
        }

        public void DeleteFileIfExists(string sourceFilePath, SftpFile file, FtpSingleFileCallback fileCallback,
            OnErrorCallback onErrorCallback)
        {
            this.DoSingleFtpFileOperation(FtpFileOperation.DeleteRemoteFileIfExists, sourceFilePath, "", file, fileCallback,
                onErrorCallback);
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
                {
                    this.DoMultipleFilesOperation(FtpFileOperation.DeleteRemoteFileIfExists, sourceDirectory,
                        subDirsAlso,
                        fileMasks, "", "", "",
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

        public void DeleteFiles(string sourceDirectory, bool subDirsAlso, string[] fileMasks,
            FtpSingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            this.DoMultipleFilesOperation(FtpFileOperation.DeleteRemoteFileIfExists, sourceDirectory, subDirsAlso,
                fileMasks,
                "", "", "",
                fileCallback, onErrorCallback);
        }

        public void DeleteFiles(string sourceDirectory, bool subDirsAlso, string fileMask,
            FtpSingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            this.DoMultipleFilesOperation(FtpFileOperation.DeleteRemoteFileIfExists, sourceDirectory, subDirsAlso,
                fileMask,
                "", "", "",
                fileCallback, onErrorCallback);
        }

        public void DoSingleFtpFileOperation(FtpFileOperation fileOperation, string sourceFilePath, string destFilePath, SftpFile file, FtpSingleFileCallback fileCallback,
            OnErrorCallback onErrorCallback)
        {
            try
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
                    this.client.DownloadFile(sourceFilePath, stream);
                    fileContents = File.ReadAllText(tempFilePath);
                    //
                    File.Delete(tempFilePath);
                    //
                }
                else if (fileOperation == FtpFileOperation.DeleteRemoteFileIfExists)
                {
                    this.client.DeleteFile(sourceFilePath);
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
                    this.client.UploadFile(stream, destFilePath);

                    if (fileOperation == FtpFileOperation.UploadAndDelete)
                    {
                        srcFileInfo.Delete();
                    }
                }
                else if (fileOperation == FtpFileOperation.Download ||
                         fileOperation == FtpFileOperation.DownloadAndDelete)
                {
                    var fileInfo = new FileInfo(destFilePath);
                    if (fileInfo.Exists)
                    {
                        fileInfo.Delete();
                    }

                    using var stream = File.Open(destFilePath, FileMode.OpenOrCreate);
                    this.client.DownloadFile(sourceFilePath, stream);

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
                if (fileCallback != null)
                {
                    fileCallback(sourceFilePath, destFilePath, file, fileContents);
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

        public SftpFile GetRemoteFileInfo(string sourceFilePath)
        {
            //
            this.EnsureConnection();
            //

            return this.client.Get(sourceFilePath);
        }

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
            {
                this.IterateDirectory(sourceDirectory, iterateType, subDirsAlso, fileMasks, fileCallback,
                    onErrorCallback);
            }
        }

        public void IterateDirectory(string directory, DirectoryIterateType iterateType, bool subDirsAlso,
            string[] fileMasks, FtpSingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            if (fileMasks == null || fileMasks.Length == 0)
            {
                fileMasks = new[] { "*.*" };
            }

            // iterate for each fileMask
            foreach (var fileMask in fileMasks)
            {
                this.IterateDirectory(directory, iterateType, subDirsAlso, fileMask, fileCallback,
                    onErrorCallback);
            }
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
                //  string message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : onErrorCallback should be set";
                //  throw new Exception(message);
                //}

                // check dir exists
                this.EnsureConnection();
                //
                if (this.client.Exists(directory) == false)
                {
                    this.client.CreateDirectory(directory);
                }

                this.client.ChangeDirectory(directory);

                // get all files in dir
                IEnumerable<SftpFile> ftpFiles = this.client.ListDirectory(directory);

                // ReSharper disable once PossibleMultipleEnumeration
                foreach (var ftpFile in ftpFiles)
                {
                    try
                    {
                        // isDirectory?
                        if (ftpFile.IsDirectory)
                        {
                            if (iterateType == DirectoryIterateType.Directories &&
                                Utils.TextMatchesPattern(ftpFile.Name, fileMask))
                            {
                                fileCallback(ftpFile.FullName, null, ftpFile, "");
                            }

                            // iterate subDir if asked
                            if (subDirsAlso)
                            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                            {
                                this.IterateDirectory(ftpFile.FullName, iterateType, subDirsAlso, fileMask,
                                    fileCallback,
                                    null);
                            }
                        }
                        else if (ftpFile.IsSymbolicLink)
                        {
                            // Console.WriteLine("Ignoring symbolic link {0}", ftpFile.FullName);
                        }
                        else if (ftpFile.IsRegularFile && !FileUtils.IgnoreFile(ftpFile.Name) &&
                                 Utils.TextMatchesPattern(ftpFile.Name, fileMask))
                        {
                            fileCallback(ftpFile.FullName, null, ftpFile, "");
                        }
                    }
                    catch (Exception ex)
                    {
                        // callback for complete
                        if (onErrorCallback != null)
                        {
                            onErrorCallback(directory, fileMask, ex);
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
                    onErrorCallback(directory, fileMask, ex);
                }
                else
                {
                    throw;
                }
            }
        }

        public void ReadFile(string sourceFilePath, SftpFile file, FtpSingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            this.DoSingleFtpFileOperation(FtpFileOperation.ReadRemoteFile, sourceFilePath, "", file, fileCallback,
                onErrorCallback);
        }

        private void DoMultipleFilesOperation(FtpFileOperation fileOperation, string sourceDirectory,
            bool subDirsAlso, string[] fileMasks, string destDirectory, string destFileName, string destFileExt,
            FtpSingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            if (fileMasks == null || fileMasks.Length == 0)
            {
                fileMasks = new[] { "*.*" };
            }

            // iterate for each fileMask
            foreach (var fileMask in fileMasks)
            {
                this.DoMultipleFilesOperation(fileOperation, sourceDirectory, subDirsAlso, fileMask, destDirectory,
                    destFileName, destFileExt, fileCallback,
                    onErrorCallback);
            }
        }

        private void DoMultipleFilesOperation(FtpFileOperation fileOperation, string sourceDirectory,
            bool subDirsAlso, string fileMask, string destDirectory, string destFileName, string destFileExt,
            FtpSingleFileCallback fileCallback, OnErrorCallback onErrorCallback)
        {
            this.IterateDirectory(sourceDirectory, DirectoryIterateType.Files, subDirsAlso, fileMask,
                // handle each found file
                (foundFile, dummy, file, fileContents) =>
                {
                    //
                    var destFileNameOnly =
                        Path.GetFileNameWithoutExtension(Utils.IsBlank(destFileName) ? foundFile : destFileName);

                    var destFileExtOnly = Utils.IsBlank(destFileExt) ? Path.GetExtension(foundFile) : destFileExt;
                    var destFullFilePath = $"{destDirectory}/{destFileNameOnly}{destFileExtOnly}";
                    //
                    this.DoSingleFtpFileOperation(fileOperation, foundFile, destFullFilePath, file, fileCallback,
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

        #endregion IOOperations
    }
}