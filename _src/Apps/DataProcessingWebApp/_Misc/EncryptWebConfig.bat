cd C:\Windows\Microsoft.NET\Framework\v4.0.30319
aspnet_regiis.exe -pef "AppSettings" C:\___Clarity\clarity_dev\r1_Data_Processing\_src\Apps\DataProcessingWebApp -prov "DataProtectionConfigurationProvider"
aspnet_regiis.exe -pef "connectionStrings" C:\___Clarity\clarity_dev\r1_Data_Processing\_src\Apps\DataProcessingWebApp -prov "DataProtectionConfigurationProvider"