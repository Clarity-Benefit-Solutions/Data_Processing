use [Alegeus_ErrorLog];

/*mbi, no res*/
select * from dbo.mbi_no_errors;

/*res no mbi*/
select * from dbo.RESwithnoMBIrecords
;

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
