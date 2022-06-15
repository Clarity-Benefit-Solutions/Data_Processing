create or
alter
    function format_field( @value varchar(max) ) returns varchar(max)
as
begin
    return REPLACE(
            REPLACE(
                    REPLACE(
                            REPLACE(
                                    REPLACE(
                                            REPLACE(
                                                    LTRIM(
                                                            RTRIM(
                                                                    UPPER(
                                                                            isnull( @value , '' ) )
                                                                )
                                                        ) , '&' , '' ) ,
                                            ',' , '' ) ,
                                    '.' , '' ) ,
                            '- ' , '-' ) ,
                    '- ' , '-' ) ,
            '-' , '' )
end
go

