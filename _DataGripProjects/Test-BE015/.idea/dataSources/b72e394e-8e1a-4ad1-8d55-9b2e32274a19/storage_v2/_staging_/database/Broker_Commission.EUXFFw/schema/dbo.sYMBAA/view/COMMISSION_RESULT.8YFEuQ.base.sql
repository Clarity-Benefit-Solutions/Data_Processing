CREATE VIEW [dbo].[COMMISSION_RESULT]
    AS
        WITH
            CTE_RESULT AS
                (
                    SELECT *
                    FROM
                        [dbo].[COMMISSION_RESULT_NAME0]
                    UNION ALL
                    SELECT *
                    FROM
                        [dbo].[COMMISSION_RESULT_NAME1]
                    UNION ALL
                    SELECT *
                    FROM
                        [dbo].[COMMISSION_RESULT_NAME2]
                    UNION ALL
                    SELECT *
                    FROM
                        [dbo].[COMMISSION_RESULT_NAME3]
                    UNION ALL
                    SELECT *
                    FROM
                        [dbo].[COMMISSION_RESULT_NAME4]
                    UNION ALL
                    SELECT *
                    FROM
                        [dbo].[COMMISSION_RESULT_NAME5]
                    UNION ALL
                    SELECT *
                    FROM
                        [dbo].[COMMISSION_RESULT_NAME6]
                    UNION ALL
                    SELECT *
                    FROM
                        [dbo].[VW_STATEMENT_DETAILS_ADD]
                
                )
            /*sumeet: note: modified to get all lines even if an invoice has been sent - instead we return total invoice sent amoumt  for downstream checks*/
        SELECT distinct
            A.*
          , dbo.get_invoice_sent_total_open_balance( A.Num ) as Total_Invoice_Sent_Open_Balance
        FROM
            CTE_RESULT AS A
                LEFT JOIN [dbo].[SENT_INVOICE] AS B ON RTRIM( LTRIM( A.[Num] ) ) = RTRIM( LTRIM( B.[INVOICE_NUM] ) )
go

