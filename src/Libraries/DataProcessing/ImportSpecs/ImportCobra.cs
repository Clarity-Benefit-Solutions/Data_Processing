using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CoreUtils;
using CoreUtils.Classes;
using Org.BouncyCastle.Crypto.Engines;
using Sylvan.Data.Csv;
//using ETLBox.Connection;
//using ETLBox.DataFlow;
//using ETLBox.DataFlow.Connectors;
using SylvanCsvDataReader = Sylvan.Data.Csv.CsvDataReader;

// ReSharper disable All


namespace DataProcessing
{

    public static partial class Import
    {

        public static Boolean IsCobraImportFile(string srcFilePath)
        {
            // COBRA files, first line starts with [VERSION],
            string contents = FileUtils.GetFlatFileContents(srcFilePath, 1);

            if (contents.Contains("[VERSION],"))
            {
                return true;
            }


            return false;
        }
        public static Boolean IsCobraImportQbFile(string srcFilePath)
        {
            Boolean isCobraFile = IsCobraImportFile(srcFilePath);

            // toDo: how to distincguih QB and NPM files?
            return isCobraFile;
        }

        public static string GetCobraFileVersionNoFromFile(string srcFilePath)
        {
            string versionNo = "";

            // convert excel files to csv to check
            if (FileUtils.IsExcelFile(srcFilePath))
            {
                var csvFilePath = Path.GetTempFileName() + ".csv";

                FileUtils.ConvertExcelFileToCsv(srcFilePath, csvFilePath,
                    Import.GetPasswordsToOpenExcelFiles(srcFilePath),
                    null,
                    null);

                srcFilePath = csvFilePath;
            }

            int rowNo = 0;
            string firstLine = null;

            using var inputFile = new StreamReader(srcFilePath);
            while (true)
            {
                string line = inputFile.ReadLine()!;
                rowNo++;

                // if we reached end of file, return what we got
                if (line == null)
                {
                    return versionNo;
                }

                string[] columns = ImpExpUtils.GetCsvColumnsFromText(line);
                var columnCount = columns.Length;
                if (columnCount == 0)
                {
                    continue;
                }

                // take first non blank line with 2 columns
                if (Utils.IsBlank(firstLine) && columnCount >= 2 && columns[0].ToUpperInvariant() == "[VERSION]")
                {
                    versionNo = columns[1];
                    return versionNo;
                }
            }
        }

        public static TypedCsvSchema GetCobraFileImportMappings(string rowType, string versionNo, Boolean forImport = true)
        {
            var mappings = new TypedCsvSchema();

            Boolean isResultFile = false;// GetAlegeusFileFormatIsResultFile(fileFormat);

            // add src file path as res_file_name and mbi_file_name
            if (forImport)
            {
                // tracks rowNo in submitted file as a single file can jhave than format lines
                mappings.Add(new TypedCsvColumn("source_row_no", "source_row_no", FormatType.String, null, 0, 0, 0, 0));
                //
                if (isResultFile)
                {
                    //
                    // add duplicated entire line as first column to ensure it is parsed correctly
                    mappings.Add(new TypedCsvColumn("error_row", "error_row", FormatType.String, null, 0, 0, 0, 0));

                    // source filename
                    mappings.Add(new TypedCsvColumn("cobra_res_file_name", "cobra_res_file_name", FormatType.String, null, 0, 0, 0,
                        0));

                }
                else
                {
                    // add duplicated entire line as first column to ensure it isd parsed correctly
                    mappings.Add(new TypedCsvColumn("data_row", "data_row", FormatType.String, null, 0, 0, 0, 0));
                    // source filename
                    mappings.Add(new TypedCsvColumn("cobra_file_name", "cobra_file_name", FormatType.String, null, 0, 0, 0,
                        0));
                }
            }

            // the row_type
            mappings.Add(new TypedCsvColumn("row_type", "row_type", FormatType.String, null, 0, 0, 0, 0));

            switch (rowType)
            {
                case "[VERSION]":
                    mappings.Add(new CobraTypedCsvColumn("VersionNumber", FormatType.Integer, 0, 1, "Use “1.0” for this import specification", "1.0|1.1|1.2"));
                    break;

                #region "QB"
                //////////////////////////////
                // QB
                /// //////////////////////////////
                case "[QB]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("ClientName", FormatType.String, 100, 1, "The unique Client name assigned in COBRA & Direct Billing "));
                            mappings.Add(new CobraTypedCsvColumn("ClientDivisionName", FormatType.String, 50, 1, "The unique Client Division name assigned in COBRA & Direct Billing. If there are no Divisions, then use the ClientName."));
                            mappings.Add(new CobraTypedCsvColumn("Salutation", FormatType.String, 35, 0, "", "MR|MRS|MS|MISS|DR|"));
                            mappings.Add(new CobraTypedCsvColumn("FirstName", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("MiddleInitial", FormatType.String, 1, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("LastName", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("SSN", FormatType.SSN, 9, 1, "Social Security Number"));
                            mappings.Add(new CobraTypedCsvColumn("IndividualID", FormatType.String, 50, 0, "Optional, used to store Employee ID #s or any other type of secondary identification"));
                            mappings.Add(new CobraTypedCsvColumn("Email", FormatType.Email, 100, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("Phone", FormatType.Phone, 10, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("Phone2", FormatType.Phone, 10, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("Address1", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("Address2", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("City", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("StateOrProvince", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("PostalCode", FormatType.String, 35, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("Country", FormatType.String, 50, 0, "Leave blank if the QB resides in the USA"));
                            mappings.Add(new CobraTypedCsvColumn("PremiumAddressSameAsPrimary", FormatType.CobraYesNo, 0, 1, "Always set to TRUE"));
                            mappings.Add(new CobraTypedCsvColumn("PremiumAddress1", FormatType.String, 50, 0, "[Deprecated – do not use]"));
                            mappings.Add(new CobraTypedCsvColumn("PremiumAddress2", FormatType.String, 50, 0, "[Deprecated – do not use]"));
                            mappings.Add(new CobraTypedCsvColumn("PremiumCity", FormatType.String, 50, 0, "[Deprecated – do not use]"));
                            mappings.Add(new CobraTypedCsvColumn("PremiumStateOrProvince", FormatType.String, 50, 0, "[Deprecated – do not use]"));
                            mappings.Add(new CobraTypedCsvColumn("PremiumPostalCode", FormatType.String, 35, 0, "[Deprecated – do not use]"));
                            mappings.Add(new CobraTypedCsvColumn("PremiumCountry", FormatType.String, 50, 0, "[Deprecated – do not use]"));
                            mappings.Add(new CobraTypedCsvColumn("Sex", FormatType.String, 1, 1, "F, M, U", "F|M|U"));
                            mappings.Add(new CobraTypedCsvColumn("DOB", FormatType.CobraDate, 0, 1, "Date of Birth – needed for age based plans and also for the Medicare Letter"));
                            mappings.Add(new CobraTypedCsvColumn("TobaccoUse", FormatType.String, 35, 1, "YES, NO, UNKNOWN"));
                            mappings.Add(new CobraTypedCsvColumn("EmployeeType", FormatType.String, 35, 1, "FTE, PTE, H1B, CONSULTANT, SABBATICAL, PROBATIONARY, CONTINGENT, TELECOMMUTING, INTERN, GROUPLEADER, ASSOCIATE, PARTNER, UNKNOWN"));
                            mappings.Add(new CobraTypedCsvColumn("EmployeePayrollType", FormatType.String, 35, 1, "EXEMPT, NONEXEMPT, UNKNOWN"));
                            mappings.Add(new CobraTypedCsvColumn("YearsOfService", FormatType.Integer, 0, 0, "Not used currently and only informational"));
                            mappings.Add(new CobraTypedCsvColumn("PremiumCouponType", FormatType.String, 35, 1, "PREMIUMNOTICE, COUPONBOOK, NONE"));
                            mappings.Add(new CobraTypedCsvColumn("UsesHCTC", FormatType.CobraYesNo, 0, 1, "TRUE if this QB uses the Health Care Tax Credit (HCTC) system"));
                            mappings.Add(new CobraTypedCsvColumn("Active", FormatType.YesNo, 0, 1, "Should always be set to TRUE", "TRUE|YES|1|Yes|True"));
                            mappings.Add(new CobraTypedCsvColumn("AllowMemberSSO", FormatType.CobraYesNo, 1, 1, "TRUE or FALSE"));
                            mappings.Add(new CobraTypedCsvColumn("BenefitGroup", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("AccountStructure", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("ClientSpecificData", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("SSOIdentifier", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("PlanCategory", FormatType.String, 0, 0, "Separate multiple entries with a comma (each individual entry cannot exceed 100 characters). Angle Brackets “< >” are not permitted."));
                            //
                            break;
                    }
                    break;
                case "[QBEVENT]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("EventType", FormatType.String, 35, 1, "DIVORCELEGASSEPARATION,DEATH,INELIGIBLEDEPENDENT,MEDICARE,TERMINATION,RETIREMENT,REDUCTIONINHOURS-STATUSCHANGE,REDUCTIONINFORCE,BANKRUPTCY,STATECONTINUATION,LOSSOFELIGIBILITY,REDUCTIONINHOURS-ENDOFLEAVE,WORKSTOPPAGE,USERRA-TERMINATION,USERRA-REDUCTIONINHOURS,INVOLUNTARYTERMINATION,TERMINATIONWITHSEVERANCE,RETIREEBANKRUPTCY"));
                            mappings.Add(new CobraTypedCsvColumn("EventDate", FormatType.CobraDate, 0, 0, "The qualifying event date that the event type occurred on. Do not adjust for plan benefit termination types, just use the actual date of the event."));
                            mappings.Add(new CobraTypedCsvColumn("EnrollmentDate", FormatType.CobraDate, 0, 1, "Original enrollment date of the member’s current medical plan - used for HIPAA certificate to show length of continuous coverage"));
                            mappings.Add(new CobraTypedCsvColumn("EmployeeSSN", FormatType.SSN, 9, 1, "The original employee’s SSN. Required if the event type is a dependent type event, such as DEATH, DIVORCELEGALSEPARATION, INELIGIBLEDEPENDENT or MEDICARE."));
                            mappings.Add(new CobraTypedCsvColumn("EmployeeName", FormatType.String, 100, 1, "The original employee’s name. Required if the event type is a dependent type event, such as DEATH, DIVORCELEGALSEPARATION, INELIGIBLEDEPENDENT or MEDICARE."));
                            mappings.Add(new CobraTypedCsvColumn("SecondEventOriginalFDOC", FormatType.CobraDate, 1, 1, "DEPRECATED – any value will be ignored"));
                            //
                            break;
                    }
                    break;
                //

                case "[QBLEGACY]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("DateSpecificRightsNoticeWasPrinted", FormatType.CobraDate, 0, 1, "The date that the original Specific Rights Notice was printed (or postmarked)"));
                            mappings.Add(new CobraTypedCsvColumn("PostmarkDateOfElection", FormatType.CobraDate, 0, 0, "If the QB has elected, then the date of that election (or the postmark of the election form receipt) should be entered as the postmark date of election form"));
                            mappings.Add(new CobraTypedCsvColumn("IsPaidThroughLastDayOfCOBRA", FormatType.CobraYesNo, 0, 1, "If the QB is paid all the way through the end of COBRA then set this to TRUE, otherwise FALSE"));
                            mappings.Add(new CobraTypedCsvColumn("NextPremiumOwedMonth", FormatType.Integer, 0, 1, "The month (1-12) of the next payment that is owed"));
                            mappings.Add(new CobraTypedCsvColumn("NextPremiumOwedYear", FormatType.Integer, 0, 1, "The year (for example 2009) of the next payment that is owed"));
                            mappings.Add(new CobraTypedCsvColumn("NextPremiumOwedAmountReceived", FormatType.CobraMoney, 0, 1, "Always set to 0"));
                            mappings.Add(new CobraTypedCsvColumn("SendTakeoverLetter", FormatType.CobraYesNo, 0, 1, "Set to TRUE if a Takeover Letter is to be sent to this legacy QB. Takeover Letters are typically sent out when a QB is switching from one TPA to another."));
                            mappings.Add(new CobraTypedCsvColumn("IsConversionLetterSent", FormatType.CobraYesNo, 0, 1, "Set to TRUE if this Legacy QB has already received a Conversion Letter. Conversion Letters are sent out 180 days before the end of COBRA explaining the QB’s right to convert their coverage."));
                            mappings.Add(new CobraTypedCsvColumn("SendDODSubsidyExtension", FormatType.CobraYesNo, 0, 1, "Deprecated – any value will be ignored, set to FALSE."));
                            //
                            break;
                    }
                    break;
                case "[QBPLANINITIAL]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("PlanName", FormatType.String, 50, 1, "The unique Client plan name"));
                            mappings.Add(new CobraTypedCsvColumn("CoverageLevel", FormatType.String, 35, 1, "EE,EE+SPOUSE,EE+CHILD,EE+CHILDREN,EE+FAMILY,EE+1,EE+2,SPOUSEONLY,SPOUSE+CHILD,CHILDREN,EE+1Child,EE+2Children,EE+3Children,EE+4Children,EE+5orMoreChildren,EE+Spouse+1Child,EE+Spouse+2Children,EE+Spouse+3Children,EE+Spouse+4Children,EE+Spouse+5orMoreChildren,SPOUSE+1CHILD,SPOUSE+2CHILDREN,SPOUSE+3CHILDREN,SPOUSE+4CHILDREN,SPOUSE+5ORMORECHILDREN,EE+DOMESTICPARTNER,EE1UNDER19,EE+SPOUSE1UNDER19,EE+SPOUSE2UNDER19,EE+CHILDREN1UNDER19,EE+CHILDREN2UNDER19,EE+CHILDREN3UNDER19,EE+FAMILY1UNDER19,EE+FAMILY2UNDER19,EE+FAMILY3UNDER19"));
                            mappings.Add(new CobraTypedCsvColumn("NumberOfUnit", FormatType.CobraMoney, 0, 0, "Sets the # of units for this plan. Required if plan is units based (e.g. Life)."));
                            //
                            break;
                    }
                    break;
                case "[QBPLAN]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("PlanName", FormatType.String, 50, 1, "The unique Client plan name"));
                            mappings.Add(new CobraTypedCsvColumn("StartDate", FormatType.CobraDate, 0, 1, "The start date that the QB will begin coverage on this plan. This should be set to the FDOC for the plan in the field above."));
                            mappings.Add(new CobraTypedCsvColumn("EndDate", FormatType.CobraDate, 0, 0, "Optional, the end date the QB will cease coverage on this plan. This should be set to the LDOC for the plan in the field above."));
                            mappings.Add(new CobraTypedCsvColumn("CoverageLevel", FormatType.String, 35, 1, "EE, EE+SPOUSE, EE+CHILD, EE+CHILDREN, EE+FAMILY, EE+1, EE+2, SPOUSEONLY, SPOUSE+CHILD, CHILDREN,EE + 1Child, EE + 2Children, EE + 3Children, EE + 4Children, EE + 5orMoreChildren, EE + Spouse + 1Child, EE + Spouse + 2Children, EE + Spouse + 3Children, EE + Spouse + 4Children, EE + Spouse + 5orMoreChildren, SPOUSE + 1CHILD, SPOUSE + 2CHILDREN, SPOUSE + 3CHILDREN,SPOUSE + 4CHILDREN, SPOUSE + 5ORMORECHILDREN"));
                            mappings.Add(new CobraTypedCsvColumn("FirstDayOfCOBRA", FormatType.CobraDate, 0, 0, "The First Day of COBRA. Unless you wish to override the calculated FDOC, this field should be left blank. If left blank the system will determine the FDOC based on the EventDate and the plan benefit termination type."));
                            mappings.Add(new CobraTypedCsvColumn("LastDayOfCOBRA", FormatType.CobraDate, 0, 0, "The Last Day of COBRA. This field should be left blank. The system will determine the LDOC based on the FDOC and COBRADurationMonths."));
                            mappings.Add(new CobraTypedCsvColumn("COBRADurationMonths", FormatType.Integer, 0, 0, "The number of months of COBRA is usually determined by the event type. This may be left blank (preferred) and the system will determine the correct number of months. It is typically 18 months, but can be extended on Dependent Event Types and USERRA Event Types."));
                            mappings.Add(new CobraTypedCsvColumn("DaysToElect", FormatType.Integer, 0, 0, "The number of days the QB has to elect coverage under COBRA. This may be left blank (preferred) and the system will determine the correct number of days. It is typically 60 days."));
                            mappings.Add(new CobraTypedCsvColumn("DaysToMake1stPayment", FormatType.Integer, 0, 0, "The number of days the QB has to make their 1st full payment under COBRA. This may be left blank (preferred) and the system will determine the correct number of days. It is typically 45 days."));
                            mappings.Add(new CobraTypedCsvColumn("DaysToMakeSubsequentPayments", FormatType.Integer, 0, 0, "The total number of days the member has to make subsequent payments after their initial payment. This may be left blank (preferred) and the system will determine the correct number of days. It is typically 45 days."));
                            mappings.Add(new CobraTypedCsvColumn("ElectionPostmarkDate", FormatType.CobraDate, 0, 0, "Always leave blank"));
                            mappings.Add(new CobraTypedCsvColumn("LastDateRatesNotified", FormatType.CobraDate, 0, 0, "Always leave blank"));
                            mappings.Add(new CobraTypedCsvColumn("NumberOfUnits", FormatType.CobraMoney, 0, 0, "Sets the # of units for this plan. Required if plan is units based (e.g. Life)."));
                            mappings.Add(new CobraTypedCsvColumn("SendPlanChangeLetterForLegacy", FormatType.CobraYesNo, 1, 1, "Set to TRUE to send a plan change letter after this record is entered."));
                            mappings.Add(new CobraTypedCsvColumn("PlanBundleName", FormatType.String, 50, 0, "Conditionally required for plans that are part of a bundle."));
                            //
                            break;
                    }
                    break;
                case "[QBDEPENDENT]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("SSN", FormatType.SSN, 9, 0, "Social Security Number"));
                            mappings.Add(new CobraTypedCsvColumn("Relationship", FormatType.String, 35, 1, "SPOUSE, CHILD, DOMESTICPARTNER"));
                            mappings.Add(new CobraTypedCsvColumn("Salutation", FormatType.String, 35, 0, "", "MR|MRS|MS|MISS|DR|"));
                            mappings.Add(new CobraTypedCsvColumn("FirstName", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("MiddleInitial", FormatType.String, 1, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("LastName", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("Email", FormatType.Email, 100, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("Phone", FormatType.Phone, 10, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("Phone2", FormatType.Phone, 10, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("AddressSameAsQB", FormatType.CobraYesNo, 1, 1, "Set to TRUE if the dependent’s address is the same as the QB’s address"));
                            mappings.Add(new CobraTypedCsvColumn("Address1", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("Address2", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("City", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("StateOrProvince", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("PostalCode", FormatType.String, 35, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("Country", FormatType.String, 50, 0, "Leave empty if the dependent resides in the USA"));
                            mappings.Add(new CobraTypedCsvColumn("EnrollmentDate", FormatType.CobraDate, 0, 0, "Original enrollment date of the dependent’s medical plan - used for HIPAA certificate"));
                            mappings.Add(new CobraTypedCsvColumn("Sex", FormatType.String, 1, 0, "F, M, U (this is required if the dependent is on a sex based plan that sets rates based on the dependent’s sex)", "F|M|U"));
                            mappings.Add(new CobraTypedCsvColumn("DOB", FormatType.CobraDate, 0, 0, "Date of birth (this is required if the dependent is on an age based plan that sets rates based on the dependent’s age)"));
                            mappings.Add(new CobraTypedCsvColumn("IsQMCSO", FormatType.CobraYesNo, 1, 0, "TRUE if the dependent is under a Qualified Medical Child Support Order (QMCSO)"));
                            //
                            break;
                    }
                    break;
                case "[QBDEPENDENTPLANINITIAL]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("PlanName", FormatType.String, 50, 1, "The unique Client plan name"));
                            //
                            break;
                    }
                    break;
                case "[QBDEPENDENTPLAN]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("PlanName", FormatType.String, 50, 1, "The unique Client plan name"));
                            mappings.Add(new CobraTypedCsvColumn("StartDate", FormatType.CobraDate, 0, 1, "The start date of the dependent on the plan. This should be set to the FDOC for the plan in the field above."));
                            mappings.Add(new CobraTypedCsvColumn("EndDate", FormatType.CobraDate, 0, 0, "The end date of the dependent on the plan. This should be set to the LDOC for the plan in the field above unless it is known that the dependent will be ending the plan before LDOC."));
                            mappings.Add(new CobraTypedCsvColumn("UsesFirstDayOfCoverage", FormatType.CobraYesNo, 1, 0, "Set to TRUE if the dependent’s plan starts on the QB’s FDOC. Default value is TRUE."));
                            //
                            break;
                    }
                    break;
                case " [QBNOTE]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("NoteType", FormatType.String, 35, 1, "MANUAL, AUTONOTE"));
                            mappings.Add(new CobraTypedCsvColumn("DateTime", FormatType.CobraDateTime, 0, 1, "Date and time of the note"));
                            mappings.Add(new CobraTypedCsvColumn("NoteText", FormatType.String, 2000, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("UserName", FormatType.String, 0, 0, "Always leave blank"));
                            //
                            break;
                    }
                    break;
                case "[QBSUBSIDYSCHEDULE]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("InsuranceType", FormatType.String, 35, 1, "MEDICAL, DENTAL, VISION, PHARMACY, FSA, HCRA, EAP, GAP, 401k, LIFE, NULIFE, MSA, PBA, HSA, NUOTHER1, NUOTHER2, GRPLIFE, NUGRPLIFE, VOLLIFE, NUVOLLIFE, CANCER, MERP, DEPLIFE1, DEPLIFE2, DEPLIFE3, NUDEPLIFE1, NUDEPLIFE2, NUDEPLIFE3, MEDSTURIDER1, MEDSTURIDER2, MEDSTURIDER3, LTD, AD&D, CHIROPRACTIC, VEBA, CUSTOMBILLING, LTDNONUNITBASED, LTDUNITBASED, STDNONUNITBASED, STDUNITBASED, CRITICALILLNESS, ACCIDENTNONUNITBASED, ACCIDENTUNITBASED, VOLUNTARYOTHER, UOTHER1, UOTHER2, UOTHER3"));
                            mappings.Add(new CobraTypedCsvColumn("SubsidyAmountType", FormatType.String, 35, 0, "?", "FLAT or PERCENTAGE is required if RatePeriodSubsidy is False"));
                            mappings.Add(new CobraTypedCsvColumn("StartDate", FormatType.CobraDate, 0, 1, "Start date of the subsidy"));
                            mappings.Add(new CobraTypedCsvColumn("EndDate", FormatType.CobraDate, 0, 1, "End date of the subsidy"));
                            mappings.Add(new CobraTypedCsvColumn("Amount", FormatType.CobraMoney, 0, 0, "Required if RatePeriodSubsidy is False. Amount if FLAT or Percentage if PERCENTAGE of the subsidy.For example, use “50” if it is a 50 % subsidy and the SubsidyAmountType is set to PERCENTAGE."));
                            mappings.Add(new CobraTypedCsvColumn("SubsidyType", FormatType.String, 35, 0, "EMPLOYER (defaults to EMPLOYER)"));
                            mappings.Add(new CobraTypedCsvColumn("RatePeriodSubsidy", FormatType.CobraYesNo, 0, 0, "True = '1', 'Y', 'YES', 'T' or 'TRUE'; False = '0', 'N', 'NO', 'F' or 'FALSE'; Blank value. If RatePeriodSubsidy = blank, True or False value will be applied after import by the system according to the existing logic. RatePeriodSubsidy is not allowed to import on Employer Portal. True or False value will be applied after import by the system according to the existing logic."));
                            //
                            break;
                    }
                    break;
                case "[QBSTATEINSERTS]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("CASRINSERT", FormatType.CobraYesNo, 0, 0, "California Specific Rights Letter Insert. Default is FALSE."));
                            mappings.Add(new CobraTypedCsvColumn("CTSRINSERT", FormatType.CobraYesNo, 0, 0, "Connecticut Specific Rights Letter Insert. Default is FALSE."));
                            mappings.Add(new CobraTypedCsvColumn("MNLIFEINSERT", FormatType.CobraYesNo, 0, 0, "Minnesota Life Specific Rights Letter Insert. Default is FALSE."));
                            mappings.Add(new CobraTypedCsvColumn("MNCONTINSERT", FormatType.CobraYesNo, 0, 0, "Minnesota Continuation Specific Rights Insert. Default is FALSE."));
                            mappings.Add(new CobraTypedCsvColumn("ORSRINSERT", FormatType.CobraYesNo, 0, 0, "Oregon Specific Rights Letter Insert. Default is FALSE."));
                            mappings.Add(new CobraTypedCsvColumn("TXSRINSERT", FormatType.CobraYesNo, 0, 0, "Texas Specific Rights Letter Insert. Default is FALSE."));
                            mappings.Add(new CobraTypedCsvColumn("NY-SR INSERT", FormatType.CobraYesNo, 0, 0, "New York State Continuation. Default is FALSE."));
                            mappings.Add(new CobraTypedCsvColumn("VEBASRINSERT", FormatType.CobraYesNo, 0, 0, "VEBA Specific Rights Letter Insert. Default is FALSE."));
                            mappings.Add(new CobraTypedCsvColumn("ILSRINSERT", FormatType.CobraYesNo, 0, 0, "Illinois State Continuation. Default is FALSE."));
                            mappings.Add(new CobraTypedCsvColumn("RISRINSERT", FormatType.CobraYesNo, 0, 0, "Rhode Island State Continuation. Default is FALSE."));
                            mappings.Add(new CobraTypedCsvColumn("GASRINSERT", FormatType.CobraYesNo, 0, 0, "Georgia State Continuation. Default is FALSE. Default is FALSE."));
                            mappings.Add(new CobraTypedCsvColumn("VASRINSERT", FormatType.CobraYesNo, 0, 0, "Commonwealth of VA Continuation. Default is FALSE."));
                            //
                            break;
                    }
                    break;
                case "[QBDISABILITYEXTENSION]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("DisabilityApproved", FormatType.CobraYesNo, 1, 1, "Set to TRUE if the Disability Extension is approved or FALSE if it is not"));
                            mappings.Add(new CobraTypedCsvColumn("PostmarkOfDisabilityExtension", FormatType.CobraDate, 0, 1, "Set to the postmark date that the Disability Extension when it was received"));
                            mappings.Add(new CobraTypedCsvColumn("DateDisabled", FormatType.CobraDate, 0, 1, "The date the member was disabled"));
                            mappings.Add(new CobraTypedCsvColumn("DenialReason", FormatType.String, 35, 0, "DISABILITYDATE, SUBMISSIONDATE – required if DisabilityApproved is FALSE"));
                            //
                            break;
                    }
                    break;
                case "[QBPLANMEMBERSPECIFICRATEINITIAL]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("PlanName", FormatType.String, 50, 1, "The unique Client plan name"));
                            mappings.Add(new CobraTypedCsvColumn("Rate", FormatType.CobraMoney, 0, 1, "The amount of the member specific rate"));
                            //
                            break;
                    }
                    break;
                case "[QBPLANMEMBERSPECIFICRATE]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("PlanName", FormatType.String, 50, 1, "The unique Client plan name"));
                            mappings.Add(new CobraTypedCsvColumn("StartDate", FormatType.CobraDate, 0, 1, "The start date of the dependent on the plan. This should be set to the FDOC for the plan in the field above."));
                            mappings.Add(new CobraTypedCsvColumn("EndDate", FormatType.CobraDate, 0, 1, "End date of the member specific rate"));
                            mappings.Add(new CobraTypedCsvColumn("Rate", FormatType.CobraMoney, 0, 1, "The amount of the member specific rate"));
                            //
                            break;
                    }
                    break;
                case "[QBPLANTERMREINSTATE]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("PlanName", FormatType.String, 50, 1, "The unique Client plan name"));
                            mappings.Add(new CobraTypedCsvColumn("TermOrReinstate", FormatType.String, 20, 1, "TERMINATE or REINSTATE"));
                            mappings.Add(new CobraTypedCsvColumn("EffectiveDate", FormatType.CobraDate, 0, 1, "Effective date of the term or reinstate"));
                            mappings.Add(new CobraTypedCsvColumn("Reason", FormatType.String, 35, 1, "Reason for the termination or reinstatement"));
                            //
                            break;
                    }
                    break;
                case "[QBLETTERATTACHMENT]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("LetterAttachmentName", FormatType.String, 100, 1, "The unique name of letter attachment."));
                            //
                            break;
                    }
                    break;
                case "[QBLOOKUP]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("ClientName", FormatType.String, 100, 1, "N/A"));
                            mappings.Add(new CobraTypedCsvColumn("SSN", FormatType.SSN, 9, 1, "N/A"));
                            mappings.Add(new CobraTypedCsvColumn("QualifyingEventDate", FormatType.CobraDate, 0, 1, "N/A"));
                            //
                            break;
                    }
                    break;
                case "[MEMBERUSERDEFINEDFIELD]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("UserDefinedFieldName", FormatType.String, 0, 1, "The unique name of the user defined field."));
                            mappings.Add(new CobraTypedCsvColumn("UserDefinedFieldValue", FormatType.String, 2000, 0, "Any provided value, including blank, will be saved."));
                            //
                            break;
                    }
                    break;
                #endregion


                #region "SPM"
                //////////////////////////////
                // SPM
                /// //////////////////////////////
                case "[SPM]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("ClientName", FormatType.String, 100, 1, "The unique Client name assigned in COBRA & Direct Billing "));
                            mappings.Add(new CobraTypedCsvColumn("ClientDivisionName", FormatType.String, 50, 1, "The unique Client Division name assigned in COBRA & Direct Billing. If there are no Divisions, then use the ClientName."));
                            mappings.Add(new CobraTypedCsvColumn("Salutation", FormatType.String, 35, 0, "", "MR|MRS|MS|MISS|DR|"));
                            mappings.Add(new CobraTypedCsvColumn("FirstName", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("MiddleInitial", FormatType.String, 1, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("LastName", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("SSN", FormatType.SSN, 9, 1, "Social Security Number"));
                            mappings.Add(new CobraTypedCsvColumn("IndividualID", FormatType.String, 50, 0, "Optional, used to store Employee ID #s or any other type of secondary identification"));
                            mappings.Add(new CobraTypedCsvColumn("Email", FormatType.Email, 100, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("Phone", FormatType.Phone, 10, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("Phone2", FormatType.Phone, 10, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("Address1", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("Address2", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("City", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("StateOrProvince", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("PostalCode", FormatType.String, 35, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("Country", FormatType.String, 50, 0, "Leave blank if the QB resides in the USA"));
                            mappings.Add(new CobraTypedCsvColumn("Sex", FormatType.String, 1, 1, "F, M, U", "F|M|U"));
                            mappings.Add(new CobraTypedCsvColumn("DOB", FormatType.CobraDate, 0, 1, "Date of Birth – needed for age based plans and also for the Medicare Letter"));

                            mappings.Add(new CobraTypedCsvColumn("BillingStartDate", FormatType.CobraDate, 0, 1, "Date to start billing SPM"));
                            mappings.Add(new CobraTypedCsvColumn("BillingEndDate", FormatType.CobraDate, 0, 1, "Date to end billing SPM"));
                            mappings.Add(new CobraTypedCsvColumn("BillingType", FormatType.String, 35, 1, "RETIREE,PREMIUMPAY, CASHPAY, FMLA, LOANREPAYMENT,LEAVEOFABSENCE, LTDPREMIUM, STDPREMIUM, DISABILITYPREMIUM, CUSTOM"));
                            mappings.Add(new CobraTypedCsvColumn("BillingFrequency", FormatType.String, 35, 1, "MONTHLY, WEEKLY, BIWEEKLY, QUARTERLY, YEARLY, SEMIMONTHLY"));

                            mappings.Add(new CobraTypedCsvColumn("IsCOBRAEligible", FormatType.YesNo, 0, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("IsCOBRAEligibleAtTermination", FormatType.YesNo, 0, 1, ""));

                            mappings.Add(new CobraTypedCsvColumn("SubsequentGracePeriodNrOfDays", FormatType.Integer, 0, 1, "Overrides the Subsequent Grace Period for this SPM"));
                            mappings.Add(new CobraTypedCsvColumn("SPMSubsequentGracePeriodOptionType", FormatType.String, 0, 1, "CLIENTDEFAULT, IGNORE, CUSTOM"));

                            mappings.Add(new CobraTypedCsvColumn("IsLegacy", FormatType.YesNo, 0, 1, "TRUE if this SPM existed in a prior billing system – this is used for conditional text in the SPM Welcome Letter"));

                            mappings.Add(new CobraTypedCsvColumn("TobaccoUse", FormatType.String, 35, 1, "YES, NO, UNKNOWN"));

                            mappings.Add(new CobraTypedCsvColumn("EnrollmentDate", FormatType.CobraDate, 35, 1, "Original enrollment date of the SPM’s plan - used for HIPAA certificate."));
                            mappings.Add(new CobraTypedCsvColumn("EmployeeType", FormatType.String, 35, 1, "FTE, PTE, H1B, CONSULTANT, SABBATICAL, PROBATIONARY, CONTINGENT, TELECOMMUTING, INTERN, GROUPLEADER, ASSOCIATE, PARTNER, UNKNOWN"));
                            mappings.Add(new CobraTypedCsvColumn("EmployeePayrollType", FormatType.String, 35, 1, "EXEMPT, NONEXEMPT, UNKNOWN"));

                            mappings.Add(new CobraTypedCsvColumn("YearsOfService", FormatType.Integer, 0, 0, "Not used currently and only informational"));
                            mappings.Add(new CobraTypedCsvColumn("PremiumCouponType", FormatType.String, 35, 1, "PREMIUMNOTICE, COUPONBOOK, NONE"));

                            mappings.Add(new CobraTypedCsvColumn("Active", FormatType.YesNo, 0, 1, "Should always be set to TRUE", "TRUE|YES|1|Yes|True"));
                            mappings.Add(new CobraTypedCsvColumn("AllowMemberSSO", FormatType.CobraYesNo, 1, 1, "TRUE or FALSE"));

                            mappings.Add(new CobraTypedCsvColumn("BenefitGroup", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("AccountStructure", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("ClientSpecificData", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("SSOIdentifier", FormatType.String, 50, 0, ""));

                            mappings.Add(new CobraTypedCsvColumn("EventDate", FormatType.CobraDate, 0, 1, "Date the SPM’s event occurred. Flexible Billing must be enabled."));
                            mappings.Add(new CobraTypedCsvColumn("InitialGracePeriodDate", FormatType.CobraDate, 0, 0, "Date the SPM’s Initial Grace Period will start. Flexible Billing is enabled or disabled."));
                            mappings.Add(new CobraTypedCsvColumn("BillingPeriodSeedDate", FormatType.CobraDate, 0, 0, "1st payroll date for SPMs with a billing frequency of Semi-Monthly, Weekly, or Bi-Weekly."));
                            mappings.Add(new CobraTypedCsvColumn("SecondBillingPeriodSeedDate", FormatType.CobraDate, 0, 0, "2nd payroll date for SPMs with a billing frequency of Semi-Monthly."));

                            mappings.Add(new CobraTypedCsvColumn("PlanCategory", FormatType.String, 1000, 0, "Separate multiple entries with a comma (each individual entry cannot exceed 100 characters). Angle Brackets “< >” are not permitted."));
                            mappings.Add(new CobraTypedCsvColumn("Invalid", FormatType.YesNo, 0, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("SPMInitialGracePeriodOptionType", FormatType.String, 0, 0, "Initial grace period option – “CUSTOM” or “CLIENT DEFAULT” or “IGNORE”. Not required, if not specified – “CLIENT DEFAULT” is used by default. Flexible Billing must be disabled."));
                            mappings.Add(new CobraTypedCsvColumn("InitialGracePeriodDays", FormatType.Integer, 0, 0, "Required when SPMInitialGracePeriodOptionType is “CUSTOM”. Number of days for the initial grace period that a non-elected SPM will have.Flexible Billing must be disabled."));


                            //
                            break;
                    }
                    break;
                case "[SPMPLAN]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("PlanName", FormatType.String, 50, 1, "The unique Client plan name"));
                            mappings.Add(new CobraTypedCsvColumn("StartDate", FormatType.CobraDate, 0, 1, "The start date that the QB will begin coverage on this plan. This should be set to the FDOC for the plan in the field above."));
                            mappings.Add(new CobraTypedCsvColumn("EndDate", FormatType.CobraDate, 0, 0, "Optional, the end date the QB will cease coverage on this plan. This should be set to the LDOC for the plan in the field above."));
                            mappings.Add(new CobraTypedCsvColumn("CoverageLevel", FormatType.String, 35, 1, "EE, EE+SPOUSE, EE+CHILD, EE+CHILDREN, EE+FAMILY, EE+1, EE+2, SPOUSEONLY, SPOUSE+CHILD, CHILDREN,EE + 1Child, EE + 2Children, EE + 3Children, EE + 4Children, EE + 5orMoreChildren, EE + Spouse + 1Child, EE + Spouse + 2Children, EE + Spouse + 3Children, EE + Spouse + 4Children, EE + Spouse + 5orMoreChildren, SPOUSE + 1CHILD, SPOUSE + 2CHILDREN, SPOUSE + 3CHILDREN,SPOUSE + 4CHILDREN, SPOUSE + 5ORMORECHILDREN"));
                            mappings.Add(new CobraTypedCsvColumn("FirstDayOfCoverage", FormatType.CobraDate, 0, 0, "The First Day of COBRA. Unless you wish to override the calculated FDOC, this field should be left blank. If left blank the system will determine the FDOC based on the EventDate and the plan benefit termination type."));
                            mappings.Add(new CobraTypedCsvColumn("LastDayOfCoverage", FormatType.CobraDate, 0, 0, "The Last Day of COBRA. This field should be left blank. The system will determine the LDOC based on the FDOC and COBRADurationMonths."));
                            mappings.Add(new CobraTypedCsvColumn("LastDateRatesNotified", FormatType.CobraDate, 0, 0, "Always Leave Blank", ""));

                            mappings.Add(new CobraTypedCsvColumn("SendPlanChangeLetterForLegacy", FormatType.CobraYesNo, 1, 1, "Set to TRUE to send a plan change letter after this record is entered."));
                            mappings.Add(new CobraTypedCsvColumn("NumberOfUnits", FormatType.CobraMoney, 0, 0, "Sets the # of units for this plan. Required if plan is units based (e.g. Life)."));
                            mappings.Add(new CobraTypedCsvColumn("PlanBundleName", FormatType.String, 50, 0, "Conditionally required for plans that are part of a bundle."));
                            //
                            break;
                    }
                    break;
                case "[SPMDEPENDENT]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("SSN", FormatType.SSN, 9, 0, "Social Security Number"));
                            mappings.Add(new CobraTypedCsvColumn("Relationship", FormatType.String, 35, 1, "SPOUSE, CHILD, DOMESTICPARTNER"));
                            mappings.Add(new CobraTypedCsvColumn("Salutation", FormatType.String, 35, 0, "", "MR|MRS|MS|MISS|DR|"));
                            mappings.Add(new CobraTypedCsvColumn("FirstName", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("MiddleInitial", FormatType.String, 1, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("LastName", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("Email", FormatType.Email, 100, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("Phone", FormatType.Phone, 10, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("Phone2", FormatType.Phone, 10, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("AddressSameAsSPM", FormatType.CobraYesNo, 1, 1, "Set to TRUE if the dependent’s address is the same as the QB’s address"));
                            mappings.Add(new CobraTypedCsvColumn("Address1", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("Address2", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("City", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("StateOrProvince", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("PostalCode", FormatType.String, 35, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("Country", FormatType.String, 50, 0, "Leave empty if the dependent resides in the USA"));
                            mappings.Add(new CobraTypedCsvColumn("EnrollmentDate", FormatType.CobraDate, 0, 1, "Original enrollment date of the dependent’s medical plan - used for HIPAA certificate"));
                            mappings.Add(new CobraTypedCsvColumn("Sex", FormatType.String, 1, 0, "F, M, U (this is required if the dependent is on a sex based plan that sets rates based on the dependent’s sex)", "F|M|U"));
                            mappings.Add(new CobraTypedCsvColumn("DOB", FormatType.CobraDate, 0, 0, "Date of birth (this is required if the dependent is on an age based plan that sets rates based on the dependent’s age)"));
                            mappings.Add(new CobraTypedCsvColumn("IsQMCSO", FormatType.CobraYesNo, 1, 0, "TRUE if the dependent is under a Qualified Medical Child Support Order (QMCSO)"));
                            //
                            break;
                    }
                    break;
                case "[SPMDEPENDENTPLAN]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("PlanName", FormatType.String, 50, 1, "The unique Client plan name"));
                            mappings.Add(new CobraTypedCsvColumn("StartDate", FormatType.CobraDate, 0, 1, "The start date of the dependent on the plan. This should be set to the FDOC for the plan in the field above."));
                            mappings.Add(new CobraTypedCsvColumn("EndDate", FormatType.CobraDate, 0, 0, "The end date of the dependent on the plan. This should be set to the LDOC for the plan in the field above unless it is known that the dependent will be ending the plan before LDOC."));
                            mappings.Add(new CobraTypedCsvColumn("UsesFirstDayOfCoverage", FormatType.CobraYesNo, 1, 1, "Set to TRUE if the dependent’s plan starts on the QB’s FDOC. Default value is TRUE."));
                            //
                            break;
                    }
                    break;
                case " [SPMNOTE]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("NoteType", FormatType.String, 35, 1, "MANUAL, AUTONOTE"));
                            mappings.Add(new CobraTypedCsvColumn("DateTime", FormatType.CobraDateTime, 0, 1, "Date and time of the note"));
                            mappings.Add(new CobraTypedCsvColumn("NoteText", FormatType.String, 2000, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("UserName", FormatType.String, 0, 0, "Always leave blank", ""));
                            //
                            break;
                    }
                    break;
                case "[SPMSUBSIDYSCHEDULE]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("InsuranceType", FormatType.String, 35, 1, "MEDICAL, DENTAL, VISION, PHARMACY, FSA, HCRA, EAP, GAP, 401k, LIFE, NULIFE, MSA, PBA, HSA, NUOTHER1, NUOTHER2, GRPLIFE, NUGRPLIFE, VOLLIFE, NUVOLLIFE, CANCER, MERP, DEPLIFE1, DEPLIFE2, DEPLIFE3, NUDEPLIFE1, NUDEPLIFE2, NUDEPLIFE3, MEDSTURIDER1, MEDSTURIDER2, MEDSTURIDER3, LTD, AD&D, CHIROPRACTIC, VEBA, CUSTOMBILLING, LTDNONUNITBASED, LTDUNITBASED, STDNONUNITBASED, STDUNITBASED, CRITICALILLNESS, ACCIDENTNONUNITBASED, ACCIDENTUNITBASED, VOLUNTARYOTHER, UOTHER1, UOTHER2, UOTHER3"));
                            mappings.Add(new CobraTypedCsvColumn("SubsidyAmountType", FormatType.String, 35, 0, "?", "FLAT or PERCENTAGE is required if RatePeriodSubsidy is False"));
                            mappings.Add(new CobraTypedCsvColumn("StartDate", FormatType.CobraDate, 0, 1, "Start date of the subsidy"));
                            mappings.Add(new CobraTypedCsvColumn("EndDate", FormatType.CobraDate, 0, 1, "End date of the subsidy"));
                            mappings.Add(new CobraTypedCsvColumn("Amount", FormatType.CobraMoney, 0, 0, "Required if RatePeriodSubsidy is False. Amount if FLAT or Percentage if PERCENTAGE of the subsidy.For example, use “50” if it is a 50 % subsidy and the SubsidyAmountType is set to PERCENTAGE."));
                            mappings.Add(new CobraTypedCsvColumn("SubsidyType", FormatType.String, 35, 0, "EMPLOYER (defaults to EMPLOYER)"));
                            mappings.Add(new CobraTypedCsvColumn("RatePeriodSubsidy", FormatType.CobraYesNo, 0, 0, "True = '1', 'Y', 'YES', 'T' or 'TRUE'; False = '0', 'N', 'NO', 'F' or 'FALSE'; Blank value. If RatePeriodSubsidy = blank, True or False value will be applied after import by the system according to the existing logic. RatePeriodSubsidy is not allowed to import on Employer Portal. True or False value will be applied after import by the system according to the existing logic."));
                            //
                            break;
                    }
                    break;
                case "[SPMPLANMEMBERSPECIFICRATE]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("PlanName", FormatType.String, 50, 1, "The unique Client plan name"));
                            mappings.Add(new CobraTypedCsvColumn("StartDate", FormatType.CobraDate, 0, 1, "The start date of the dependent on the plan. This should be set to the FDOC for the plan in the field above."));
                            mappings.Add(new CobraTypedCsvColumn("EndDate", FormatType.CobraDate, 0, 1, "End date of the member specific rate"));
                            mappings.Add(new CobraTypedCsvColumn("Rate", FormatType.CobraMoney, 0, 1, "The amount of the member specific rate"));
                            //
                            break;
                    }
                    break;
                case "[SPMLETTERATTACHMENT]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("LetterAttachmentName", FormatType.String, 100, 1, "The unique name of letter attachment."));
                            //
                            break;
                    }
                    break;
                case "[SPMLOOKUP]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("ClientName", FormatType.String, 100, 1, "N/A"));
                            mappings.Add(new CobraTypedCsvColumn("SSN", FormatType.SSN, 9, 1, "N/A"));
                            mappings.Add(new CobraTypedCsvColumn("BillingStartDate", FormatType.CobraDate, 0, 1, "N/A"));
                            mappings.Add(new CobraTypedCsvColumn("BillingType", FormatType.String, 35, 1, "RETIREE,PREMIUMPAY, CASHPAY, FMLA, LOANREPAYMENT,LEAVEOFABSENCE, LTDPREMIUM, STDPREMIUM, DISABILITYPREMIUM, CUSTOM"));
                            mappings.Add(new CobraTypedCsvColumn("BillingFrequency", FormatType.String, 35, 1, "MONTHLY, WEEKLY, BIWEEKLY, QUARTERLY, YEARLY, SEMIMONTHLY"));

                            //
                            break;
                    }
                    break;
                #endregion

                #region NPM
                //////////////////////////////
                // NPM
                /// //////////////////////////////
                case "[NPM]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("SSN", FormatType.SSN, 9, 1, "Social Security Number"));
                            mappings.Add(new CobraTypedCsvColumn("IndividualID", FormatType.String, 50, 0, "Optional, used to store Employee ID #s or any other type of secondary identification"));
                            mappings.Add(new CobraTypedCsvColumn("ClientName", FormatType.String, 100, 1, "The unique Client name assigned in COBRA & Direct Billing "));
                            mappings.Add(new CobraTypedCsvColumn("ClientDivisionName", FormatType.String, 50, 1, "The unique Client Division name assigned in COBRA & Direct Billing. If there are no Divisions, then use the ClientName."));
                            mappings.Add(new CobraTypedCsvColumn("FirstName", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("MiddleInitial", FormatType.String, 1, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("LastName", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("Salutation", FormatType.String, 35, 0, "", "MR|MRS|MS|MISS|DR|"));
                            mappings.Add(new CobraTypedCsvColumn("Email", FormatType.Email, 100, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("Phone", FormatType.Phone, 10, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("Phone2", FormatType.Phone, 10, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("Address1", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("Address2", FormatType.String, 50, 0, ""));
                            mappings.Add(new CobraTypedCsvColumn("City", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("StateOrProvince", FormatType.String, 50, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("PostalCode", FormatType.String, 35, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("Country", FormatType.String, 50, 0, "Leave blank if the NPM resides in the USA"));
                            mappings.Add(new CobraTypedCsvColumn("UsesFamilyInAddress", FormatType.YesNo, 0, 0, "Adds: and Family to Address Labels; Defaults to FALSE"));
                            mappings.Add(new CobraTypedCsvColumn("HasWaivedAllCoverage", FormatType.YesNo, 0, 0, "Defaults to FALSE"));
                            mappings.Add(new CobraTypedCsvColumn("SendGRNotice", FormatType.String, 35, 1, "Defaults to TRUE. If omitted, the field defaults to TRUE which has always been the behavior.  Set to FALSE if you wish for this NPM to NOT send the General Rights notice."));
                            mappings.Add(new CobraTypedCsvColumn("HireDate", FormatType.CobraDate, 0, 0, "Required if NPM with same SSN exists. Null is valid."));

                            //
                            break;
                    }
                    break;

                case "[NPMHIPAADATA]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("CoverageLevel", FormatType.String, 35, 1, "EE,EE+SPOUSE,EE+CHILD,EE+CHILDREN,EE+FAMILY,EE+1,EE+2,SPOUSEONLY,SPOUSE+CHILD,CHILDREN,EE+1Child,EE+2Children,EE+3Children,EE+4Children,EE+5orMoreChildren,EE+Spouse+1Child,EE+Spouse+2Children,EE+Spouse+3Children,EE+Spouse+4Children,EE+Spouse+5orMoreChildren,SPOUSE+1CHILD,SPOUSE+2CHILDREN,SPOUSE+3CHILDREN,SPOUSE+4CHILDREN,SPOUSE+5ORMORECHILDREN,EE+DOMESTICPARTNER,EE1UNDER19,EE+SPOUSE1UNDER19,EE+SPOUSE2UNDER19,EE+CHILDREN1UNDER19,EE+CHILDREN2UNDER19,EE+CHILDREN3UNDER19,EE+FAMILY1UNDER19,EE+FAMILY2UNDER19,EE+FAMILY3UNDER19"));
                            mappings.Add(new CobraTypedCsvColumn("OriginalEnrollmentDate", FormatType.CobraDate, 0, 1, "Original enrollment date of the dependent’s medical plan - used for HIPAA certificate"));
                            mappings.Add(new CobraTypedCsvColumn("LastDayOfCoverage", FormatType.CobraDate, 0, 0, "The Last Day of COBRA. This field should be left blank. The system will determine the LDOC based on the FDOC and COBRADurationMonths."));
                            mappings.Add(new CobraTypedCsvColumn("PlanName", FormatType.String, 50, 1, "The unique Client plan name"));
                            //
                            break;
                    }
                    break;

                case "[NPMDEPENDENT]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("FullName", FormatType.String, 100, 1, ""));
                            mappings.Add(new CobraTypedCsvColumn("Relationship", FormatType.String, 35, 1, "SPOUSE, CHILD, DOMESTICPARTNER"));
                            mappings.Add(new CobraTypedCsvColumn("OriginalEnrollmentDate", FormatType.CobraDate, 0, 0, "Original enrollment date of the dependent’s medical plan - used for HIPAA certificate"));
                            mappings.Add(new CobraTypedCsvColumn("LastDayOfCoverage", FormatType.CobraDate, 0, 1, "The Last Day of COBRA. This field should be left blank. The system will determine the LDOC based on the FDOC and COBRADurationMonths."));
                            //
                            break;
                    }
                    break;
                case "[NPMLOOKUP]":
                    switch (versionNo)
                    {
                        case "1.2":
                        case "1.1":
                        case "1.0":
                            mappings.Add(new CobraTypedCsvColumn("ClientName", FormatType.String, 100, 1, "N/A"));
                            mappings.Add(new CobraTypedCsvColumn("SSN", FormatType.SSN, 9, 1, "N/A"));
                            mappings.Add(new CobraTypedCsvColumn("HireDate", FormatType.CobraDate, 0, 0, "Required if NPM exists with the same SSN. Null is valid."));
                            //
                            break;
                    }
                    break;
                #endregion
                default:
                    var message = $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : fileFormat : {rowType} is invalid";
                    throw new Exception(message);
            }

            // entrire line
            return mappings;
        }

        public static string GetCobraRowFormatFromLine(string line)
        {
            if (Utils.IsBlank(line))
            {
                return "[UKNOWN]";
            }

            string[] columns = ImpExpUtils.GetCsvColumnsFromText(line);
            var columnCount = columns.Length;
            if (columnCount == 0)
            {
                return "[UKNOWN]";
            }

            var firstColValue = columns[0];

            //todo: improce this
            return firstColValue;

        }

        public static Boolean GetCobraFileFormatIsResultFile(string srcFilepath)
        {
            return false;
        }

        public static void ImportCobraFile(DbConnection dbConn, string srcFilePath, string orgSrcFilePath,
          Boolean hasHeaderRow, FileOperationLogParams fileLogParams
          , OnErrorCallback onErrorCallback)
        {

            try
            {
                string fileName = Path.GetFileName(srcFilePath);
                fileLogParams?.SetFileNames(Utils.GetUniqueIdFromFileName(fileName), fileName, srcFilePath, "", "",
                    "ImportCobraFile", $"Starting: Import {fileName}", "Starting");

                // split text fileinto multiple files
                Dictionary<EdiFileFormat, Object[]> files = new Dictionary<EdiFileFormat, Object[]>();

                var versionNo = GetCobraFileVersionNoFromFile(srcFilePath);
                //
                Boolean isResultFile = GetCobraFileFormatIsResultFile(srcFilePath);

                //
                string tableName = isResultFile ? "[dbo].[cobra_res_file_table_stage]" : "[dbo].[cobra_file_table_stage]";
                string postImportProc = isResultFile
                    ? "dbo.process_cobra_res_file_table_stage_import"
                    : "dbo.process_cobra_file_table_stage_import";

                // truncate staging table
                DbUtils.TruncateTable(dbConn, tableName,
                    fileLogParams?.GetMessageLogParams());

                // open file for reading
                // read each line and insert
                using (var inputFile = new StreamReader(srcFilePath))
                {
                    int rowNo = 0;
                    string line;
                    while ((line = inputFile.ReadLine()) != null)
                    {
                        rowNo++;

                        // import the line with manual insert statement
                        ImportCobraCsvLine(Path.GetFileName(srcFilePath), rowNo, line, dbConn, tableName, versionNo, fileLogParams, onErrorCallback);
                    }
                }
                // run postimport query to take from staging to final table
                string queryString = $"exec {postImportProc};";
                DbUtils.DbQuery(DbOperation.ExecuteNonQuery, dbConn, queryString, null,
                    fileLogParams?.GetMessageLogParams());
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                {
                    onErrorCallback(srcFilePath, "", ex);
                }
                else
                {
                    throw;
                }
            }
        }

        //imports passed line into table as per passed columns
        public static void ImportCobraCsvLine(string srcFileName, int srcRowNo, string line, DbConnection dbConn, string tableName,
        string versionNo, FileOperationLogParams fileLogParams,
        OnErrorCallback onErrorCallback)
        {
            // get row format for current lione as file can have multiple row types
            string rowType = GetCobraRowFormatFromLine(line);

            // get mappings for event type & version
            TypedCsvSchema mappings = GetCobraFileImportMappings(rowType, versionNo);

            // add fixed columns to line
            line = $"{srcRowNo},\"{line}\",{srcFileName},{line}";

            // import into DB
            ImpExpUtils.ImportCsvLine(line, dbConn, tableName, mappings, fileLogParams, onErrorCallback);

        } // routine


    }
}