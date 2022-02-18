del %~dp0FTP\AlegeusSourceFiles_New\*.* /q 
del %~dp0FTP\AlegeusSourceFiles_Old\*.* /q  
del %~dp0FTP\AlegeusSourceFiles_NoChange\*.* /q 
del %~dp0FTP\AlegeusSourceFiles_Own\*.* /q 

xcopy %~dp0AlegeusSourceFiles_New\*.* %~dp0FTP\AlegeusSourceFiles_New\ /y
xcopy %~dp0AlegeusSourceFiles_Old\*.* %~dp0FTP\AlegeusSourceFiles_Old\ /y
xcopy %~dp0AlegeusSourceFiles_NoChange\*.* %~dp0FTP\AlegeusSourceFiles_NoChange\ /y
xcopy %~dp0AlegeusSourceFiles_Own\*.* %~dp0FTP\AlegeusSourceFiles_Own\ /y