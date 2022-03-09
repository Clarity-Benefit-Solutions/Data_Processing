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

        public static string WebAppRootPath { get; set; }

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

       
        public string localTestRoot
        {
            get
            {
#if (CTXSUMEETDEV)
                string path = "";
                if (!IsRunningAsWebApp)
                {
                    path = $"{Utils.GetExeBaseDir()}/../../../__LocalTestDirsAndFiles";
                }
                else
                {
                    path = $"{WebAppRootPath}/../__LocalTestDirsAndFiles"; 
                }
                

                DirectoryInfo dirInfo = new DirectoryInfo(path);
                if (!dirInfo.Exists)
                {
                    dirInfo.Create();
                }

                return dirInfo.FullName;
#else
              return "G:/FTP"; ;
#endif
            }
        }

        public string localFtpRoot
        {
            get
            {
#if (CTXSUMEETDEV)
                return $"{localTestRoot}/FTP";
#else
              return "G:/FTP"; ;
#endif
            }
        }

        public string localFtpItRoot
        {
            get
            {
#if (CTXSUMEETDEV)
                return $"{localTestRoot}/FTP-IT";
#else
              return "G:/FTP-IT";
#endif
            }
        }

        public string prodLocalFtpRoot => "G:/FTP";

        public string prodLocalFtpItRoot => "G:/FTP-IT";

        public string processingRoot
        {
            get
            {
#if (CTXSUMEETDEV)
                return $"{localTestRoot}";
#else
                return "G:/FTP";
#endif
            }
        }

        public string paylocityFtpRoot => $"{localFtpRoot}/Paylocity";

        public string fromBoomiFtpRoot => $"{localFtpItRoot}/fromBoomi";

        public string toBoomiFtpRoot => $"{localFtpItRoot}/ToBoomi";

        public string salesForceCrmListPath => $"{localFtpRoot}/fromBoomi/CRM_List.csv";

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
                        {"G:/FTP", localFtpRoot}
                    };
                return _prodToRunningCtxPathReplacePatterns;
            }
        }

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
                return "/" + FileUtils.FixPath($"{localTestRoot}/_local_FTP_Server_Server/Alegeus");
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
                return $"{localTestRoot}/_local_FTP_Server_Server/COBRA";
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

        #region CobraFileProcessingPaths

        public string cobraImportRoot => $"{processingRoot}/COBRA IMPORTS";

        public string cobraImportHoldingRoot => $"{processingRoot}/COBRA IMPORTS/Holding";

        public string cobraImportTestFilesRoot => $"{processingRoot}/COBRA IMPORTS/COBRA_testfiles";

        public string cobraImportHoldingPreparedQbRoot => $"{processingRoot}/COBRA IMPORTS/Holding/PreparedQB";

        public string cobraImportArchiveDoneRoot => $"{processingRoot}/COBRA IMPORTS/Archive - Done";

        public string cobraImportArchiveEmptyRoot => $"{processingRoot}/COBRA IMPORTS/Archive - Empty";

        public string cobraImportArchiveErrorRoot => $"{processingRoot}/COBRA IMPORTS/Archive - Error";

        public string cobraImportHoldingDecryptRoot => $"{processingRoot}/COBRA IMPORTS/Holding/ToDecrypt";

        public string[] cobraIgnoreFtpSourceDirs
        {
            get
            {
                string[] dirs =
                {
                    $"{localFtpRoot}/processing@claritybenefitsolutions", cobraImportRoot, cobraImportHoldingRoot,
                    cobraImportHoldingDecryptRoot, alegeusFilesPreCheckRoot, alegeusFilesPreCheckHoldAllRoot,
                    alegeusFileHeadersArchiveRoot, alegeusFileHeadersRoot
                };
                return dirs;
            }
        }

        #endregion

        #region AlegeusFileProcessingPaths

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

        public string alegeusFileHeadersRoot => $"{processingRoot}/AutomatedHeaderV1_Files";

        public string alegeusFileHeadersArchiveRoot => $"{processingRoot}/AutomatedHeaderV1_Files/Archive";

        public string alegeusFilesPreCheckRoot => $"{processingRoot}/To_Alegeus_Pre_Process";
        public string alegeusFilesPreCheckOKRoot => $"{processingRoot}/To_Alegeus_Pre_Process/Check_OK";
        public string alegeusFilesPreCheckFailRoot => $"{processingRoot}/To_Alegeus_Pre_Process/Check_FAIL";

        public string alegeusFilesPreCheckHoldAllRoot => $"{processingRoot}/To_Alegeus_Pre_Process/HoldALL";

        public string alegeusFilesFTPHoldingRoot => $"{processingRoot}/To_Alegeus_FTP_Holding";

        #endregion

        #region ErrorLogPaths

        public string alegeusErrorLogMbiFilesRoot => $"{processingRoot}/AlegeusErrorLog/mbiFiles";

        public string alegeusErrorLogMbiFilesArchiveRoot => $"{processingRoot}/AlegeusErrorLog/mbiFiles/Archive";

        public string alegeusErrorLogResFilesRoot => $"{processingRoot}/AlegeusErrorLog/resFiles";

        public string alegeusErrorLogResFilesArchiveRoot => $"{processingRoot}/AlegeusErrorLog/resFiles/Archive";

        #endregion
    }
}