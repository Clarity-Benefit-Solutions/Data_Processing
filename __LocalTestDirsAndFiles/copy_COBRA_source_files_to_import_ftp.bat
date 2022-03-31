del %~dp0FTP\_COBRA_SourceFiles\ /q
mkdir %~dp0FTP\_COBRA_SourceFiles\ 
mkdir %~dp0_AlegeusSourceFiles\
xcopy %~dp0COBRA_source_files\*.* %~dp0FTP\_COBRA_SourceFiles\ /y
xcopy %~dp0COBRA_source_files\*.* %~dp0FTP\_AlegeusSourceFiles\  /y

