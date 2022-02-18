use Alegeus_ErrorLog;
go

select
    row_num
  , error_code
  , error_message
  , error_row

from
    dbo.res_file_table_stage
order by
    res_file_table_stage.row_num;;
go

exec dbo.SplitAllErrorRowStringsToFields '';

go
alter procedure dbo.SplitAllErrorRowStringsToFields( @tableName nvarchar(250) ) as
begin
    
    --
    declare @row_num int;
    declare @error_row nvarchar(max);
    --
    declare @row_type nvarchar(100);
    declare @bencode nvarchar(100);
    declare @employee_id nvarchar(100);
    declare @plan_id nvarchar(100);
    declare @start_date nvarchar(100);
    declare @end_date nvarchar(100);
    declare @error_code nvarchar(100);
    declare @error_message nvarchar(500);
    declare @dummy nvarchar(100);
    
    --
    DECLARE db_cursor CURSOR FOR
        SELECT /*top 3*/
            row_num
          , error_row
        FROM
            dbo.res_file_table_stage
        order by
            res_file_table_stage.row_num;
    
    OPEN db_cursor
    FETCH NEXT FROM db_cursor INTO @row_num, @error_row;
    
    WHILE @@FETCH_STATUS = 0
        BEGIN
            ;
            with
                T( row_num, row_type, bencode, employee_id, plan_id, start_date, end_date, error_code
                 , error_message, dummy)
                    as (
                           select top 1
                               @row_num row_num
                             , row_type
                             , bencode
                             , employee_id
                             , plan_id
                             , start_date
                             , end_date
                             , error_code
                             , error_message
                             , dummy
                           from
                               dbo.SplitErrorRowStringToFields( @error_row )
                       )
            update dbo.res_file_table_stage
            set
                row_type      = T.row_type
              , bencode       = T.bencode
              , employee_id   = T.employee_id
              , plan_id       = T.plan_id
              , start_date    = T.start_date
              , end_date      = T.end_date
              , error_code    = T.error_code
              , error_message = T.error_message
                --               , dummy         = @dummy
            from
                dbo.res_file_table_stage r
                    inner join T on r.row_num = T.row_num
            
            where
                r.row_num = @row_num;
            /**/
            FETCH NEXT FROM db_cursor INTO @row_num, @error_row;
        END
    
    CLOSE db_cursor
    DEALLOCATE db_cursor
    
    RETURN

end;
go



alter function dbo.SplitErrorRowStringToFields( @csvString nvarchar(max) )
    RETURNS @t table
               (
               row_type      nvarchar(100),
               bencode       nvarchar(100),
               employee_id   nvarchar(100),
               plan_id       nvarchar(100),
               start_date    nvarchar(100),
               end_date      nvarchar(100),
               error_code    nvarchar(100),
               error_message nvarchar(500),
               dummy         nvarchar(100)
               )
    begin
        
        declare @field_id int;
        declare @field_value nvarchar(max);
        --
        declare @row_type nvarchar(100);
        declare @bencode nvarchar(100);
        declare @employee_id nvarchar(100);
        declare @plan_id nvarchar(100);
        declare @start_date nvarchar(100);
        declare @end_date nvarchar(100);
        declare @error_code nvarchar(100);
        declare @error_message nvarchar(500);
        declare @dummy nvarchar(100);
        --
        DECLARE db_cursor CURSOR FOR
            SELECT *
            FROM
                dbo.CsvSplit1( @csvString , ',' );
        
        OPEN db_cursor
        FETCH NEXT FROM db_cursor INTO @field_id, @field_value;
        
        WHILE @@FETCH_STATUS = 0
            BEGIN
                if @field_id = 1
                    set @row_type = @field_value;
                if @field_id = 2
                    set @bencode = @field_value;
                if @field_id = 3
                    set @employee_id = @field_value;
                if @field_id = 4
                    set @plan_id = @field_value;
                if @field_id = 5
                    set @start_date = @field_value;
                if @field_id = 6
                    set @end_date = @field_value;
                if @field_id = 7
                    set @error_code = @field_value;
                if @field_id = 8
                    set @error_message = @field_value;
                if @field_id = 9
                    set @dummy = @field_value;
                /**/
                FETCH NEXT FROM db_cursor INTO @field_id, @field_value;
            END
        
        CLOSE db_cursor
        DEALLOCATE db_cursor
        
        insert into @t(
            --                       error_row,
                      row_type,
                      bencode,
                      employee_id,
                      plan_id,
                      start_date,
                      end_date,
                      error_code,
                      error_message,
                      dummy
        )
        select
            /*   @csvString
             ,*/
            @row_type
          , @bencode
          , @employee_id
          , @plan_id
          , @start_date
          , @end_date
          , @error_code
          , @error_message
          , @dummy;
        
        RETURN
    end
go

alter FUNCTION dbo.CsvSplit1(
    @delimited nvarchar(max),
    @delimiter nvarchar(100) )
    RETURNS @t table
               (
                   -- Id column can be commented out, not required for sql splitting string
               id  int identity (1,1), -- I use this column for numbering splitted parts
               val nvarchar(max)
               )
    AS
    BEGIN
        declare @xml xml
        set @xml = N'<root><r>' + replace( @delimited , @delimiter , '</r><r>' ) + '</r></root>'
        
        insert into @t(
            val
        )
        select
            replace( r.value( '.' , 'varchar(max)' ) , '"' , '' ) item
        from
            @xml.nodes( '//root/r' ) as records(r)
        
        RETURN
    END
GO

select *
from
    dbo.CsvSplit1(
            'RC,BENWHOA,157720473,FSA,20210101,20211231,15028,Add/Update did not process.  Employer must be in an active status in order to Add/Update employee and other related information successfully.,' ,
            ',' );
go
select *
from
    dbo.SplitErrorRowStringToFields(
            'RC,BENWHOA,157720473,FSA,20210101,20211231,15028,Add/Update did not process.  Employer must be in an active status in order to Add/Update employee and other related information successfully.,' );

go
