use Alegeus_ErrorLog;
/**/
go


drop VIEW dbo.[error_log_results_AL];
go

drop VIEW dbo.mbi_log;
go


alter VIEW dbo.[CRM_Listview]
    AS
        SELECT
            BENCODE
          , CRM
        FROM
            /*[Low_balance_reporting].dbo.[CRM_List_contacts]*/
            CRM_List
go


alter VIEW dbo.error_log_results
    AS
        SELECT
            error_row
          , mbi_file_name
          , res_file_name
          , error_code
          , error_message
          , row_num
          , row_type
          , EmployerId
          , EmployeeID
          , DependentID
          , PlanId
        FROM
            dbo.res_file_table
        WHERE
            (
                    error_code IS NOT NULL
                    and (isnumeric( error_code ) = 1 and error_code > '0')
                );
go


alter VIEW dbo.RESwithnoMBIrecords
    AS
        SELECT DISTINCT TOP (100) PERCENT
            r.mbi_file_name
        FROM
            dbo.res_file_table AS r
                LEFT OUTER JOIN
                dbo.mbi_file_table AS m ON m.mbi_file_name = r.mbi_file_name
        WHERE
              (m.mbi_file_name IS NULL)
          AND (r.mbi_file_name IS NOT NULL)
go

alter VIEW dbo.[error_log_results_withmbi]
    AS
        SELECT
            e.mbi_file_name
          , e.res_file_name
          , e.row_type
          , e.error_row
          , e.error_code
          , e.error_message
          , e.EmployerId
          , e.EmployeeID
          , e.DependentID
          , e.PlanId
          , e.row_num error_row_num
          , m.row_num mbi_row_num
          , m.data_row mbi_line
        FROM
            dbo.error_log_results
                AS e
                Left JOIN
                dbo.mbi_file_table AS m ON e.mbi_file_name = m.mbi_file_name
                    AND (e.EmployerId = m.EmployerId and e.EmployeeID = m.EmployeeID and e.DependentID = m.DependentID)
/* WHERE
     e.row_type IN ('IH', 'IB', 'IC', 'IZ', 'II', 'ID')*/
go


alter VIEW dbo.[error_log_results_withmbi_with_CRM]
    AS
        SELECT
            dbo.CRM_Listview.CRM
          , mbi_file_name
          , res_file_name
          , row_type
          , error_row
          , error_code
          , error_message
          , EmployerId
          , EmployeeID
          , DependentID
          , PlanId
          , error_row_num
          , mbi_row_num
          , mbi_line
        FROM
            dbo.error_log_results_withmbi
                LEFT OUTER JOIN
                dbo.CRM_Listview ON dbo.error_log_results_withmbi.EmployerId = dbo.CRM_Listview.BENCODE
go


alter VIEW dbo.[mbi_no_errors]
    AS
        SELECT distinct
            m.mbi_file_name --, m.data_row AS mbi_line
        FROM
            dbo.mbi_file_table AS m
                left join (
                              select distinct
                                  mbi_file_name
                              from
                                  dbo.[res_file_table]
                          ) as r
                          on m.mbi_file_name = r.mbi_file_name
        WHERE
              m.row_type IN ('IH', 'IB', 'IC', 'IZ', 'II', 'ID')
          and r.mbi_file_name is null
go

alter VIEW dbo.[mbi_errors]
    AS
        SELECT distinct
            e.mbi_file_name --, m.data_row AS mbi_line
        FROM
            dbo.error_log_results e
go



alter VIEW dbo.DistinctErrors
    AS
        SELECT DISTINCT
            error_message
        FROM
            error_log_results

go


