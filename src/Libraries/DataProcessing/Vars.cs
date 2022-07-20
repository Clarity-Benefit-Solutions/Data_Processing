using CoreUtils;
using CoreUtils.Classes;
using DataProcessing.DataModels.CobraPoint;
using DataProcessing.DataModels.DataProcessing;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Web.Hosting;


// ReSharper disable All

namespace DataProcessing
{

    public class Vars
    {
        private static string _RunTimeEnvironment = null;

        public static string RunTimeEnvironment
        {
            get { return _RunTimeEnvironment ?? "TEST"; }
            set
            {
                if (_RunTimeEnvironment != null)
                {
                    throw new Exception($"Environment can be set only once. It is currently {_RunTimeEnvironment}");
                }

                _RunTimeEnvironment = value;
            }
        }

        public static Boolean UseVPNToConnectToPortal
        {
            get { return _UseVPNToConnectToPortal; }
            set { _UseVPNToConnectToPortal = value; }
        }

        #region LocalRootPaths

        public string ConvertFilePathFromProdToCtx(string prodFilePath)
        {
            if (Utils.IsBlank(prodFilePath)) return prodFilePath;
            if (RunTimeEnvironment == "PROD")
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
                {
                    fixedPath = entry.Value + prodFilePath.Substring(entry.Key.Length);
                }
            }

            fixedPath = FileUtils.FixPath(fixedPath);

            return fixedPath;
        }

        #endregion

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


        public static string GetProcessExeDir()
        {
            if (IsRunningAsWebApp())
            {
                return FileUtils.FixPath(AppDomain.CurrentDomain.BaseDirectory) + "\\bin";
            }
            else
            {
                var processModule = Process.GetCurrentProcess().MainModule;
                if (processModule != null)
                {
                    var exePath = processModule.FileName;
                    var directoryPath = $"{Path.GetDirectoryName(exePath)}";
                    return FileUtils.FixPath(directoryPath);
                }
            }

            return "";
        }


        public string ConnStrNameDataProcessing
        {
            get { return "Data_ProcessingEntities"; }
        }

        private Data_ProcessingEntities _dbCtxDataProcessingDefault;

        public Data_ProcessingEntities DbCtxDataProcessingDefault
        {
            get
            {
                if (_dbCtxDataProcessingDefault == null)
                    _dbCtxDataProcessingDefault = new Data_ProcessingEntities("name=" + ConnStrNameCobra);
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
                    _dbConnDataProcessing =
 new StackExchange.Profiling.Data.ProfiledDbConnection(_dbConnDataProcessing, MiniProfiler.Current);
#endif

                    if (_dbConnDataProcessing.State != ConnectionState.Open) _dbConnDataProcessing.Open();
                }

                return _dbConnDataProcessing;
            }
        }

        #endregion

        #region BrokerCommission
        public string ConnStrNameBrokerCommission
        {
            get { return "Broker_CommissionConnectionString"; }
        }

        private DbConnection _dbConnBrokerCommission;

        public DbConnection dbConnBrokerCommission
        {
            get
            {
                if (_dbConnBrokerCommission == null)
                {
                    string connString =
                        Utils.GetProviderConnString(ConnStrNameBrokerCommission);
                    //
                    _dbConnBrokerCommission = new SqlConnection(connString);
                    // profiling
#if PROFILE
                    _dbConnBrokerCommission =
 new StackExchange.Profiling.Data.ProfiledDbConnection(_dbConnBrokerCommission, MiniProfiler.Current);
#endif

                    if (_dbConnBrokerCommission.State != ConnectionState.Open) _dbConnBrokerCommission.Open();
                }

                return _dbConnBrokerCommission;
            }
        }

        #endregion


        public string ConnStrNameHangfire
        {
            get { return "Hangfire"; }
        }

        #region DbConnPortalWc

        private static Boolean _UseVPNToConnectToPortal = false;

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
                    _dbConnPortalWc =
 new StackExchange.Profiling.Data.ProfiledDbConnection(_dbConnPortalWc, MiniProfiler.Current);
#endif

                    if (_dbConnPortalWc.State != ConnectionState.Open) _dbConnPortalWc.Open();
                }

                return _dbConnPortalWc;
            }
        }

        #endregion

        #region DbConnCobra

        private static Boolean _UseVPNToConnectToCobra = false;
        public static Boolean UseVPNToConnectToCobra
        {
            get { return _UseVPNToConnectToCobra; }
            set { _UseVPNToConnectToCobra = value; }
        }

        public string ConnStrNameCobra
        {
            get
            {
                if (UseVPNToConnectToCobra)
                {
                    return "CobraPointEntities";
                }
                else
                {
                    return "CobraPointEntities";
                }
            }
        }


        private DbConnection _dbConnCobra;

        public DbConnection dbConnCobra
        {
            get
            {
                if (_dbConnCobra == null)
                {
                    string connString =
                        Utils.GetProviderConnString(ConnStrNameCobra);
                    //
                    _dbConnCobra = new SqlConnection(connString);

                    // profiling
#if PROFILE
                    _dbConnCobra =
 new StackExchange.Profiling.Data.ProfiledDbConnection(_dbConnCobra, MiniProfiler.Current);
#endif

                    if (_dbConnCobra.State != ConnectionState.Open) _dbConnCobra.Open();
                }

                return _dbConnCobra;
            }
        }

        private CobraPointEntities _dbCtxCobraDefault;

        public CobraPointEntities DbCtxCobraDefault
        {
            get
            {
                if (_dbCtxCobraDefault == null)
                    _dbCtxCobraDefault = new CobraPointEntities("name=" + ConnStrNameCobra);
                return _dbCtxCobraDefault;
            }
        }
        public CobraPointEntities dbCtxCobraNew
        {
            get { return new CobraPointEntities("name=" + ConnStrNameCobra); }
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

        public FileOperationLogParams GetDbFileProcessingLogParams(string platform = "Alegeus")
        {
            FileOperationLogParams logParams = dbFileProcessingLogParams;
            if (!Utils.IsBlank(platform) || logParams.Platform != platform)
            {
                logParams.Platform = platform;
            }
            return logParams;
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

                        w.Platform = "Unknown";
                        w.LogTableName = "dbo.[file_processing_log]";
                    });
                }

                return _dbFileProcessingLogParams;
            }
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
                    if (RunTimeEnvironment == "TEST")
                    {
                        _remoteAlegeusFtpConnection = new SFtpConnection("BE015", 22, "alegeus", "3214@Clarity");
                    }
                    else if (RunTimeEnvironment == "PROD")
                    {
                        _remoteAlegeusFtpConnection =
                            new SFtpConnection("ftp.wealthcareadmin.com", 22, "benefledi", "VzVR4s4y");
                    }
                    else
                    {
                        throw new Exception($"Sorry, Current Environemtn {RunTimeEnvironment} is Not valid ");
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
                if (RunTimeEnvironment == "TEST")
                {
                    _remoteCobraFtpConnection = new SFtpConnection("BE015", 22, "cobra", "3214@Clarity");
                }
                else if (RunTimeEnvironment == "PROD")
                {
                    _remoteCobraFtpConnection = new SFtpConnection("xxx", 22, "xx", "xx@");
                }
                else
                {
                    throw new Exception($"Sorry, Current Environemtn {RunTimeEnvironment} is Not valid ");
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
                    "select * from dbo.app_settings where is_active = 1 order by environment, setting_name", null, null,
                    false, true);

                if (appSettings == null)
                {
                    string message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : Could Not get appSettings";
                    throw new Exception(message);
                }
            }

            // try exact environment
            string filter = $"environment In ('{RunTimeEnvironment}') and setting_name = '{settingName}' ";

            DataRow[] dbRows = appSettings.Select(filter);
            if (dbRows.Length == 0)
            {
                // try PROD env
                filter = $"environment In ('PROD') and setting_name = '{settingName}' ";
                dbRows = appSettings.Select(filter);
            }

            if (dbRows.Length == 0)
            {
                string message =
                    $"The App Setting {settingName} could not be found for environments ({RunTimeEnvironment}, 'PROD')";
                throw new Exception(message);
            }
            else
            {
                return dbRows[0]["setting_value"].ToString();
            }
        }

        public static string ftpSubFolderPath
        {
            get
            {
                string value = Environment.GetEnvironmentVariable("ftpSubFolderPath");
                return Utils.IsBlank(value) ? "" : value;
            }
            set
            {
                if (value == null)
                {
                    value = "";
                }
                Environment.SetEnvironmentVariable("ftpSubFolderPath", value);
            }
        }
        public string localFtpRoot
        {
            get
            {
                if (Utils.IsBlank(ftpSubFolderPath))
                {
                    return FileUtils.FixPath($"{GetAppSetting("FtpPath")}");
                }
                else
                {
                    return FileUtils.FixPath($"{GetAppSetting("FtpPath")}/{ftpSubFolderPath}");
                }
            }
        }
        public string localFtpItRoot
        {
            get
            {
                if (Utils.IsBlank(ftpSubFolderPath))
                {
                    return FileUtils.FixPath(localFtpItRoot);
                }
                else
                {
                    return FileUtils.FixPath(localFtpItRoot);
                }
            }
        }

        public string paylocityFtpRoot => FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("paylocityFtpPath")}");

        public string fromBoomiFtpRoot => FileUtils.FixPath($"{localFtpItRoot}/{GetAppSetting("FromBoomiFtpItPath")}");

        public string toBoomiFtpRoot => FileUtils.FixPath($"{localFtpItRoot}/{GetAppSetting("ToBoomiFtpItPath")}");

        public string salesForceCrmListPath =>
            FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("SalesForceCrmListPath")}");

        public string cobraFilesImportHoldingPath =>
            FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("cobraFilesImportHoldingPath")}");

        public string cobraFilesImportHoldingArchivePath =>
            FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("cobraFilesImportHoldingArchivePath")}");

        public string cobraFilesToProcessPath =>
            FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("cobraFilesToProcessPath")}");

        public string cobraFilesPreparedQbPath =>
            FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("cobraFilesPreparedQbPath")}");

        public string cobraFilesEmptyPath =>
            FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("cobraFilesEmptyPath")}");

        public string cobraFilesPassedPath =>
            FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("cobraFilesPassedPath")}");

        public string cobraFilesRejectsPath =>
            FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("cobraFilesRejectsPath")}");

        public string cobraFilesTestPath =>
                  FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("cobraFilesTestPath")}");

        public string cobraFilesDecryptPath =>
            FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("cobraFilesDecryptPath")}");

        public string cobraFilesToReProcessPath =>
            FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("cobraFilesToReProcessPath")}");

        public string alegeusFilesImportHoldingPath =>
            FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusFilesImportHoldingPath")}");

        public string unknownFilesImportHoldingPath =>
            FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("unknownFilesImportHoldingPath")}");

        public string unknownFilesRejectsPath =>
            FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("unknownFilesRejectsPath")}");


        public string alegeusFilesImportHoldingArchivePath =>
            FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusFilesImportHoldingArchivePath")}");


        public string alegeusFileHeadersRoot =>
            FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusFileHeadersPath")}");

        public string alegeusFilesToProcessPath =>
            FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusFilesToProcessPath")}");

        public string alegeusFilesPassedPath =>
            FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusFilesPassedPath")}");

        public string alegeusFilesEmptyPath =>
            FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusFilesEmptyPath")}");

        public string unknownFilesEmptyPath =>
                    FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("unknownFilesEmptyPath")}");

        public string alegeusFilesRejectsPath =>
            FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusFilesRejectsPath")}");

        public string alegeusFilesTestPath =>
            FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusFilesTestPath")}");

        public string alegeusFilesToReProcessPath =>
            FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusFilesToReProcessPath")}");


        public string alegeusParticipantEnrollmentFilesDownloadPath =>
            FileUtils.FixPath($"{GetAppSetting("alegeusParticipantEnrollmentFilesDownloadPath")}");

        public string alegeusParticipantEnrollmentFilesDecryptedPath =>
            FileUtils.FixPath($"{GetAppSetting("alegeusParticipantEnrollmentFilesDecryptedPath")}");

        #endregion

        #region ErrorLogPaths

        public string AlegeusErrorLogMbiFilesRoot =>
            FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusErrorLogMbiFilesPath")}");

        public string AlegeusErrorLogMbiFilesArchiveRoot =>
            FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusErrorLogMbiFilesArchivePath")}");

        public string AlegeusErrorLogResFilesRoot =>
            FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusErrorLogResFilesPath")}");


        public string AlegeusErrorLogResFilesArchiveRoot =>
            FileUtils.FixPath($"{localFtpRoot}/{GetAppSetting("alegeusErrorLogResFilesArchivePath")}");


        public string[] cobraIgnoreFtpSourceDirs
        {
            get
            {
                string[] dirs =
                {
                    $"{GetAppSetting("localTestFilesPath")}/processing@claritybenefitsolutions",
                    $"{GetAppSetting("localTestFilesPath")}/ImportHolding",
                    $"{GetAppSetting("localTestFilesPath")}/Processed",
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
                    $"{GetAppSetting("localTestFilesPath")}/processing@claritybenefitsolutions",
                    $"{GetAppSetting("localTestFilesPath")}/ImportHolding",
                    $"{GetAppSetting("localTestFilesPath")}/Processed",
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
                        /*{
                            "\\\\Fs009\\user_files_d\\BENEFLEX\\DEPTS\\FTP\\To_Alegeus_FTP_Holding\\Archive",
                            alegeusFilesImportHoldingArchivePath
                        },
                        {
                            "\\\\Fs009\\user_files_d\\BENEFLEX\\DEPTS\\FTP\\To_Alegeus_FTP_Holding\\HoldALL",
                            alegeusFilesToProcessPath
                        },
                        {
                            "\\\\Fs009\\user_files_d\\BENEFLEX\\DEPTS\\FTP\\To_Alegeus_FTP_Holding",
                            alegeusFilesToProcessPath
                        },
                        {
                            "\\\\Fs009\\user_files_d\\BENEFLEX\\DEPTS\\FTP\\AutomatedHeaderV1_Files",
                            alegeusFileHeadersRoot
                        },*/
                        {
                            "\\\\Fs009\\user_files_d\\BENEFLEX\\DEPTS\\FTP", localFtpRoot
                        },
                        {
                            "\\\\fs009\\USER_FILES_D\\BENEFLEX\\Public\\__For_FileChecker\\FTP", localFtpRoot
                        },
                    };
                return _prodToRunningCtxPathReplacePatterns;
            }
        }

        #endregion
    }

}