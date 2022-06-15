use [Alegeus_ErrorLog];

/*res_stage*/
select *
from
    [Alegeus_ErrorLog].[dbo].[res_file_table_stage]
order by
    mbi_file_name
  , row_num;

/*res*/
select *
from
    [Alegeus_ErrorLog].[dbo].[res_file_table]
order by
    mbi_file_name
  , row_num;

/*mbi-stage*/
select *
from
    [Alegeus_ErrorLog].[dbo].[mbi_file_table_stage]
order by
    mbi_file_name
  , row_num;

/*mbi*/
select *
from
    [Alegeus_ErrorLog].[dbo].[mbi_file_table]
order by
    mbi_file_name
  , row_num;

/* dups res*/
select
    mbi_file_name
  , row_num
  , count( * )
from
    Alegeus_ErrorLog.dbo.[res_file_table]
group by
    mbi_file_name
  , row_num
having
    count( * ) > 1;

/* dups mbi*/
select
    mbi_file_name
  , row_num
  , count( * )
from
    Alegeus_ErrorLog.dbo.[mbi_file_table]
group by
    mbi_file_name
  , row_num
having
    count( * ) > 1;

select
    count( * )
from
    dbo.mbi_file_table;
select
    count( * )
from
    dbo.res_file_table;

delete from dbo.res_file_table where row_id <= 1000;
delete from dbo.mbi_file_table where row_id <= 1000;

select top 3 * from dbo.res_file_table_stage order by res_file_table_stage.row_num;
exec process_res_file_table_stage_import;
select top 3 result_template, * from dbo.res_file_table where  mbi_file_name= '110--Wisenbaker_Election_01272022.mbi' and row_num <=3;

select top 3 * from dbo.mbi_file_table_stage order by mbi_file_table_stage.row_num;
exec process_mbi_file_table_stage_import;
select top 3 data_row, row_type, * from dbo.mbi_file_table where  mbi_file_name= '110--wisenbaker_election_02032022.mbi' and row_num <=3;
