use Alegeus_File_Processing;
go

create table app_settings
(
id            int identity,
environment nvarchar(50) default 'PROD',
setting_name  nvarchar(100),
setting_value nvarchar(2000),
created_at    datetime default getdate( )
)
go
create unique index app_settings_uk on app_settings(environment, setting_name);


update app_settings set setting_value = replace(setting_value, '\', '/');
