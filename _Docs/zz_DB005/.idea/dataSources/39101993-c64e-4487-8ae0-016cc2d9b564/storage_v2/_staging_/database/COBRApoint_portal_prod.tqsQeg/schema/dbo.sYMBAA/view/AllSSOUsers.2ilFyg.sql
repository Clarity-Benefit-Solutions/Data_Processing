alter VIEW [dbo].[AllSSOUsers]
    AS /**/
    /* All QB*/
        SELECT
            20 orderseq
          , 'QB' entitytype
          , memberid
          , '-1' brokerid
          , NULL clientcontactid
          , a.clientid
          , a.clientdivisionid
          , c.clientname organizationname
          , 1 /*c.Active */AS organizationisactive
          , d.divisionname divisionname
          , individualidentifier
          , 'PRIMARY' contacttype
          , salutation
          , firstname
          , lastname
          , LOWER( email ) email
          , NULL title
          , NULL department
          , a.phone
          , phone2
          , a.Address1
          , a.Address2
          , a.city city
          , a.State state
          , a.postalcode postalcode
          , a.country
          , dbo.fixbool( a.active ) active
          , NULL loginstatus
          , NULL registrationcode
          , NULL registrationdate
          , NULL userdisplayname
          , dbo.fixbool( allowsso ) allowsso
          , ssoidentifier
          , NULL userid
          , ssn
          , ssn employeeid
          , dob dob
          , c.clientalternate employerid
          , dbo.GetQBQualifyingEventDate( a.MemberID ) QualEventDate
        FROM
            qb a
                LEFT JOIN client c ON a.clientid = c.clientid
                LEFT JOIN dbo.clientdivision d ON a.clientdivisionid = d.clientdivisionid
        WHERE
                a.memberid IN (
                                  SELECT
                                      memberid
                                  FROM
                                      dbo.qbplan p
                                  WHERE
                                        p.status IN ('E', 'E45', 'P', 'PR')
                                    AND p.enddate > GETDATE( )
                                    AND p.ldoc > GETDATE( )
                              )
            
            /**/
        UNION
        /**/
        
        /* All SPM*/
        SELECT
            10 orderseq
          , 'SPM' entitytype
          , memberid
          , '-1' brokerid
          , NULL clientcontactid
          , a.clientid
          , a.clientdivisionid
          , c.clientname organizationname
          , 1 /*c.Active */AS organizationisactive
          , d.divisionname divisionname
          , NULL individualidentifier
          , 'PRIMARY' contacttype
          , salutation
          , firstname
          , lastname
          , LOWER( email ) email
          , NULL title
          , NULL department
          , a.phone
          , phone2
          , a.Address1
          , a.Address2
          , a.city city
          , a.State state
          , a.postalcode postalcode
          , a.country
          , dbo.fixbool( a.active ) active
          , NULL loginstatus
          , NULL registrationcode
          , NULL registrationdate
          , NULL userdisplayname
          , dbo.fixbool( allowsso ) allowsso
          , ssoidentifier
          , NULL userid
          , ssn
          , ssn employeeid
          , dob dob
          , c.clientalternate employerid
          , NULL QualEventDate
        FROM
            spm a
                LEFT JOIN client c ON a.clientid = c.clientid
                LEFT JOIN dbo.clientdivision d ON a.clientdivisionid = d.clientdivisionid
            
            /**/
        UNION
        /**/
        
        /* All client division level contacts last to ensure we preserve clientdivid when upserting*/
        SELECT
            a.orderseq
          , a.entitytype
          , a.memberid
          , a.brokerid
          , a.clientdivisioncontactid
          , a.clientid
          , a.clientdivisionid
          , c.clientname
          , 1 /*c.Active */AS organizationisactive
          , d.divisionname
          , a.individualidentifier
          , a.contacttype
          , a.salutation
          , a.firstname
          , a.lastname
          , a.email
          , a.title
          , a.department
          , a.phone
          , a.phone2
          , a.Address1
          , a.Address2
          , a.city city
          , a.State state
          , a.postalcode postalcode
          , a.country
          , a.active
          , a.loginstatus
          , a.registrationcode
          , a.registrationdate
          , a.userdisplayname
          , a.allowsso
          , a.ssoidentifier
          , a.userid
          , a.ssn
          , a.employeeid
          , NULL dob
          , c.clientalternate employerid
          , NULL QualEventDate
        FROM
            (
                SELECT
                    30 orderseq
                  , 'CLIENT_CONTACT' entitytype
                  , NULL memberid
                  , '-1' brokerid
                  , clientdivisioncontactid
                  , [dbo].[GetClientId]( clientdivisionid ) clientid
                  , clientdivisionid
                  , NULL individualidentifier
                  , contacttype
                  , salutation
                  , firstname
                  , lastname
                  , LOWER( email ) email
                  , title
                  , department
                  , phone
                  , phone2
                  , Address1
                  , Address2
                  , city city
                  , State state
                  , postalcode postalcode
                  , country
                  , dbo.fixbool( active ) active
                  , loginstatus
                  , registrationcode
                  , registrationdate
                  , NULL userdisplayname
                  , dbo.fixbool( allowsso ) allowsso
                  , ssoidentifier
                  , NULL userid
                  , NULL ssn
                  , NULL employeeid
                  , NULL dob
                  , NULL QualEventDate
                FROM
                    clientdivisioncontact
            ) AS a
                LEFT JOIN client c ON a.clientid = c.clientid
                LEFT JOIN dbo.clientdivision d ON a.clientdivisionid = d.clientdivisionid
            
            /**/
        UNION
        /**/
        
        /* All client level contacts last to ensure we preserve clientid when upserting*/
        SELECT
            40 orderseq
          , 'CLIENT_CONTACT' entitytype
          , NULL memberid
          , '-1' brokerid
          , a.clientcontactid
          , a.clientid
          , '0' clientdivisionid
          , c.clientname organizationname
          , 1 /*c.Active */AS organizationisactive
          , NULL divisionname
          , NULL individualidentifier
          , contacttype
          , salutation
          , firstname
          , lastname
          , LOWER( email ) email
          , title
          , department
          , a.phone
          , phone2
          , a.Address1
          , a.Address2
          , a.city city
          , a.State state
          , a.postalcode postalcode
          , a.country
          , dbo.fixbool( a.active ) active
          , loginstatus
          , registrationcode
          , registrationdate
          , NULL userdisplayname
          , dbo.fixbool( allowsso ) allowsso
          , ssoidentifier
          , NULL userid
          , NULL ssn
          , NULL employeeid
          , NULL dob
          , c.clientalternate employerid
          , NULL QualEventDate
        FROM
            clientcontact a
                LEFT JOIN client c ON a.clientid = c.clientid/*
         LEFT JOIN dbo.ClientDivision d ON a.ClientDivisionID = d.ClientDivisionID*/
            
            /**/
        UNION
        /**/
        
        /* All broker contacts */
        SELECT
            50 orderseq
          , 'BROKER_CONTACT' entitytype
          , NULL memberid
          , a.brokerid
          , NULL clientcontactid
          , '-1' clientid
          , '-1' clientdivisionid
          , c.brokername organizationname
          , c.active organizationisactive
          , NULL divisionname
          , NULL individualidentifier
          , contacttype
          , salutation
          , firstname
          , lastname
          , LOWER( email ) email
          , title
          , department
          , a.phone
          , a.phone2
          , a.Address1
          , a.Address2
          , a.city city
          , a.State state
          , a.postalcode postalcode
          , a.country
          , dbo.fixbool( a.active ) active
          , loginstatus
          , registrationcode
          , registrationdate
          , userdisplayname
          , dbo.fixbool( allowsso ) allowsso
          , ssoidentifier
          , NULL userid
          , NULL ssn
          , NULL employeeid
          , NULL dob
          , NULL employerid
          , NULL QualEventDate
        FROM
            brokercontact a
                LEFT JOIN dbo.broker c ON a.brokerid = c.brokerid
            
            /**/
        UNION
        /**/
        /* All TPAAdmin Users*/
        SELECT
            60 orderseq
          , 'TPA_ADMIN_USER' entitytype
          , NULL memberid
          , NULL brokerid
          , NULL clientcontactid
          , NULL clientid
          , NULL clientdivisionid
          , NULL organizationname
          , 1 /*c.Active */AS organizationisactive
          , NULL divisionname
          , NULL individualidentifier
          , NULL contacttype
          , NULL salutation
          , NULL firstname
          , NULL lastname
          , LOWER( ca.useremailaddress ) email
          , NULL title
          , NULL department
          , NULL phone
          , NULL phone2
          , NULL Address1
          , NULL Address2
          , NULL city
          , NULL state
          , NULL postalcode
          , NULL country
          , dbo.fixbool( ca.active ) active
          , NULL loginstatus
          , NULL registrationcode
          , NULL registrationdate
          , NULL userdisplayname
          , 1 allowsso
          , NULL ssoidentifier
          , LOWER( CAST( ct.[UserID] AS nvarchar(36) ) ) userid
          , NULL ssn
          , NULL employeeid
          , NULL dob
          , NULL employerid
          , NULL QualEventDate
        FROM
            admintpauser ct
                INNER JOIN adminuser ca ON ca.username = ct.username
GO

