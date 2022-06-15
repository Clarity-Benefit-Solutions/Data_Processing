use Alegeus_ErrorLog;
go

create procedure check_imports_file as
begin
    select
        1;
end
    alter table res_file_table_stage
        add source_row_no int;
    alter table res_file_table
        add source_row_no int;
    
    alter table mbi_file_table_stage
        add source_row_no int;
    alter table mbi_file_table
        add source_row_no int;
    
    alter table res_file_table_stage
        add check_type nvarchar(50) default 'Platform';
    alter table res_file_table
        add check_type nvarchar(50) default 'Platform';
    
    alter table mbi_file_table_stage
        add check_type nvarchar(50) default 'Platform';
    alter table mbi_file_table
        add check_type nvarchar(50) default 'Platform';
    
    /**/
    alter table res_file_table_stage
        add data_row nvarchar(max);

go

alter table mbi_file_table_stage
    add error_code nvarchar(50);
alter table mbi_file_table_stage
    add error_message nvarchar(500);
alter table mbi_file_table_stage
    add error_message_calc nvarchar(500);

alter table mbi_file_table
    add error_code nvarchar(50);
alter table mbi_file_table
    add error_message nvarchar(500);
alter table mbi_file_table
    add error_message_calc nvarchar(500);


go

ALTER TABLE mbi_file_table
ADD CONSTRAINT mbi_file_table_PK PRIMARY KEY(row_id);

ALTER TABLE res_file_table
ADD CONSTRAINT res_file_table_PK PRIMARY KEY(row_id);


ALTER TABLE dbo.mbi_file_table_stage
ADD CONSTRAINT mbi_file_table_stage_PK PRIMARY KEY(row_num);

ALTER TABLE dbo.res_file_table_stage
ADD CONSTRAINT res_file_table_stage_PK PRIMARY KEY(row_num);
