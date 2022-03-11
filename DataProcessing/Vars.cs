using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;
using System.IO;
using CoreUtils;
using CoreUtils.Classes;
using DataProcessing.DataModels.AlegeusErrorLog;
using DataProcessing.DataModels.AlegeusFileProcessing;
using DataProcessing.DataModels.COBRA;
using MySqlConnector;
using StackExchange.Profiling;

// ReSharper disable All

namespace DataProcessing
{
    public class Vars
    {
        #region DbCobraFileProcessing

        private string _connStrNameCobraFileProcessing;

        public static bool IsRunningAsWebApp { get; set; }

        private string connStrNameCobraFileProcessing
        {
            get
            {
                if (Utils.IsBlank(_connStrNameCobraFileProcessing))
                {
#if (CTXSUMEETDEV)
                    _connStrNameCobraFileProcessing = "COBRAEntitiesCTXSUMEETDEV";
#else
                    _connStrNameCobraFileProcessing = "COBRAEntitiesCTXPROD";
#endif
                }

                return _connStrNameCobraFileProcessing;
            }
        }

        private COBRAEntities _dbCtxCobraFileProcessingDefault;

        private COBRAEntities DbCtxCobraFileProcessingDefault
        {
            get
            {
                if (_dbCtxCobraFileProcessingDefault == null)
                    _dbCtxCobraFileProcessingDefault = new COBRAEntities("name=" + connStrNameCobraFileProcessing);
                return _dbCtxCobraFileProcessingDefault;
            }
        }

        private COBRAEntities DbCtxCobraFileProcessingNew
        {
            get { return new COBRAEntities("name=" + connStrNameCobraFileProcessing); }
        }


        private DbConnection _dbConnCobraFileProcessing;

        public DbConnection dbConnCobraFileProcessing
        {
            get
            {
                if (_dbConnCobraFileProcessing == null)
                {

                    string connString =
                        DbUtils.GetProviderConnString(connStrNameCobraFileProcessing);
                    //
                    _dbConnCobraFileProcessing = new SqlConnection(connString);
                    // profiling
#if PROFILE
                    _dbConnCobraFileProcessing = new StackExchange.Profiling.Data.ProfiledDbConnection(_dbConnCobraFileProcessing, MiniProfiler.Current);
#endif

                    if (_dbConnCobraFileProcessing.State != ConnectionState.Open) _dbConnCobraFileProcessing.Open();
                }

                return _dbConnCobraFileProcessing;
            }
        }

        #endregion

        #region DbAlegeusCobraFileProcessing

        private string _connStrNameAlegeusFileProcessing;

        private string connStrNameAlegeusFileProcessing
        {
            get
            {
                if (Utils.IsBlank(_connStrNameAlegeusFileProcessing))
                {
#if (CTXSUMEETDEV)
                    _connStrNameAlegeusFileProcessing = "Alegeus_File_ProcessingEntitiesCTXSUMEETDEV";
#else
                    _connStrNameAlegeusFileProcessing = "Alegeus_File_ProcessingEntitiesCTXPROD";
#endif
                }

                return _connStrNameAlegeusFileProcessing;
            }
        }

        private Alegeus_File_ProcessingEntities _dbCtxAlegeusFileProcessingDefault;

        private Alegeus_File_ProcessingEntities DbCtxAlegeusFileProcessingDefault
        {
            get
            {
                if (_dbCtxAlegeusFileProcessingDefault == null)
                    _dbCtxAlegeusFileProcessingDefault =
                        new Alegeus_File_ProcessingEntities("name=" + connStrNameAlegeusFileProcessing);
                return _dbCtxAlegeusFileProcessingDefault;
            }
        }

        private Alegeus_File_ProcessingEntities DbCtxAlegeusFileProcessingNew
        {
            get { return new Alegeus_File_ProcessingEntities("name=" + connStrNameAlegeusFileProcessing); }
        }

        private DbConnection _dbConnAlegeusFileProcessing;

        public DbConnection dbConnAlegeusFileProcessing
        {
            get
            {
                if (_dbConnAlegeusFileProcessing == null)
                {
                    string connString =
                        DbUtils.GetProviderConnString(connStrNameAlegeusFileProcessing);
                    //
                    _dbConnAlegeusFileProcessing = new SqlConnection(connString);
                    // profiling
#if PROFILE
                    _dbConnAlegeusFileProcessing = new StackExchange.Profiling.Data.ProfiledDbConnection(_dbConnAlegeusFileProcessing, MiniProfiler.Current);
#endif

                    if (_dbConnAlegeusFileProcessing.State != ConnectionState.Open) _dbConnAlegeusFileProcessing.Open();
                }

                return _dbConnAlegeusFileProcessing;
            }
        }

        #endregion

        #region DBAlegeusErroLog

        private string _connStrNameAlegeusErrorLog;

        private string connStrNameAlegeusErrorLog
        {
            get
            {
                if (Utils.IsBlank(_connStrNameAlegeusErrorLog))
                {
#if (CTXSUMEETDEV)
                    _connStrNameAlegeusErrorLog = "Alegeus_ErrorLogEntitiesCTXSUMEETDEV";
#else
                    _connStrNameAlegeusErrorLog = "Alegeus_ErrorLogEntitiesCTXPROD";
#endif
                }

                return _connStrNameAlegeusErrorLog;
            }
        }

        private Alegeus_ErrorLogEntities _dbCtxAlegeusErrorLogDefault;

        public Alegeus_ErrorLogEntities DbCtxAlegeusErrorLogDefault
        {
            get
            {
                if (_dbCtxAlegeusErrorLogDefault == null)
                    _dbCtxAlegeusErrorLogDefault = new Alegeus_ErrorLogEntities("name=" + connStrNameAlegeusErrorLog);
                return _dbCtxAlegeusErrorLogDefault;
            }
        }

        public Alegeus_ErrorLogEntities dbCtxAlegeusErrorLogNew
        {
            get { return new Alegeus_ErrorLogEntities("name=" + connStrNameAlegeusErrorLog); }
        }

        private DbConnection _dbConnAlegeusErrorLog;

        public DbConnection dbConnAlegeusErrorLog
        {
            get
            {
                if (_dbConnAlegeusErrorLog == null)
                {
                    string connString =
                        DbUtils.GetProviderConnString(connStrNameAlegeusErrorLog);
                    //
                    _dbConnAlegeusErrorLog = new SqlConnection(connString);
                    // profiling
#if PROFILE
                    _dbConnAlegeusErrorLog = new StackExchange.Profiling.Data.ProfiledDbConnection(_dbConnAlegeusErrorLog, MiniProfiler.Current);
#endif

                    if (_dbConnAlegeusErrorLog.State != ConnectionState.Open) _dbConnAlegeusErrorLog.Open();
                }

                return _dbConnAlegeusErrorLog;
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
#if (CTXSUMEETDEV)
                    _connStrNamePortalWc = "PortalWcCTXSUMEETDEV";
#else
                    _connStrNamePortalWc = "PortalWcCTXPROD";
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
                        w.DbConnection = dbConnAlegeusFileProcessing;
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
                        w.DbConnection = dbConnAlegeusFileProcessing;
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
#if (CTXSUMEETDEV)
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
#if (CTXSUMEETDEV)
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
#if (CTXSUMEETDEV)
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
#if (CTXSUMEETDEV)
                _remoteCobraFtpConnection = new SFtpConnection("localhost", 21, "alegeus", "a");
#else
                _remoteCobraFtpConnection = new SFtpConnection("ftp.wealthcareadmin.com", 21, "benefledi", "VzVR4s4y");;
#endif

                return _remoteCobraFtpConnection;
            }
        }


        #endregion

        #region FileProcessingPaths

        private string GetAppSetting(string settingName)
        {
            return "";
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

        public string alegeusFilesPreCheckHoldAllRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("1alegeusFilesPreCheckHoldAllPath")}";

        #endregion

        #region ErrorLogPaths

        public string alegeusErrorLogMbiFilesRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("alegeusErrorLogMbiFilesPath")}";

        public string alegeusErrorLogMbiFilesArchiveRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("alegeusErrorLogMbiFilesArchivePath")}";

        public string alegeusErrorLogResFilesRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("alegeusErrorLogResFilesPath")}";

        public string alegeusErrorLogResFilesArchiveRoot => $"{GetAppSetting("FtpPath")}{GetAppSetting("alegeusErrorLogResFilesArchivePath")}";


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