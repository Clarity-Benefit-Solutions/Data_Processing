use [Alegeus_ErrorLog];

/*mbi_stage,errors precheck*/
select
    check_type
  , error_code
  , error_message
  , mbi_file_name
  , data_row
  , row_num
  , source_row_no
  , row_type
  , AccountStatus
  , AccountTypeCode
  , DeleteAccount
  , DependentID
  , DepositType
  , EmployeeDepositAmount
  , EmployeeID
  , EmployeeStatus
  , EmployerDepositAmount
  , EmployerId
  , Phone
  , PlanEndDate
  , PlanId
  , PlanStartDate
--   , row_id
  , error_message_calc
from
    Alegeus_ErrorLog..mbi_file_table_stage
where
      len( error_message ) > 0
order by
   row_num;

/*mbi,errors precheck*/
select
    check_type
  , error_code
  , error_message
  , mbi_file_name
  , data_row
  , row_num
  , source_row_no
  , row_type
  , AccountStatus
  , AccountTypeCode
  , DeleteAccount
  , DependentID
  , DepositType
  , EmployeeDepositAmount
  , EmployeeID
  , EmployeeStatus
  , EmployerDepositAmount
  , EmployerId
  , Phone
  , PlanEndDate
  , PlanId
  , PlanStartDate
  , row_id
  , error_message_calc
from
    Alegeus_ErrorLog..mbi_file_table
where
      len( error_message ) > 0
--   and check_type = 'PreCheck'
order by
    mbi_file_table.mbi_file_name
  , row_num;

/*res no mbi*/
select *
from
    dbo.RESwithnoMBIrecords;

/* all errors */
select *
from
    [Alegeus_ErrorLog]..error_log_results_withmbi_with_CRM
order by
    mbi_file_name;

/* distinct tracked error codes*/
select *
from
    dbo_tracked_errors_local;

/* all tracked error records*/
select *
from
    dbo_error_log_results_workflow_local;
