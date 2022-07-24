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

        public Boolean CheckQBPlanNotExists(string versionNo, cobra_file_table_stage fileRow, TypedCsvSchema mappings)
        {
            string errorMessage = "";

            var plan = GetQBPlan(versionNo, fileRow, mappings);

            if (plan != null && plan.MemberID > 0)
            {
                if (currentClientDivisionID > 0)
                {
                    errorMessage += $"Duplicate QB Plan. The Plan '{fileRow.PlanName}' already exists for Client '{currentClient.ClientName}' and Division '{currentClientDivision.DivisionName}' and SSN '{currentQB.SSN}'";
                }
                else
                {
                    errorMessage += $"Duplicate QB Plan. The Plan '{fileRow.PlanName}' already exists for Client '{currentClient.ClientName}' and SSN '{currentQB.SSN}'";
                }
            }

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddCobraErrorForRow(fileRow, "Duplicate QB Plan", $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public QBPlan GetQBPlan(string versionNo, cobra_file_table_stage fileRow, TypedCsvSchema mappings)
        {
            var clientPlan = GetClientQBPlan(versionNo, fileRow, mappings);
            if (clientPlan == null || clientPlan.ClientID <= 0)
            {
                return null;
            }


            // check client+division has plan
            var qbPlans = clientPlan.QBPlans
                .Where(
                    x => x.MemberID == currentQB.MemberID
                    )
                .ToList();
            //
            return qbPlans.FirstOrDefault();

            //
        }

        public Boolean CheckClientQBPlanExists(string versionNo, cobra_file_table_stage fileRow, TypedCsvSchema mappings)
        {
            string errorMessage = "";

            ClientPlanQB plan = GetClientQBPlan(versionNo, fileRow, mappings);

            if (plan == null || plan.ClientID <= 0)
            {
                if (currentClientDivisionID > 0)
                {
                    errorMessage += $"The Plan '{fileRow.PlanName}' could not found for Client '{currentClient.ClientName}' and Division '{currentClientDivision.DivisionName}'";
                }
                else
                {
                    errorMessage += $"The Plan '{fileRow.PlanName}' could not found for Client '{currentClient.ClientName}'";
                }
            }

            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddCobraErrorForRow(fileRow, "No Such Plan Found", $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public ClientPlanQB GetClientQBPlan(string versionNo, cobra_file_table_stage fileRow, TypedCsvSchema mappings)
        {

            // check client+division has plan
            var clientPlans = currentClient.ClientPlanQBs
                .Where(
                    x => x.PlanName == fileRow.PlanName
                    )
                .ToList();
            //
            if (clientPlans.Count == 0)
            {
                return null;
            }
            else if (currentClientDivisionID > 0)
            {
                foreach (var clientPlan in clientPlans)
                {
                    var divPlans = currentClientDivision.ClientDivisionQBPlans.Where(x => x.ClientPlanQBID == clientPlan.ClientPlanQBID).ToList();

                    //
                    if (divPlans.Count > 0)
                    {
                        return clientPlan;
                    }
                }
            }
            return null;
            //
        }


        public Boolean CheckCobraRecordData(string versionNo, cobra_file_table_stage fileRow, TypedCsvSchema mappings)
        {
            Boolean lineHasError;

            // is start of a main entity
            switch (fileRow.row_type.ToUpper())
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
                    lineHasError = this.CheckCobraRecordDataMainEntity(versionNo, fileRow, mappings);
                    if (lineHasError)
                    {
                        return true;
                    }

                    break;

                // none of the above major entity types
                default:
                    lineHasError = CheckCobraRecordDataIsQBSubline(versionNo, fileRow, mappings);
                    if (lineHasError)
                    {
                        return true;
                    }

                    lineHasError = CheckCobraRecordDataIsSPMSubline(versionNo, fileRow, mappings);
                    if (lineHasError)
                    {
                        return true;
                    }
                    lineHasError = CheckCobraRecordDataIsNPMSubline(versionNo, fileRow, mappings);
                    if (lineHasError)
                    {
                        return true;
                    }

                    // break out of switch ((fileRow.row_type.ToUpper()))
                    break;
            }

            //
            return false;
        }

        public Boolean CheckCobraRecordDataIsNPMSubline(string versionNo, cobra_file_table_stage fileRow, TypedCsvSchema mappings)
        {
            Boolean lineHasError = false;

            // check which type of entity subline it is and whether a main entity line ocurrs before this
            if (Utils.Left(fileRow.row_type.ToUpper(), 4) == "[NPM")
            {
                // subline of NPM
                if (this.currentNPM == null || Utils.IsBlank(this.currentNPM.SSN))
                {
                    this.AddCobraErrorForRow(fileRow, "No Current NPM", $"Could Not Process this Record Type '{fileRow.row_type}' as no Valid NPM is referenced above");
                    return true;
                }

                // check specific duplicates or missing plans
                switch ((fileRow.row_type.ToUpper()))
                {
                    default:
                        break;
                } // switch ((fileRow.row_type.ToUpper()))
            } // NPM line types

            return lineHasError;
        }

        public Boolean CheckCobraRecordDataIsQBSubline(string versionNo, cobra_file_table_stage fileRow, TypedCsvSchema mappings)
        {
            Boolean lineHasError = false;

            // check which type of entity subline it is and whether a main entity line ocurrs before this
            if (Utils.Left(fileRow.row_type.ToUpper(), 3) == "[QB" || Utils.Left(fileRow.row_type.ToUpper(), 3) != "ME")
            {
                // subline of QB
                if (this.currentQB == null || Utils.IsBlank(this.currentQB.SSN))
                {
                    this.AddCobraErrorForRow(fileRow, "No Current QB", $"Could Not Process this Record Type '{fileRow.row_type}' as no Valid QB is referenced above");
                    return true;
                }

                // check specific duplicates or missing plans
                switch ((fileRow.row_type.ToUpper()))
                {
                    case "[QBPLANINITIAL]":
                        // check client+division has plan
                        lineHasError = CheckClientQBPlanExists(versionNo, fileRow, mappings);
                        if (!lineHasError)
                        {
                            lineHasError = CheckQBPlanNotExists(versionNo, fileRow, mappings);
                        }
                        break;

                    case "[QBPLAN]":
                        // check client+division has plan
                        lineHasError = CheckClientQBPlanExists(versionNo, fileRow, mappings);
                        if (!lineHasError)
                        {
                            lineHasError = CheckQBPlanNotExists(versionNo, fileRow, mappings);
                        }
                        break;

                    case "[QBDEPENDENTPLANINITIAL]":
                        // check client+division has plan
                        lineHasError = CheckClientQBPlanExists(versionNo, fileRow, mappings);
                        break;

                    case "[QBDEPENDENTPLAN]":
                        // check client+division has plan
                        lineHasError = CheckClientQBPlanExists(versionNo, fileRow, mappings);
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
                } // switch ((fileRow.row_type.ToUpper()))
            } // QB line types

            return lineHasError;
        }

        public Boolean CheckCobraRecordDataIsSPMSubline(string versionNo, cobra_file_table_stage fileRow, TypedCsvSchema mappings)
        {
            Boolean lineHasError = false;

            // check which type of entity subline it is and whether a main entity line ocurrs before this
            if (Utils.Left(fileRow.row_type.ToUpper(), 4) == "[SPM")
            {
                // subline of SPM
                if (this.currentSPM == null || Utils.IsBlank(this.currentSPM.SSN))
                {
                    this.AddCobraErrorForRow(fileRow, "No Current SPM", $"Could Not Process this Record Type '{fileRow.row_type}' as no Valid SPM is referenced above");
                    return true;
                }

                // check specific duplicates or missing plans
                switch ((fileRow.row_type.ToUpper()))
                {
                    default:
                        break;
                } // switch ((fileRow.row_type.ToUpper()))
            } // SPM line types

            return lineHasError;
        }

        public Boolean CheckCobraRecordDataMainEntity(string versionNo, cobra_file_table_stage fileRow, TypedCsvSchema mappings)
        {
            // clear any previous cached record
            this.currentQB = null;
            this.currentSPM = null;
            this.currentNPM = null;

            //get client and division
            this.SetCobraClientAndDivision(fileRow);
            if (this.currentClientID <= 0)
            {
                this.AddCobraErrorForRow(fileRow, "Client And DiVision Does Not Exist", $"Client with '{fileRow.ClientName}' and Division Name '{fileRow.ClientDivisionName}' Not Found. ");
                return true;
            }

            // check if it is the start of a new EntityType
            switch (fileRow.row_type)
            {
                case "[QB]":
                case "[QBLEGACY]":

                    // check QB exists - if so raise error
                    QB theQB = this.GetCobraQB(fileRow);
                    if (theQB != null && theQB.MemberID > 0 && this.FailIfMainEntityAlreadyExists)
                    {
                        this.AddCobraErrorForRow(fileRow, "QB Already exists", $"Duplicate QB. QB with SSN '{theQB.SSN}' already exists");
                        return true;
                    }

                    //OK - there is no such QB in the database - we instantiate  one for checkiong events etc
                    this.currentQB = this.CreateCobraQBForDataRow(fileRow);

                    break;

                case "[QBLOOKUP]":

                    // check QB exists - if so raise error
                    QB theQB2 = this.GetCobraQB(fileRow);
                    if (theQB2 != null && theQB2.MemberID > 0)
                    {
                        //ok
                    }
                    else
                    {
                        this.AddCobraErrorForRow(fileRow, "QB Not Found", $"QB with SSN '{theQB2.SSN}' not found");
                        return true;
                    }

                    //OK - there is no such QB in the database - we instantiate  one for checkiong events etc
                    this.currentQB = this.CreateCobraQBForDataRow(fileRow);

                    break;

                case "[SPM]":

                    // check division exists
                    this.SetCobraClientAndDivision(fileRow);

                    // check SPM exists - if so raise error
                    SPM theSPM = this.GetCobraSPM(fileRow);
                    if (theSPM != null && theSPM.MemberID > 0 && this.FailIfMainEntityAlreadyExists)
                    {
                        this.AddCobraErrorForRow(fileRow, "SPM Already exists", $"Duplicate SPM. SPM with SSN '{theSPM.SSN}' already exists");
                        return true;
                    }

                    //OK - there is no such SPM in the database - we instantiate  one for checkiong events etc
                    this.currentSPM = this.CreateCobraSPMForDataRow(fileRow);

                    break;

                case "[SPMLOOKUP]":

                    // check SPM exists - if so raise error
                    SPM theSPM2 = this.GetCobraSPM(fileRow);
                    if (theSPM2 != null && theSPM2.MemberID > 0)
                    {
                        //ok
                    }
                    else
                    {
                        this.AddCobraErrorForRow(fileRow, "SPM Not Found", $"SPM with SSN '{theSPM2.SSN}' not found");
                        return true;
                    }

                    //OK - there is no such SPM in the database - we instantiate  one for checkiong events etc
                    this.currentSPM = this.CreateCobraSPMForDataRow(fileRow);

                    break;

                case "[NPM]":

                    // check division exists
                    this.SetCobraClientAndDivision(fileRow);

                    // check NPM exists - if so raise error
                    NPM theNPM = this.GetCobraNPM(fileRow);
                    if (theNPM != null && theNPM.MemberID > 0 && this.FailIfMainEntityAlreadyExists)
                    {
                        this.AddCobraErrorForRow(fileRow, "NPM Already exists", $"Duplicate NPM. NPM with SSN '{theNPM.SSN}' already exists");
                        return true;
                    }

                    //OK - there is no such NPM in the database - we instantiate  one for checkiong events etc
                    this.currentNPM = this.CreateCobraNPMForDataRow(fileRow);

                    break;

                case "[NPMLOOKUP]":

                    // check NPM exists - if so raise error
                    NPM theNPM2 = this.GetCobraNPM(fileRow);
                    if (theNPM2 != null && theNPM2.MemberID > 0)
                    {
                        //ok
                    }
                    else
                    {
                        this.AddCobraErrorForRow(fileRow, "NPM Not Found", $"NPM with SSN '{theNPM2.SSN}' not found");
                        return true;
                    }

                    //OK - there is no such NPM in the database - we instantiate  one for checkiong events etc
                    this.currentNPM = this.CreateCobraNPMForDataRow(fileRow);

                    break;
            }

            //
            return false;
        }


        public Boolean CheckForDuplicateCobraPosting(cobra_file_table_stage fileRow,
                  string versionNo)
        {
            string errorMessage = "";


            //
            if (!Utils.IsBlank(errorMessage))
            {
                this.AddCobraErrorForRow(fileRow, "DuplicatePosting", $"{errorMessage}");
                // do not check any more
                return true;
            }
            else
            {
                return false;
            }
        }

        public NPM CreateCobraNPMForDataRow(cobra_file_table_stage fileRow)
        {
            NPM npm = new NPM();

            //npm.AccountStructure = fileRow.AccountStructure;
            npm.Active = Utils.ToBool(fileRow.Active);
            npm.Address1 = fileRow.Address1;
            npm.Address2 = fileRow.Address2;
            //npm.AllowSSO = Utils.ToBool(fileRow.AllowSSO);
            //npm.BenefitGroup = fileRow.BenefitGroup;
            npm.City = fileRow.City;
            //NPM.ClientCustomData = fileRow.ClientCustomData;
            npm.ClientDivisionID = this.currentClientDivisionID;
            //npm.ClientID = this.currentClientID;
            npm.Country = fileRow.Country;
            //npm.DOB = Utils.ToDateTime(fileRow.DOB);
            npm.Email = fileRow.Email;
            //npm.EmployeeType = fileRow.EmployeeType;
            //NPM.EnteredByUser = fileRow.EnteredByUser;
            //NPM.EnteredDateTime = fileRow.EnteredDateTime;
            npm.FirstName = fileRow.FirstName;
            //npm.Gender = fileRow.Sex;
            //npm.IndividualIdentifier = "";
            //NPM.LastModifiedDate = fileRow.LastModifiedDate;
            npm.LastName = fileRow.LastName;
            npm.MemberID = int.MaxValue;
            //NPM.MethodEntered = fileRow.MethodEntered;
            npm.MiddleInitial = fileRow.MiddleInitial;
            //NPM.OnlineElectionProcessedDate = fileRow.OnlineElectionProcessedDate;
            //NPM.PaidThroughDate = fileRow.PaidThroughDate;
            //NPM.PayrollType = fileRow.PayrollType;
            npm.Phone = fileRow.Phone;
            npm.Phone2 = fileRow.Phone2;
            //npm.PlanCategory = fileRow.PlanCategory;
            npm.PostalCode = fileRow.PostalCode;
            //npm.PremiumCouponType = fileRow.PremiumCouponType;
            npm.Salutation = fileRow.Salutation;
            npm.SSN = fileRow.SSN;
            //npm.SSOIdentifier = fileRow.SSOIdentifier;
            npm.State = fileRow.StateOrProvince;
            //npm.TobaccoUse = fileRow.TobaccoUse;
            //npm.UsesHCTC = Utils.ToBool(fileRow.UsesHCTC);
            //npm.YearsOfService = Utils.ToInt(fileRow.YearsOfService);

            return npm;
        }

        public QB CreateCobraQBForDataRow(cobra_file_table_stage fileRow)
        {
            QB qb = new QB();

            qb.AccountStructure = fileRow.AccountStructure;
            qb.Active = Utils.ToBool(fileRow.Active);
            qb.Address1 = fileRow.Address1;
            qb.Address2 = fileRow.Address2;
            qb.AllowSSO = Utils.ToBool(fileRow.AllowMemberSSO);
            qb.BenefitGroup = fileRow.BenefitGroup;
            qb.City = fileRow.City;
            //qb.ClientCustomData = fileRow.ClientCustomData;
            qb.ClientDivisionID = this.currentClientDivisionID;
            qb.ClientID = this.currentClientID;
            qb.Country = fileRow.Country;
            qb.DOB = Utils.ToDateTime(fileRow.DOB);
            qb.Email = fileRow.Email;
            qb.EmployeeType = fileRow.EmployeeType;
            //qb.EnteredByUser = fileRow.EnteredByUser;
            //qb.EnteredDateTime = fileRow.EnteredDateTime;
            qb.FirstName = fileRow.FirstName;
            qb.Gender = fileRow.Sex;
            qb.IndividualIdentifier = "";
            //qb.LastModifiedDate = fileRow.LastModifiedDate;
            qb.LastName = fileRow.LastName;
            qb.MemberID = int.MaxValue;
            //qb.MethodEntered = fileRow.MethodEntered;
            qb.MiddleInitial = fileRow.MiddleInitial;
            //qb.OnlineElectionProcessedDate = fileRow.OnlineElectionProcessedDate;
            //qb.PaidThroughDate = fileRow.PaidThroughDate;
            //qb.PayrollType = fileRow.PayrollType;
            qb.Phone = fileRow.Phone;
            qb.Phone2 = fileRow.Phone2;
            qb.PlanCategory = fileRow.PlanCategory;
            qb.PostalCode = fileRow.PostalCode;
            qb.PremiumCouponType = fileRow.PremiumCouponType;
            qb.Salutation = fileRow.Salutation;
            qb.SSN = fileRow.SSN;
            qb.SSOIdentifier = fileRow.SSOIdentifier;
            qb.State = fileRow.StateOrProvince;
            qb.TobaccoUse = fileRow.TobaccoUse;
            qb.UsesHCTC = Utils.ToBool(fileRow.UsesHCTC);
            qb.YearsOfService = Utils.ToInt(fileRow.YearsOfService);

            return qb;
        }

        public SPM CreateCobraSPMForDataRow(cobra_file_table_stage fileRow)
        {
            SPM SPM = new SPM();

            SPM.AccountStructure = fileRow.AccountStructure;
            SPM.Active = Utils.ToBool(fileRow.Active);
            SPM.Address1 = fileRow.Address1;
            SPM.Address2 = fileRow.Address2;
            //SPM.AllowSSO = Utils.ToBool(fileRow.AllowSSO);
            SPM.BenefitGroup = fileRow.BenefitGroup;
            SPM.City = fileRow.City;
            //SPM.ClientCustomData = fileRow.ClientCustomData;
            SPM.ClientDivisionID = this.currentClientDivisionID;
            SPM.ClientID = this.currentClientID;
            SPM.Country = fileRow.Country;
            SPM.DOB = Utils.ToDateTime(fileRow.DOB);
            SPM.Email = fileRow.Email;
            SPM.EmployeeType = fileRow.EmployeeType;
            //SPM.EnteredByUser = fileRow.EnteredByUser;
            //SPM.EnteredDateTime = fileRow.EnteredDateTime;
            SPM.FirstName = fileRow.FirstName;
            SPM.Gender = fileRow.Sex;
            //SPM.IndividualIdentifier = "";
            //SPM.LastModifiedDate = fileRow.LastModifiedDate;
            SPM.LastName = fileRow.LastName;
            SPM.MemberID = int.MaxValue;
            //SPM.MethodEntered = fileRow.MethodEntered;
            SPM.MiddleInitial = fileRow.MiddleInitial;
            //SPM.OnlineElectionProcessedDate = fileRow.OnlineElectionProcessedDate;
            //SPM.PaidThroughDate = fileRow.PaidThroughDate;
            //SPM.PayrollType = fileRow.PayrollType;
            SPM.Phone = fileRow.Phone;
            SPM.Phone2 = fileRow.Phone2;
            SPM.PlanCategory = fileRow.PlanCategory;
            SPM.PostalCode = fileRow.PostalCode;
            SPM.PremiumCouponType = fileRow.PremiumCouponType;
            SPM.Salutation = fileRow.Salutation;
            SPM.SSN = fileRow.SSN;
            SPM.SSOIdentifier = fileRow.SSOIdentifier;
            SPM.State = fileRow.StateOrProvince;
            SPM.TobaccoUse = fileRow.TobaccoUse;
            //SPM.UsesHCTC = Utils.ToBool(fileRow.UsesHCTC);
            SPM.YearsOfService = Utils.ToInt(fileRow.YearsOfService);

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

        public List<Client> GetCobraClient(cobra_file_table_stage fileRow)
        {
            List<Client> clientRows = null;
            // check DB
            if (Utils.IsBlank(fileRow.ClientName))
            {
                clientRows = new List<Client>();
            }
            else
            {
                DbSet<Client> dbResults = GetAllCobraClients();

                // planid is not always present e.g. in deposit file
                var rows = dbResults.Where(x => x.ClientName == fileRow.ClientName);

                //
                clientRows = rows
                            .AsNoTracking()
                            .ToList();
            } // check client
              //

            return clientRows;
        }

        public List<ClientPlanQB> GetCobraClientPlanQBs(int clientID, cobra_file_table_stage fileRow)
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

        public List<ClientPlanSPM> GetCobraClientPlanSPMs(int clientID, cobra_file_table_stage fileRow)
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

        public NPM GetCobraNPM(cobra_file_table_stage fileRow)
        {
            NPM row = null;

            // check DB
            if (currentClientID <= 0 || currentClientDivisionID <= 0)
            {
                AddCobraErrorForRow(fileRow, "ClientDivisionId", $"The Client and Division ID cannot be blank");
            }
            else if (Utils.IsBlank(fileRow.SSN))
            {
                AddCobraErrorForRow(fileRow, "SSN", $"The SSN cannot be blank");
            }
            else
            {
                if (currentClientDivisionID > 0)
                {
                    row = currentClientDivision.NPMs
                     .Where(x => x.SSN == fileRow.SSN || x.SSN == Utils.FormatSsnWithDashes(fileRow.SSN))
                     .ToList()
                     .FirstOrDefault();
                }
            }
            //
            return row;
        }

        public QB GetCobraQB(cobra_file_table_stage fileRow)
        {
            QB row = null;

            // check DB
            if (currentClientID <= 0)
            {
                AddCobraErrorForRow(fileRow, "clientId", $"The ClientID cannot be blank");
            }
            else if (Utils.IsBlank(fileRow.SSN))
            {
                AddCobraErrorForRow(fileRow, "SSN", $"The SSN cannot be blank");
            }
            else
            {
                if (currentClientDivisionID > 0)
                {
                    row = currentClientDivision.QBs
                     .Where(x => x.SSN == fileRow.SSN || x.SSN == Utils.FormatSsnWithDashes(fileRow.SSN))
                     .ToList()
                     .FirstOrDefault();
                }

                if (row == null)
                {
                    row = currentClient.QBs
                         .Where(x => x.SSN == fileRow.SSN || x.SSN == Utils.FormatSsnWithDashes(fileRow.SSN))
                         .ToList()
                         .FirstOrDefault();
                }
            }
            //
            return row;
        }

        public SPM GetCobraSPM(cobra_file_table_stage fileRow)
        {
            SPM row = null;

            // check DB
            if (currentClientID <= 0)
            {
                AddCobraErrorForRow(fileRow, "clientId", $"The ClientID cannot be blank");
            }
            else if (Utils.IsBlank(fileRow.SSN))
            {
                AddCobraErrorForRow(fileRow, "SSN", $"The SSN cannot be blank");
            }
            else
            {
                if (currentClientDivisionID > 0)
                {
                    row = currentClientDivision.SPMs
                     .Where(x => x.SSN == fileRow.SSN || x.SSN == Utils.FormatSsnWithDashes(fileRow.SSN))
                     .ToList()
                     .FirstOrDefault();
                }

                if (row == null)
                {
                    row = currentClient.SPMs
                         .Where(x => x.SSN == fileRow.SSN || x.SSN == Utils.FormatSsnWithDashes(fileRow.SSN))
                         .ToList()
                         .FirstOrDefault();
                }
            }
            //
            return row;
        }

        public void SetCobraClientAndDivision(cobra_file_table_stage fileRow)
        {
            Client row = new Client();
            var errorMessage = "";
            List<Client> dbRows = GetCobraClient(fileRow);

            if (dbRows.Count == 0)
            {
                errorMessage = $"The Client Name '{fileRow.ClientName}' could not be found";
                this.AddCobraErrorForRow(fileRow, "ClientAndDivision", errorMessage);
            }
            else
            {
                row = dbRows.FirstOrDefault();
                if (row == null || row.ClientID <= 0)
                {
                    this.currentClient = null;
                    this.currentClientDivision = null;
                }
                else if (!Utils.IsBlank(fileRow.ClientDivisionName))
                {
                    this.currentClient = row;

                    //
                    var div = row.ClientDivisions.Where(x => x.DivisionName == fileRow.ClientDivisionName).ToList().FirstOrDefault();
                    if (div != null || div.ClientDivisionID > 0)
                    {
                        this.currentClientDivision = div;
                    }
                }
            } // results.count
        }

        private void AddCobraErrorForRow(cobra_file_table_stage fileRow, string errCode, string errMessage, Boolean isWarningOnly = false)
        {
            // add to fileRow so it will be saved back to DB for fileRow by fileRow data

            if (Utils.IsBlank(fileRow.error_code))
            {
                fileRow.error_code = errCode;
            }
            else
            {
                fileRow.error_code = ErrorSeparator + errCode;
            }

            if (Utils.IsBlank(fileRow.error_message))
            {
                fileRow.error_message = errMessage;
            }
            else
            {
                fileRow.error_message = ErrorSeparator + errMessage;
            }

            if (fileRow.error_code.StartsWith(ErrorSeparator))
            {
                fileRow.error_code = fileRow.error_code.Substring(1);
            }

            if (fileRow.error_message.StartsWith(ErrorSeparator))
            {
                fileRow.error_message = fileRow.error_message.Substring(1);
            }

            if (isWarningOnly)
            {
                fileRow.error_code = $"Warning: {fileRow.error_code}";
                fileRow.error_message = $"Warning: {fileRow.error_message}";
            }

            //
            int key = fileRow.source_row_no ?? 0;
            if (this.fileCheckResults.ContainsKey(key))
            {
                this.fileCheckResults[key] = $"{fileRow.source_row_no}: {fileRow.error_code} : {fileRow.error_message}";
            }
            else
            {
                this.fileCheckResults.Add(key, $"{fileRow.source_row_no}: {fileRow.error_code} : {fileRow.error_message}");
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
                .OrderBy(fileRow => fileRow.source_row_no)
                //.AsNoTracking()
                .ToList();

            var versionNo = Import.GetCobraFileVersionNoFromFile(SrcFilePath);

            //check each fileRow
            int rowNo = 0;
            foreach (var fileRow in dataRows)
            {
                // get row format for current lione as file can have multiple row types
                string rowType = fileRow.row_type;

                // get mappings for event type & version
                TypedCsvSchema mappings = Import.GetCobraFileImportMappings(rowType, versionNo);

                rowNo++;
                try
                {
                    // clear any previous values
                    fileRow.error_code = "";
                    fileRow.error_message = "";
                    //
                    this.CheckCobraFileData(versionNo, fileRow, mappings);
                }
                catch (Exception ex)
                {
                    this.AddCobraErrorForRow(fileRow, "Error", ex.Message);
                }
            }

            // save any changes
            dbDataProcessing.SaveChanges();
        }

        private void CheckCobraFileData(string versionNo, cobra_file_table_stage fileRow, TypedCsvSchema mappings)
        {
            // don't check header fileRow
            if (fileRow.row_type == "")
            {
                return;
            }

            Boolean lineHasError = false;

            // first fix all columns
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

                // save Org Column value before changes
                var orgValue = fileRow.ColumnValue(column.SourceColumn) ?? "";

                // 1. valid Format and general rules check - save corrected value to row
                var formattedValue = EnsureValueIsOfFormatAndMatchesRules(fileRow, column, mappings);
            }

            if (fileRow.hasError)
            {
                return;
            }

            // check record data and logic
            lineHasError = this.CheckCobraRecordData(versionNo, fileRow, mappings);
            if (lineHasError || fileRow.hasError)
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
            if (!lineHasError && !fileRow.hasError)
            {
                lineHasError = CheckForDuplicateCobraPosting(fileRow, versionNo);
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

        public string EnsureValueIsOfFormatAndMatchesRules(cobra_file_table_stage fileRow, TypedCsvColumn column,
            TypedCsvSchema mappings)
        {
            var orgValue = fileRow.ColumnValue(column.SourceColumn) ?? "";
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
                            this.AddCobraErrorForRow(fileRow, column.SourceColumn,
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
                            this.AddCobraErrorForRow(fileRow, column.SourceColumn,
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
                            this.AddCobraErrorForRow(fileRow, column.SourceColumn,
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
                                this.AddCobraErrorForRow(fileRow, column.SourceColumn,
                                    $"{column.SourceColumn} must be in format YYYYMMDD. '{orgValue}' is not valid");
                            }
                        }
                        catch (Exception ex)
                        {
                            this.AddCobraErrorForRow(fileRow, column.SourceColumn,
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
                                this.AddCobraErrorForRow(fileRow, column.SourceColumn,
                                    $"{column.SourceColumn} must be in format YYYYMMDD HHMMSS. '{orgValue}' is not valid");
                            }
                        }
                        catch (Exception ex)
                        {
                            this.AddCobraErrorForRow(fileRow, column.SourceColumn,
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
                                this.AddCobraErrorForRow(fileRow, column.SourceColumn,
                                    $"{column.SourceColumn} must be in format MM/DD/YYYY. '{orgValue}' is not valid");
                            }
                        }
                        catch (Exception ex)
                        {
                            this.AddCobraErrorForRow(fileRow, column.SourceColumn,
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
                                this.AddCobraErrorForRow(fileRow, column.SourceColumn,
                                    $"{column.SourceColumn} must be in format MM/DD/YYYY HH:mm AM. '{orgValue}' is not valid");
                            }
                        }
                        catch (Exception ex)
                        {
                            this.AddCobraErrorForRow(fileRow, column.SourceColumn,
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
                            this.AddCobraErrorForRow(fileRow, column.SourceColumn,
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
                    this.AddCobraErrorForRow(fileRow, column.SourceColumn,
                        $"{column.SourceColumn} must always be one of or ezxactly {column.FixedValue}. '{orgValue}' is not valid");
                }
            }

            // set row column value to the fixed value if it has changed
            if (value != orgValue)
            {
                fileRow.SetColumnValue(column.SourceColumn, value);
                fileRow.data_row = GetDelimitedDataRow(fileRow, mappings);
            }

            // 2. check against GENERAL rules
            // 2b. check matches possible values

            // minLength
            if (column.MinLength > 0 && value.Length < column.MinLength)
            {
                this.AddCobraErrorForRow(fileRow, column.SourceColumn,
                    $"{column.SourceColumn} must be minimum {column.MinLength} characters long. '{orgValue}' is not valid");
            }

            // maxLength
            if (column.MaxLength > 0 && value.Length > column.MaxLength)
            {
                this.AddCobraErrorForRow(fileRow, column.SourceColumn,
                    $"{column.SourceColumn} must be maximum {column.MaxLength} characters long. '{orgValue}' is not valid");
            }

            // min/max value
            if (column.MinValue != 0 || column.MaxValue != 0)
            {
                if (!Utils.IsNumeric(value))
                {
                    this.AddCobraErrorForRow(fileRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number. '{orgValue}' is not valid");
                }

                float numValue = Utils.ToNumber(value);
                if (numValue < column.MinValue)
                {
                    this.AddCobraErrorForRow(fileRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number with a value greater than ${column.MinValue}. '{orgValue}' is not valid");
                }

                if (numValue > column.MaxValue)
                {
                    this.AddCobraErrorForRow(fileRow, column.SourceColumn,
                        $"{column.SourceColumn} must be a number with a value less than ${column.MaxValue}. '{orgValue}' is not valid");
                }
            }

            return value;
        }

        private string GetDelimitedDataRow(cobra_file_table_stage fileRow, TypedCsvSchema mappings)
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

                string fieldValue = fileRow.ColumnValue(column.SourceColumn);
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