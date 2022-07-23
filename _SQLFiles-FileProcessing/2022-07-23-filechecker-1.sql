use Data_Processing;
go
create or
alter
    PROCEDURE [dbo].[proc_alegeus_ExportImportFile](
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
                        ' and isnull(row_type,'''') <> ''IA'' ';
        end;
    if (@exportType) = 'original_file'
        begin
            /* sumeet: use org_data_row as that as the data we actually imported*/
            set @recordsSql =
                        'select ltrim(rtrim(org_data_row)) as file_row, source_row_no from [dbo].[mbi_file_table]  ' +
                        ' where mbi_file_name = @mbi_file_name ' +
                        --                         ' and (len(isnull(error_message, ''''))) = 0 ' +
                        ' and isnull(row_type,'''') <> ''IA'' ';
        end;
    if (@exportType) = 'rejected_lines'
        begin
            set @recordsSql =
                        'select ltrim(rtrim(data_row)) as file_row, source_row_no from [dbo].[mbi_file_table]  ' +
                        ' where mbi_file_name = @mbi_file_name ' +
                        ' and (len(isnull(error_message, ''''))) > 0 ' +
                        ' and (error_message not like ''IRRELEVANT_LINE%'') ' +
                        ' and isnull(row_type,'''') <> ''IA'' ';
        end;
    if (@exportType) = 'all_lines_with_errors'
        begin
            set @recordsSql =
                        'select ltrim(rtrim(concat(' +
                        ' data_row, ' +
                        ' dbo.get_error_message_for_display(error_message)' +
                        ' ))) as file_row ' +
                        ', source_row_no from [dbo].[mbi_file_table]  ' +
                        ' where mbi_file_name = @mbi_file_name ' +
                        --  ' and (len(isnull(error_message, ''''))) = 0 ' +
                        ' and isnull(row_type,'''') <> ''IA'' ';
        end;
    if (@exportType) = 'rejected_lines_with_errors'
        begin
            set @recordsSql =
                        'select ltrim(rtrim(concat(' +
                        ' data_row, ' +
                        ' dbo.get_error_message_for_display(error_message)' +
                        ' ))) as file_row ' +
                        ', source_row_no from [dbo].[mbi_file_table]  ' +
                        ' where mbi_file_name = @mbi_file_name ' +
                        ' and (len(isnull(error_message, ''''))) <> 0 ' +
                        ' and (error_message not like ''IRRELEVANT_LINE%'') ' +
                        ' and isnull(row_type,'''') <> ''IA'' ';
        end;
    
    --     print @recordsSql;
    
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
    
    --     print @headerSql;
    set @finalSql = 'Select file_row, source_row_no from (' + @headerSql + ' UNION ALL ' + @recordsSql +
                    ') t2 order by source_row_no';
    
    EXECUTE sp_executesql @finalSql , N'@mbi_file_name nvarchar(2000), @batchId nvarchar(2000), @cnt int OUTPUT' ,
            @mbi_file_name = @mbi_file_name , @batchId = @batchId , @cnt = @count OUTPUT;
END
go

create or
alter function get_error_message_for_display( @error_message nvarchar(max) ) returns nvarchar(max) as
begin
    set @error_message = ltrim( rtrim( isnull( @error_message , '' ) ) );
    
    if (len( @error_message ) = 0)
        begin
            return ' | OK';
        end
    
    if @error_message like 'IRRELEVANT_LINE:%'
        begin
            return ' | WARNING: This line was ignored. Please remove this line from future files';
        end
    
    return concat( ' | ERROR: ' , @error_message );

end;

go

create or
alter
    PROCEDURE [dbo].[proc_cobra_ExportImportFile](
                                                 @cobra_file_name nvarchar(2000),
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
                        'select ltrim(rtrim(data_row)) as file_row, source_row_no from [dbo].[cobra_file_table]  ' +
                        ' where cobra_file_name = @cobra_file_name ' +
                        ' and (len(isnull(error_message, ''''))) = 0 ' +
                        ' and isnull(row_type,'''') <> ''IA'' ';
        end;
    if (@exportType) = 'original_file'
        begin
            set @recordsSql =
                        'select ltrim(rtrim(data_row)) as file_row, source_row_no from [dbo].[cobra_file_table]  ' +
                        ' where cobra_file_name = @cobra_file_name ' +
                        ' and isnull(row_type,'''') <> ''IA'' ';
        end;
    if (@exportType) = 'rejected_lines'
        begin
            set @recordsSql =
                        'select ltrim(rtrim(data_row)) as file_row, source_row_no from [dbo].[cobra_file_table]  ' +
                        ' where cobra_file_name = @cobra_file_name ' +
                        ' and (len(isnull(error_message, ''''))) > 0 ' +
                        ' and (error_message not like ''IRRELEVANT_LINE%'') ' +
                        ' and isnull(row_type,'''') <> ''IA'' ';
        end;
    if (@exportType) = 'all_lines_with_errors'
        begin
            set @recordsSql =
                        'select ltrim(rtrim(concat(' +
                        ' data_row, ' +
                        ' dbo.get_error_message_for_display(error_message)' +
                        ' ))) as file_row ' +
                        ', source_row_no from [dbo].[cobra_file_table]  ' +
                        ' where cobra_file_name = @cobra_file_name ' +
                        ' and isnull(row_type,'''') <> ''IA'' ';
        end;
    if (@exportType) = 'rejected_lines_with_errors'
        begin
            set @recordsSql =
                        'select ltrim(rtrim(concat(' +
                        ' data_row, ' +
                        ' dbo.get_error_message_for_display(error_message)' +
                        ' ))) as file_row ' +
                        ', source_row_no from [dbo].[cobra_file_table]  ' +
                        ' where cobra_file_name = @cobra_file_name ' +
                        ' and (len(isnull(error_message, ''''))) <> 0 ' +
                        ' and (error_message not like ''IRRELEVANT_LINE%'') ' +
                        ' and isnull(row_type,'''') <> ''IA'' ';
        end;
    
    --     print @recordsSql;
    if (isnull( @recordsSql , '' ) = '')
        begin
            set @errorMsg = 'Incorrect exportType: ' + @exporttype;
            throw 500001, @errorMsg, 1;
        end;
    
    /**/
    set @countSql = 'Select @cnt=count(*) from ( ' + @recordsSql + ') as T';
    EXECUTE sp_executesql @countSql , N'@cobra_file_name nvarchar(2000), @cnt int OUTPUT' ,
            @cobra_file_name = @cobra_file_name , @cnt = @count OUTPUT;
    /**/
    set @headerSql =
                'select top 1 ' +
                ' replace(concat(ltrim(rtrim(data_row)), '','', @batchId), ''XX'', @cnt) as file_row, source_row_no from [dbo].[cobra_file_table]  ' +
                ' where cobra_file_name = @cobra_file_name ' +
                ' and row_type = ''IA'' ';
    
    --     print @headerSql;
    set @finalSql = 'Select file_row, source_row_no from (' + @headerSql + ' UNION ALL ' + @recordsSql +
                    ') t2 order by source_row_no';
    
    EXECUTE sp_executesql @finalSql , N'@cobra_file_name nvarchar(2000), @batchId nvarchar(2000), @cnt int OUTPUT' ,
            @cobra_file_name = @cobra_file_name , @batchId = @batchId , @cnt = @count OUTPUT;
END
go

exec [dbo].[proc_alegeus_ExportImportFile] 'BENSOSCPU_IH_AL_20220723--132217006.mbi' , 'rejected_lines' , 1;
go
exec [dbo].[proc_alegeus_ExportImportFile] 'BENSOSCPU_IH_AL_20220723--132217006.mbi' , 'rejected_lines_with_errors' ,
     1;
go
