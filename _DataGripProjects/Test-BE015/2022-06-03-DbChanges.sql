use broker_commission;
go


CREATE or alter view VW_STATEMENT_DETAILS_ADD
as
    select
        [DETAIL_ID] ID
      , BROKER_NAME BROKER_NAME
      , BROKER_NAME QB_BROKER_NAME
      , BROKER_NAME QB_AGENT
      , QB_CLIENT_NAME CLIENT_NAME
      , QB_CLIENT_NAME QB_CLIENT
      , QB_FEE QB_FEE
      , QB_FEE MEMO
      , QUANTITY Qty
      , SALES_PRICE [Sales Price]
      , SALES_PRICE [Amount]
      , 0 [Open Balance]
      , COMMISSION_RATE COMMISSION_RATE
      , UNIT UNIT
      , TOTAL_PRICE [COMMISSION AMOUNT]
        /* take status as broker status so we know which statement lines came from statement_details_add*/
      , STATUS [BROKER_STATUS]
      , '' EMAIL
      , QB_CLIENT_NAME QB_CLIENT_NAME
      , QB_FEE FEE_MEMO
      , [BROKER_ID] BROKER_ID
      , GETDATE( ) Date
      , [PAYLOCITY_ID] PAYLOCITY_ID
      , CONVERT( varchar(10) , [START_DATE] ) [START_DATE]
      ,/* CONVERT( varchar(10) , [DETAIL_ID] )*/
        left( concat( BROKER_ID , '-' , DETAIL_ID , '-' , START_DATE ) , 49 ) Num
      , start_date invoice_date
      , 0 [CLIENT_ID]
    FROM
        [dbo].[STATEMENT_DETAILS_ADD]
go


create or alter view vw_statement_design_view
as
    select *
    from
        (
            /* dont show those lines that came from statement_details_add (broker_status = Appended*/
            select
                DETAIL_ID
              , HEADER_ID
              , INVOICE_DATE
              , INVOICE_NUM
              , QB_CLIENT_NAME
              , CLIENT_NAME
              , BROKER_ID
              , BROKER_NAME
              , QB_FEE
              , FEE_MEMO
              , QUANTITY
              , COMMISSION_RATE
              , UNIT
              , STATUS
              , SALES_PRICE
              , TOTAL_PRICE
              , START_DATE
              , BROKER_STATUS
              , OPEN_BALANCE
              , month
              , year
              , line_payment_status
              , created_at
              , created_by
            from
                dbo.STATEMENT_DETAILS
            where
                  BROKER_STATUS != 'Appended'
              and line_payment_status in ('paid', 'pending')
            union all
            /* showe all lines from statement_details_add*/
            select
                DETAIL_ID
              , HEADER_ID
              , START_DATE INVOICE_DATE
              , left( concat( BROKER_ID , '-' , DETAIL_ID , '-' , START_DATE ) , 49 ) INVOICE_NUM
              , QB_CLIENT_NAME
              , CLIENT_NAME
              , BROKER_ID
              , BROKER_NAME
              , QB_FEE
              , FEE_MEMO
              , QUANTITY
              , COMMISSION_RATE
              , UNIT
              , STATUS
              , SALES_PRICE
              , TOTAL_PRICE
              , START_DATE
              , BROKER_STATUS
              , 0
              , null month
              , null year
              , 'Appended' line_payment_status
              , created_at
              , created_by
            
            from
                dbo.STATEMENT_DETAILS_ADD
        
        ) t
go
create or
alter
    PROCEDURE [dbo].[SP_IMPORT_FILE_SENT_SSIS]
@month nvarchar(30),
@year int
AS
BEGIN
    /* 0. check args are valid*/
    /* 0. check args are valid*/
    if isnull( @month , '' ) = ''
        begin
            THROW 51000, 'Month Cannot be Empty', 1;
        end
    
    if isnull( @year , 0 ) = 0
        begin
            THROW 51000, 'Year Cannot be Empty', 1;
        end
    
    set @month = dbo.format_field( @month );
    
    /*1. update current statement month and year in imported data for archival purposes*/
    update dbo.Import_OCT
    set
        statement_year  = @Year,
        statement_month = @Month,
        [Sales Price]   = isnull( [Sales Price] , 0 ),
        [Amount]        = isnull( [Amount] , 0 ),
        [Open Balance]  = isnull( [Open Balance] , 0 ),
        [Qty]           = isnull( [Qty] , 0 ),
        Num             = dbo.format_field( num ),
        Name= dbo.format_field( name ),
        Agent= dbo.format_field( Agent ),
        Memo= dbo.format_field( Memo ),
        is_deleted      =0;
    
    /* 2. receate Import-Archive for passed month and year */
    delete
    from
        dbo.Import_Archive
    where
          statement_month = @Month
      and statement_year = @Year;
    
    --     insert new records into import_archive
    insert into dbo.Import_Archive (
                                   ID,
                                   Type,
                                   Date,
                                   Num,
                                   Name,
                                   Memo,
                                   Agent,
                                   Qty,
                                   [Sales Price],
                                   Amount,
                                   [Open Balance],
                                   NUM_FORMATTED,
                                   memo_FORMATTED,
                                   Agent_FORMATTED,
                                   Name_FORMATTED,
                                   created_at,
                                   statement_month,
                                   statement_year,
                                   is_deleted
    )
    select
        ID
      , Type
      , Date
      , Num
      , Name
      , Memo
      , Agent
      , Qty
      , [Sales Price]
      , Amount
      , [Open Balance]
      , NUM_FORMATTED
      , memo_FORMATTED
      , Agent_FORMATTED
      , Name_FORMATTED
      , created_at
      , statement_month
      , statement_year
      , is_deleted
    from
        dbo.Import_OCT
    where
          statement_month = @Month
      and statement_year = @Year;
    
    /* 3. clear current statements header and details - DONT truncate so we opreserve header id over month by month iterations*/
    -- delete first curent statement details due to FK
    DELETE
    FROM
        [dbo].[STATEMENT_DETAILS];
    
    -- delete curent statement header
    DELETE
    FROM
        [dbo].[STATEMENT_HEADER];
    
    /* 4. generate new statements header and details fr om imported data joiniong imported data agent witgh various possible broker names in master */
    -- create distinct statement header
    INSERT INTO [dbo].[STATEMENT_HEADER]
    (
    [MONTH],
    [YEAR],
    [BROKER_ID],
    [BROKER_NAME],
    [FLAG],
    [Change_Date],
    PAYLOCITY_ID
    )
    SELECT
        @Month
      , @Year
      , RT.[BROKER_ID]
      , RT.[BROKER_NAME]
      , 0
      , getdate( )
      , RT.PAYLOCITY_ID
    FROM
        [dbo].[COMMISSION_RESULT] AS RT
    WHERE
          isnull( RT.[BROKER_NAME] , '' ) != ''
      AND isnull( RT.BROKER_ID , '' ) != ''
    GROUP BY
        RT.[BROKER_ID]
      , RT.[BROKER_NAME]
      , RT.PAYLOCITY_ID;
    
    -- create distinct statement details
    INSERT INTO [dbo].[STATEMENT_DETAILS]
    (
        [HEADER_ID]
    ,   [QB_CLIENT_NAME]
    ,   [CLIENT_NAME]
    ,   [BROKER_ID]
    ,   [BROKER_NAME]
    ,   [QB_FEE]
    ,   [FEE_MEMO]
    ,   [QUANTITY]
    ,   [COMMISSION_RATE]
    ,   [UNIT]
    ,   [SALES_PRICE]
    ,   [TOTAL_PRICE]
    ,   [START_DATE]
    ,   [STATUS]
    ,   [BROKER_STATUS]
    ,   [OPEN_BALANCE]
    ,   [INVOICE_NUM]
    ,   [INVOICE_DATE]
    ,   month
    ,   year
    )
    SELECT
        header.HEADER_ID
      , R.[QB_CLIENT]
      , R.[CLIENT_NAME]
      , R.[BROKER_ID]
      , R.[BROKER_NAME]
      , R.[QB_FEE]
      , R.[MEMO]
      , R.[Qty]
      , R.[COMMISSION_RATE]
      , R.[UNIT]
      , R.[Sales Price]
      , R.[COMMISSION AMOUNT]
      , R.[START_DATE]
      , R.[PAYLOCITY_ID]
      , R.[BROKER_STATUS]
      , R.[Open Balance]
      , RTRIM( LTRIM( R.[Num] ) )
      , [INVOICE_DATE]
      , HEADER.MONTH
      , HEADER.YEAR
    FROM
        [dbo].[STATEMENT_HEADER] AS HEADER
            LEFT JOIN [dbo].[COMMISSION_RESULT] AS R ON HEADER.[BROKER_ID] = R.[BROKER_ID]
    WHERE
          month = @Month
      and year = @Year;
    
    /* recalc totals*/
    exec SP_UPDATE_STATEMENT_PAYMENT_STATUS @month , @year;

END
go


CREATE or alter VIEW [dbo].[COMMISSION_RESULT_NAME0]
AS
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



CREATE or alter VIEW [dbo].[COMMISSION_RESULT]
AS
    WITH
        CTE_RESULT AS
            (
                SELECT
                    ID
                  , BROKER_NAME
                  , QB_BROKER_NAME
                  , QB_AGENT
                  , CLIENT_NAME
                  , QB_CLIENT
                  , QB_FEE
                  , MEMO
                  , Qty
                  , [Sales Price]
                  , Amount
                  , [Open Balance]
                  , COMMISSION_RATE
                  , UNIT
                  , [COMMISSION AMOUNT]
                  , BROKER_STATUS
                  , EMAIL
                  , QB_CLIENT_NAME
                  , FEE_MEMO
                  , BROKER_ID
                  , Date
                  , PAYLOCITY_ID
                  , START_DATE
                  , Num
                  , INVOICE_DATE
                  , CLIENT_ID
                FROM
                    [dbo].[COMMISSION_RESULT_NAME0]
                UNION ALL
                SELECT
                    ID
                  , BROKER_NAME
                  , QB_BROKER_NAME
                  , QB_AGENT
                  , CLIENT_NAME
                  , QB_CLIENT
                  , QB_FEE
                  , MEMO
                  , Qty
                  , [Sales Price]
                  , Amount
                  , [Open Balance]
                  , COMMISSION_RATE
                  , UNIT
                  , [COMMISSION AMOUNT]
                  , BROKER_STATUS
                  , EMAIL
                  , QB_CLIENT_NAME
                  , FEE_MEMO
                  , BROKER_ID
                  , Date
                  , PAYLOCITY_ID
                  , START_DATE
                  , Num
                  , INVOICE_DATE
                  , CLIENT_ID
                FROM
                    [dbo].[COMMISSION_RESULT_NAME1]
                UNION ALL
                SELECT
                    ID
                  , BROKER_NAME
                  , QB_BROKER_NAME
                  , QB_AGENT
                  , CLIENT_NAME
                  , QB_CLIENT
                  , QB_FEE
                  , MEMO
                  , Qty
                  , [Sales Price]
                  , Amount
                  , [Open Balance]
                  , COMMISSION_RATE
                  , UNIT
                  , [COMMISSION AMOUNT]
                  , BROKER_STATUS
                  , EMAIL
                  , QB_CLIENT_NAME
                  , FEE_MEMO
                  , BROKER_ID
                  , Date
                  , PAYLOCITY_ID
                  , START_DATE
                  , Num
                  , INVOICE_DATE
                  , CLIENT_ID
                FROM
                    [dbo].[COMMISSION_RESULT_NAME2]
                UNION ALL
                SELECT
                    ID
                  , BROKER_NAME
                  , QB_BROKER_NAME
                  , QB_AGENT
                  , CLIENT_NAME
                  , QB_CLIENT
                  , QB_FEE
                  , MEMO
                  , Qty
                  , [Sales Price]
                  , Amount
                  , [Open Balance]
                  , COMMISSION_RATE
                  , UNIT
                  , [COMMISSION AMOUNT]
                  , BROKER_STATUS
                  , EMAIL
                  , QB_CLIENT_NAME
                  , FEE_MEMO
                  , BROKER_ID
                  , Date
                  , PAYLOCITY_ID
                  , START_DATE
                  , Num
                  , INVOICE_DATE
                  , CLIENT_ID
                FROM
                    [dbo].[COMMISSION_RESULT_NAME3]
                UNION ALL
                SELECT
                    ID
                  , BROKER_NAME
                  , QB_BROKER_NAME
                  , QB_AGENT
                  , CLIENT_NAME
                  , QB_CLIENT
                  , QB_FEE
                  , MEMO
                  , Qty
                  , [Sales Price]
                  , Amount
                  , [Open Balance]
                  , COMMISSION_RATE
                  , UNIT
                  , [COMMISSION AMOUNT]
                  , BROKER_STATUS
                  , EMAIL
                  , QB_CLIENT_NAME
                  , FEE_MEMO
                  , BROKER_ID
                  , Date
                  , PAYLOCITY_ID
                  , START_DATE
                  , Num
                  , INVOICE_DATE
                  , CLIENT_ID
                FROM
                    [dbo].[COMMISSION_RESULT_NAME4]
                UNION ALL
                SELECT
                    ID
                  , BROKER_NAME
                  , QB_BROKER_NAME
                  , QB_AGENT
                  , CLIENT_NAME
                  , QB_CLIENT
                  , QB_FEE
                  , MEMO
                  , Qty
                  , [Sales Price]
                  , Amount
                  , [Open Balance]
                  , COMMISSION_RATE
                  , UNIT
                  , [COMMISSION AMOUNT]
                  , BROKER_STATUS
                  , EMAIL
                  , QB_CLIENT_NAME
                  , FEE_MEMO
                  , BROKER_ID
                  , Date
                  , PAYLOCITY_ID
                  , START_DATE
                  , Num
                  , INVOICE_DATE
                  , CLIENT_ID
                FROM
                    [dbo].[COMMISSION_RESULT_NAME5]
                UNION ALL
                SELECT
                    ID
                  , BROKER_NAME
                  , QB_BROKER_NAME
                  , QB_AGENT
                  , CLIENT_NAME
                  , QB_CLIENT
                  , QB_FEE
                  , MEMO
                  , Qty
                  , [Sales Price]
                  , Amount
                  , [Open Balance]
                  , COMMISSION_RATE
                  , UNIT
                  , [COMMISSION AMOUNT]
                  , BROKER_STATUS
                  , EMAIL
                  , QB_CLIENT_NAME
                  , FEE_MEMO
                  , BROKER_ID
                  , Date
                  , PAYLOCITY_ID
                  , START_DATE
                  , Num
                  , INVOICE_DATE
                  , CLIENT_ID
                FROM
                    [dbo].[COMMISSION_RESULT_NAME6]
                UNION ALL
                SELECT
                    ID
                  , BROKER_NAME
                  , QB_BROKER_NAME
                  , QB_AGENT
                  , CLIENT_NAME
                  , QB_CLIENT
                  , QB_FEE
                  , MEMO
                  , Qty
                  , [Sales Price]
                  , Amount
                  , [Open Balance]
                  , COMMISSION_RATE
                  , UNIT
                  , [COMMISSION AMOUNT]
                  , BROKER_STATUS
                  , EMAIL
                  , QB_CLIENT_NAME
                  , FEE_MEMO
                  , BROKER_ID
                  , Date
                  , PAYLOCITY_ID
                  , START_DATE
                  , Num
                  , INVOICE_DATE
                  , CLIENT_ID
                FROM
                    [dbo].[VW_STATEMENT_DETAILS_ADD]
            
            )
        /*sumeet: note: modified to get all lines even if an invoice has been sent - instead we return total invoice sent amoumt  for downstream checks*/
    SELECT distinct
        A.ID
      , A.BROKER_NAME
      , A.QB_BROKER_NAME
      , A.QB_AGENT
      , A.CLIENT_NAME
      , A.QB_CLIENT
      , A.QB_FEE
      , A.MEMO
      , A.Qty
      , A.[Sales Price]
      , A.Amount
      , A.[Open Balance]
      , A.COMMISSION_RATE
      , A.UNIT
      , A.[COMMISSION AMOUNT]
      , A.BROKER_STATUS
      , A.EMAIL
      , A.QB_CLIENT_NAME
      , A.FEE_MEMO
      , A.BROKER_ID
      , A.Date
      , A.PAYLOCITY_ID
      , A.START_DATE
      , A.Num
      , A.INVOICE_DATE
      , A.CLIENT_ID
      , dbo.get_invoice_sent_total_open_balance( A.Num ) Total_Invoice_Sent_Open_Balance
    FROM
        CTE_RESULT AS A
            LEFT JOIN [dbo].[SENT_INVOICE] AS B ON RTRIM( LTRIM( A.[Num] ) ) = RTRIM( LTRIM( B.[INVOICE_NUM] ) )
go

