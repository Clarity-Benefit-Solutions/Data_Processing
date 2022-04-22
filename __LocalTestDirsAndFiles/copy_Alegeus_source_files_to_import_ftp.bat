del %~dp0FTP\_AlegeusSourceFiles\*.* /q 

rmdir %~dp0FTP\_AlegeusSourceFiles\ /Q /S
mkdir %~dp0FTP\_AlegeusSourceFiles\

xcopy %~dp0AlegeusSourceFiles\*.* %~dp0FTP\_AlegeusSourceFiles\ /y
