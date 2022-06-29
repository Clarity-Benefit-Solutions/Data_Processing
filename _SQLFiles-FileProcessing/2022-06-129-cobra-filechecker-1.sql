use Data_Processing;
GO

create table dbo.cobra_file_table
(
	mbi_file_name nvarchar(2000),
	data_row nvarchar(max),
	row_num int,
	row_type nvarchar(200),
	/*AccountStatus nvarchar(200),
	AccountTypeCode nvarchar(200),
	AddressLine1 nvarchar(200),
	AddressLine2 nvarchar(200),
	BirthDate nvarchar(200),
	City nvarchar(200),
	Country nvarchar(200),
	DeleteAccount nvarchar(200),
	DependentID nvarchar(200),
	DepositType nvarchar(200),
	Division nvarchar(200),
	EffectiveDate nvarchar(200),
	EligibilityDate nvarchar(200),
	Email nvarchar(200),
	EmployeeDepositAmount nvarchar(200),
	EmployeeID nvarchar(200),
	EmployeePayPeriodElection nvarchar(200),
	EmployeeSocialSecurityNumber nvarchar(200),
	EmployeeStatus nvarchar(200),
	EmployerDepositAmount nvarchar(200),
	EmployerId nvarchar(200),
	EmployerPayPeriodElection nvarchar(200),
	FirstName nvarchar(200),
	LastName nvarchar(200),
	MiddleInitial nvarchar(200),
	MobileNumber nvarchar(200),
	OriginalPrefunded nvarchar(200),
	Phone nvarchar(200),
	PlanEndDate nvarchar(200),
	PlanId nvarchar(200),
	PlanStartDate nvarchar(200),
	Relationship nvarchar(200),
	State nvarchar(200),
	TerminationDate nvarchar(200),
	TpaId nvarchar(200),
	Zip nvarchar(200),*/
	row_id int identity
		constraint cobra_file_table_PK
			primary key,
	source_row_no int,
	check_type nvarchar(200) default 'Platform',
	error_code nvarchar(200),
	error_message nvarchar(2000),
	error_message_calc nvarchar(2000),
/*	OngoingPrefunded nvarchar(200),
	AlternateId nvarchar(200),
	Class nvarchar(200),
	AccountSegmentId nvarchar(255),*/
	CreatedAt datetime default sysdatetime()
)
go

create index row_type
	on dbo.cobra_file_table (row_type)
go

create index AccountStatus
	on dbo.cobra_file_table (AccountStatus)
go

create index AccountTypeCode
	on dbo.cobra_file_table (AccountTypeCode)
go

create index DeleteAccount
	on dbo.cobra_file_table (DeleteAccount)
go

create index DependentID
	on dbo.cobra_file_table (DependentID)
go

create index DepositType
	on dbo.cobra_file_table (DepositType)
go

create index mbi_file_name
	on dbo.cobra_file_table (mbi_file_name)
go

create index row_num
	on dbo.cobra_file_table (row_num)
go

create index Division
	on dbo.cobra_file_table (Division)
go

create index EffectiveDate
	on dbo.cobra_file_table (EffectiveDate)
go

create index EligibilityDate
	on dbo.cobra_file_table (EligibilityDate)
go

create index Email
	on dbo.cobra_file_table (Email)
go

create index EmployeeID
	on dbo.cobra_file_table (EmployeeID)
go

create index EmployeeSocialSecurityNumber
	on dbo.cobra_file_table (EmployeeSocialSecurityNumber)
go

create index EmployeeStatus
	on dbo.cobra_file_table (EmployeeStatus)
go

create index EmployerId
	on dbo.cobra_file_table (EmployerId)
go

create index FirstName
	on dbo.cobra_file_table (FirstName)
go

create index LastName
	on dbo.cobra_file_table (LastName)
go

create index MobileNumber
	on dbo.cobra_file_table (MobileNumber)
go

create index OriginalPrefunded
	on dbo.cobra_file_table (OriginalPrefunded)
go

create index Phone
	on dbo.cobra_file_table (Phone)
go

create index PlanEndDate
	on dbo.cobra_file_table (PlanEndDate)
go

create index PlanId
	on dbo.cobra_file_table (PlanId)
go

create index PlanStartDate
	on dbo.cobra_file_table (PlanStartDate)
go

create index Relationship
	on dbo.cobra_file_table (Relationship)
go

create index TerminationDate
	on dbo.cobra_file_table (TerminationDate)
go

create unique index uk_mbi_name_row_num
	on dbo.cobra_file_table (mbi_file_name, row_num)
go

create table dbo.cobra_file_table_stage
(
	mbi_file_name nvarchar(2000),
	data_row nvarchar(max),
	row_num int identity
		constraint cobra_file_table_stage_PK
			primary key,
	row_type nvarchar(200),
/*	AccountStatus nvarchar(200),
	AccountTypeCode nvarchar(200),
	AddressLine1 nvarchar(200),
	AddressLine2 nvarchar(200),
	BirthDate nvarchar(200),
	City nvarchar(200),
	Country nvarchar(200),
	DeleteAccount nvarchar(200),
	DependentID nvarchar(200),
	DepositType nvarchar(200),
	Division nvarchar(200),
	EffectiveDate nvarchar(200),
	EligibilityDate nvarchar(200),
	Email nvarchar(200),
	EmployeeDepositAmount nvarchar(200),
	EmployeeID nvarchar(200),
	EmployeePayPeriodElection nvarchar(200),
	EmployeeSocialSecurityNumber nvarchar(200),
	EmployeeStatus nvarchar(200),
	EmployerDepositAmount nvarchar(200),
	EmployerId nvarchar(200),
	EmployerPayPeriodElection nvarchar(200),
	FirstName nvarchar(200),
	LastName nvarchar(200),
	MiddleInitial nvarchar(200),
	MobileNumber nvarchar(200),
	OriginalPrefunded nvarchar(200),
	Phone nvarchar(200),
	PlanEndDate nvarchar(200),
	PlanId nvarchar(200),
	PlanStartDate nvarchar(200),
	Relationship nvarchar(200),
	State nvarchar(200),
	TerminationDate nvarchar(200),
	TpaId nvarchar(200),
	Zip nvarchar(200),*/
	source_row_no int,
	check_type nvarchar(200),
	error_code nvarchar(200),
	error_message nvarchar(2000),
	error_message_calc nvarchar(2000),
	/*OngoingPrefunded nvarchar(200),
	AlternateId nvarchar(200),
	Class nvarchar(200),
	AccountSegmentId nvarchar(255)*/
)
go

create table dbo.cobra_res_file_table
(
	mbi_file_name nvarchar(2000),
	error_row nvarchar(max),
	error_code nvarchar(200),
	error_message nvarchar(2000),
	row_num int,
/*	AccountStatus nvarchar(200),
	AccountTypeCode nvarchar(200),
	AddressLine1 nvarchar(200),
	AddressLine2 nvarchar(200),
	BirthDate nvarchar(200),
	City nvarchar(200),
	Country nvarchar(200),
	DeleteAccount nvarchar(200),
	DependentID nvarchar(200),
	DepositType nvarchar(200),
	Division nvarchar(200),
	EffectiveDate nvarchar(200),
	EligibilityDate nvarchar(200),
	Email nvarchar(200),
	EmployeeDepositAmount nvarchar(200),
	EmployeeID nvarchar(200),
	EmployeePayPeriodElection nvarchar(200),
	EmployeeSocialSecurityNumber nvarchar(200),
	EmployeeStatus nvarchar(200),
	EmployerDepositAmount nvarchar(200),
	EmployerId nvarchar(200),
	EmployerPayPeriodElection nvarchar(200),
	FirstName nvarchar(200),
	LastName nvarchar(200),
	MiddleInitial nvarchar(200),
	MobileNumber nvarchar(200),
	OriginalPrefunded nvarchar(200),
	Phone nvarchar(200),
	PlanEndDate nvarchar(200),
	PlanId nvarchar(200),
	PlanStartDate nvarchar(200),
	Relationship nvarchar(200),
	State nvarchar(200),
	TerminationDate nvarchar(200),
	TpaId nvarchar(200),
	Zip nvarchar(200),*/
	row_type varchar(50),
	error_message_calc varchar(500),
	res_file_name nvarchar(2000),
	result_template nvarchar(100),
	row_id int identity
		constraint cobra_res_file_table_PK
			primary key,
	source_row_no int,
	check_type nvarchar(200) default 'Platform',
/*	OngoingPrefunded nvarchar(200),
	Class nvarchar(200),
	AlternateId nvarchar(200),
	AccountSegmentId nvarchar(255)*/
)
go

create index row_type
	on dbo.cobra_res_file_table (row_type)
go

create index error_message_calc
	on dbo.cobra_res_file_table (error_message_calc)
go

create index error_code
	on dbo.cobra_res_file_table (error_code)
go

create index error_message
	on dbo.cobra_res_file_table (error_message)
go

create index res_file_name
	on dbo.cobra_res_file_table (res_file_name)
go

create index row_num
	on dbo.cobra_res_file_table (row_num)
go

create index AccountStatus
	on dbo.cobra_res_file_table (AccountStatus)
go

create index AccountTypeCode
	on dbo.cobra_res_file_table (AccountTypeCode)
go

create index DeleteAccount
	on dbo.cobra_res_file_table (DeleteAccount)
go

create index DependentID
	on dbo.cobra_res_file_table (DependentID)
go

create index DepositType
	on dbo.cobra_res_file_table (DepositType)
go

create index Division
	on dbo.cobra_res_file_table (Division)
go

create index EffectiveDate
	on dbo.cobra_res_file_table (EffectiveDate)
go

create index EligibilityDate
	on dbo.cobra_res_file_table (EligibilityDate)
go

create index Email
	on dbo.cobra_res_file_table (Email)
go

create index EmployeeID
	on dbo.cobra_res_file_table (EmployeeID)
go

create index EmployeeSocialSecurityNumber
	on dbo.cobra_res_file_table (EmployeeSocialSecurityNumber)
go

create index EmployeeStatus
	on dbo.cobra_res_file_table (EmployeeStatus)
go

create index EmployerId
	on dbo.cobra_res_file_table (EmployerId)
go

create index FirstName
	on dbo.cobra_res_file_table (FirstName)
go

create index LastName
	on dbo.cobra_res_file_table (LastName)
go

create index MobileNumber
	on dbo.cobra_res_file_table (MobileNumber)
go

create index OriginalPrefunded
	on dbo.cobra_res_file_table (OriginalPrefunded)
go

create index Phone
	on dbo.cobra_res_file_table (Phone)
go

create index PlanEndDate
	on dbo.cobra_res_file_table (PlanEndDate)
go

create index PlanId
	on dbo.cobra_res_file_table (PlanId)
go

create index PlanStartDate
	on dbo.cobra_res_file_table (PlanStartDate)
go

create index Relationship
	on dbo.cobra_res_file_table (Relationship)
go

create index TerminationDate
	on dbo.cobra_res_file_table (TerminationDate)
go

create unique index uk_name_row_num
	on dbo.cobra_res_file_table (res_file_name, row_num)
go

create table dbo.cobra_res_file_table_stage
(
	mbi_file_name nvarchar(2000),
	result_template nvarchar(200),
	error_row nvarchar(max),
	error_code nvarchar(200),
	error_message nvarchar(2000),
	row_num int identity
		constraint cobra_res_file_table_stage_PK
			primary key,
	row_type nvarchar(200),
/*	AccountStatus nvarchar(200),
	AccountTypeCode nvarchar(200),
	AddressLine1 nvarchar(200),
	AddressLine2 nvarchar(200),
	BirthDate nvarchar(200),
	City nvarchar(200),
	Country nvarchar(200),
	DeleteAccount nvarchar(200),
	DependentID nvarchar(200),
	DepositType nvarchar(200),
	Division nvarchar(200),
	EffectiveDate nvarchar(200),
	EligibilityDate nvarchar(200),
	Email nvarchar(200),
	EmployeeDepositAmount nvarchar(200),
	EmployeeID nvarchar(200),
	EmployeePayPeriodElection nvarchar(200),
	EmployeeSocialSecurityNumber nvarchar(200),
	EmployeeStatus nvarchar(200),
	EmployerDepositAmount nvarchar(200),
	EmployerId nvarchar(200),
	EmployerPayPeriodElection nvarchar(200),
	FirstName nvarchar(200),
	LastName nvarchar(200),
	MiddleInitial nvarchar(200),
	MobileNumber nvarchar(200),
	OriginalPrefunded nvarchar(200),
	Phone nvarchar(200),
	PlanEndDate nvarchar(200),
	PlanId nvarchar(200),
	PlanStartDate nvarchar(200),
	Relationship nvarchar(200),
	State nvarchar(200),
	TerminationDate nvarchar(200),
	TpaId nvarchar(200),
	Zip nvarchar(200),*/
	error_message_calc varchar(500),
	res_file_name nvarchar(2000),
	data_row nvarchar(max),
	source_row_no int,
	check_type nvarchar(200) default 'Platform',
/*	OngoingPrefunded nvarchar(200),
	AlternateId nvarchar(200),
	Class nvarchar(200),
	AccountSegmentId nvarchar(255)*/
)
go

create index row_type
	on dbo.cobra_res_file_table_stage (row_type)
go

create index error_message_calc
	on dbo.cobra_res_file_table_stage (error_message_calc)
go

create index res_file_name
	on dbo.cobra_res_file_table_stage (res_file_name)
go

create index row_num
	on dbo.cobra_res_file_table_stage (row_num)
go

