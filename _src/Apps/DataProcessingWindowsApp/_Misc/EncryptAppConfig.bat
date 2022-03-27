copy %~dp0..\App.Config %~dp0..\Web.config /y

cd C:\Windows\Microsoft.NET\Framework\v4.0.30319
aspnet_regiis.exe -pef "AppSettings" C:\___Clarity\clarity_dev\r1_Data_Processing\_src\Apps\DataProcessingWindowsApp  -prov "DataProtectionConfigurationProvider"
aspnet_regiis.exe -pef "connectionStrings" C:\___Clarity\clarity_dev\r1_Data_Processing\_src\Apps\DataProcessingWindowsApp  -prov "DataProtectionConfigurationProvider"

copy %~dp0..\Web.Config %~dp0..\App.config /y