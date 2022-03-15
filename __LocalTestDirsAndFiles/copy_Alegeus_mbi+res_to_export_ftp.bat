del %~dp0_local_FTP_Server_Server\Alegeus\*.* /q 

xcopy %~dp0AlegeusResultFiles\mbiFiles\*.* %~dp0_local_FTP_Server_Server\Alegeus\ /y
xcopy %~dp0AlegeusResultFiles\resFiles\*.* %~dp0_local_FTP_Server_Server\Alegeus\ /y