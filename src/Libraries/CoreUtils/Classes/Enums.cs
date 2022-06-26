namespace CoreUtils.Classes
{

    public enum FileOperation
    {
        Move = 1,
        Copy,
        Delete,
        DeleteIfExists,
        Read,
    }


    public enum FtpFileOperation
    {
        Download,
        Upload,
        DownloadAndDelete,
        UploadAndDelete,
        DeleteRemoteFileIfExists,
        ReadRemoteFile,
    }


    public enum DbOperation
    {
        ExecuteScalar = 1,
        ExecuteReader,
        ExecuteNonQuery,
    }

    public enum DirectoryIterateType
    {
        [Utils.DisplayText("DirectoryIterateType.Files")]
        Files = 1,

        [Utils.DisplayText("DirectoryIterateType.Directories")]
        Directories,
    }

    public enum FormatType
    {
        [Utils.DisplayText("")] Any = 0,
        [Utils.DisplayText("String")] String,
        [Utils.DisplayText("SSN")] SSN,
        [Utils.DisplayText("Email")] Email,
        [Utils.DisplayText("Zip")] Zip,
        [Utils.DisplayText("Phone")] Phone,
        [Utils.DisplayText("AlphaNumeric")] AlphaNumeric,
        [Utils.DisplayText("AlphaOnly")] AlphaOnly,
        [Utils.DisplayText("AlphaAndDashes")] AlphaAndDashes,

        [Utils.DisplayText("NumbersAndDashes")]
        NumbersAndDashes,
        [Utils.DisplayText("Integer")] Integer,
        [Utils.DisplayText("Double")] Double,
        [Utils.DisplayText("one decimal, no commas, #0.00")] CobraMoney,
        [Utils.DisplayText("FixedConstant")] FixedConstant,
        [Utils.DisplayText("YYYYMMDD")] IsoDate,
        [Utils.DisplayText("MM/DD/YYYY")] CobraDate,

        [Utils.DisplayText("YYYYMMDD HHMMNNSS")] IsoDateTime,
        [Utils.DisplayText("MM/DD/YYYY HH:mm AM")] CobraDateTime,
        [Utils.DisplayText("Yes Or No")] YesNo,
        [Utils.DisplayText("YES Or NO")] CobraYesNo,
        [Utils.DisplayText("True Or False")] TrueFalse,
    }

    public enum HeaderType
    {
        [Utils.DisplayText("HeaderType.NotApplicable")]
        NotApplicable = 0,
        [Utils.DisplayText("HeaderType.Own")] Own,
        [Utils.DisplayText("HeaderType.Old")] Old,
        [Utils.DisplayText("HeaderType.New")] New,

        [Utils.DisplayText("HeaderType.SegmentedFunding")]
        SegmentedFunding,

        [Utils.DisplayText("HeaderType.NoChange")]
        NoChange,
    }

    public enum PlatformType
    {
        Unknown = 0,

        [Utils.DisplayText("Platform.Alegeus")]
        Alegeus,

        Cobra,
        //
    }
    public enum Channel
    {
        Unknown = 0,

        [Utils.DisplayText("Channel.SFTP")]
        SFtp
    }

    public enum FileCheckType
    {
        [Utils.DisplayText("FileCheckType.FormatOnly")]
        FormatOnly = 1,

        [Utils.DisplayText("FileCheckType.AllData")]
        AllData,
        //
    }

    public enum FileCheckProcessType
    {
        [Utils.DisplayText("FileCheckProcessType.MoveToDestDirectories")]
        MoveToDestDirectories = 1,

        [Utils.DisplayText("FileCheckProcessType.ReturnResults")]
        ReturnResults,
        //
    }

    public enum OperationResultType
    {
        [Utils.DisplayText("OperationResultType.Unknown")]
        Unknown = 0,

        [Utils.DisplayText("OperationResultType.Ok")]
        Ok = 1,

        [Utils.DisplayText("OperationResultType.PartialFail")]
        PartialFail,

        [Utils.DisplayText("OperationResultType.CompleteFail")]
        CompleteFail,

        [Utils.DisplayText("OperationResultType.ProcessingError")]
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
        [Utils.DisplayText("EdiFileFormat.BrokerCommission.QBRawData")]
        BrokerCommissionQBRawData,

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

        [Utils.DisplayText("EdiFileFormat.Alegeus.AlegeusEmployeeCardFees")]
        AlegeusEmployeeCardFees,

        [Utils.DisplayText("EdiFileFormat.Alegeus.AlegeusResultsEmployeeCardFees")]
        AlegeusResultsEmployeeCardFees,
    }

}