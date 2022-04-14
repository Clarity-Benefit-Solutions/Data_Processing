using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Web;
using System.Web.Hosting;
using CoreUtils;
using CoreUtils.Classes;
using DataProcessing.DataModels.DataProcessing;


using MySqlConnector;
using StackExchange.Profiling;

// ReSharper disable All

namespace DataProcessing
{
    public class Vars
    {
        private static string _Environment = null;
        public static string Environment
        {
            get
            {
                return _Environment ?? "TEST";
            }
            set
            {
                if (_Environment != null)
                {
                    throw new Exception($"Environment can be set only once. It is currently {_Environment}");
                }

                _Environment = value;
            }
        }
        private static Boolean _UseVPNToConnectToPortal = false;
        public static Boolean UseVPNToConnectToPortal
        {
            get
            {
                return _UseVPNToConnectToPortal;
            }
            set
            {
                _UseVPNToConnectToPortal = value;
            }
        }

        #region DBDataProcessing

        public static Boolean IsRunningAsWebApp()
        {
            return HostingEnvironment.IsHosted;
        }

        public static string GetProcessBaseDir()
        {
            if (IsRunningAsWebApp())
            {
                return FileUtils.FixPath(AppDomain.CurrentDomain.BaseDirectory);
            }
            else
            {
                //todo: modify for PROD
                var processModule = Process.GetCurrentProcess().MainModule;
                if (processModule != null)
                {
                    var exePath = processModule.FileName;
                    var directoryPath = $"{Path.GetDirectoryName(exePath)}/..";
                    return FileUtils.FixPath(directoryPath);
                }
            }

            return "";
        }



        public string ConnStrNameDataProcessing
        {
            get
            {

                return "Data_ProcessingEntities";

            }
        }

        private Data_ProcessingEntities _dbCtxDataProcessingDefault;

        public Data_ProcessingEntities DbCtxDataProcessingDefault
        {
            get
            {
                if (_dbCtxDataProcessingDefault == null)
                    _dbCtxDataProcessingDefault = new Data_ProcessingEntities("name=" + ConnStrNameDataProcessing);
                return _dbCtxDataProcessingDefault;
            }
        }

        public Data_ProcessingEntities dbCtxDataProcessingNew
        {
            get { return new Data_ProcessingEntities("name=" + ConnStrNameDataProcessing); }
        }

        private DbConnection _dbConnDataProcessing;

        public DbConnection dbConnDataProcessing
        {
            get
            {
                if (_dbConnDataProcessing == null)
                {
                    string connString =
                        Utils.GetProviderConnString(ConnStrNameDataProcessing);
                    //
                    _dbConnDataProcessing = new SqlConnection(connString);
                    // profiling
#if PROFILE
                    _dbConnDataProcessing = new StackExchange.Profiling.Data.ProfiledDbConnection(_dbConnDataProcessing, MiniProfiler.Current);
#endif

                    if (_dbConnDataProcessing.State != ConnectionState.Open) _dbConnDataProcessing.Open();
                }

                return _dbConnDataProcessing;
            }
        }

        #endregion

        #region DbConnPortalWc


        public string ConnStrNamePortalWc
        {
            get
            {

                if (UseVPNToConnectToPortal)
                {
                    return "PortalWcVPN";
                }
                else
                {
                    return "PortalWc";
                }

            }
        }
        public string ConnStrNameHangfire
        {
            get
            {
                return "Hangfire";
            }
        }

        //private  PortalWc _dbCtxPortalWcDefault;

        //public  PortalWc dbCtxPortalWcDefault
        //{
        //    get
        //    {
        //        if (_dbCtxPortalWcDefault == null)
        //            _dbCtxPortalWcDefault = new PortalWc(ConnStrNamePortalWc);
        //        return _dbCtxPortalWcDefault;
        //    }
        //}

        private DbConnection _dbConnPortalWc;

        public DbConnection dbConnPortalWc
        {
            get
            {
                if (_dbConnPortalWc == null)
                {
                    string connString =
                        Utils.GetProviderConnString(ConnStrNamePortalWc);
                    //
                    _dbConnPortalWc = new MySqlConnection(connString);

                    // profiling
#if PROFILE
                    _dbConnPortalWc = new StackExchange.Profiling.Data.ProfiledDbConnection(_dbConnPortalWc, MiniProfiler.Current);
#endif

                    if (_dbConnPortalWc.State != ConnectionState.Open) _dbConnPortalWc.Open();

                }

                return _dbConnPortalWc;
            }
        }

        #endregion

        #region DbLogging

        private MessageLogParams _dbMessageLogParams;

        public MessageLogParams dbMessageLogParams
        {
            get
            {
                if (_dbMessageLogParams == null || _dbMessageLogParams.DbConnection == null)
                {
                    _dbMessageLogParams = new MessageLogParams();
                    _dbMessageLogParams.With(w =>
                    {
                        w.DbConnection = dbConnDataProcessing;
                        w.Platform = "Alegeus";
                        w.LogTableName = @"dbo.[message_log]";
                        w.ModuleName = "AlegeusFileProcessing";
                    });
                }

                return _dbMessageLogParams;
            }
        }


        private FileOperationLogParams _dbFileProcessingLogParams;

        public FileOperationLogParams dbFileProcessingLogParams
        {
            get
            {
                if (_dbFileProcessingLogParams == null || _dbFileProcessingLogParams.DbConnection == null)
                {
                    _dbFileProcessingLogParams = new FileOperationLogParams();
                    _dbFileProcessingLogParams.With(w =>
                    {
                        w.DbConnection = dbConnDataProcessing;
                        w.DbMessageLogParams = dbMessageLogParams;

                        w.Platform = "Alegeus";
                        w.LogTableName = "dbo.[file_processing_log]";
                    });
                }

                return _dbFileProcessingLogParams;
            }
        }

        #endregion

        #region LocalRootPaths



        public string ConvertFilePathFromProdToCtx(string prodFilePath)
        {
            if (Utils.IsBlank(prodFilePath)) return prodFilePath;
            if (Environment == "PROD")
            {
                return prodFilePath;
            }

            // we cannot fix path as we are using windows style remote computer unc...
            /*prodFilePath = FileUtils.FixPath(prodFilePath, false);
            var matchProdFilePath = FileUtils.FixPath(prodFilePath, true);
            var fixedPath = prodFilePath;
            */
            var matchProdFilePath = prodFilePath.ToLower();
            var fixedPath = prodFilePath;
            //
            var replacements = prodToRunningCtxPathReplacePatterns;
            foreach (var entry in replacements)
            {
                //var entryKey = FileUtils.FixPath(entry.Key, true);
                var entryKey = entry.Key.ToLower();
                // compare lower to lower and standardized slashes
                if (prodFilePath.ToLower().StartsWith(entryKey))
                    fixedPath = entry.Value + prodFilePath.Substring(entry.Key.Length);
            }

            return fixedPath;
        }

        #endregion

        #region remoteFtp

        private SFtpConnection _remoteAlegeusFtpConnection;

        public SFtpConnection RemoteAlegeusFtpConnection
        {
            get
            {
                if (_remoteAlegeusFtpConnection == null)
                {
                    if (Environment == "TEST")
                    {
                        _remoteAlegeusFtpConnection = new SFtpConnection("BE015", 22, "alegeus", "3214@Clarity");
                    }
                    else if (Environment == "PROD")
                    {
                        _remoteAlegeusFtpConnection = new SFtpConnection("ftp.wealthcareadmin.com", 22, "benefledi", "VzVR4s4y");
                    }
                    else
                    {
                        throw new Exception($"Sorry, Current Environemtn {Environment} is Not valid ");
                    }
                }

                return _remoteAlegeusFtpConnection;
            }
        }

        public string remoteAlegeusFtpRootPath
        {
            get
            {
                return FileUtils.FixPath($"{GetAppSetting("alegeusRemoteFTPPath")}");
                //return "/";
            }
        }

        public string remoteCobraFtpRootPath
        {
            get
            {

                //return "/" + FileUtils.FixPath($"{GetAppSetting("cobraRemoteFTPPath")}");
                return "/";
            }
        }

        private SFtpConnection _remoteCobraFtpConnection;

        public SFtpConnection RemoteCobraFtpConnection
        {
            get
            {
                if (Environment == "TEST")
                {
                    _remoteCobraFtpConnection = new SFtpConnection("BE015", 22, "cobra", "3214@Clarity");
                }
                else if (Environment == "PROD")
                {
                    _remoteCobraFtpConnection = new SFtpConnection("xxx", 22, "xx", "xx@");
                }
                else
                {
                    throw new Exception($"Sorry, Current Environemtn {Environment} is Not valid ");
                }


                return _remoteCobraFtpConnection;
            }
        }


        #endregion

        #region FileProcessingPaths

        private static DataTable appSettings;


        public string GetAppSetting(string settingName)
        {
            if (appSettings == null)
            {
                appSettings = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConnDataProcessing,
                    "select * from dbo.app_settings where is_active = 1 order by environment, setting_name", null, null, false, true);

                if (appSettings == null)
                {
                    string message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : Could Not get appSettings";
                    throw new Exception(message);
                }
            }

            // try exact environment
            string filter = $"environment In ('{Environment}') and setting_name = '{settingName}' ";

            DataRow[] dbRows = appSettings.Select(filter);
            if (dbRows.Length == 0)
            {
                // try PROD env
                filter = $"environment In ('PROD') and setting_name = '{settingName}' ";
                dbRows = appSettings.Select(filter);

            }

            if (dbRows.Length == 0)
            {
                string message = $"The App Setting {settingName} could not be found for environments ({Environment}, 'PROD')";
                throw new Exception(message);
            }
            else
            {
                return dbRows[0]["setting_value"].ToString();

            }
        }

        public string localFtpRoot => FileUtils.FixPath($"{GetProcessBaseDir()}/{GetAppSetting("FtpPath")}");
        public string localFtpItRoot => FileUtils.FixPath($"{GetProcessBaseDir()}/{localFtpItRoot}/");

        public string paylocityFtpRoot => FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("paylocityFtpPath")}");

        public string fromBoomiFtpRoot => FileUtils.FixPath($"{localFtpItRoot}/{GetAppSetting("FromBoomiFtpItPath")}");

        public string toBoomiFtpRoot => FileUtils.FixPath($"{localFtpItRoot}/{GetAppSetting("ToBoomiFtpItPath")}");

        public string salesForceCrmListPath => FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("SalesForceCrmListPath")}");

        public string cobraImportRoot => FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("CobraImportPath")}");

        public string cobraImportHoldingRoot => FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("CobraImportHoldingPath")}");

        public string cobraImportTestFilesRoot => FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("CobraImportTestFilesPath")}");

        public string cobraImportHoldingPreparedQbRoot => FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("cobraImportHoldingPreparedQbPath")}");

        public string cobraImportArchiveDoneRoot => FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("cobraImportArchiveDonePath")}");

        public string cobraImportArchiveEmptyRoot => FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("cobraImportArchiveEmptyPath")}");

        public string cobraImportArchiveErrorRoot => FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("cobraImportArchiveErrorPath")}");

        public string cobraImportHoldingDecryptRoot => FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("cobraImportHoldingDecryptPath")}");

        public string alegeusFileHeadersRoot => FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusFileHeadersPath")}");

        public string alegeusFileHeadersArchiveRoot => FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusFileHeadersArchivePath")}");

        public string alegeusFilesPreCheckRoot => FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusFilesPreCheckPath")}");

        public string alegeusFilesPreCheckOKRoot => FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusFilesPreCheckOKPath")}");

        public string alegeusFilesPreCheckOKArchiveRoot => FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusFilesPreCheckOKArchivePath")}");

        public string alegeusFilesPreCheckFailRoot => FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusFilesPreCheckFailPath")}");

        public string alegeusFilesPreCheckTestRoot => FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusFilesPreCheckTestPath")}");

        public string alegeusFilesReprocessRoot => FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusFilesReprocessPath")}");

        public string alegeusFilesPreCheckHoldAllRoot => FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusFilesPreCheckHoldAllPath")}");

        public string alegeusFilesPreCheckHoldAllOriginalRoot => FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusFilesPreCheckHoldAllPath")}");
        public string alegeusParticipantEnrollmentFilesDownloadPath => FileUtils.FixPath($"{GetAppSetting("alegeusParticipantEnrollmentFilesDownloadPath")}");
        public string alegeusParticipantEnrollmentFilesDecryptedPath => FileUtils.FixPath($"{GetAppSetting("alegeusParticipantEnrollmentFilesDecryptedPath")}");


        #endregion

        #region ErrorLogPaths

        public string AlegeusErrorLogMbiFilesRoot => FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusErrorLogMbiFilesPath")}");

        public string AlegeusErrorLogMbiFilesArchiveRoot => FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusErrorLogMbiFilesArchivePath")}");

        public string AlegeusErrorLogResFilesRoot => FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusErrorLogResFilesPath")}");


        public string AlegeusErrorLogResFilesArchiveRoot => FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusErrorLogResFilesArchivePath")}");


        public string[] cobraIgnoreFtpSourceDirs
        {
            get
            {
                string[] dirs =
                {
                    $"{GetAppSetting("localTestFilesPath")}/processing@claritybenefitsolutions", cobraImportRoot, cobraImportHoldingRoot,
                    cobraImportHoldingDecryptRoot, alegeusFilesPreCheckRoot, alegeusFilesPreCheckHoldAllRoot,
                    alegeusFileHeadersArchiveRoot, alegeusFileHeadersRoot
                };
                return dirs;
            }
        }


        public string[] alegeusIgnoreFtpSourceDirs
        {
            get
            {
                string[] dirs =
                {
                    cobraImportRoot, cobraImportHoldingRoot, cobraImportHoldingDecryptRoot, alegeusFilesPreCheckRoot,
                    alegeusFilesPreCheckHoldAllRoot, alegeusFileHeadersArchiveRoot, alegeusFileHeadersRoot
                };
                return dirs;
            }
        }

        private Dictionary<string, string> _prodToRunningCtxPathReplacePatterns;

        public Dictionary<string, string> prodToRunningCtxPathReplacePatterns
        {
            get
            {
                if (_prodToRunningCtxPathReplacePatterns == null)
                    _prodToRunningCtxPathReplacePatterns = new Dictionary<string, string>
                    {
                        {"\\\\Fs009\\user_files_d\\BENEFLEX\\DEPTS\\FTP\\To_Alegeus_FTP_Holding\\Archive", alegeusFileHeadersArchiveRoot},
                        {"\\\\Fs009\\user_files_d\\BENEFLEX\\DEPTS\\FTP\\To_Alegeus_FTP_Holding\\HoldALL", alegeusFilesPreCheckHoldAllRoot},
                        {"\\\\Fs009\\user_files_d\\BENEFLEX\\DEPTS\\FTP\\To_Alegeus_FTP_Holding", alegeusFilesPreCheckRoot},
                        {"\\\\Fs009\\user_files_d\\BENEFLEX\\DEPTS\\FTP\\AutomatedHeaderV1_Files", alegeusFileHeadersRoot},
                        {"\\\\Fs009\\user_files_d\\BENEFLEX\\DEPTS\\FTP", localFtpRoot}
                    };
                return _prodToRunningCtxPathReplacePatterns;
            }
        }
        #endregion
    }
}
