use Alegeus_ErrorLog;
/**/
go

exec sp_rename 'dbo_error_log_results_workflow_local.file_type' , row_type , 'COLUMN'
go
exec sp_rename 'dbo_tracked_errors_local.error' , error_message , 'COLUMN'
go

alter table dbo_tracked_errors_local
    alter column error_message nvarchar(500) null
go

/*
truncate table dbo_tracked_errors_local;
truncate table dbo_error_log_results_workflow_local;
*/

create index [Error_code]
    on dbo_tracked_errors_local (Error_code)

create index [error_message]
    on dbo_tracked_errors_local (error_message)

create index [crm]
    on dbo_tracked_errors_local (crm)
create index [processing]
    on dbo_tracked_errors_local (processing)
create index [control]
    on dbo_tracked_errors_local (control)

alter table dbo_error_log_results_workflow_local
    add EmployerId nvarchar(50),
        EmployeeID nvarchar(50),
        DependentID nvarchar(50),
        PlanId nvarchar(50);

alter table dbo_error_log_results_workflow_local
    add error_row_num int null
go

create index [EmployerId]
    on dbo_error_log_results_workflow_local (EmployerId)
create index [EmployeeID]
    on dbo_error_log_results_workflow_local (EmployeeID)
create index [DependentID]
    on dbo_error_log_results_workflow_local (DependentID)
create index [PlanId]
    on dbo_error_log_results_workflow_local (PlanId)
go



alter procedure [dbo].[alegeus_errorlog_track_new_ftp_errors] as
begin
    /* insert new error codes and messages into dbo_tracked_errors_local setting crm, processing and control flags*/
    INSERT into dbo_tracked_errors_local (
                                         Error_code,
                                         error_message,
                                         crm,
                                         processing,
                                         control
    )
    SELECT distinct
        wf.[Error_code]
      , wf.[error_message]
      , 0
      , 255
      , 255
    FROM
        [Alegeus_ErrorLog].[dbo].[error_log_results_withmbi] as wf
            left join dbo_tracked_errors_local as te
                      on wf.error_code = te.Error_code
    where
        te.error_code is null;
    
    /* insert new errors  into dbo_tracked_errors_local setting crm, processing and control flags*/
    INSERT INTO [dbo].[dbo_error_log_results_workflow_local]
    (
        [mbi_file_name]
    ,   [error_row]
    ,   [error_code]
    ,   [error_message]
    ,   [error_row_num]
    ,   mbi_row_num
    ,   [mbi_line]
    ,   [CRM_Check]
    ,   [Proc_Check]
    ,   [Archive_Check]
    ,   [updated_by]
    ,   [updated_timestamp]
    ,   EmployerId
    ,   EmployeeID
    ,   DependentID
    ,   PlanId
    ,   [CRM]
    ,   [row_type]
    )
    SELECT distinct
        d.[mbi_file_name]
      , d.[error_row]
      , d.[error_code]
      , d.[error_message]
      , d.[error_row_num]
      , d.mbi_row_num
      , d.[mbi_line]
      , 0
      , 0
      , 0
      , ''
      , getdate( )
      , d.EmployerId
      , d.EmployeeID
      , d.DependentID
      , d.PlanId
      , d.CRM
      , d.row_type
    FROM
        [Alegeus_ErrorLog].[dbo].[error_log_results_withmbi_with_CRM] as d
            left join [dbo].[dbo_error_log_results_workflow_local] wfl
                      on wfl.mbi_file_name = d.mbi_file_name and wfl.error_row_num = d.error_row_num
    WHERE
        /*          d.error_row is null
              and*/
            d.error_code in (
                                select
                                    error_code
                                from
                                    dbo_tracked_errors_local
                            )
      and   wfl.mbi_file_name is null
      and   wfl.error_row_num is null;
    
    /* update crm*/
    UPDATE [Alegeus_ErrorLog].[dbo].[dbo_error_log_results_workflow_local]
    set
        CRM = replace( c.crm , '""' , '' )
    FROM
        [Alegeus_ErrorLog].[dbo].[dbo_error_log_results_workflow_local] as l
            JOIN[Alegeus_ErrorLog].[dbo].[CRM_List] as c
                on l.EmployerId = c.bencode;
    
    -- exec [dbo].[Alegeus_ErrorLog_refreshsuccess_alert]

end;
go
exec [dbo].[alegeus_errorlog_track_new_ftp_errors];

use Alegeus_File_Processing;
go
alter function dbo.getFileLogId(
    @filePathWithoutExtension varchar(200) ) returns int
    as
    begin
        declare @fileLogId int = null;
        
        if @filePathWithoutExtension is null or rtrim( ltrim( @filePathWithoutExtension ) )=''
            begin
                return 0;
            end
        
        /* take last id for filename (wihout path or extension*/
        select top 1
            @fileLogId = fileLogId
        from
            dbo.file_processing_log
        where
             originalFileName like concat( @filePathWithoutExtension , '%' )
          or newFileName like concat( @filePathWithoutExtension , '%' )
        order by
            fileLogId desc;
        
        if @fileLogId is not null and @fileLogId <> 0
            begin
                return @fileLogId;
            end
        else
            begin
                return 0;
            end
        
        return @fileLogId
    end
go

