use [Alegeus_ErrorLog];
go
select *
from
    [Alegeus_ErrorLog].[dbo].[res_file_table_stage];
go
exec [Alegeus_ErrorLog].dbo.process_res_file_table_stage_import;
go
select *
from
    [Alegeus_ErrorLog].[dbo].[res_file_table]
order by
    res_file_table.mbi_file_name
  , res_file_table.row_num;

select
    mbi_file_name
  , res_file_name
  , row_type
  , error_row
  , error_code
  , error_message
  , EmployerId
  , EmployeeId
  , DependentID
  , PlanId
from
    [Alegeus_ErrorLog].[dbo].[res_file_table]
where
    1 = 1
    --     and row_type in ('RZ')
    -- and mbi_file_name <> res_file_name
order by
    row_type
  , res_file_table.mbi_file_name
  , res_file_table.row_num
go

select
    mbi_file_name
  , data_row
  , row_type
  , EmployerId
  , EmployeeId
  , DependentID
  , PlanId
from
    [Alegeus_ErrorLog].[dbo].[mbi_file_table]
where
    1 = 1
    --     and row_type in ('RZ')
    -- and mbi_file_name <> res_file_name
order by
    row_type
  , mbi_file_name
  , row_num

select *
from
    res_file_table_stage;
/**/
GO

