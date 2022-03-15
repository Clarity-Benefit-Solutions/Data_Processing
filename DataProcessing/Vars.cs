using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
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
     
        #region DBDataProcessing

        private string _connStrNameDataProcessing;

        private string connStrNameDataProcessing
        {
            get
            {
                if (Utils.IsBlank(_connStrNameDataProcessing))
                {
#if (TEST)
                    _connStrNameDataProcessing = "Data_ProcessingEntitiesTEST";
#else
                    _connStrNameDataProcessing = "Data_ProcessingEntities";
#endif
                }

                return _connStrNameDataProcessing;
            }
        }

        private Data_ProcessingEntities _dbCtxDataProcessingDefault;

        public Data_ProcessingEntities DbCtxDataProcessingDefault
        {
            get
            {
                if (_dbCtxDataProcessingDefault == null)
                    _dbCtxDataProcessingDefault = new Data_ProcessingEntities("name=" + connStrNameDataProcessing);
                return _dbCtxDataProcessingDefault;
            }
        }

        public Data_ProcessingEntities dbCtxDataProcessingNew
        {
            get { return new Data_ProcessingEntities("name=" + connStrNameDataProcessing); }
        }

        private DbConnection _dbConnDataProcessing;

        public DbConnection dbConnDataProcessing
        {
            get
            {
                if (_dbConnDataProcessing == null)
                {
                    string connString =
                        DbUtils.GetProviderConnString(connStrNameDataProcessing);
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

        private string _connStrNamePortalWc;

        private string connStrNamePortalWc
        {
            get
            {
                if (Utils.IsBlank(_connStrNamePortalWc))
                {
#if (TEST)
                    _connStrNamePortalWc = "PortalWcTEST";
#else
                    _connStrNamePortalWc = "PortalWc";
#endif
                }

                return _connStrNamePortalWc;
            }
        }

        //private  PortalWc _dbCtxPortalWcDefault;

        //public  PortalWc dbCtxPortalWcDefault
        //{
        //    get
        //    {
        //        if (_dbCtxPortalWcDefault == null)
        //            _dbCtxPortalWcDefault = new PortalWc(connStrNamePortalWc);
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
                        DbUtils.GetProviderConnString(connStrNamePortalWc);
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

            prodFilePath = FileUtils.FixPath(prodFilePath, false);
            var matchProdFilePath = FileUtils.FixPath(prodFilePath, true);
            var fixedPath = prodFilePath;
            //
            var replacements = prodToRunningCtxPathReplacePatterns;
            foreach (var entry in replacements)
            {
                var entryKey = FileUtils.FixPath(entry.Key, true);
                // compare lower to lower and standardized slashes
                if (matchProdFilePath.StartsWith(entryKey))
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
#if (TEST)
                    _remoteAlegeusFtpConnection = new SFtpConnection("localhost", 22, "alegeus", "a");
#else
                    _remoteAlegeusFtpConnection = new SFtpConnection("ftp.wealthcareadmin.com", 21, "benefledi", "VzVR4s4y");;
#endif
                }

                return _remoteAlegeusFtpConnection;
            }
        }

        public string remoteAlegeusFtpRootPath
        {
            get
            {
#if (TEST)
                return "/" + FileUtils.FixPath($"{GetAppSetting("localTestFilesPath")}/_local_FTP_Server_Server/Alegeus");
#else
                return "/";
#endif
            }
        }

        public string remoteCobraFtpRootPath
        {
            get
            {
#if (TEST)
                return $"{GetAppSetting("localTestFilesPath")}/_local_FTP_Server_Server/COBRA";
#else
                return "/";
#endif
            }
        }

        private SFtpConnection _remoteCobraFtpConnection;

        public SFtpConnection RemoteCobraFtpConnection
        {
            get
            {
#if (TEST)
                _remoteCobraFtpConnection = new SFtpConnection("localhost", 21, "alegeus", "a");
#else
                _remoteCobraFtpConnection = new SFtpConnection("ftp.wealthcareadmin.com", 21, "benefledi", "VzVR4s4y");;
#endif

                return _remoteCobraFtpConnection;
            }
        }


        #endregion

        #region FileProcessingPaths

        private static DataTable appSettings;

        private string GetAppSetting(string settingName)
        {
            if (appSettings == null)
            {
                appSettings = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, dbConnDataProcessing,
                    "select * from Alegeus_File_Processing.dbo.app_settings order by environment, setting_name", null, null, false, true);

                if (appSettings == null)
                {
                    string message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : Could Not get appSettings";
                    throw new Exception(message);
                }
            }
            string environment = "";
# if TEST
            environment = "TEST";
#else
          environment = "PROD";
#endif
            // try exact environment
            string filter = $"environment In ('{environment}') and setting_name = '{settingName}' ";

            DataRow[] dbRows = appSettings.Select(filter);
            if (dbRows.Length == 0)
            {
                // try PROD env
                filter = $"environment In ('PROD') and setting_name = '{settingName}' ";
                dbRows = appSettings.Select(filter);

            }

            if (dbRows.Length == 0)
            {
                string message = $"The App Setting {settingName} could not be found for environments ({environment}, 'PROD')";
                throw new Exception(message);
            }
            else
            {
                return dbRows[0]["setting_value"].ToString();

            }
        }

        public string localFtpRoot => $"{GetAppSetting("FtpPath")}";
        public string localFtpItRoot => $"{GetAppSetting("FtpItPath")}";

        public string paylocityFtpRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("paylocityFtpPath")}";

        public string fromBoomiFtpRoot => $"{GetAppSetting("FtpItPath")}{GetAppSetting("FromBoomiFtpItPath")}";

        public string toBoomiFtpRoot => $"{GetAppSetting("FtpItPath")}{GetAppSetting("ToBoomiFtpItPath")}";

        public string salesForceCrmListPath => $"{GetAppSetting("FtpPath")}{GetAppSetting("SalesForceCrmListPath")}";

        public string cobraImportRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("SalesForceCrmListPath")}";

        public string cobraImportHoldingRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("CobraImportPath")}";

        public string cobraImportTestFilesRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("CobraImportTestFilesPath")}";

        public string cobraImportHoldingPreparedQbRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("cobraImportHoldingPreparedQbPath")}";

        public string cobraImportArchiveDoneRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("cobraImportArchiveDonePath")}";

        public string cobraImportArchiveEmptyRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("cobraImportArchiveEmptyPath")}";

        public string cobraImportArchiveErrorRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("cobraImportArchiveErrorPath")}";

        public string cobraImportHoldingDecryptRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("cobraImportHoldingDecryptPath")}";


        public string alegeusFileHeadersRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("alegeusFileHeadersPath")}";

        public string alegeusFileHeadersArchiveRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("alegeusFileHeadersArchivePath")}";

        public string alegeusFilesPreCheckRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("alegeusFilesPreCheckPath")}";
        public string alegeusFilesPreCheckOKRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("alegeusFilesPreCheckOKPath")}";
        public string alegeusFilesPreCheckOKArchiveRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("alegeusFilesPreCheckOKArchivePath")}";
        public string alegeusFilesPreCheckFailRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("alegeusFilesPreCheckFailPath")}";
        public string alegeusFilesReprocessRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("alegeusFilesReprocessPath")}";

        public string alegeusFilesPreCheckHoldAllRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("alegeusFilesPreCheckHoldAllPath")}";

        #endregion

        #region ErrorLogPaths

        public string AlegeusErrorLogMbiFilesRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("alegeusErrorLogMbiFilesPath")}";

        public string AlegeusErrorLogMbiFilesArchiveRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("alegeusErrorLogMbiFilesArchivePath")}";

        public string AlegeusErrorLogResFilesRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("alegeusErrorLogResFilesPath")}";

        public string AlegeusErrorLogResFilesArchiveRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("alegeusErrorLogResFilesArchivePath")}";


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
                        {"G:/FTP/To_Alegeus_FTP_Holding/Archive", alegeusFileHeadersArchiveRoot},
                        {"G:/FTP/To_Alegeus_FTP_Holding/HoldALL", alegeusFilesPreCheckHoldAllRoot},
                        {"G:/FTP/To_Alegeus_FTP_Holding", alegeusFilesPreCheckRoot},
                        {"G:/FTP/AutomatedHeaderV1_Files", alegeusFileHeadersRoot},
                        {"G:/FTP", GetAppSetting("FtpPath")}
                    };
                return _prodToRunningCtxPathReplacePatterns;
            }
        }
        #endregion
    }
}