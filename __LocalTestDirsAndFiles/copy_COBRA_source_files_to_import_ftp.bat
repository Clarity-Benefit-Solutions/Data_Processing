del %~dp0FTP\_COBRA_InFiles\ /q
mkdir %~dp0FTP\_COBRA_InFiles\ 
mkdir %~dp0_AlegeusSourceFiles\
xcopy %~dp0COBRA_source_files\*.* %~dp0FTP\_COBRA_InFiles\ /y
xcopy %~dp0COBRA_source_files\*.* %~dp0FTP\_AlegeusSourceFiles\  /y

