/* drop not needed columns*/
alter table res_file_table_stage drop column processing_time;
alter table res_file_table_stage drop column processing_time2;
alter table res_file_table_stage drop column flag;
alter table dbo.res_file_table_stage drop column result_template
alter table dbo.res_file_table_stage drop column plan_id;
alter table dbo.res_file_table_stage drop column employee_id;

/**/
drop index plan_id on res_file_table_stage
drop index employee_id on res_file_table_stage
drop index error_code on res_file_table_stage
drop index error_message on res_file_table_stage
drop index result_template on res_file_table_stage
go

/* add cols to stage*/
alter table res_file_table_stage
    add AccountStatus nvarchar(50),
        AccountTypeCode nvarchar(50),
        AddressLine1 nvarchar(50),
        AddressLine2 nvarchar(50),
        BirthDate nvarchar(50),
        City nvarchar(50),
        Country nvarchar(50),
        DeleteAccount nvarchar(50),
        DependentID nvarchar(50),
        DepositType nvarchar(50),
        Division nvarchar(50),
        EffectiveDate nvarchar(50),
        EligibilityDate nvarchar(50),
        Email nvarchar(50),
        EmployeeDepositAmount nvarchar(50),
        EmployeeID nvarchar(50),
        EmployeePayPeriodElection nvarchar(50),
        EmployeeSocialSecurityNumber nvarchar(50),
        EmployeeStatus nvarchar(50),
        EmployerDepositAmount nvarchar(50),
        EmployerId nvarchar(50),
        EmployerPayPeriodElection nvarchar(50),
        FirstName nvarchar(50),
        LastName nvarchar(50),
        MiddleInitial nvarchar(50),
        MobileNumber nvarchar(50),
        OriginalPrefunded nvarchar(50),
        Phone nvarchar(50),
        PlanEndDate nvarchar(50),
        PlanId nvarchar(50),
        PlanStartDate nvarchar(50),
        Relationship nvarchar(50),
        State nvarchar(50),
        TerminationDate nvarchar(50),
        TpaId nvarchar(50),
        Zip nvarchar(50);
go

/* add cols to final*/
alter table res_file_table
    add AccountStatus nvarchar(50),
        AccountTypeCode nvarchar(50),
        AddressLine1 nvarchar(50),
        AddressLine2 nvarchar(50),
        BirthDate nvarchar(50),
        City nvarchar(50),
        Country nvarchar(50),
        DeleteAccount nvarchar(50),
        DependentID nvarchar(50),
        DepositType nvarchar(50),
        Division nvarchar(50),
        EffectiveDate nvarchar(50),
        EligibilityDate nvarchar(50),
        Email nvarchar(50),
        EmployeeDepositAmount nvarchar(50),
        EmployeeID nvarchar(50),
        EmployeePayPeriodElection nvarchar(50),
        EmployeeSocialSecurityNumber nvarchar(50),
        EmployeeStatus nvarchar(50),
        EmployerDepositAmount nvarchar(50),
        EmployerId nvarchar(50),
        EmployerPayPeriodElection nvarchar(50),
        FirstName nvarchar(50),
        LastName nvarchar(50),
        MiddleInitial nvarchar(50),
        MobileNumber nvarchar(50),
        OriginalPrefunded nvarchar(50),
        Phone nvarchar(50),
        PlanEndDate nvarchar(50),
        PlanId nvarchar(50),
        PlanStartDate nvarchar(50),
        Relationship nvarchar(50),
        State nvarchar(50),
        TerminationDate nvarchar(50),
        TpaId nvarchar(50),
        Zip nvarchar(50);

/* drop not needed columns*/
alter table res_file_table drop column processing_date;
alter table res_file_table drop column processing_time;
alter table res_file_table drop column processing_time2;
alter table res_file_table drop column flag;
alter table res_file_table drop column result_template;

/* migrate data from old columns*/
update dbo.res_file_table set res_file_table.EmployeeID = employee_id;
update dbo.res_file_table set res_file_table.PlanId = plan_id;
update dbo.res_file_table set res_file_table.EmployerId = bencode;

drop index plan_id on res_file_table;
drop index employee_id on res_file_table;

alter table dbo.res_file_table drop column plan_id;
alter table dbo.res_file_table drop column employee_id;
/**/
alter table res_file_table drop column plan_id;
alter table res_file_table drop column employee_id;
go
alter table dbo.res_file_table_stage drop column processing_date;


go

create index result_template
	on res_file_table (result_template)
go


create index AccountStatus on res_file_table (AccountStatus);
create index AccountTypeCode on res_file_table (AccountTypeCode);
create index DeleteAccount on res_file_table (DeleteAccount);
create index DependentID on res_file_table (DependentID);
create index DepositType on res_file_table (DepositType);
create index Division on res_file_table (Division);
create index EffectiveDate on res_file_table (EffectiveDate);
create index EligibilityDate on res_file_table (EligibilityDate);
create index Email on res_file_table (Email);
create index EmployeeID on res_file_table (EmployeeID);
create index EmployeeSocialSecurityNumber on res_file_table (EmployeeSocialSecurityNumber);
create index EmployeeStatus on res_file_table (EmployeeStatus);
create index EmployerId on res_file_table (EmployerId);
create index FirstName on res_file_table (FirstName);
create index LastName on res_file_table (LastName);
create index MobileNumber on res_file_table (MobileNumber);
create index OriginalPrefunded on res_file_table (OriginalPrefunded);
create index Phone on res_file_table (Phone);
create index PlanEndDate on res_file_table (PlanEndDate);
create index PlanId on res_file_table (PlanId);
create index PlanStartDate on res_file_table (PlanStartDate);
create index Relationship on res_file_table (Relationship);
create index TerminationDate on res_file_table (TerminationDate);

/**/
alter table dbo.res_file_table add row_type varchar(50);
alter table dbo.res_file_table_stage add row_type varchar(50);
create index row_type on res_file_table (row_type);
create index row_type on dbo.res_file_table_stage (row_type);
/**/
alter table dbo.res_file_table add error_message_calc varchar(500);
alter table dbo.res_file_table_stage add error_message_calc varchar(500);
create index error_message_calc on res_file_table (error_message_calc);
create index error_message_calc on dbo.res_file_table_stage (error_message_calc);
/**/
alter table res_file_table alter column mbi_file_name nvarchar(500) null;
alter table dbo.res_file_table_stage alter column mbi_file_name nvarchar(500) null;
/**/
alter table res_file_table add res_file_name nvarchar(500) null;
alter table dbo.res_file_table_stage add res_file_name nvarchar(500) null;
create index res_file_name on res_file_table (res_file_name);
create index res_file_name on dbo.res_file_table_stage (res_file_name);

/**/
drop index initial_code on mbi_file_table_stage
go

drop index tpa_code on mbi_file_table_stage
go

drop index bencode on mbi_file_table_stage
go

drop index import_template on mbi_file_table_stage
go

drop index result_template on mbi_file_table_stage
go

drop index export_template on mbi_file_table_stage
go


alter table mbi_file_table_stage
    drop column initial_code
go

alter table mbi_file_table_stage
    drop column tpa_code
go

alter table mbi_file_table_stage
    drop column bencode
go

alter table mbi_file_table_stage
    drop column import_template
go

alter table mbi_file_table_stage
    drop column result_template
go

alter table mbi_file_table_stage
    drop column export_template
go

drop index initial_code on mbi_file_table
go

drop index tpa_code on mbi_file_table
go

drop index bencode on mbi_file_table
go

drop index import_template on mbi_file_table
go

drop index result_template on mbi_file_table
go

drop index export_template on mbi_file_table
go


alter table mbi_file_table
    drop column initial_code
go

alter table mbi_file_table
    drop column tpa_code
go

alter table mbi_file_table
    drop column bencode
go

alter table mbi_file_table
    drop column import_template
go

alter table mbi_file_table
    drop column result_template
go

alter table mbi_file_table
    drop column export_template
go

