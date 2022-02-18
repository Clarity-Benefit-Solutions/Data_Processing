use Alegeus_ErrorLog;
/* add cols to stage*/
alter table mbi_file_table_stage
    add
        row_type nvarchar(50),
        AccountStatus nvarchar(50),
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
alter table mbi_file_table
    add row_type nvarchar(50),
        AccountStatus nvarchar(50),
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

create index row_type on mbi_file_table (row_type);
create index AccountStatus on mbi_file_table (AccountStatus);
create index AccountTypeCode on mbi_file_table (AccountTypeCode);
create index DeleteAccount on mbi_file_table (DeleteAccount);
create index DependentID on mbi_file_table (DependentID);
create index DepositType on mbi_file_table (DepositType);
create index Division on mbi_file_table (Division);
create index EffectiveDate on mbi_file_table (EffectiveDate);
create index EligibilityDate on mbi_file_table (EligibilityDate);
create index Email on mbi_file_table (Email);
create index EmployeeID on mbi_file_table (EmployeeID);
create index EmployeeSocialSecurityNumber on mbi_file_table (EmployeeSocialSecurityNumber);
create index EmployeeStatus on mbi_file_table (EmployeeStatus);
create index EmployerId on mbi_file_table (EmployerId);
create index FirstName on mbi_file_table (FirstName);
create index LastName on mbi_file_table (LastName);
create index MobileNumber on mbi_file_table (MobileNumber);
create index OriginalPrefunded on mbi_file_table (OriginalPrefunded);
create index Phone on mbi_file_table (Phone);
create index PlanEndDate on mbi_file_table (PlanEndDate);
create index PlanId on mbi_file_table (PlanId);
create index PlanStartDate on mbi_file_table (PlanStartDate);
create index Relationship on mbi_file_table (Relationship);
create index TerminationDate on mbi_file_table (TerminationDate);

/**/
/**/
/**/
alter table mbi_file_table
    alter column mbi_file_name nvarchar(500) null;
alter table dbo.mbi_file_table_stage
    alter column mbi_file_name nvarchar(500) null;
/**/

alter table Alegeus_File_Processing..alegeus_file_final
	add row_num int identity
go
