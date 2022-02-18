using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.IO;
using CoreUtils.Classes;
using DataProcessing.DataModels.AlegeusErrorLog;
using DataProcessing.DataModels.AlegeusFileProcessing;
using DataProcessing.DataModels.COBRA;
using MySql.Data.MySqlClient;

// ReSharper disable All

namespace DataProcessing
{
    public static class Vars
    {
        #region DbCobraFileProcessing

        private static string _connStrNameCobraFileProcessing;

        private static string connStrNameCobraFileProcessing
        {
            get
            {
                if (Utils.IsBlank(_connStrNameCobraFileProcessing))
                {
#if (CTXSUMEETDEV)
                    _connStrNameCobraFileProcessing = "name=COBRAEntitiesCTXSUMEETDEV";
#else
                    _connStrNameCobraFileProcessing = "name=COBRAEntitiesCTXPROD";
#endif
                }

                return _connStrNameCobraFileProcessing;
            }
        }

        private static COBRAEntities _dbCtxCobraFileProcessingDefault;

        private static COBRAEntities DbCtxCobraFileProcessingDefault
        {
            get
            {
                if (_dbCtxCobraFileProcessingDefault == null)
                    _dbCtxCobraFileProcessingDefault = new COBRAEntities(connStrNameCobraFileProcessing);
                return _dbCtxCobraFileProcessingDefault;
            }
        }
        private static COBRAEntities DbCtxCobraFileProcessingNew
        {
            get
            {
                return new COBRAEntities(connStrNameCobraFileProcessing);
            }
        }


        private static DbConnection _dbConnCobraFileProcessing;

        public static DbConnection dbConnCobraFileProcessing
        {
            get
            {
                if (_dbConnCobraFileProcessing == null)
                {
                    _dbConnCobraFileProcessing = DbCtxCobraFileProcessingNew.Database.Connection;
                    if (_dbConnCobraFileProcessing.State != ConnectionState.Open) _dbConnCobraFileProcessing.Open();
                }

                return _dbConnCobraFileProcessing;
            }
        }

        #endregion

        #region DbAlegeusCobraFileProcessing

        private static string _connStrNameAlegeusFileProcessing;

        private static string connStrNameAlegeusFileProcessing
        {
            get
            {
                if (Utils.IsBlank(_connStrNameAlegeusFileProcessing))
                {
#if (CTXSUMEETDEV)
                    _connStrNameAlegeusFileProcessing = "name=Alegeus_File_ProcessingEntitiesCTXSUMEETDEV";
#else
                    _connStrNameAlegeusFileProcessing = "name=Alegeus_File_ProcessingEntitiesCTXPROD";
#endif
                }

                return _connStrNameAlegeusFileProcessing;
            }
        }

        private static Alegeus_File_ProcessingEntities _dbCtxAlegeusFileProcessingDefault;

        private static Alegeus_File_ProcessingEntities DbCtxAlegeusFileProcessingDefault
        {
            get
            {
                if (_dbCtxAlegeusFileProcessingDefault == null)
                    _dbCtxAlegeusFileProcessingDefault = new Alegeus_File_ProcessingEntities(connStrNameAlegeusFileProcessing);
                return _dbCtxAlegeusFileProcessingDefault;
            }
        }
        private static Alegeus_File_ProcessingEntities DbCtxAlegeusFileProcessingNew
        {
            get
            {
                return new Alegeus_File_ProcessingEntities(connStrNameAlegeusFileProcessing);
            }
        }

        private static DbConnection _dbConnAlegeusFileProcessing;

        public static DbConnection dbConnAlegeusFileProcessing
        {
            get
            {
                if (_dbConnAlegeusFileProcessing == null)
                {
                    _dbConnAlegeusFileProcessing = DbCtxAlegeusFileProcessingNew.Database.Connection;
                    if (_dbConnAlegeusFileProcessing.State != ConnectionState.Open) _dbConnAlegeusFileProcessing.Open();
                }

                return _dbConnAlegeusFileProcessing;
            }
        }

        #endregion

        #region DBAlegeusErroLog

        private static string _connStrNameAlegeusErrorLog;

        private static string connStrNameAlegeusErrorLog
        {
            get
            {
                if (Utils.IsBlank(_connStrNameAlegeusErrorLog))
                {
#if (CTXSUMEETDEV)
                    _connStrNameAlegeusErrorLog = "name=Alegeus_ErrorLogEntitiesCTXSUMEETDEV";
#else
                    _connStrNameAlegeusErrorLog = "name=Alegeus_ErrorLogEntitiesCTXPROD";
#endif
                }

                return _connStrNameAlegeusErrorLog;
            }
        }

        private static Alegeus_ErrorLogEntities _dbCtxAlegeusErrorLogDefault;

        public static Alegeus_ErrorLogEntities DbCtxAlegeusErrorLogDefault
        {
            get
            {
                if (_dbCtxAlegeusErrorLogDefault == null)
                    _dbCtxAlegeusErrorLogDefault = new Alegeus_ErrorLogEntities(connStrNameAlegeusErrorLog);
                return _dbCtxAlegeusErrorLogDefault;
            }
        }
        public static Alegeus_ErrorLogEntities dbCtxAlegeusErrorLogNew
        {
            get
            {
               return new Alegeus_ErrorLogEntities(connStrNameAlegeusErrorLog);
            }
        }

        private static DbConnection _dbConnAlegeusErrorLog;

        public static DbConnection dbConnAlegeusErrorLog
        {
            get
            {
                if (_dbConnAlegeusErrorLog == null)
                {
                    _dbConnAlegeusErrorLog = dbCtxAlegeusErrorLogNew.Database.Connection;
                    if (_dbConnAlegeusErrorLog.State != ConnectionState.Open) _dbConnAlegeusErrorLog.Open();
                }

                return _dbConnAlegeusErrorLog;
            }
        }

        #endregion
        #region DbConnPortalWc

        private static string _connStrNamePortalWc;

        private static string connStrNamePortalWc
        {
            get
            {
                if (Utils.IsBlank(_connStrNamePortalWc))
                {
#if (CTXSUMEETDEV)
                    _connStrNamePortalWc = "name=PortalWcCTXSUMEETDEV";
#else
                    _connStrNamePortalWc = "name=PortalWcCTXPROD";
#endif
                }

                return _connStrNamePortalWc;
            }
        }

        //private static PortalWc _dbCtxPortalWcDefault;

        //public static PortalWc dbCtxPortalWcDefault
        //{
        //    get
        //    {
        //        if (_dbCtxPortalWcDefault == null)
        //            _dbCtxPortalWcDefault = new PortalWc(connStrNamePortalWc);
        //        return _dbCtxPortalWcDefault;
        //    }
        //}

        private static MySqlConnection _dbConnPortalWc;

        public static MySqlConnection dbConnPortalWc
        {
            get
            {
                if (_dbConnPortalWc == null)
                {
                    string connStringName = connStrNamePortalWc.Replace("name=", "");
                    string connString = ConfigurationManager.ConnectionStrings[connStringName].ToString();
                    if (!Utils.IsBlank(connString))
                    {
                        _dbConnPortalWc = new MySqlConnection(connString);
                        if (_dbConnPortalWc.State != ConnectionState.Open) _dbConnPortalWc.Open();

                    }
                    //_dbConnPortalWc = dbCtxPortalWcDefault.Database.Connection;
                    //if (_dbConnPortalWc.State != ConnectionState.Open) _dbConnPortalWc.Open();
                }

                return _dbConnPortalWc;
            }
        }

        #endregion

        #region DbLogging

        private static MessageLogParams _dbMessageLogParams;

        public static MessageLogParams dbMessageLogParams
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


        private static FileOperationLogParams _dbFileProcessingLogParams;

        public static FileOperationLogParams dbFileProcessingLogParams
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

        public static string localTestRoot
        {
            get
            {
#if (CTXSUMEETDEV)
                string path = $"{Utils.GetExeBaseDir()}/../../../__LocalTestDirsAndFiles";
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

        public static string localFtpRoot
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

        public static string localFtpItRoot
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

        public static string prodLocalFtpRoot => "G:/FTP";

        public static string prodLocalFtpItRoot => "G:/FTP-IT";

        public static string processingRoot
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

        public static string paylocityFtpRoot => $"{localFtpRoot}/Paylocity";

        public static string fromBoomiFtpRoot => $"{localFtpItRoot}/fromBoomi";

        public static string toBoomiFtpRoot => $"{localFtpItRoot}/ToBoomi";

        public static string salesForceCrmListPath => $"{localFtpRoot}/fromBoomi/CRM_List.csv";

        private static Dictionary<string, string> _prodToRunningCtxPathReplacePatterns;

        public static Dictionary<string, string> prodToRunningCtxPathReplacePatterns
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

        public static string ConvertFilePathFromProdToCtx(string prodFilePath)
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

        private static SFtpConnection _remoteAlegeusFtpConnection;

        public static SFtpConnection RemoteAlegeusFtpConnection
        {
            get
            {
                if (_remoteAlegeusFtpConnection == null)
                {
#if (CTXSUMEETDEV)
                    _remoteAlegeusFtpConnection = new SFtpConnection("localhost", 22, "alegeus", "a");
#else
                    _remoteAlegeusFtpHost = = new SFtpConnection("ftp.wealthcareadmin.com", 21, "benefledi", "VzVR4s4y");;
#endif
                }

                return _remoteAlegeusFtpConnection;
            }
        }
        public static string remoteAlegeusFtpRootPath
        {
            get
            {


#if (CTXSUMEETDEV)
                return "/C:/Users/alegeus/alegeus_ftp_root";
#else
                    return "/";
#endif

            }
        }

        private static SFtpConnection _remoteCobraFtpConnection;

        public static SFtpConnection RemoteCobraFtpConnection
        {
            get
            {
#if (CTXSUMEETDEV)
                _remoteCobraFtpConnection = new SFtpConnection("localhost", 21, "alegeus", "a");
#else
                _remoteCobraFtpConnection = = new SFtpConnection("ftp.wealthcareadmin.com", 21, "benefledi", "VzVR4s4y");;
#endif

                return _remoteCobraFtpConnection;
            }
        }

        #endregion


        #region CobraFileProcessingPaths

        public static string cobraImportRoot => $"{processingRoot}/COBRA IMPORTS";

        public static string cobraImportHoldingRoot => $"{processingRoot}/COBRA IMPORTS/Holding";

        public static string cobraImportTestFilesRoot => $"{processingRoot}/COBRA IMPORTS/COBRA_testfiles";

        public static string cobraImportHoldingPreparedQbRoot => $"{processingRoot}/COBRA IMPORTS/Holding/PreparedQB";

        public static string cobraImportArchiveDoneRoot => $"{processingRoot}/COBRA IMPORTS/Archive - Done";

        public static string cobraImportArchiveEmptyRoot => $"{processingRoot}/COBRA IMPORTS/Archive - Empty";

        public static string cobraImportArchiveErrorRoot => $"{processingRoot}/COBRA IMPORTS/Archive - Error";

        public static string cobraImportHoldingDecryptRoot => $"{processingRoot}/COBRA IMPORTS/Holding/ToDecrypt";

        public static string[] cobraIgnoreFtpSourceDirs
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

        public static string[] alegeusIgnoreFtpSourceDirs
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

        public static string alegeusFileHeadersRoot => $"{processingRoot}/AutomatedHeaderV1_Files";

        public static string alegeusFileHeadersArchiveRoot => $"{processingRoot}/AutomatedHeaderV1_Files/Archive";

        public static string alegeusFilesPreCheckRoot => $"{processingRoot}/To_Alegeus_Pre_Process";
        public static string alegeusFilesPreCheckOKRoot => $"{processingRoot}/To_Alegeus_Pre_Process/Check_OK";
        public static string alegeusFilesPreCheckFailRoot => $"{processingRoot}/To_Alegeus_Pre_Process/Check_FAIL";

        public static string alegeusFilesPreCheckHoldAllRoot => $"{processingRoot}/To_Alegeus_Pre_Process/HoldALL";

        public static string alegeusFilesFTPHoldingRoot => $"{processingRoot}/To_Alegeus_FTP_Holding";

        #endregion

        #region ErrorLogPaths

        public static string alegeusErrorLogMbiFilesRoot => $"{processingRoot}/AlegeusErrorLog/mbiFiles";

        public static string alegeusErrorLogMbiFilesArchiveRoot => $"{processingRoot}/AlegeusErrorLog/mbiFiles/Archive";

        public static string alegeusErrorLogResFilesRoot => $"{processingRoot}/AlegeusErrorLog/resFiles";

        public static string alegeusErrorLogResFilesArchiveRoot => $"{processingRoot}/AlegeusErrorLog/resFiles/Archive";

        #endregion
    }
}