use COBRApoint_portal_prod;
go

create view AllClientsAndDivisions
as
    select
        c.ClientID
      , c.ClientName
      , c.ClientGroupID
      , c.DBAName
      , c.EIN
      , c.Address1
      , c.Address2
      , c.City
      , c.State
      , c.PostalCode
      , c.Country
      , c.Phone
      , c.Fax
      , c.BillingStartDate
      , c.SendHIPAACertWithQBSRandQBTermination
      , c.SendHIPAACertWithWelcomeLetter
      , c.AllowClientPortalAccess
      , c.AutomaticallyCreateQBSubsidyForInsignificantAmount
      , c.IgnoreSPMPaymentGracePeriods
      , c.MigratedClient
      , c.ClientPremiumNotice
      , c.EmployeeCountRange
      , c.WeeklyBillingFirstDayOfWeek
      , c.ClientRequiresAEI2009SubsidyEligibleAttestationToSubsidize
      , c.ClientAllowsAEI2009PlanEnrollmentOptions
      , c.ClientDoesOwnAEI2009SubsidyTakenReportPosting
      , c.ClientAlternate
      , c.AllowClientSSO
      , c.SPMInitialGracePeriodDays
      , c.SPMConvenienceFee
      , c.SPMPartnerConvenienceFee
      , c.QBConvenienceFee
      , c.QBPartnerConvenienceFee
      , d.ClientDivisionID
      , d.DivisionName
      , d.Address1 DivisionAddress1
      , d.Address2 DivisionAddress2
      , d.City DivisionCity
      , d.State DivisionState
      , d.PostalCode DivisionPostalCode
      , d.Country DivisionCountry
      , d.Phone DivisionPhone
      , d.Fax DivisionFaxc
      , d.ClientDivisionDoesOwnAEI2009SubsidyTakenReportPosting
      , d.SendHIPAACertWithSPMWelcomeAndTerminationLetters DivisionSendHIPAACert
      , d.AllowClientDivisionSSO
      , d.Active DivisionActive
    from
        dbo.Client as c
            left join dbo.ClientDivision d on c.ClientID = d.ClientID
go
select * from AllClientsAndDivisions where ClientName like 'Acelero%';
SELECT *, replace(SSN, '-', '') as SSNFormatted
FROM
    dbo.QB
where
    ClientId = '1289'
ORDER by
    MemberId;

SELECT *, replace(SSN, '-', '') as SSNFormatted  FROM dbo.SPM  where ClientId = '1289'  ORDER by MemberId
SELECT *, replace(SSN, '-', '') as SSNFormatted  FROM dbo.NPM  where ClientDivisionID = '1289'  ORDER by MemberId
