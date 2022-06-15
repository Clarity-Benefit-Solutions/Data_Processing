CREATE or alter view VW_STATEMENT_DETAILS_ADD
as
    select
                                                [RESULTID] ID
      ,                                         BROKER_NAME BROKER_NAME
      ,                                         BROKER_NAME QB_BROKER_NAME
      ,                                         BROKER_NAME QB_AGENT
      ,                                         QB_CLIENT_NAME CLIENT_NAME
      ,                                         QB_CLIENT_NAME QB_CLIENT
      ,                                         QB_FEE QB_FEE
      ,                                         QB_FEE MEMO
      ,                                         QUANTITY Qty
      ,                                         SALES_PRICE [Sales Price]
      ,                                         SALES_PRICE [Amount]
      ,                                         0 [Open Balance]
      ,                                         COMMISSION_RATE COMMISSION_RATE
      ,                                         UNIT UNIT
      ,                                         TOTAL_PRICE [COMMISSION AMOUNT]
      ,                                         BROKER_STATUS [BROKER_STATUS]
      ,                                         '' EMAIL
      ,                                         QB_CLIENT_NAME QB_CLIENT_NAME
      ,                                         QB_FEE FEE_MEMO
      ,                                         [BROKER_ID] BROKER_ID
      ,                                         GETDATE( ) Date
      ,                                         [PAYLOCITY_ID] PAYLOCITY_ID
      ,                                         CONVERT( varchar(10) , [START_DATE] ) [START_DATE]
      ,/* CONVERT( varchar(10) , [DETAIL_ID] )*/null Num
      ,                                         start_date invoice_date
      ,                                         0 [CLIENT_ID]
    FROM
        [dbo].[STATEMENT_DETAILS_ADD]
go

