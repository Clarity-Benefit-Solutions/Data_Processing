using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CoreUtils;
using CoreUtils.Classes;
using DataProcessing.DataModels.CobraPoint;
using DataProcessing.DataModels.DataProcessing;

// ReSharper disable All

// ReSharper disable once CheckNamespace
namespace DataProcessing
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public partial class FileChecker : IDisposable
    {
        private Client currentClient;

        //
        private ClientDivision currentClientDivision;

        private NPM currentNPM;

        private QB currentQB;

        private SPM currentSPM;

        // set to true to raise errors on duplicate QB
        private bool FailIfMainEntityAlreadyExists = false;

        private int currentClientDivisionID
        {
            get
            {
                if (this.currentClientDivision != null && currentClientDivision.ClientDivisionID > 0)
                {
                    return this.currentClientDivision.ClientDivisionID;
                }
                return 0;
            }
        }

        private int currentClientID
        {
            get
            {
                if (this.currentClient != null)
                {
                    return (int)this.currentClient.ClientID;
                }
                return 0;
            }
        }

        private List<NPM> GetAllCobraNPMForClient(int clientId, int divisionId)
        {
            List<NPM> dbResults = null;

            dbResults = dbCtxCobra.NPMs
                //.Where(x => x.ClientID == clientId)
                .Where(x => x.ClientDivisionID == divisionId)
                .OrderBy(x => x.MemberID)
                .AsNoTracking()
                .ToList();

            return dbResults;
        }

        // cache all EE for ER to reduce number of queries to database - each query for a single EE takes around 150 ms so we aree saving significant time esp for ER witjh many EE
        private List<QB> GetAllCobraQBForClient(int clientId, int divisionId)
        {
            List<QB> dbResults = null;

            dbResults = dbCtxCobra.QBs
                .Where(x => x.ClientID == clientId)
                .Where(x => x.ClientDivisionID == divisionId)
                .OrderBy(x => x.MemberID)
                .AsNoTracking()
                .ToList();

            return dbResults;
        }

        private List<SPM> GetAllCobraSPMForClient(int clientId, int divisionId)
        {
            List<SPM> dbResults = null;

            dbResults = dbCtxCobra.SPMs
                .Where(x => x.ClientID == clientId)
                .Where(x => x.ClientDivisionID == divisionId)
                .OrderBy(x => x.MemberID)
                .AsNoTracking()
                .ToList();

            return dbResults;
        }

        private class ClientAndDivision
        {
            public Client Client;
            public ClientDivision ClientDivision;
        }

        #region CheckCobraFile

        public Boolean CheckClientQBPlanExists(string versionNo, cobra_file_table_stage dataRow, TypedCsvSchema mappings)
        {
            string errorMessage = "";

            // check client+division has plan
            var clientPlans = currentClient.ClientPlanQBs
                .Where(
                    x => x.PlanName == dataRow.PlanName
                    )
                .ToList();
            //
            if (clientPlans.Count == 0)
            {
                errorMessage += $"The Plan '{dataRow.PlanName}' could not found for Client '{currentClient.ClientName}'";
            }
            else if (currentClientDivisionID > 0)
            {
                int divPlansFound = 0;
                foreach (var clientPlan in clientPlans)
                {
                    var divPlans = currentClientDivision.ClientDivisionQBPlans.Where(x => x.ClientPlanQBID == clientPlan.ClientPlanQBID).ToList();

                    //
                    if (divPlans.Count > 0)
                    {
                        divPlansFound++;
                    }
                }
                if (divPlansFound == 0)
                {
                    errorMessage += $"The Plan '{dataRow.PlanName}' could not found for Client '{currentClient.ClientName}' and Division '{currentClientDivision.DivisionName}'";
                }
            }
            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddCobraErrorForRow(dataRow, "No Such Plan Found", $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckCobraClientPlanExists(cobra_file_table_stage dataRow, TypedCsvColumn column,
            string versionNo)
        {
            var errorMessage = "";
            //var cacheKey =
            //    $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{dataRow.ClientName}-{dataRow.AccountTypeCode}-{dataRow.PlanId}";
            //if (_cache.ContainsKey(cacheKey))
            //{
            //    errorMessage = _cache.Get(cacheKey)?.ToString();
            //}
            //else
            //{
            //    // check DB
            //    if (Utils.IsBlank(dataRow.ClientName))
            //    {
            //        errorMessage += $"The Client Name cannot be blank";
            //        ;
            //    }
            //    else if (Utils.IsBlank(dataRow.AccountTypeCode))
            //    {
            //        errorMessage += $"The AccountTypeCode cannot be blank";
            //        ;
            //    }
            //    else
            //    {
            //        DataTable dbResults = GetAllCobraPlansForClient(dataRow.ClientName);

            //        // planid is not always present e.g. in deposit file
            //        string filter = $"ClientName = '{dataRow.ClientName}'";
            //        if (!Utils.IsBlank(dataRow.AccountTypeCode))
            //        {
            //            filter += $" and account_type_code = '{dataRow.AccountTypeCode}' ";
            //        }

            //        if (!Utils.IsBlank(dataRow.PlanId))
            //        {
            //            filter += $" and plan_id = '{dataRow.PlanId}' ";
            //        }

            //        DataRow[] dbRows = dbResults.Select(filter);

            //        if (dbRows.Length == 0)
            //        {
            //            errorMessage +=
            //                $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                $" could not be found for Client Name {dataRow.ClientName}";
            //            ;
            //        }
            //        else
            //        {
            //            DataRow dbData = dbRows[0];
            //            DateTime actualPlanStartDate = (DateTime)dbData["plan_year_start_date"];
            //            DateTime actualPlanEndDate = (DateTime)dbData["plan_year_end_date"];
            //            //DateTime actualGracePeriodEndDate = (DateTime)dbData["grace_period_end_date"];

            //            //check start and end dates
            //            if (!Utils.IsBlank(dataRow.PlanStartDate) && !Utils.IsBlank(dataRow.PlanEndDate) &&
            //                Utils.ToDate(dataRow.PlanStartDate) > Utils.ToDate(dataRow.PlanEndDate))
            //            {
            //                errorMessage +=
            //                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                    $" Start Date {dataRow.PlanStartDate} must be before the Plan End Date {dataRow.PlanEndDate}";
            //            }

            //            //check plan dates match Cobra
            //            if (!Utils.IsBlank(dataRow.PlanStartDate) &&
            //                actualPlanStartDate > Utils.ToDate(dataRow.PlanStartDate))
            //            {
            //                errorMessage +=
            //                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                    $" starts only on {Utils.ToDateString(actualPlanStartDate)} and is not yet started on {dataRow.PlanStartDate}";
            //            }

            //            if (!Utils.IsBlank(dataRow.PlanEndDate) &&
            //                actualPlanEndDate < Utils.ToDate(dataRow.PlanEndDate))
            //            {
            //                errorMessage =
            //                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                    $" ended on {Utils.ToDateString(actualPlanEndDate)} and is no longer active on {dataRow.PlanStartDate}";
            //                ;
            //            }

            //            //check effectivedate is within plan dates
            //            if (!Utils.IsBlank(dataRow.EffectiveDate) &&
            //                actualPlanStartDate > Utils.ToDate(dataRow.EffectiveDate))
            //            {
            //                errorMessage +=
            //                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                    $" starts only on {Utils.ToDateString(actualPlanStartDate)} and is not yet started on {dataRow.EffectiveDate}";
            //            }

            //            if (!Utils.IsBlank(dataRow.EffectiveDate) &&
            //                actualPlanEndDate < Utils.ToDate(dataRow.EffectiveDate))
            //            {
            //                errorMessage =
            //                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                    $" ended on {Utils.ToDateString(actualPlanEndDate)} and is no longer active on {dataRow.EffectiveDate}";
            //                ;
            //            }
            //        }

            //        //
            //        _cache.Add(cacheKey, errorMessage);
            //    }
            //}

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddCobraErrorForRow(dataRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckCobraEmployeePlanExists(cobra_file_table_stage dataRow, TypedCsvColumn column,
            string versionNo)
        {
            var errorMessage = "";
            //var cacheKey =
            //    $"{MethodBase.GetCurrentMethod()?.Name}-{this.PlatformType.ToDescription()}-{dataRow.ClientName}-{dataRow.MemberId}-{dataRow.AccountTypeCode}-{dataRow.PlanId}";
            //if (_cache.ContainsKey(cacheKey))
            //{
            //    errorMessage = _cache.Get(cacheKey)?.ToString();
            //}
            //else
            //{
            //    // check DB
            //    if (Utils.IsBlank(dataRow.ClientName))
            //    {
            //        errorMessage += $"The Client Name cannot be blank";
            //        ;
            //    }
            //    else if (Utils.IsBlank(dataRow.MemberId))
            //    {
            //        errorMessage += $"The Client Name cannot be blank";
            //        ;
            //    }
            //    else if (Utils.IsBlank(dataRow.AccountTypeCode))
            //    {
            //        errorMessage += $"The AccountTypeCode cannot be blank";
            //        ;
            //    }
            //    else
            //    {
            //        //// if we are enrolling an employee in a plan, only check if ER has this EE
            //        //if (versionNo == string.CobraEnrollment)
            //        //{
            //        //  //as it is an enrollment file, check the EE exists and enroll in the plan
            //        //  var hasError = this.CheckEmployeeExists(dataRow);
            //        //  //return hasError;
            //        //}

            //        DataTable dbResults = GetAllCobraEmployeePlansForClient(dataRow.ClientName);

            //        // planid is not always present e.g. in deposit file
            //        string filter = $" MemberId = '{dataRow.MemberId}' ";
            //        if (!Utils.IsBlank(dataRow.AccountTypeCode))
            //        {
            //            filter += $" and plancode = '{dataRow.AccountTypeCode}' ";
            //        }

            //        if (!Utils.IsBlank(dataRow.PlanId))
            //        {
            //            filter += $" and plandesc = '{dataRow.PlanId}' ";
            //        }

            //        DataRow[] dbRows = dbResults.Select(filter);

            //        if (dbRows.Length == 0)
            //        {
            //            if (true  /*|| versionNo == string.CobraEnrollment*/)
            //            {
            //                // as it is an enrollment, enroll the EE in this plan demographics file,
            //                DataRow newRow = dbResults.NewRow();
            //                newRow["ClientName"] = dataRow.ClientName;
            //                newRow["MemberId"] = dataRow.MemberId;
            //                newRow["plancode"] = dataRow.AccountTypeCode;
            //                newRow["plandesc"] = dataRow.PlanId;
            //                newRow["planstart"] = Utils.ToDateTime(dataRow.PlanStartDate);
            //                newRow["planend"] = Utils.ToDateTime(dataRow.PlanEndDate);

            //                dbResults.Rows.Add(newRow);

            //                var cacheKey2 =
            //                    $"GetAllEmployeePlansForClient-{this.PlatformType.ToDescription()}-{dataRow.ClientName}-AllEmployeePlans";
            //                //
            //                _cache.Add(cacheKey2, dbResults);

            //                //
            //                return false;
            //            }

            //            errorMessage +=
            //                $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                $" could not be found for Employee Id {dataRow.MemberId}";
            //            ;
            //        }
            //        else
            //        {
            //            DataRow dbData = dbRows[0];

            //            // for demographics file, the employee will not yet exist or the status may be changing (activating or terminating) - do not check

            //            DateTime actualPlanStartDate = (DateTime)dbData["planstart"];
            //            DateTime actualPlanEndDate = (DateTime)dbData["planend"];
            //            //DateTime? actualGracePeriodEndDate = Utils.ToDate(dbData["actualGracePeriodEndDate"]?.ToString());

            //            //note: we need to ensure we got Cobra plans going back many years properly. we have data from 2004 onwards in the portal
            //            //check start and end dates
            //            if (!Utils.IsBlank(dataRow.PlanStartDate) && !Utils.IsBlank(dataRow.PlanEndDate) &&
            //                Utils.ToDate(dataRow.PlanStartDate) > Utils.ToDate(dataRow.PlanEndDate))
            //            {
            //                errorMessage +=
            //                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                    $" Start Date {dataRow.PlanStartDate} must be before the Plan End Date {dataRow.PlanEndDate} for Employee Id {dataRow.MemberId}";
            //            }

            //            //check plan dates match Cobra
            //            if (!Utils.IsBlank(dataRow.PlanStartDate) &&
            //                actualPlanStartDate > Utils.ToDate(dataRow.PlanStartDate))
            //            {
            //                errorMessage +=
            //                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                    $" starts only on {Utils.ToDateString(actualPlanStartDate)} and is not yet started on {dataRow.PlanStartDate} for Employee Id {dataRow.MemberId}";
            //            }

            //            if (!Utils.IsBlank(dataRow.PlanEndDate) &&
            //                actualPlanEndDate < Utils.ToDate(dataRow.PlanEndDate)
            //                && dataRow.PlanEndDate != "20991231")
            //            {
            //                errorMessage =
            //                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                    $" ended on {Utils.ToDateString(actualPlanEndDate)} and is no longer active on {dataRow.PlanEndDate} for Employee Id {dataRow.MemberId}";
            //                ;
            //            }

            //            //check effectivedate is within plan dates
            //            if (!Utils.IsBlank(dataRow.EffectiveDate) &&
            //                actualPlanStartDate > Utils.ToDate(dataRow.EffectiveDate))
            //            {
            //                errorMessage +=
            //                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                    $" starts only on {Utils.ToDateString(actualPlanStartDate)} and is not yet started on {dataRow.EffectiveDate} for Employee Id {dataRow.MemberId}";
            //            }

            //            if (!Utils.IsBlank(dataRow.EffectiveDate) &&
            //                actualPlanEndDate < Utils.ToDate(dataRow.EffectiveDate))
            //            {
            //                errorMessage =
            //                    $"The AccountTypeID {dataRow.AccountTypeCode}" +
            //                    (!Utils.IsBlank(dataRow.PlanId) ? $" and Plan ID {dataRow.PlanId}" : "") +
            //                    $" ended on {Utils.ToDateString(actualPlanEndDate)} and is no longer active on {dataRow.EffectiveDate} for Employee Id {dataRow.MemberId}";
            //                ;
            //            }
            //        }
            //    }

            //    //
            //    _cache.Add(cacheKey, errorMessage);
            //}

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddCobraErrorForRow(dataRow, column.SourceColumn, $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckCobraRecordData(string versionNo, cobra_file_table_stage dataRow, TypedCsvSchema mappings)
        {
            Boolean lineHasError;

            // is start of a main entity
            switch (dataRow.row_type.ToUpper())
            {
                case "[VERSION]":
                    break;

                case "[QB]":
                case "[QBLEGACY]":
                case "[QBLOOKUP]":
                case "[SPM]":
                case "[SPMLOOKUP]":
                case "[NPM]":
                case "[NPMLOOKUP]":
                    lineHasError = this.CheckCobraRecordDataMainEntity(versionNo, dataRow, mappings);
                    if (lineHasError)
                    {
                        return true;
                    }

                    break;

                // none of the above major entity types
                default:
                    lineHasError = CheckCobraRecordDataIsQBSubline(versionNo, dataRow, mappings);
                    if (lineHasError)
                    {
                        return true;
                    }

                    lineHasError = CheckCobraRecordDataIsSPMSubline(versionNo, dataRow, mappings);
                    if (lineHasError)
                    {
                        return true;
                    }
                    lineHasError = CheckCobraRecordDataIsNPMSubline(versionNo, dataRow, mappings);
                    if (lineHasError)
                    {
                        return true;
                    }

                    // break out of switch ((dataRow.row_type.ToUpper()))
                    break;
            }

            //
            return false;
        }

        public Boolean CheckCobraRecordDataIsNPMSubline(string versionNo, cobra_file_table_stage dataRow, TypedCsvSchema mappings)
        {
            Boolean lineHasError = false;

            // check which type of entity subline it is and whether a main entity line ocurrs before this
            if (Utils.Left(dataRow.row_type.ToUpper(), 4) == "[NPM")
            {
                // subline of NPM
                if (this.currentNPM == null || Utils.IsBlank(this.currentNPM.SSN))
                {
                    this.AddCobraErrorForRow(dataRow, "No Current NPM", $"Could Not Process this Record Type '{dataRow.row_type}' as no Valid NPM is referenced above");
                    return true;
                }

                // check specific duplicates or missing plans
                switch ((dataRow.row_type.ToUpper()))
                {
                    default:
                        break;
                } // switch ((dataRow.row_type.ToUpper()))
            } // NPM line types

            return lineHasError;
        }

        public Boolean CheckCobraRecordDataIsQBSubline(string versionNo, cobra_file_table_stage dataRow, TypedCsvSchema mappings)
        {
            Boolean lineHasError = false;

            // check which type of entity subline it is and whether a main entity line ocurrs before this
            if (Utils.Left(dataRow.row_type.ToUpper(), 3) == "[QB" || Utils.Left(dataRow.row_type.ToUpper(), 3) != "ME")
            {
                // subline of QB
                if (this.currentQB == null || Utils.IsBlank(this.currentQB.SSN))
                {
                    this.AddCobraErrorForRow(dataRow, "No Current QB", $"Could Not Process this Record Type '{dataRow.row_type}' as no Valid QB is referenced above");
                    return true;
                }

                // check specific duplicates or missing plans
                switch ((dataRow.row_type.ToUpper()))
                {
                    case "[QBPLANINITIAL]":
                        // check client+division has plan
                        lineHasError = CheckClientQBPlanExists(versionNo, dataRow, mappings);
                        break;

                    case "[QBPLAN]":
                        // check client+division has plan
                        lineHasError = CheckClientQBPlanExists(versionNo, dataRow, mappings);
                        break;

                    case "[QBDEPENDENTPLANINITIAL]":
                        // check client+division has plan
                        lineHasError = CheckClientQBPlanExists(versionNo, dataRow, mappings);
                        break;

                    case "[QBDEPENDENTPLAN]":
                        // check client+division has plan
                        lineHasError = CheckClientQBPlanExists(versionNo, dataRow, mappings);
                        break;

                    case "[QBSUBSIDYSCHEDULE]":
                        // check client+division has subsidy schedule
                        break;

                    case "[QBPLANMEMBERSPECIFICRATEINITIAL]":
                        // check client+division has rate
                        break;

                    case "[QBPLANMEMBERSPECIFICRATE]":
                        // check client+division has rate
                        break;

                    case "[QBEVENT]":
                        // can have only one per QB?
                        break;

                    case "[QBLEGACY]":
                        break;

                    case "[QBDEPENDENT]":
                        // can have only one per QBcheck each dependant is added only once
                        break;

                    case "[QBNOTE]":
                        // ignore?
                        break;

                    case "[QBLETTERATTACHMENT]":
                        // ignore?
                        break;

                    case "[QBPLANTERMREINSTATE]":
                        // ignore?
                        break;

                    case "[QBSTATEINSERTS]":
                        // ignore?
                        break;

                    case "[QBDISABILITYEXTENSION]":
                        // ignore?
                        break;
                } // switch ((dataRow.row_type.ToUpper()))
            } // QB line types

            return lineHasError;
        }

        public Boolean CheckCobraRecordDataIsSPMSubline(string versionNo, cobra_file_table_stage dataRow, TypedCsvSchema mappings)
        {
            Boolean lineHasError = false;

            // check which type of entity subline it is and whether a main entity line ocurrs before this
            if (Utils.Left(dataRow.row_type.ToUpper(), 4) == "[SPM")
            {
                // subline of SPM
                if (this.currentSPM == null || Utils.IsBlank(this.currentSPM.SSN))
                {
                    this.AddCobraErrorForRow(dataRow, "No Current SPM", $"Could Not Process this Record Type '{dataRow.row_type}' as no Valid SPM is referenced above");
                    return true;
                }

                // check specific duplicates or missing plans
                switch ((dataRow.row_type.ToUpper()))
                {
                    default:
                        break;
                } // switch ((dataRow.row_type.ToUpper()))
            } // SPM line types

            return lineHasError;
        }

        public Boolean CheckCobraRecordDataMainEntity(string versionNo, cobra_file_table_stage dataRow, TypedCsvSchema mappings)
        {
            // clear any previous cached record
            this.currentQB = null;
            this.currentSPM = null;
            this.currentNPM = null;

            //get client and division
            this.SetCobraClientAndDivision(dataRow);
            if (this.currentClientID <= 0)
            {
                this.AddCobraErrorForRow(dataRow, "Client And DiVision Does Not Exist", $"Client with '{dataRow.ClientName}' and Division Name '{dataRow.ClientDivisionName}' Not Found. ");
                return true;
            }

            // check if it is the start of a new EntityType
            switch (dataRow.row_type)
            {
                case "[QB]":
                case "[QBLEGACY]":

                    // check QB exists - if so raise error
                    QB theQB = this.GetCobraQB(dataRow);
                    if (theQB != null && theQB.MemberID > 0 && this.FailIfMainEntityAlreadyExists)
                    {
                        this.AddCobraErrorForRow(dataRow, "QB Already exists", $"Duplicate QB. QB with SSN '{theQB.SSN}' already exists");
                        return true;
                    }

                    //OK - there is no such QB in the database - we instantiate  one for checkiong events etc
                    this.currentQB = this.CreateCobraQBForDataRow(dataRow);

                    break;

                case "[QBLOOKUP]":

                    // check QB exists - if so raise error
                    QB theQB2 = this.GetCobraQB(dataRow);
                    if (theQB2 != null && theQB2.MemberID > 0)
                    {
                        //ok
                    }
                    else
                    {
                        this.AddCobraErrorForRow(dataRow, "QB Not Found", $"QB with SSN '{theQB2.SSN}' not found");
                        return true;
                    }

                    //OK - there is no such QB in the database - we instantiate  one for checkiong events etc
                    this.currentQB = this.CreateCobraQBForDataRow(dataRow);

                    break;

                case "[SPM]":

                    // check division exists
                    this.SetCobraClientAndDivision(dataRow);

                    // check SPM exists - if so raise error
                    SPM theSPM = this.GetCobraSPM(dataRow);
                    if (theSPM != null && theSPM.MemberID > 0 && this.FailIfMainEntityAlreadyExists)
                    {
                        this.AddCobraErrorForRow(dataRow, "SPM Already exists", $"Duplicate SPM. SPM with SSN '{theSPM.SSN}' already exists");
                        return true;
                    }

                    //OK - there is no such SPM in the database - we instantiate  one for checkiong events etc
                    this.currentSPM = this.CreateCobraSPMForDataRow(dataRow);

                    break;

                case "[SPMLOOKUP]":

                    // check SPM exists - if so raise error
                    SPM theSPM2 = this.GetCobraSPM(dataRow);
                    if (theSPM2 != null && theSPM2.MemberID > 0)
                    {
                        //ok
                    }
                    else
                    {
                        this.AddCobraErrorForRow(dataRow, "SPM Not Found", $"SPM with SSN '{theSPM2.SSN}' not found");
                        return true;
                    }

                    //OK - there is no such SPM in the database - we instantiate  one for checkiong events etc
                    this.currentSPM = this.CreateCobraSPMForDataRow(dataRow);

                    break;

                case "[NPM]":

                    // check division exists
                    this.SetCobraClientAndDivision(dataRow);

                    // check NPM exists - if so raise error
                    NPM theNPM = this.GetCobraNPM(dataRow);
                    if (theNPM != null && theNPM.MemberID > 0 && this.FailIfMainEntityAlreadyExists)
                    {
                        this.AddCobraErrorForRow(dataRow, "NPM Already exists", $"Duplicate NPM. NPM with SSN '{theNPM.SSN}' already exists");
                        return true;
                    }

                    //OK - there is no such NPM in the database - we instantiate  one for checkiong events etc
                    this.currentNPM = this.CreateCobraNPMForDataRow(dataRow);

                    break;

                case "[NPMLOOKUP]":

                    // check NPM exists - if so raise error
                    NPM theNPM2 = this.GetCobraNPM(dataRow);
                    if (theNPM2 != null && theNPM2.MemberID > 0)
                    {
                        //ok
                    }
                    else
                    {
                        this.AddCobraErrorForRow(dataRow, "NPM Not Found", $"NPM with SSN '{theNPM2.SSN}' not found");
                        return true;
                    }

                    //OK - there is no such NPM in the database - we instantiate  one for checkiong events etc
                    this.currentNPM = this.CreateCobraNPMForDataRow(dataRow);

                    break;
            }

            //
            return false;
        }

        public Boolean CheckCobraTpaExists(cobra_file_table_stage dataRow)
        {
            var errorMessage = "";
            //var cacheKey = $"{MethodBase.GetCurrentMethod()?.Name}-{dataRow.TpaId}";
            //if (_cache.ContainsKey(cacheKey))
            //{
            //    errorMessage = _cache.Get(cacheKey)?.ToString();
            //}
            //else
            //{
            //    if (PlatformType == PlatformType.Cobra)
            //    {
            //        // check DB
            //        if (Utils.IsBlank(column.FixedValue))
            //        {
            //            if (Utils.IsBlank(dataRow.TpaId))
            //            {
            //                errorMessage = "TPA ID cannot be blank. It must always be BENEFL";
            //            }
            //            else if (dataRow.TpaId != "BENEFL")
            //            {
            //                errorMessage = $"TPA ID {dataRow.TpaId} is invalid. It must always be BENEFL";
            //            }
            //        }
            //    }
            //    else
            //    {
            //        throw new Exception($"{PlatformType.ToDescription()} is not yet handled");
            //    }

            //    //
            //    _cache.Add(cacheKey, errorMessage);
            //}

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddCobraErrorForRow(dataRow, "TPA", $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean CheckForDuplicateCobraPosting(cobra_file_table_stage dataRow,
                  string versionNo)
        {
            string errorMessage = "";

            ////
            //switch (versionNo)
            //{
            //    default:
            //        string queryString =
            //            $"select * from  [cobra_file_table] " +
            //            $" where " +
            //            $" TpaId='{dataRow.TpaId}'" +
            //            $" and ClientName='{dataRow.ClientName}'" +
            //            $" and MemberId='{dataRow.MemberId}'" +
            //            $" and AccountTypeCode='{dataRow.AccountTypeCode}'" +
            //            $" and PlanEndDate='{dataRow.PlanEndDate}'" +
            //            $" and PlanStartDate='{dataRow.PlanStartDate}'" +
            //            $" and EffectiveDate='{dataRow.EffectiveDate}'" +
            //            $" and DepositType='{dataRow.DepositType}'" +
            //            $" and len(isnull(error_message, '')) = 0" +
            //            $" order by row_id desc, cobra_file_name, source_row_no ;";
            //        //
            //        DataTable dbResults = (DataTable)DbUtils.DbQuery(DbOperation.ExecuteReader, DbConn,
            //             queryString, null,
            //             FileLogParams?.GetMessageLogParams());
            //        //
            //        if (dbResults.Rows.Count == 0)
            //        {
            //            return false;
            //        }

            //        DataRow prvRow = dbResults.Rows[0];
            //        //
            //        errorMessage = $"Potential Duplicate Posting! Was probably posted earlier on {Utils.ToIsoDateString(prvRow["CreatedAt"])} as part of file  {prvRow["cobra_file_name"]}";
            //        break;

            //}

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddCobraErrorForRow(dataRow, "DuplicatePosting", $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public NPM CreateCobraNPMForDataRow(cobra_file_table_stage dataRow)
        {
            NPM npm = new NPM();

            //npm.AccountStructure = dataRow.AccountStructure;
            npm.Active = Utils.ToBool(dataRow.Active);
            npm.Address1 = dataRow.Address1;
            npm.Address2 = dataRow.Address2;
            //npm.AllowSSO = Utils.ToBool(dataRow.AllowSSO);
            //npm.BenefitGroup = dataRow.BenefitGroup;
            npm.City = dataRow.City;
            //NPM.ClientCustomData = dataRow.ClientCustomData;
            npm.ClientDivisionID = this.currentClientDivisionID;
            //npm.ClientID = this.currentClientID;
            npm.Country = dataRow.Country;
            //npm.DOB = Utils.ToDateTime(dataRow.DOB);
            npm.Email = dataRow.Email;
            //npm.EmployeeType = dataRow.EmployeeType;
            //NPM.EnteredByUser = dataRow.EnteredByUser;
            //NPM.EnteredDateTime = dataRow.EnteredDateTime;
            npm.FirstName = dataRow.FirstName;
            //npm.Gender = dataRow.Sex;
            //npm.IndividualIdentifier = "";
            //NPM.LastModifiedDate = dataRow.LastModifiedDate;
            npm.LastName = dataRow.LastName;
            npm.MemberID = int.MaxValue;
            //NPM.MethodEntered = dataRow.MethodEntered;
            npm.MiddleInitial = dataRow.MiddleInitial;
            //NPM.OnlineElectionProcessedDate = dataRow.OnlineElectionProcessedDate;
            //NPM.PaidThroughDate = dataRow.PaidThroughDate;
            //NPM.PayrollType = dataRow.PayrollType;
            npm.Phone = dataRow.Phone;
            npm.Phone2 = dataRow.Phone2;
            //npm.PlanCategory = dataRow.PlanCategory;
            npm.PostalCode = dataRow.PostalCode;
            //npm.PremiumCouponType = dataRow.PremiumCouponType;
            npm.Salutation = dataRow.Salutation;
            npm.SSN = dataRow.SSN;
            //npm.SSOIdentifier = dataRow.SSOIdentifier;
            npm.State = dataRow.StateOrProvince;
            //npm.TobaccoUse = dataRow.TobaccoUse;
            //npm.UsesHCTC = Utils.ToBool(dataRow.UsesHCTC);
            //npm.YearsOfService = Utils.ToInt(dataRow.YearsOfService);

            return npm;
        }

        public QB CreateCobraQBForDataRow(cobra_file_table_stage dataRow)
        {
            QB qb = new QB();

            qb.AccountStructure = dataRow.AccountStructure;
            qb.Active = Utils.ToBool(dataRow.Active);
            qb.Address1 = dataRow.Address1;
            qb.Address2 = dataRow.Address2;
            qb.AllowSSO = Utils.ToBool(dataRow.AllowMemberSSO);
            qb.BenefitGroup = dataRow.BenefitGroup;
            qb.City = dataRow.City;
            //qb.ClientCustomData = dataRow.ClientCustomData;
            qb.ClientDivisionID = this.currentClientDivisionID;
            qb.ClientID = this.currentClientID;
            qb.Country = dataRow.Country;
            qb.DOB = Utils.ToDateTime(dataRow.DOB);
            qb.Email = dataRow.Email;
            qb.EmployeeType = dataRow.EmployeeType;
            //qb.EnteredByUser = dataRow.EnteredByUser;
            //qb.EnteredDateTime = dataRow.EnteredDateTime;
            qb.FirstName = dataRow.FirstName;
            qb.Gender = dataRow.Sex;
            qb.IndividualIdentifier = "";
            //qb.LastModifiedDate = dataRow.LastModifiedDate;
            qb.LastName = dataRow.LastName;
            qb.MemberID = int.MaxValue;
            //qb.MethodEntered = dataRow.MethodEntered;
            qb.MiddleInitial = dataRow.MiddleInitial;
            //qb.OnlineElectionProcessedDate = dataRow.OnlineElectionProcessedDate;
            //qb.PaidThroughDate = dataRow.PaidThroughDate;
            //qb.PayrollType = dataRow.PayrollType;
            qb.Phone = dataRow.Phone;
            qb.Phone2 = dataRow.Phone2;
            qb.PlanCategory = dataRow.PlanCategory;
            qb.PostalCode = dataRow.PostalCode;
            qb.PremiumCouponType = dataRow.PremiumCouponType;
            qb.Salutation = dataRow.Salutation;
            qb.SSN = dataRow.SSN;
            qb.SSOIdentifier = dataRow.SSOIdentifier;
            qb.State = dataRow.StateOrProvince;
            qb.TobaccoUse = dataRow.TobaccoUse;
            qb.UsesHCTC = Utils.ToBool(dataRow.UsesHCTC);
            qb.YearsOfService = Utils.ToInt(dataRow.YearsOfService);

            return qb;
        }

        public SPM CreateCobraSPMForDataRow(cobra_file_table_stage dataRow)
        {
            SPM SPM = new SPM();

            SPM.AccountStructure = dataRow.AccountStructure;
            SPM.Active = Utils.ToBool(dataRow.Active);
            SPM.Address1 = dataRow.Address1;
            SPM.Address2 = dataRow.Address2;
            //SPM.AllowSSO = Utils.ToBool(dataRow.AllowSSO);
            SPM.BenefitGroup = dataRow.BenefitGroup;
            SPM.City = dataRow.City;
            //SPM.ClientCustomData = dataRow.ClientCustomData;
            SPM.ClientDivisionID = this.currentClientDivisionID;
            SPM.ClientID = this.currentClientID;
            SPM.Country = dataRow.Country;
            SPM.DOB = Utils.ToDateTime(dataRow.DOB);
            SPM.Email = dataRow.Email;
            SPM.EmployeeType = dataRow.EmployeeType;
            //SPM.EnteredByUser = dataRow.EnteredByUser;
            //SPM.EnteredDateTime = dataRow.EnteredDateTime;
            SPM.FirstName = dataRow.FirstName;
            SPM.Gender = dataRow.Sex;
            //SPM.IndividualIdentifier = "";
            //SPM.LastModifiedDate = dataRow.LastModifiedDate;
            SPM.LastName = dataRow.LastName;
            SPM.MemberID = int.MaxValue;
            //SPM.MethodEntered = dataRow.MethodEntered;
            SPM.MiddleInitial = dataRow.MiddleInitial;
            //SPM.OnlineElectionProcessedDate = dataRow.OnlineElectionProcessedDate;
            //SPM.PaidThroughDate = dataRow.PaidThroughDate;
            //SPM.PayrollType = dataRow.PayrollType;
            SPM.Phone = dataRow.Phone;
            SPM.Phone2 = dataRow.Phone2;
            SPM.PlanCategory = dataRow.PlanCategory;
            SPM.PostalCode = dataRow.PostalCode;
            SPM.PremiumCouponType = dataRow.PremiumCouponType;
            SPM.Salutation = dataRow.Salutation;
            SPM.SSN = dataRow.SSN;
            SPM.SSOIdentifier = dataRow.SSOIdentifier;
            SPM.State = dataRow.StateOrProvince;
            SPM.TobaccoUse = dataRow.TobaccoUse;
            //SPM.UsesHCTC = Utils.ToBool(dataRow.UsesHCTC);
            SPM.YearsOfService = Utils.ToInt(dataRow.YearsOfService);

            return SPM;
        }

        public Client GetCobraClient(int clientID)
        {
            Client row;
            // check DB
            if (clientID <= 0)
            {
                row = new Client();
            }
            else
            {
                DbSet<Client> dbResults = GetAllCobraClients();

                // planid is not always present e.g. in deposit file
                var rows = dbResults.Where(x => x.ClientID == clientID);

                var dbRows = rows
                            .AsNoTracking()
                            .ToList();

                row = dbRows.FirstOrDefault();
            } // check client

            return row;
        }

        public List<Client> GetCobraClient(cobra_file_table_stage dataRow)
        {
            List<Client> clientRows = null;
            // check DB
            if (Utils.IsBlank(dataRow.ClientName))
            {
                clientRows = new List<Client>();
            }
            else
            {
                DbSet<Client> dbResults = GetAllCobraClients();

                // planid is not always present e.g. in deposit file
                var rows = dbResults.Where(x => x.ClientName == dataRow.ClientName);

                //
                clientRows = rows
                            .AsNoTracking()
                            .ToList();
            } // check client
              //

            return clientRows;
        }

        public List<ClientPlanQB> GetCobraClientPlanQBs(int clientID, cobra_file_table_stage dataRow)
        {
            List<ClientPlanQB> dbRows = null;
            // check DB
            if (clientID <= 0)
            {
                dbRows = new List<ClientPlanQB>();
            }
            else
            {
                DbSet<ClientPlanQB> dbResults = GetAllCobraClientPlanQBs();

                // planid is not always present e.g. in deposit file
                var rows = dbResults.Where(x => x.ClientID == clientID);

                dbRows = rows
                            .AsNoTracking()
                            .ToList();
            } // check client

            return dbRows;
        }

        public List<ClientPlanSPM> GetCobraClientPlanSPMs(int clientID, cobra_file_table_stage dataRow)
        {
            List<ClientPlanSPM> dbRows = null;
            // check DB
            if (clientID <= 0)
            {
                dbRows = new List<ClientPlanSPM>();
            }
            else
            {
                DbSet<ClientPlanSPM> dbResults = GetAllCobraClientPlanSPMs();

                // planid is not always present e.g. in deposit file
                var rows = dbResults.Where(x => x.ClientID == clientID);

                dbRows = rows
                            .AsNoTracking()
                            .ToList();
            } // check client

            return dbRows;
        }

        public NPM GetCobraNPM(cobra_file_table_stage dataRow)
        {
            NPM row = null;

            // check DB
            if (currentClientID <= 0 || currentClientDivisionID <= 0)
            {
                AddCobraErrorForRow(dataRow, "ClientDivisionId", $"The Client and Division ID cannot be blank");
            }
            else if (Utils.IsBlank(dataRow.SSN))
            {
                AddCobraErrorForRow(dataRow, "SSN", $"The SSN cannot be blank");
            }
            else
            {
                if (currentClientDivisionID > 0)
                {
                    row = currentClientDivision.NPMs
                     .Where(x => x.SSN == dataRow.SSN || x.SSN == Utils.FormatSsnWithDashes(dataRow.SSN))
                     .ToList()
                     .FirstOrDefault();
                }
            }
            //
            return row;
        }

        public QB GetCobraQB(cobra_file_table_stage dataRow)
        {
            QB row = null;

            // check DB
            if (currentClientID <= 0)
            {
                AddCobraErrorForRow(dataRow, "clientId", $"The ClientID cannot be blank");
            }
            else if (Utils.IsBlank(dataRow.SSN))
            {
                AddCobraErrorForRow(dataRow, "SSN", $"The SSN cannot be blank");
            }
            else
            {
                if (currentClientDivisionID > 0)
                {
                    row = currentClientDivision.QBs
                     .Where(x => x.SSN == dataRow.SSN || x.SSN == Utils.FormatSsnWithDashes(dataRow.SSN))
                     .ToList()
                     .FirstOrDefault();
                }

                if (row == null)
                {
                    row = currentClient.QBs
                         .Where(x => x.SSN == dataRow.SSN || x.SSN == Utils.FormatSsnWithDashes(dataRow.SSN))
                         .ToList()
                         .FirstOrDefault();
                }
            }
            //
            return row;
        }

        public SPM GetCobraSPM(cobra_file_table_stage dataRow)
        {
            SPM row = null;

            // check DB
            if (currentClientID <= 0)
            {
                AddCobraErrorForRow(dataRow, "clientId", $"The ClientID cannot be blank");
            }
            else if (Utils.IsBlank(dataRow.SSN))
            {
                AddCobraErrorForRow(dataRow, "SSN", $"The SSN cannot be blank");
            }
            else
            {
                if (currentClientDivisionID > 0)
                {
                    row = currentClientDivision.SPMs
                     .Where(x => x.SSN == dataRow.SSN || x.SSN == Utils.FormatSsnWithDashes(dataRow.SSN))
                     .ToList()
                     .FirstOrDefault();
                }

                if (row == null)
                {
                    row = currentClient.SPMs
                         .Where(x => x.SSN == dataRow.SSN || x.SSN == Utils.FormatSsnWithDashes(dataRow.SSN))
                         .ToList()
                         .FirstOrDefault();
                }
            }
            //
            return row;
        }

        public void SetCobraClientAndDivision(cobra_file_table_stage dataRow)
        {
            Client row = new Client();
            var errorMessage = "";
            List<Client> dbRows = GetCobraClient(dataRow);

            if (dbRows.Count == 0)
            {
                errorMessage = $"The Client Name '{dataRow.ClientName}' could not be found";
                this.AddCobraErrorForRow(dataRow, "ClientAndDivision", errorMessage);
            }
            else
            {
                row = dbRows.FirstOrDefault();
                if (row == null || row.ClientID <= 0)
                {
                    this.currentClient = null;
                    this.currentClientDivision = null;
                }
                else if (!Utils.IsBlank(dataRow.ClientDivisionName))
                {
                    this.currentClient = row;

                    //
                    var div = row.ClientDivisions.Where(x => x.DivisionName == dataRow.ClientDivisionName).ToList().FirstOrDefault();
                    if (div != null || div.ClientDivisionID > 0)
                    {
                        this.currentClientDivision = div;
                    }
                }
            } // results.count
        }

        private void AddCobraErrorForRow(cobra_file_table_stage dataRow, string errCode, string errMessage,
            Boolean markAsCompleteFail = false)
        {
            // add to dataRow so it will be saved back to DB for dataRow by dataRow data

            if (Utils.IsBlank(dataRow.error_code))
            {
                dataRow.error_code = errCode;
            }
            else
            {
                dataRow.error_code = ErrorSeparator + errCode;
            }

            if (Utils.IsBlank(dataRow.error_message))
            {
                dataRow.error_message = errMessage;
            }
            else
            {
                dataRow.error_message = ErrorSeparator + errMessage;
            }

            if (dataRow.error_code.StartsWith(ErrorSeparator))
            {
                dataRow.error_code = dataRow.error_code.Substring(1);
            }

            if (dataRow.error_message.StartsWith(ErrorSeparator))
            {
                dataRow.error_message = dataRow.error_message.Substring(1);
            }

            //
            int key = dataRow.source_row_no ?? 0;
            if (this.fileCheckResults.ContainsKey(key))
            {
                this.fileCheckResults[key] = $"{dataRow.source_row_no}: {errCode} : {errMessage}";
            }
            else
            {
                this.fileCheckResults.Add(key, $"{dataRow.source_row_no}: {errCode} : {errMessage}");
            }

            //
            if (markAsCompleteFail)
            {
                this.fileCheckResults.MarkAsCompleteFail = true;
            }
        }

        private void CheckCobraFile(string currentFilePath)
        {
            string tableName = "";
            TypedCsvSchema mappings = new TypedCsvSchema();
            string postImportProc = "";

            // check mappings and type of file (Import or Result)
            Boolean isResultFile = Import.GetCobraFileFormatIsResultFile(currentFilePath);
            if (isResultFile)
            {
                return;
            }

            //
            tableName = "[dbo].[cobra_file_table_stage]";
            postImportProc = "[dbo].[process_cobra_file_table_stage_import]";
            //
            Import.ImportCobraFile(DbConn, currentFilePath, currentFilePath, this.hasHeaderRow, FileLogParams, null);

            //
            // update check type for table
            string queryString1 = $" update {tableName} set " +
                                  $" /* set check type */check_type = 'PreCheck', " +
                                  $" /* remove extra csv commas added to line */ data_row = replace(data_row, ',,,,,,,,,,,,,,,,,,,,', '') " +
                                  $" where 1 = 1;";
            DbUtils.DbQuery(DbOperation.ExecuteNonQuery, DbConn, queryString1, null,
                FileLogParams?.GetMessageLogParams()
            );

            // check file data from the table based on mappings
            CheckCobraFileData();

            // run post import proc to take data from stage into final table
            string queryString = $"exec {postImportProc};";
            //
            DbUtils.DbQuery(DbOperation.ExecuteNonQuery, DbConn, queryString, null,
                FileLogParams?.GetMessageLogParams()
            );
        }

        private void CheckCobraFileData()
        {
            // ensure previously cached data is not used so
            // so create a new db context to ensure stale data will NOT be used
            var dbDataProcessing = Vars.dbCtxDataProcessingNew;

            // get all dbRows without caching
            var dataRows = dbDataProcessing.cobra_file_table_stage
                .OrderBy(dataRow => dataRow.source_row_no)
                //.AsNoTracking()
                .ToList();

            var versionNo = Import.GetCobraFileVersionNoFromFile(SrcFilePath);

            //check each dataRow
            int rowNo = 0;
            foreach (var dataRow in dataRows)
            {
                // get row format for current lione as file can have multiple row types
                string rowType = dataRow.row_type;

                // get mappings for event type & version
                TypedCsvSchema mappings = Import.GetCobraFileImportMappings(rowType, versionNo);

                rowNo++;
                try
                {
                    // clear any previous values
                    dataRow.error_code = "";
                    dataRow.error_message = "";
                    //
                    this.CheckCobraFileData(versionNo, dataRow, mappings);
                }
                catch (Exception ex)
                {
                    this.AddCobraErrorForRow(dataRow, "Error", ex.Message);
                }
            }

            // save any changes
            dbDataProcessing.SaveChanges();
        }

        private void CheckCobraFileData(string versionNo, cobra_file_table_stage dataRow, TypedCsvSchema mappings)
        {
            // don't check header dataRow
            if (dataRow.row_type == "")
            {
                return;
            }

            Boolean lineHasError = false;

            // first fix all columns
            foreach (var column in mappings.Columns)
            {
                // if previous column caused an error, skip other columns
                if (lineHasError)
                {
                    break;
                }

                // skip some columns
                switch (column.SourceColumn?.ToLowerInvariant() ?? "")
                {
                    case "":
                    case "source_row_no":
                    case "error_row":
                    case "data_row":
                    case "res_file_name":
                    case "cobra_file_name":
                    case "row_type":
                    case "check_type":
                        continue;
                    //
                    default:
                        break;
                }

                // save Org Column value before changes
                var orgValue = dataRow.ColumnValue(column.SourceColumn) ?? "";

                // 1. valid Format and general rules check - save corrected value to row
                var formattedValue = EnsureValueIsOfFormatAndMatchesRules(dataRow, column, mappings);
            }

            // check record data and logic
            lineHasError = this.CheckCobraRecordData(versionNo, dataRow, mappings);
            if (lineHasError)
            {
                return;
            }

            // just check data constraints for each column
            foreach (var column in mappings.Columns)
            {
                // skip some columns
                switch (column.SourceColumn?.ToLowerInvariant() ?? "")
                {
                    case "":
                    case "source_row_no":
                    case "error_row":
                    case "data_row":
                    case "res_file_name":
                    case "cobra_file_name":
                    case "row_type":
                    case "check_type":
                        continue;
                    //
                    default:
                        break;
                }
            }

            // check for duplicate posting of the row
            if (!lineHasError)
            {
                lineHasError = CheckForDuplicateCobraPosting(dataRow, versionNo);
            }
            // check for duplicate posting of the row
        }

        private DbSet<ClientPlanQB> GetAllCobraClientPlanQBs()
        {
            DbSet<ClientPlanQB> dbResults = null;
            //
            dbResults = dbCtxCobra.ClientPlanQBs;
            //
            return dbResults;
        }

        private DbSet<ClientPlanSPM> GetAllCobraClientPlanSPMs()
        {
            DbSet<ClientPlanSPM> dbResults = null;
            //
            dbResults = dbCtxCobra.ClientPlanSPMs;
            //
            return dbResults;
        }

        private DbSet<Client> GetAllCobraClients()
        {
            DbSet<Client> dbResults = null;
            //
            dbResults = dbCtxCobra.Clients;
            //
            return dbResults;
        }

        #endregion CheckCobraFile

        #region CheckUtils

        public string EnsureValueIsOfFormatAndMatchesRules(cobra_file_table_stage dataRow, TypedCsvColumn column,
            TypedCsvSchema mappings)
        {
            var orgValue = dataRow.ColumnValue(column.SourceColumn) ?? "";
            var value = orgValue;

            // always trim
            value = value?.Trim();

            //1. Check and fix format
            if (!Utils.IsBlank(value))
            {
                // fix value if possible
                switch (column.FormatType)
                {
                    case FormatType.Any:
                    case FormatType.String:
                        break;

                    case FormatType.Email:
                        if (!Utils.IsValidEmail(value))
                        {
                            this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be a valid Email. '{orgValue}' is not valid");
                        }

                        break;

                    case FormatType.Zip:
                        value = Utils.regexInteger.Replace(value, String.Empty);
                        if (!Utils.IsBlank(value) && value.Length > column.MaxLength)
                        {
                            value = value.Substring(0, column.MaxLength);
                        }

                        break;

                    case FormatType.Phone:
                        value = Utils.regexInteger.Replace(value, String.Empty);
                        if (!Utils.IsBlank(value) && value.Length > column.MaxLength)
                        {
                            value = Utils.Right(value, column.MaxLength);
                        }

                        break;

                    case FormatType.AlphaNumeric:
                        // replace all non alphanumeric
                        value = Utils.regexAlphaNumeric.Replace(value, String.Empty);
                        break;

                    case FormatType.AlphaNumericAndDashes:
                        // replace all non alphanumeric
                        value = Utils.regexAlphaNumericAndDashes.Replace(value, String.Empty);
                        break;

                    case FormatType.AlphaOnly:
                        // replace all non alphanumeric
                        value = Utils.regexAlphaOnly.Replace(value, String.Empty);
                        break;

                    case FormatType.FixedConstant:
                        // default to fixed value always!
                        value = column.FixedValue;
                        break;

                    case FormatType.AlphaAndDashes:
                        // replace all non alphanumeric
                        value = Utils.regexAlphaAndDashes.Replace(value, String.Empty);
                        break;

                    case FormatType.NumbersAndDashes:
                        // replace all non alphanumeric
                        value = Utils.regexNumericAndDashes.Replace(value, String.Empty);
                        break;

                    case FormatType.Integer:
                        // remove any non digits
                        //todo: Sumeet: dont replace any invalid characters - just trim spaces and commas
                        //value = Utils.regexInteger.Replace(value, String.Empty);
                        value = value.Replace(",", "").Replace(" ", "");
                        //
                        if (!Utils.IsInteger(value))
                        {
                            this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be numbers only. '{orgValue}' is not valid");
                        }
                        else
                        {
                            // format as 0
                            var intValue = Utils.ToInt(value);
                            value = intValue.ToString("0");
                        }

                        break;

                    case FormatType.Double:
                        // remove any non digits and non . and non ,
                        //todo: Sumeet: dont replace any invalid characters - just trim spaces and commas
                        //value = Utils.regexDouble.Replace(value, String.Empty);
                        value = value.Replace(",", "").Replace(" ", "");

                        if (!Utils.IsDouble(value))
                        {
                            this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be a Currency Value. '{orgValue}' is not valid");
                        }
                        else
                        {
                            // format as 0.00
                            var dblValue = Utils.ToDouble(value);
                            value = dblValue.ToString("0.00");
                        }
                        break;

                    case FormatType.IsoDate:
                        // remove any non digits
                        value = Utils.regexDate.Replace(value, String.Empty);
                        try
                        {
                            value = Utils.ToIsoDateString(Utils.ToDate(value));
                            if (!Utils.IsIsoDate(value, column.MaxLength > 0))
                            {
                                this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                                    $"{column.SourceColumn} must be in format YYYYMMDD. '{orgValue}' is not valid");
                            }
                        }
                        catch (Exception ex)
                        {
                            this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be in format YYYYMMDD. '{orgValue}' is not valid");
                        }

                        break;

                    case FormatType.IsoDateTime:
                        // remove any non digits
                        value = Utils.regexDate.Replace(value, String.Empty);
                        try
                        {
                            value = Utils.ToDateTimeString(Utils.ToDateTime(value));

                            if (!Utils.IsIsoDateTime(value, column.MaxLength > 0))
                            {
                                this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                                    $"{column.SourceColumn} must be in format YYYYMMDD HHMMSS. '{orgValue}' is not valid");
                            }
                        }
                        catch (Exception ex)
                        {
                            this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be in format YYYYMMDD HHMMSS. '{orgValue}' is not valid");
                        }

                        break;

                    case FormatType.CobraDate:
                        // remove any non digits
                        value = Utils.regexDate.Replace(value, String.Empty);
                        try
                        {
                            value = Utils.ToCobraDateString(Utils.ToDateTime(value));

                            if (!Utils.IsCobraDate(value, column.MaxLength > 0))
                            {
                                this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                                    $"{column.SourceColumn} must be in format MM/DD/YYYY. '{orgValue}' is not valid");
                            }
                        }
                        catch (Exception ex)
                        {
                            this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be in format MM/DD/YYYY. '{orgValue}' is not valid");
                        }

                        break;

                    case FormatType.CobraDateTime:
                        // remove any non digits
                        value = Utils.regexDate.Replace(value, String.Empty);
                        try
                        {
                            value = Utils.ToCobraDateTimeString(Utils.ToDateTime(value));

                            if (!Utils.IsCobraDateTime(value, column.MaxLength > 0))
                            {
                                this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                                    $"{column.SourceColumn} must be in format MM/DD/YYYY HH:mm AM. '{orgValue}' is not valid");
                            }
                        }
                        catch (Exception ex)
                        {
                            this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be in format MM/DD/YYYY HH:mm AM. '{orgValue}' is not valid");
                        }

                        break;

                    case FormatType.CobraYesNo:
                    case FormatType.YesNo:
                    case FormatType.TrueFalse:
                        switch (value.ToUpperInvariant())
                        {
                            case "TRUE":
                            case "T":
                            case "YES":
                            case "1":
                                value = "1";
                                break;

                            case "NO":
                            case "FALSE":
                            case "F":
                            case "0":
                                value = "0";
                                break;
                        }

                        if (!value.Equals("1", StringComparison.InvariantCultureIgnoreCase) &&
                            !value.Equals("0", StringComparison.InvariantCultureIgnoreCase))
                        {
                            this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                                $"{column.SourceColumn} must be be either 1/Yes/True or 0/No/False. '{orgValue}' is not valid");
                        }

                        break;

                    default:
                        break;
                }
            }

            value = value?.Trim();

            //set default value
            if (Utils.IsBlank(value) && !Utils.IsBlank(column.DefaultValue))
            {
                value = column.DefaultValue;
            }

            // pad ssn to 9 digits with leading zeros
            if ((column.SourceColumn == "EmployeeSocialSecurityNumber" || column.SourceColumn == "SSN" || column.SourceColumn == "MemberId" || column.FormatType == FormatType.SSN))

            {
                if (!Utils.IsBlank(value))
                {
                    value = Utils.regexAlphaNumeric.Replace(value, String.Empty);
                    if (value.Length < column.MinLength)
                    {
                        value = value.PadLeft(column.MinLength, '0');
                    }
                }
            }

            if (!Utils.IsBlank(column.FixedValue) && value != column.FixedValue && column.MinLength > 0 && (!column.FixedValue.Split('|').Contains(value)))
            {
                if (column.FixedValue == "<BLANK>" & value != "")
                {
                    value = "";
                }
                else
                {
                    this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                        $"{column.SourceColumn} must always be one of or ezxactly {column.FixedValue}. '{orgValue}' is not valid");
                }
            }

            // set row column value to the fixed value if it has changed
            if (value != orgValue)
            {
                dataRow.SetColumnValue(column.SourceColumn, value);
                dataRow.data_row = GetDelimitedDataRow(dataRow, mappings);
            }

            // 2. check against GENERAL rules
            // 2b. check matches possible values

            // minLength
            if (column.MinLength > 0 && value.Length < column.MinLength)
            {
                this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                    $"{column.SourceColumn} must be minimum {column.MinLength} characters long. '{orgValue}' is not valid");
            }

            // maxLength
            if (column.MaxLength > 0 && value.Length > column.MaxLength)
            {
                this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                    $"{column.SourceColumn} must be maximum {column.MaxLength} characters long. '{orgValue}' is not valid");
            }

            // min/max value
            if (column.MinValue != 0 || column.MaxValue != 0)
            {
                if (!Utils.IsNumeric(value))
                {
                    this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number. '{orgValue}' is not valid");
                }

                float numValue = Utils.ToNumber(value);
                if (numValue < column.MinValue)
                {
                    this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number with a value greater than ${column.MinValue}. '{orgValue}' is not valid");
                }

                if (numValue > column.MaxValue)
                {
                    this.AddCobraErrorForRow(dataRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number with a value less than ${column.MaxValue}. '{orgValue}' is not valid");
                }
            }

            return value;
        }

        private string GetDelimitedDataRow(cobra_file_table_stage dataRow, TypedCsvSchema mappings)
        {
            string value = "";

            foreach (TypedCsvColumn column in mappings)
            {
                switch (column.SourceColumn?.ToLowerInvariant() ?? "")
                {
                    case "":
                    case "source_row_no":
                    case "error_row":
                    case "data_row":
                    case "res_file_name":
                    case "cobra_file_name":
                    case "check_type":
                        continue;
                    //
                    default:
                        break;
                }

                string fieldValue = dataRow.ColumnValue(column.SourceColumn);
                if (fieldValue?.IndexOf(",", StringComparison.InvariantCulture) > 0)
                {
                    fieldValue = $"\"{fieldValue}\"";
                }

                value += $",{fieldValue}";
            }

            // remove first char
            value = value.Substring(1);
            //
            return value;
        }

        #endregion CheckUtils
    }
}