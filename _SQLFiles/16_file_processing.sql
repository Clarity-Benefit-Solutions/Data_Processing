use Alegeus_File_Processing;
go
alter PROCEDURE [dbo].[proc_alegeus_AlterHeadersOwn]
AS
BEGIN
    INSERT INTO [dbo].[alegeus_file_final]
    (
        [file_row]
    ,   [folder_name]
    )
    select
        data_row
      , folder_name
    from
        [dbo].[alegeus_file_staging]
    -- where left(ltrim(data_row),3) in( 'IC,','IB,','IZ,','IH,','ID,','IA,')
    
    -- 	select *,'Final' from  [dbo].[alegeus_file_final]
    
    ---write to ftp_auto_List
    exec [dbo].[insert_to_auto_ftp_list]

END

go

alter PROCEDURE [dbo].[proc_alegeus_AlterHeaders2015]
AS
BEGIN
    
    declare @header_indicator as varchar(25)
    declare @datarow as nvarchar(max)
    declare @record_count as varchar(max)
    set @header_indicator = (
                                Select
                                    left( ltrim( data_row ) , 3 )
                                from
                                    [dbo].[alegeus_file_staging]
                                where
                                    row_num = 1
                            )
    set @datarow = (
                       Select
                           data_row
                       from
                           [dbo].[alegeus_file_staging]
                       where
                           row_num = 1
                   )
    --select @header_indicator
    set @record_count = 0
    
    truncate table [dbo].[alegeus_file_final]
    
    INSERT INTO [dbo].[Process_log]
    (
    [header_ind],
    date_stamp
    )
    VALUES (
           @header_indicator,
           getdate( )
           )
    
    -- select @header_indicator
    
    if @header_indicator not in ('IA,')
        begin
            INSERT INTO [dbo].[Process_log]
            (
            [header_ind],
            date_stamp
            )
            VALUES (
                   'Block 1',
                   getdate( )
                   )
            
            set @record_count = (
                                    select
                                        count( * )
                                    from
                                        [dbo].[alegeus_file_staging]
                                )
            -- insert header
            --IA,XX,BENEFL1,New Beneflex Standard Import Template 2015,Standard Result Template,Beneflex Standard Export Template
            INSERT INTO [dbo].[alegeus_file_final]
            (
                [file_row]
            ,   [folder_name]
            )
            select
                replace( [header_row] , ',XX,' , ',' + cast( @record_count as varchar(100) ) + ',' )
              , ''
            from
                [dbo].[ClarityStandardFileHeader2015]
            
            -- insert other rows
            INSERT INTO [dbo].[alegeus_file_final]
            (
                [file_row]
            ,   [folder_name]
            )
            select
                data_row
              , folder_name
            from
                [dbo].[alegeus_file_staging]
                --             where
                --                 left( ltrim( data_row ) , 3 ) in ('IC,', 'IB,', 'IZ,', 'IH,', 'ID,')
            order by
                row_num
        
        end
    
    ---write to ftp_auto_List
    exec [dbo].[insert_to_auto_ftp_list];

END
go


alter PROCEDURE [dbo].[proc_alegeus_AlterHeaders2019]
AS
BEGIN
    
    declare @header_indicator as varchar(25)
    declare @datarow as nvarchar(max)
    declare @record_count as varchar(max)
    set @header_indicator = (
                                Select
                                    left( ltrim( data_row ) , 3 )
                                from
                                    [dbo].[alegeus_file_staging]
                                where
                                    row_num = 1
                            )
    set @datarow = (
                       Select
                           data_row
                       from
                           [dbo].[alegeus_file_staging]
                       where
                           row_num = 1
                   )
    --select @header_indicator
    set @record_count = 0
    
    truncate table [dbo].[alegeus_file_final]
    
    INSERT INTO [dbo].[Process_log]
    (
    [header_ind],
    date_stamp
    )
    VALUES (
           @header_indicator,
           getdate( )
           )
    
    -- select @header_indicator
    
    if @header_indicator not in ('IA,')
        begin
            INSERT INTO [dbo].[Process_log]
            (
            [header_ind],
            date_stamp
            )
            VALUES (
                   'Block 1',
                   getdate( )
                   )
            
            set @record_count = (
                                    select
                                        count( * )
                                    from
                                        [dbo].[alegeus_file_staging]
                                )
            -- insert header
            --IA,XX,BENEFL1,New Beneflex Standard Import Template 2015,Standard Result Template,Beneflex Standard Export Template
            INSERT INTO [dbo].[alegeus_file_final]
            (
                [file_row]
            ,   [folder_name]
            )
            select
                replace( [header_row] , ',XX,' , ',' + cast( @record_count as varchar(100) ) + ',' )
              , ''
            from
                [dbo].[ClarityStandardFileHeader2019]
            
            -- insert other rows
            INSERT INTO [dbo].[alegeus_file_final]
            (
                [file_row]
            ,   [folder_name]
            )
            select
                data_row
              , folder_name
            from
                [dbo].[alegeus_file_staging]
                --             where
                --                 left( ltrim( data_row ) , 3 ) in ('IC,', 'IB,', 'IZ,', 'IH,', 'ID,')
            order by
                row_num
        
        end
    
    ---write to ftp_auto_List
    exec [dbo].[insert_to_auto_ftp_list];

END
go


alter PROCEDURE [dbo].[proc_alegeus_AlterHeadersNone]
AS
BEGIN
	
	declare @header_indicator as varchar(25)
	declare @datarow as nvarchar(max)
	declare @record_count as varchar(max)
    set @header_indicator = (Select left(ltrim(data_row),3) from [dbo].[alegeus_file_staging] where row_num = 1)
	set @datarow = (Select data_row from [dbo].[alegeus_file_staging] where row_num = 1)
	--select @header_indicator
	set @record_count = 0

	truncate table [dbo].[alegeus_file_final]

	
     INSERT INTO [dbo].[Process_log]
           ([header_ind],date_stamp)
     VALUES
           (@header_indicator,getdate())

	  INSERT INTO [dbo].[alegeus_file_final]
           ([file_row]
           ,[folder_name])
	  select data_row,folder_name from [dbo].[alegeus_file_staging]
-- 	  where left(ltrim(data_row),3) in( 'IC,','IB,','IZ,','IH,','ID,','IA,')



-- 	select *,'Final' from  [dbo].[alegeus_file_final]

    ---write to ftp_auto_List
	exec [dbo].[insert_to_auto_ftp_list]


END

go

