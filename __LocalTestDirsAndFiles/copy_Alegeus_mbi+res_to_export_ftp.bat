del %~dp0_local_FTP_Server_Server\Alegeus\*.* /q 

rmdir %~dp0_local_FTP_Server_Server\Alegeus\ /S /Q
mkdir %~dp0_local_FTP_Server_Server\Alegeus\


xcopy %~dp0AlegeusResultFiles\mbiFiles\*.* %~dp0_local_FTP_Server_Server\Alegeus\ /y
xcopy %~dp0AlegeusResultFiles\resFiles\*.* %~dp0_local_FTP_Server_Server\Alegeus\ /y