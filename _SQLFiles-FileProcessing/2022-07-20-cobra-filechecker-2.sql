
use SalesForce_COBRA;
GO

select * from dbo.Client where ClientID = 1257;
select * from dbo.ClientDivision where ClientID = 1257;

select * from dbo.ClientDivision where ClientDivisionID = 2108;


select * from dbo.ClientPlanQB where ClientID = 1257
and PlanName = 'Cigna DHMO 2021';

select * from dbo.ClientDivisionQBPlan where ClientDivisionID = 2108;
