rem net stop w3svc
xcopy E:\Temp\DataProcessingWebUIBin\Debug\net6.0\*.* E:\Temp\DataProcessingWebUIPublish /y /s
rem net start w3svc