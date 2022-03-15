del %~dp0_local_FTP_Server_Server\Alegeus\*.* /q 

xcopy %~dp0AlegeusResultFiles\mbi\*.* %~dp0_local_FTP_Server_Server\Alegeus\ /y
xcopy %~dp0AlegeusResultFiles\res\*.* %~dp0_local_FTP_Server_Server\Alegeus\ /y