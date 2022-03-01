using System.Runtime.InteropServices;

namespace CoreUtils.Classes
{
    [Guid("EAA4976A-ABCD-4BC5-BC0B-1234F4C3C83F")]
    [ComVisible(true)]
    public enum FileOperation
    {
        Move = 1,
        Copy,
        Delete,
        DeleteIfExists,
        Read
    }

    [Guid("EAA4976A-DCBA-4BC5-BC0B-1234F4C3C83F")]
    [ComVisible(true)]
    public enum FtpFileOperation
    {
        Download,
        Upload,
        DownloadAndDelete,
        UploadAndDelete,
        DeleteRemoteFileIfExists,
        ReadRemoteFile
    }


    [Guid("EAA4976A-45C3-4BC5-1ACD-1234F4C3C83F")]
    [ComVisible(true)]
    public enum DbOperation
    {
        ExecuteScalar = 1,
        ExecuteReader,
        ExecuteNonQuery
    }

    public enum DirectoryIterateType
    {
        [Utils.DisplayText("DirectoryIterateType.Files")]
        Files = 1,

        [Utils.DisplayText("DirectoryIterateType.Directories")]
        Directories
    }

    public enum FormatType
    {
        [Utils.DisplayText("")] Any = 0,
        [Utils.DisplayText("Number")] Number,
        [Utils.DisplayText("Double")] Double,
        [Utils.DisplayText("YYYYMMDD")] IsoDate,

        [Utils.DisplayText("YYYYMMDD HHMMNNSS")]
        IsoDateTime,
        [Utils.DisplayText("Yes Or No")] YesNo,
        [Utils.DisplayText("True Or False")] TrueFalse
    }

    public enum HeaderType
    {
        [Utils.DisplayText("HeaderType.NotApplicable")]
        NotApplicable = 0,
        [Utils.DisplayText("HeaderType.Own")] Own,
        [Utils.DisplayText("HeaderType.Old")] Old,
        [Utils.DisplayText("HeaderType.New")] New,

        [Utils.DisplayText("HeaderType.NoChange")]
        NoChange
    }

    public enum PlatformType
    {
        Unknown = 0,

        [Utils.DisplayText("Platform.Alegeus")]
        Alegeus,

        Cobra
        //
    }

    public enum FileCheckType
    {
        [Utils.DisplayText("FileCheckType.FormatOnly")]
        FormatOnly = 1,

        [Utils.DisplayText("FileCheckType.AllData")]
        AllData
        //
    }
    public enum OperationResult
    {
        [Utils.DisplayText("OperationResult.Ok")]
        Ok = 1,

        [Utils.DisplayText("OperationResult.PartialFail")]
        PartialFail,
        
        [Utils.DisplayText("OperationResult.CompleteFail")]
        CompleteFail,

        [Utils.DisplayText("OperationResult.ProcessingError")]
        ProcessingError,



    }

    public enum EdiFileFormat
    {
        [Utils.DisplayText("EdiFileFormat.Unknown")]
        Unknown = 0,

        //
        [Utils.DisplayText("EdiFileFormat.Cobra.QB")]
        CobraQb,

        [Utils.DisplayText("EdiFileFormat.Cobra.NPM")]
        CobraNpm,

        [Utils.DisplayText("EdiFileFormat.Cobra.SPM")]
        CobraSpm,

        //
        [Utils.DisplayText("EdiFileFormat.Alegeus.AlegeusHeader")]
        AlegeusHeader,

        [Utils.DisplayText("EdiFileFormat.Alegeus.Demographics")]
        AlegeusDemographics,

        [Utils.DisplayText("EdiFileFormat.Alegeus.Enrollment")]
        AlegeusEnrollment,

        [Utils.DisplayText("EdiFileFormat.Alegeus.CardCreation")]
        AlegeusCardCreation,

        [Utils.DisplayText("EdiFileFormat.Alegeus.Deposit")]
        AlegeusEmployeeDeposit,

        [Utils.DisplayText("EdiFileFormat.Alegeus.DependentDemographics")]
        AlegeusDependentDemographics,

        [Utils.DisplayText("EdiFileFormat.Alegeus.DependentLink")]
        AlegeusDependentLink,

        [Utils.DisplayText("EdiFileFormat.Alegeus.EmployeeHrInfo")]
        AlegeusEmployeeHrInfo,

        [Utils.DisplayText("EdiFileFormat.Alegeus.CardStatusChange")]
        AlegeusCardStatusChange,

        [Utils.DisplayText("EdiFileFormat.Alegeus.EmployerSplitPlan")]
        AlegeusEmployerSplitPlan,

        [Utils.DisplayText("EdiFileFormat.Alegeus.EmployerStandardPlan")]
        AlegeusEmployerStandardPlan,

        [Utils.DisplayText("EdiFileFormat.Alegeus.EmployerLogicalAccount")]
        AlegeusEmployerLogicalAccount,

        [Utils.DisplayText("EdiFileFormat.Alegeus.EmployerDemographics")]
        AlegeusEmployerDemographics,

        [Utils.DisplayText("EdiFileFormat.Alegeus.Adjudication")]
        AlegeusAdjudication,

        [Utils.DisplayText("EdiFileFormat.Alegeus.CoverageMcc")]
        AlegeusCoverageMcc,

        [Utils.DisplayText("EdiFileFormat.Alegeus.ImportRecordForExport")]
        AlegeusImportRecordForExport,

        [Utils.DisplayText("EdiFileFormat.Alegeus.CoverageOption")]
        AlegeusCoverageOption,

        [Utils.DisplayText("EdiFileFormat.Alegeus.NewEmployeeId")]
        AlegeusNewEmployeeId,

        [Utils.DisplayText("EdiFileFormat.Alegeus.CoverageGeneralSetup")]
        AlegeusCoverageGeneralSetup,

        [Utils.DisplayText("EdiFileFormat.Alegeus.EmployeeAutoReview")]
        AlegeusEmployeeAutoReview,

        [Utils.DisplayText("EdiFileFormat.Alegeus.EmployerPhysicalAccount")]
        AlegeusEmployerPhysicalAccount,

        [Utils.DisplayText("EdiFileFormat.Alegeus.EmployerDeposit")]
        AlegeusEmployerDeposit,

        //
        [Utils.DisplayText("EdiFileFormat.Alegeus.CardCreation")]
        AlegeusResultsHeader,

        //
        [Utils.DisplayText("EdiFileFormat.Alegeus.ResultsDemographics")]
        AlegeusResultsDemographics,

        [Utils.DisplayText("EdiFileFormat.Alegeus.ResultsEnrollment")]
        AlegeusResultsEnrollment,

        [Utils.DisplayText("EdiFileFormat.Alegeus.ResultsDependentLink")]
        AlegeusResultsDependentLink,

        [Utils.DisplayText("EdiFileFormat.Alegeus.ResultsDebitCardImport")]
        AlegeusResultsCardCreation,

        [Utils.DisplayText("EdiFileFormat.Alegeus.ResultsDeposit")]
        AlegeusResultsEmployeeDeposit,

        [Utils.DisplayText("EdiFileFormat.Alegeus.ResultsEmployeeHrInfo")]
        AlegeusResultsEmployeeHrInfo,

        [Utils.DisplayText("EdiFileFormat.Alegeus.ResultsDependentDemographics")]
        AlegeusResultsDependentDemographics,
        AlegeusEmployeeCardFees,
        AlegeusResultsEmployeeCardFees
    }
}