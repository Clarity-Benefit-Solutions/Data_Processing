CREATE   VIEW [dbo].[COMMISSION_RESULT_NAME0]
AS;
    SELECT
        OCT.ID
      , BC.BROKER_NAME
      , REPLACE( LTRIM( RTRIM( UPPER( BC.QB_BROKER_NAME ) ) ) , '&' , '' ) [QB_BROKER_NAME]
      , REPLACE( LTRIM( RTRIM( UPPER( OCT.Agent ) ) ) , '&' , '' ) [QB_AGENT]
      , REPLACE( LTRIM( RTRIM( UPPER( BC.CLIENT_NAME ) ) ) , '&' , '' ) [CLIENT_NAME]
      , REPLACE( LTRIM( RTRIM( UPPER( OCT.[Name] ) ) ) , '&' , '' ) [QB_CLIENT]
      , LTRIM( RTRIM( UPPER( BC.QB_FEE ) ) ) [QB_FEE]
      , LTRIM( RTRIM( UPPER( OCT.Memo ) ) ) [MEMO]
      , cast( OCT.Qty as numeric(18, 2) ) Qty
      , OCT.[Sales Price]
      , OCT.Amount
      , OCT.[Open Balance]
      , BC.COMMISSION_RATE
      , BC.UNIT
      , (CASE
             WHEN BC.UNIT = 'Per Qt' THEN BC.COMMISSION_RATE * OCT.Qty
             WHEN BC.UNIT = 'Per Amount' THEN BC.COMMISSION_RATE * OCT.Amount
             WHEN BC.UNIT = 'Flat Rate' THEN BC.COMMISSION_RATE
         END) [COMMISSION AMOUNT]
      , BC.BROKER_STATUS
      , BC.EMAIL
      , BC.QB_CLIENT_NAME
      , BC.FEE_MEMO
      , BC.ID [BROKER_ID]
      , OCT.Date
      , BC.PAYLOCITY_ID
      , BC.[START_DATE]
      , OCT.[Num]
      , OCT.Date [INVOICE_DATE]
      , BC.[CLIENT_ID]
    FROM
        [dbo].[BROKER_CLIENT] AS BC
            LEFT JOIN [dbo].[Import_OCT] AS OCT
                      ON
                              BC.QB_BROKER_NAME_FORMATTED = oct.Agent_FORMATTED
                              and BC.QB_CLIENT_NAME_FORMATTED = oct.Name_FORMATTED
                              AND bc.QB_FEE_FORMATTED = oct.memo_FORMATTED
    WHERE
          isnull( BC.QB_BROKER_NAME_FORMATTED , '' ) != ''
      AND OCT.ID IS NOT NULL
go

