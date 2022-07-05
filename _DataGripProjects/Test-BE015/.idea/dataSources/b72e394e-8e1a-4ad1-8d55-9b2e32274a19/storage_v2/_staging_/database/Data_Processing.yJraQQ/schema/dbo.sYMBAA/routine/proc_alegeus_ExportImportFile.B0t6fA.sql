create or alter PROCEDURE [dbo].[proc_alegeus_ExportImportFile](
                                                     @mbi_file_name nvarchar(2000),
                                                     @exportType nvarchar(50),
                                                     @batchId nvarchar(2000) )
AS
BEGIN
    
    declare @count int;
    declare @recordsSql nvarchar(max);
    declare @errorMsg nvarchar(2000);
    declare @headerSql nvarchar(max);
    declare @finalSql nvarchar(max);
    declare @countSql nvarchar(max);
    
    if (@exportType) = 'passed_lines'
        begin
            set @recordsSql =
                        'select ltrim(rtrim(data_row)) as file_row, source_row_no from [dbo].[mbi_file_table]  ' +
                        ' where mbi_file_name = @mbi_file_name ' +
                        ' and (len(isnull(error_message, ''''))) = 0 ' +
                        ' and row_type <> ''IA'' ';
        end;
    if (@exportType) = 'original_file'
        begin
            /* sumeet: use org_data_row as that as the data we actually imported*/
            set @recordsSql =
                        'select ltrim(rtrim(org_data_row)) as file_row, source_row_no from [dbo].[mbi_file_table]  ' +
                        ' where mbi_file_name = @mbi_file_name ' +
                        --                         ' and (len(isnull(error_message, ''''))) = 0 ' +
                        ' and row_type <> ''IA'' ';
        end;
    if (@exportType) = 'rejected_lines'
        begin
            set @recordsSql =
                        'select ltrim(rtrim(data_row)) as file_row, source_row_no from [dbo].[mbi_file_table]  ' +
                        ' where mbi_file_name = @mbi_file_name ' +
                        ' and (len(isnull(error_message, ''''))) > 0 ' +
                        ' and row_type <> ''IA'' ';
        end;
    if (@exportType) = 'all_lines_with_errors'
        begin
            set @recordsSql =
                        'select ltrim(rtrim(concat(data_row, '','', ' +
                        'case when len(error_message) > 0 then concat( ''PreCheck Errors: '' , error_message ) ' +
                        ' else ''PreCheck: OK'' end ) )) as file_row ' +
                        ', source_row_no from [dbo].[mbi_file_table]  ' +
                        ' where mbi_file_name = @mbi_file_name ' +
                        --  ' and (len(isnull(error_message, ''''))) = 0 ' +
                        ' and row_type <> ''IA'' ';
        end;
    if (@exportType) = 'rejected_lines_with_errors'
        begin
            set @recordsSql =
                        'select ltrim(rtrim(concat(data_row, '','', ' +
                        'case when len(error_message) > 0 then concat( ''PreCheck Errors: '' , error_message ) ' +
                        ' else ''PreCheck: OK'' end ) )) as file_row ' +
                        ', source_row_no from [dbo].[mbi_file_table]  ' +
                        ' where mbi_file_name = @mbi_file_name ' +
                        ' and (len(isnull(error_message, ''''))) <> 0 ' +
                        ' and row_type <> ''IA'' ';
        end;
    
    if (isnull( @recordsSql , '' ) = '')
        begin
            set @errorMsg = 'Incorrect exportType: ' + @exporttype;
            throw 500001, @errorMsg, 1;
        end;
    
    /**/
    set @countSql = 'Select @cnt=count(*) from ( ' + @recordsSql + ') as T';
    EXECUTE sp_executesql @countSql , N'@mbi_file_name nvarchar(2000), @cnt int OUTPUT' ,
            @mbi_file_name = @mbi_file_name , @cnt = @count OUTPUT;
    /**/
    set @headerSql =
                'select top 1 ' +
                ' replace(concat(ltrim(rtrim(data_row)), '','', @batchId), ''XX'', @cnt) as file_row, source_row_no from [dbo].[mbi_file_table]  ' +
                ' where mbi_file_name = @mbi_file_name ' +
                ' and row_type = ''IA'' ';
    
    print @headerSql;
    set @finalSql = 'Select file_row, source_row_no from (' + @headerSql + ' UNION ALL ' + @recordsSql +
                    ') t2 order by source_row_no';
    
    EXECUTE sp_executesql @finalSql , N'@mbi_file_name nvarchar(2000), @batchId nvarchar(2000), @cnt int OUTPUT' ,
            @mbi_file_name = @mbi_file_name , @batchId = @batchId , @cnt = @count OUTPUT;
END
go

