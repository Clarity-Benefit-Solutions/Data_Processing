﻿using System;
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



        private static readonly Regex regexALImportHeader = new Regex("IA,");
        private static readonly Regex regexALImportRecType = new Regex("I[B-Z],");
        private static readonly Regex regexALExportHeader = new Regex("RA,");
        private static readonly Regex regexALExportRecType = new Regex("R[B-Z],");

        public static TypedCsvSchema GetAlegeusFileImportMappings(EdiFileFormat fileFormat, HeaderType headerType,
            Boolean forImport = true)
        {
            var mappings = new TypedCsvSchema();

            Boolean isResultFile = GetAlegeusFileFormatIsResultFile(fileFormat);

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
                    mappings.Add(new TypedCsvColumn("res_file_name", "res_file_name", FormatType.String, null, 0, 0, 0,
                        0));
                }
                else
                {
                    // add duplicated entire line as first column to ensure it isd parsed correctly
                    mappings.Add(new TypedCsvColumn("data_row", "data_row", FormatType.String, null, 0, 0, 0, 0));
                    // source filename
                    mappings.Add(new TypedCsvColumn("mbi_file_name", "mbi_file_name", FormatType.String, null, 0, 0, 0,
                        0));
                }
            }

            // the row_type
            mappings.Add(new TypedCsvColumn("row_type", "row_type", FormatType.String, null, 0, 0, 0, 0));

            switch (fileFormat)
            {
                /////////////////////////////////////////////////////
                // IB, RB 
                case EdiFileFormat.AlegeusDemographics:
                case EdiFileFormat.AlegeusResultsDemographics:
                    //
                    if (fileFormat == EdiFileFormat.AlegeusDemographics)
                    {
                        // for all
                        mappings.Add(new TypedCsvColumn("TpaId", "TpaId", FormatType.FixedConstant, "BENEFL", 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("EmployeeSocialSecurityNumber", "EmployeeSocialSecurityNumber",
                            FormatType.AlphaNumeric, null, 9, 9, 0, 0));
                        mappings.Add(new TypedCsvColumn("LastName", "LastName", FormatType.String, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("FirstName", "FirstName", FormatType.AlphaOnly, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("MiddleInitial", "MiddleInitial", FormatType.String, null, 0, 0,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("BirthDate", "BirthDate", FormatType.IsoDate, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("Phone", "Phone", FormatType.Phone, null, 0, 10, 0, 0));
                        mappings.Add(new TypedCsvColumn("AddressLine1", "AddressLine1", FormatType.AlphaNumeric, null,
                            0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("AddressLine2", "AddressLine2", FormatType.AlphaNumeric, null,
                            0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("City", "City", FormatType.AlphaOnly, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("State", "State", FormatType.AlphaOnly, null, 2, 2, 0, 0));
                        mappings.Add(new TypedCsvColumn("Zip", "Zip", FormatType.Zip, null, 0, 5, 0, 0));
                        mappings.Add(new TypedCsvColumn("Country", "Country", FormatType.AlphaOnly, "US", 2, 2, 0, 0));
                        mappings.Add(new TypedCsvColumn("Email", "Email", FormatType.Email, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("MobileNumber", "MobileNumber", FormatType.Phone, null, 0, 10,
                            0, 0));
                        // Note: EmployeeStatus: must be populated
                        mappings.Add(new TypedCsvColumn("EmployeeStatus", "EmployeeStatus", FormatType.Integer, "2|5",
                            1, 1, 0,
                            0, "2"));

                        // for New & Segmented
                        mappings.Add(new TypedCsvColumn("EligibilityDate", "EligibilityDate", FormatType.IsoDate, null,
                            0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("TerminationDate", "TerminationDate", FormatType.IsoDate, null,
                            0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("AlternateId", "AlternateId", FormatType.String, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("Division", "Division", FormatType.String, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("Class", "Class", FormatType.String, null, 0, 0, 0, 0));
                    }

                    //
                    if (fileFormat == EdiFileFormat.AlegeusResultsDemographics)
                    {
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        //
                        mappings.Add(
                            new TypedCsvColumn("error_code", "error_code", FormatType.String, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("error_message", "error_message", FormatType.String, null, 0, 0,
                            0, 0));
                    }

                    break;

                /////////////////////////////////////////////////////
                // IC, RC
                case EdiFileFormat.AlegeusEnrollment:
                case EdiFileFormat.AlegeusResultsEnrollment:

                    //
                    if (fileFormat == EdiFileFormat.AlegeusEnrollment)
                    {
                        mappings.Add(new TypedCsvColumn("TpaId", "TpaId", FormatType.FixedConstant, "BENEFL", 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("PlanId", "PlanId", FormatType.AlphaNumericAndDashes, null, 3, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("AccountTypeCode", "AccountTypeCode", FormatType.AlphaNumericAndDashes,
                            null, 3, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("PlanStartDate", "PlanStartDate", FormatType.IsoDate, null, 0,
                            0, 0, 0));
                        mappings.Add(new TypedCsvColumn("PlanEndDate", "PlanEndDate", FormatType.IsoDate, null, 0, 0, 0,
                            0));

                        // Note: dont default
                        mappings.Add(new TypedCsvColumn("AccountStatus", "AccountStatus", FormatType.Integer, "2|5", 1,
                            1, 0, 0 /*, "2"*/));
                        mappings.Add(new TypedCsvColumn("OriginalPrefunded", "OriginalPrefunded", FormatType.Double,
                            null, 0, 0,
                            0, 0));

                        // note: Old does not have this column
                        if (headerType != HeaderType.Old)
                        {
                            mappings.Add(new TypedCsvColumn("OngoingPrefunded", "OngoingPrefunded", FormatType.Double,
                                null, 0, 0,
                                0, 0));
                        }

                        mappings.Add(new TypedCsvColumn("EmployeePayPeriodElection",
                            "EmployeePayPeriodElection", FormatType.Double, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("EmployerPayPeriodElection",
                            "EmployerPayPeriodElection", FormatType.Double, null, 0, 0, 0, 0));

                        mappings.Add(new TypedCsvColumn("EffectiveDate", "EffectiveDate", FormatType.IsoDate, null, 0,
                            0, 0, 0));
                        mappings.Add(new TypedCsvColumn("TerminationDate", "TerminationDate", FormatType.IsoDate, null,
                            0, 0, 0,
                            0));

                        if (headerType == HeaderType.SegmentedFunding)
                        {
                            mappings.Add(new TypedCsvColumn("AccountSegmentId", "AccountSegmentId", FormatType.String,
                                null, 0, 0, 0,
                                0));
                        }
                    }

                    //
                    if (fileFormat == EdiFileFormat.AlegeusResultsEnrollment)
                    {
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("PlanId", "PlanId", FormatType.AlphaNumericAndDashes, null, 3, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("PlanStartDate", "PlanStartDate", FormatType.IsoDate, null, 0,
                            0, 0, 0));
                        mappings.Add(new TypedCsvColumn("PlanEndDate", "PlanEndDate", FormatType.IsoDate, null, 0, 0, 0,
                            0));
                        //
                        mappings.Add(
                            new TypedCsvColumn("error_code", "error_code", FormatType.String, null, 0, 0, 0, 0));
                        //mappings.Add(new TypedCsvColumn("error_message", "error_message", FormatType.String, null,0, 0, 0, 0));
                    }

                    break;

                /////////////////////////////////////////////////////
                // ID, RD
                case EdiFileFormat.AlegeusDependentDemographics:
                case EdiFileFormat.AlegeusResultsDependentDemographics:

                    //
                    if (fileFormat == EdiFileFormat.AlegeusDependentDemographics)
                    {
                        mappings.Add(new TypedCsvColumn("TpaId", "TpaId", FormatType.FixedConstant, "BENEFL", 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("DependentID", "DependentID", FormatType.String, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("LastName", "LastName", FormatType.String, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("FirstName", "FirstName", FormatType.AlphaOnly, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("MiddleInitial", "MiddleInitial", FormatType.String, null, 0, 0,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("Phone", "Phone", FormatType.Phone, null, 0, 10, 0, 0));
                        mappings.Add(new TypedCsvColumn("AddressLine1", "AddressLine1", FormatType.AlphaNumeric, null,
                            0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("AddressLine2", "AddressLine2", FormatType.AlphaNumeric, null,
                            0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("City", "City", FormatType.AlphaOnly, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("State", "State", FormatType.AlphaOnly, null, 2, 2, 0, 0));
                        mappings.Add(new TypedCsvColumn("Zip", "Zip", FormatType.Zip, null, 0, 5, 0, 0));
                        mappings.Add(new TypedCsvColumn("Country", "Country", FormatType.AlphaOnly, "US", 2, 2, 0, 0));
                        mappings.Add(new TypedCsvColumn("BirthDate", "BirthDate", FormatType.IsoDate, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("Relationship", "Relationship", FormatType.String, null, 0, 0,
                            0, 0));
                    }

                    //
                    if (fileFormat == EdiFileFormat.AlegeusResultsDependentDemographics)
                    {
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("DependentID", "DependentID", FormatType.String, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));

                        //
                        mappings.Add(
                            new TypedCsvColumn("error_code", "error_code", FormatType.String, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("error_message", "error_message", FormatType.String, null, 0, 0,
                            0, 0));
                    }

                    break;

                /////////////////////////////////////////////////////
                // IE, RE
                case EdiFileFormat.AlegeusDependentLink:
                case EdiFileFormat.AlegeusResultsDependentLink:

                    //
                    if (fileFormat == EdiFileFormat.AlegeusDependentLink)
                    {
                        mappings.Add(new TypedCsvColumn("TpaId", "TpaId", FormatType.FixedConstant, "BENEFL", 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("DependentID", "DependentID", FormatType.String, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("AccountTypeCode", "AccountTypeCode", FormatType.AlphaNumericAndDashes,
                            null, 3, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("PlanStartDate", "PlanStartDate", FormatType.IsoDate, null, 0,
                            0, 0, 0));
                        mappings.Add(new TypedCsvColumn("PlanEndDate", "PlanEndDate", FormatType.IsoDate, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("DeleteAccount", "DeleteAccount", FormatType.String, null, 0, 0,
                            0, 0));
                    }

                    //
                    if (fileFormat == EdiFileFormat.AlegeusResultsDependentLink)
                    {
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("DependentID", "DependentID", FormatType.String, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("PlanId", "PlanId", FormatType.AlphaNumericAndDashes, null, 3, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("PlanStartDate", "PlanStartDate", FormatType.IsoDate, null, 0,
                            0, 0, 0));
                        mappings.Add(new TypedCsvColumn("PlanEndDate", "PlanEndDate", FormatType.IsoDate, null, 0, 0, 0,
                            0));

                        //
                        mappings.Add(
                            new TypedCsvColumn("error_code", "error_code", FormatType.String, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("error_message", "error_message", FormatType.String, null, 0, 0,
                            0, 0));
                    }

                    break;
                /////////////////////////////////////////////////////
                // IF, RF
                case EdiFileFormat.AlegeusCardCreation:
                case EdiFileFormat.AlegeusResultsCardCreation:

                    //
                    if (fileFormat == EdiFileFormat.AlegeusCardCreation)
                    {
                        mappings.Add(new TypedCsvColumn("TpaId", "TpaId", FormatType.FixedConstant, "BENEFL", 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("IssueCard", "IssueCard", FormatType.String, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("AddressLine1",
                            "AddressLine1", FormatType.AlphaNumeric, null, 0, 0, 0, 0)); // Shipping Address Code
                        mappings.Add(new TypedCsvColumn("AddressLine2",
                            "AddressLine2", FormatType.AlphaNumeric, null, 0, 0, 0, 0)); // Shipping Method Code
                        mappings.Add(new TypedCsvColumn("DependentID", "DependentID", FormatType.String, null, 0, 0, 0,
                            0));
                    }

                    //
                    if (fileFormat == EdiFileFormat.AlegeusResultsDependentLink)
                    {
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("DependentID", "DependentID", FormatType.String, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("IssueCard", "IssueCard", FormatType.String, null, 0, 0, 0, 0));

                        //?? todo: to verify next column mapping
                        mappings.Add(new TypedCsvColumn("AddressLine1", "AddressLine1", FormatType.AlphaNumeric, null,
                            0, 0, 0,
                            0)); // Shipping Address Code
                        //
                        mappings.Add(
                            new TypedCsvColumn("error_code", "error_code", FormatType.String, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("error_message", "error_message", FormatType.String, null, 0, 0,
                            0, 0));
                    }

                    break;

                /////////////////////////////////////////////////////
                //  IH, RH
                case EdiFileFormat.AlegeusEmployeeDeposit:
                case EdiFileFormat.AlegeusResultsEmployeeDeposit:
                    //
                    if (fileFormat == EdiFileFormat.AlegeusEmployeeDeposit)
                    {
                        mappings.Add(new TypedCsvColumn("TpaId", "TpaId", FormatType.FixedConstant, "BENEFL", 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("AccountTypeCode", "AccountTypeCode", FormatType.AlphaNumericAndDashes,
                            null, 3, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("PlanStartDate", "PlanStartDate", FormatType.IsoDate, null, 0,
                            0, 0, 0));
                        mappings.Add(new TypedCsvColumn("PlanEndDate", "PlanEndDate", FormatType.IsoDate, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        // Note: do NOT default deposit type 
                        mappings.Add(new TypedCsvColumn("DepositType", "DepositType", FormatType.Integer, "1", 1, 1, 0,
                            0 /*, "1"*/));
                        mappings.Add(new TypedCsvColumn("EmployeeDepositAmount", "EmployeeDepositAmount",
                            FormatType.Double, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("EmployerDepositAmount", "EmployerDepositAmount",
                            FormatType.Double, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("EffectiveDate", "EffectiveDate", FormatType.IsoDate, null, 0,
                            0, 0, 0));
                    }

                    //
                    if (fileFormat == EdiFileFormat.AlegeusResultsEmployeeDeposit)
                    {
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        // ? planid??
                        mappings.Add(new TypedCsvColumn("PlanId", "PlanId", FormatType.AlphaNumericAndDashes, null, 3, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("PlanStartDate", "PlanStartDate", FormatType.IsoDate, null, 0,
                            0, 0, 0));
                        mappings.Add(new TypedCsvColumn("PlanEndDate", "PlanEndDate", FormatType.IsoDate, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("DepositType", "DepositType", FormatType.Integer, "1", 1, 1, 0,
                            0, "1"));
                        mappings.Add(new TypedCsvColumn("EmployeeDepositAmount", "EmployeeDepositAmount",
                            FormatType.Double, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("EmployerDepositAmount", "EmployerDepositAmount",
                            FormatType.Double, null, 0, 0, 0, 0));
                        //
                        mappings.Add(
                            new TypedCsvColumn("error_code", "error_code", FormatType.String, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("error_message", "error_message", FormatType.String, null, 0, 0,
                            0, 0));
                    }

                    break;
                /////////////////////////////////////////////////////
                // LATER: FileChecker: mapping for II,RI: take from FinanceApp?
                //  II, RI
                case EdiFileFormat.AlegeusEmployeeCardFees:
                case EdiFileFormat.AlegeusResultsEmployeeCardFees:
                    //
                    if (fileFormat == EdiFileFormat.AlegeusEmployeeDeposit)
                    {
                        mappings.Add(new TypedCsvColumn("TpaId", "TpaId", FormatType.FixedConstant, "BENEFL", 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        // ? planid??
                        mappings.Add(new TypedCsvColumn("AccountTypeCode", "AccountTypeCode", FormatType.AlphaNumericAndDashes,
                            null, 3, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("PlanStartDate", "PlanStartDate", FormatType.IsoDate, null, 0,
                            0, 0, 0));
                        mappings.Add(new TypedCsvColumn("PlanEndDate", "PlanEndDate", FormatType.IsoDate, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("DepositType", "DepositType", FormatType.Integer, "1", 1, 1, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeDepositAmount", "EmployeeDepositAmount",
                            FormatType.Double, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("EmployerDepositAmount", "EmployerDepositAmount",
                            FormatType.Double, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("EffectiveDate", "EffectiveDate", FormatType.IsoDate, null, 0,
                            0, 0, 0));
                    }

                    //
                    if (fileFormat == EdiFileFormat.AlegeusResultsEmployeeDeposit)
                    {
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        // ? planid??
                        mappings.Add(new TypedCsvColumn("PlanId", "PlanId", FormatType.AlphaNumericAndDashes, null, 3, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("PlanStartDate", "PlanStartDate", FormatType.IsoDate, null, 0,
                            0, 0, 0));
                        mappings.Add(new TypedCsvColumn("PlanEndDate", "PlanEndDate", FormatType.IsoDate, null, 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("DepositType", "DepositType", FormatType.Integer, "1", 1, 1, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeDepositAmount", "EmployeeDepositAmount",
                            FormatType.Double, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("EmployerDepositAmount", "EmployerDepositAmount",
                            FormatType.Double, null, 0, 0, 0, 0));
                        //
                        mappings.Add(
                            new TypedCsvColumn("error_code", "error_code", FormatType.String, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("error_message", "error_message", FormatType.String, null, 0, 0,
                            0, 0));
                    }

                    break;

                /////////////////////////////////////////////////////
                // IZ, RZ
                case EdiFileFormat.AlegeusEmployeeHrInfo:
                case EdiFileFormat.AlegeusResultsEmployeeHrInfo:
                    //
                    if (fileFormat == EdiFileFormat.AlegeusEmployeeHrInfo)
                    {
                        mappings.Add(new TypedCsvColumn("TpaId", "TpaId", FormatType.FixedConstant, "BENEFL", 0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        mappings.Add(new TypedCsvColumn("EligibilityDate", "EligibilityDate", FormatType.IsoDate, null,
                            0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("TerminationDate", "TerminationDate", FormatType.IsoDate, null,
                            0, 0, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("Division", "Division", FormatType.String, null, 0, 0, 0, 0));
                    }

                    //
                    if (fileFormat == EdiFileFormat.AlegeusResultsEmployeeDeposit)
                    {
                        mappings.Add(new TypedCsvColumn("EmployerId", "EmployerId", FormatType.String, null, 5, 15, 0,
                            0));
                        mappings.Add(new TypedCsvColumn("EmployeeID", "EmployeeID", FormatType.AlphaNumeric, null, 9, 9,
                            0, 0));
                        //
                        mappings.Add(
                            new TypedCsvColumn("error_code", "error_code", FormatType.String, null, 0, 0, 0, 0));
                        mappings.Add(new TypedCsvColumn("error_message", "error_message", FormatType.String, null, 0, 0,
                            0, 0));
                    }

                    break;
            }

            // entrire line
            return mappings;
        }

        public static Boolean IsAlegeusImportRecLine(string text)
        {
            if (text != null & text.Length >= 3)
                return regexALImportRecType.IsMatch(text?.Trim().Substring(0, 3));

            return false;
        }
        public static Boolean IsAlegeusImportHeaderLine(string text)
        {
            if (text != null & text.Length >= 3)
                return regexALImportHeader.IsMatch(text?.Trim().Substring(0, 3));

            return false;
        }

        public static Boolean IsAlegeusExportRecLine(string text)
        {
            if (text != null & text.Length >= 3)
                return regexALExportRecType.IsMatch(text?.Trim().Substring(0, 3));

            return false;
        }
        public static Boolean IsAlegeusExportHeaderLine(string text)
        {
            if (text != null & text.Length >= 3)
                return regexALExportHeader.IsMatch(text?.Trim().Substring(0, 3));

            return false;
        }

        public static HeaderType GetAlegeusHeaderTypeFromFile(string srcFilePath)
        {
            var headerType = HeaderType.NotApplicable;
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
            using var inputFile = new StreamReader(srcFilePath);
            while (true)
            {
                string line = inputFile.ReadLine()!;
                rowNo++;

                if (line == null)
                {
                    if (headerType == HeaderType.NotApplicable)
                    {
                        string message =
                            $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : Could Not Determine Header Type for  {srcFilePath}";
                        throw new IncorrectFileFormatException(message);
                    }

                    return headerType;
                }

                string[] columns = ImpExpUtils.GetCsvColumnsFromText(line);
                var columnCount = columns.Length;
                if (columnCount == 0)
                {
                    continue;
                }

                var expectedMappingColumnsCount = 0;
                var firstColValue = columns[0];
                var fileFormat = ImpExpUtils.GetAlegeusRowFormat(firstColValue);

                // we only care for Import Headers
                if (Import.GetAlegeusFileFormatIsResultFile(fileFormat))
                {
                    return HeaderType.New;
                }


                // get file format
                // note: we are also detecting header type from conettn for prev Own and NoChange header folders
                switch (fileFormat)
                {
                    // ignore unknown and header lines
                    case EdiFileFormat.Unknown:
                    case EdiFileFormat.AlegeusHeader:
                    case EdiFileFormat.AlegeusResultsHeader:
                        continue;

                    case EdiFileFormat.AlegeusDemographics:
                        switch (columnCount)
                        {
                            case 19:
                                headerType = HeaderType.Old;
                                expectedMappingColumnsCount = columnCount;
                                break;

                            case 24:
                                headerType = HeaderType.New;
                                ;
                                expectedMappingColumnsCount = columnCount;
                                break;
                                // can also be segmented header!
                        }

                        ;
                        break;

                    case EdiFileFormat.AlegeusEnrollment:
                        switch (columnCount)
                        {
                            case 14:
                                headerType = HeaderType.Old;
                                expectedMappingColumnsCount = columnCount;
                                break;

                            case 15:
                                headerType = HeaderType.New;
                                expectedMappingColumnsCount = columnCount;
                                break;

                            case 16:
                                headerType = HeaderType.SegmentedFunding;
                                expectedMappingColumnsCount = columnCount;
                                break;
                        }

                        ;
                        break;

                    case EdiFileFormat.AlegeusEmployeeDeposit:
                        switch (columnCount)
                        {
                            case 11:
                                headerType = HeaderType.Old;
                                expectedMappingColumnsCount = columnCount;
                                break;

                            default:
                                headerType = HeaderType.NotApplicable;
                                break;
                        }

                        ;
                        break;

                    default:
                        headerType = HeaderType.New;
                        break;
                }

                if (expectedMappingColumnsCount <= 0)
                {
                    // what mappings do we expect for this format
                    var mappings = GetAlegeusFileImportMappings(fileFormat, HeaderType.NotApplicable, true);

                    // check we have the exact columns that we expected
                    expectedMappingColumnsCount =
                        mappings.Count /*subtract the extra columns we add before iomport to csv files*/ - 3;
                    if (columnCount != expectedMappingColumnsCount)
                    {
                        string message =
                            $"ERROR: {MethodBase.GetCurrentMethod()?.Name} : Source file with format {fileFormat.ToDescription()} has {columnCount} columns instead of the expected {expectedMappingColumnsCount}. Could Not Determine Header Type for {srcFilePath}";
                        throw new IncorrectFileFormatException(message);
                    }
                }

                return headerType;
            }
        }

        public static Boolean GetAlegeusFileFormatIsResultFile(EdiFileFormat fileFormat)
        {
            String fileFormatDesc = fileFormat.ToDescription();
            if (fileFormatDesc.IndexOf("Result", StringComparison.InvariantCultureIgnoreCase) >= 0)
            {
                return true;
            }

            return false;
        }

        public static Boolean IsAlegeusImportFile(string srcFilePath)
        {
            using var inputFile = new StreamReader(srcFilePath);
            string line;
            while ((line = inputFile.ReadLine()!) != null)
            {
                if (IsAlegeusImportRecLine(line))
                {
                    return true;
                }
            }

            return false;
        }

        public static void ImportAlegeusFile(DbConnection dbConn, string srcFilePath,
                  Boolean hasHeaderRow, FileOperationLogParams fileLogParams
                  , OnErrorCallback onErrorCallback)
        {
            //
            Dictionary<EdiFileFormat, List<int>> fileFormats = ImpExpUtils.GetAlegeusFileFormats(
                srcFilePath, hasHeaderRow, fileLogParams
            );

            // file may contain only a header...
            if (fileFormats.Count == 0)
            {
                Boolean isResultFile = GetAlegeusFileFormatIsResultFile(fileFormats.Keys.First());

                // import as plain text file
                ImpExpUtils.ImportSingleColumnFlatFile(dbConn, srcFilePath, Path.GetFileName(srcFilePath),
                    isResultFile ? "res_file_table_stage" : "mbi_file_table_stage",
                    isResultFile ? "res_file_name" : "mbi_file_name",
                    isResultFile ? "error_row" : "data_row",
                    (filePath1, rowNo, line) =>
                    {
                        // we only import valid import lines 
                        Boolean import = true;
                        return import;
                    },
                    fileLogParams,
                    onErrorCallback
                );

                return;
            }

            // 2. import the file
            ImportAlegeusFile(fileFormats, dbConn, srcFilePath, hasHeaderRow, fileLogParams,
                onErrorCallback);
        }

        //doesnt work - mappings not clear
        public static void ImportAlegeusFile(Dictionary<EdiFileFormat, List<int>> fileFormats,
            DbConnection dbConn, string srcFilePath, Boolean hasHeaderRow, FileOperationLogParams fileLogParams
            , OnErrorCallback onErrorCallback)
        {
            try
            {
                string fileName = Path.GetFileName(srcFilePath);
                fileLogParams?.SetFileNames(Utils.GetUniqueIdFromFileName(fileName), fileName, srcFilePath, "", "",
                    "ImportAlegeusFile", $"Starting: Import {fileName}", "Starting");

                // split text fileinto multiple files
                Dictionary<EdiFileFormat, Object[]> files = new Dictionary<EdiFileFormat, Object[]>();

                //
                foreach (EdiFileFormat fileFormat in fileFormats.Keys)
                {
                    // get temp file for each format
                    string splitFileName = Path.GetTempFileName();
                    FileUtils.EnsurePathExists(splitFileName);
                    //
                    var splitFileWriter = new StreamWriter(splitFileName, false);
                    files.Add(fileFormat, new Object[] { splitFileWriter, splitFileName });
                }

                // open file for reading
                // read each line and insert
                using (var inputFile = new StreamReader(srcFilePath))
                {
                    int rowNo = 0;
                    string line;
                    while ((line = inputFile.ReadLine()) != null)
                    {
                        rowNo++;

                        foreach (EdiFileFormat fileFormat2 in fileFormats.Keys)
                        {
                            if (fileFormats[fileFormat2].Contains(rowNo)
                                || (line?.Substring(0, 2) == "RA" || line?.Substring(0, 2) == "IA"))
                            {
                                // get temp file for each format
                                var splitFileWriter = (StreamWriter)files[fileFormat2][0];
                                // if there is prvUnwrittenLine it was probably a header line - write to the file that 

                                splitFileWriter.WriteLine(line);
                                continue;
                            }
                        }

                        // go to next line if a line was written
                    }
                }

                // close all files
                //
                foreach (var fileFormat3 in files.Keys)
                {
                    // get temp file for each format
                    var writer = (StreamWriter)files[fileFormat3][0];
                    writer.Close();

                    // import the file
                    ImportAlegeusFile(fileFormat3, dbConn, (string)files[fileFormat3][1], srcFilePath,
                        hasHeaderRow, fileLogParams, onErrorCallback);
                }
            }
            catch (Exception ex)
            {
                // callback for complete
                if (onErrorCallback != null)
                {
                    onErrorCallback(srcFilePath, fileFormats.Values.ToString(), ex);
                }
                else
                {
                    throw;
                }
            }
        }

        public static void ImportAlegeusFile(EdiFileFormat fileFormat, DbConnection dbConn,
            string srcFilePath, string orgSrcFilePath, Boolean hasHeaderRow, FileOperationLogParams fileLogParams
            , OnErrorCallback onErrorCallback)
        {
            try
            {
                // check mappinsg and type opf file (Import or Result)

                var headerType = GetAlegeusHeaderTypeFromFile(srcFilePath);
                TypedCsvSchema mappings = GetAlegeusFileImportMappings(fileFormat, headerType);
                Boolean isResultFile = GetAlegeusFileFormatIsResultFile(fileFormat);

                //
                var newPath =
                    PrefixLineWithEntireLineAndFileName(srcFilePath, orgSrcFilePath, fileLogParams);
                //
                string tableName = isResultFile ? "[dbo].[res_file_table_stage]" : "[dbo].[mbi_file_table_stage]";
                string postImportProc = isResultFile
                    ? "dbo.process_res_file_table_stage_import"
                    : "dbo.process_mbi_file_table_stage_import";

                // truncate staging table
                DbUtils.TruncateTable(dbConn, tableName,
                    fileLogParams?.GetMessageLogParams());

                // import the file with bulk copy
                ImpExpUtils.ImportCsvFileBulkCopy(dbConn, newPath, hasHeaderRow, tableName, mappings,
                    fileLogParams, onErrorCallback
                );

                //
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
                    onErrorCallback(orgSrcFilePath, fileFormat.ToDescription(), ex);
                }
                else
                {
                    throw;
                }
            }
        }



    }
}