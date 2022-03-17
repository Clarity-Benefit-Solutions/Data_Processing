del %~dp0FTP\_AlegeusSourceFiles\*.* /q 

mkdir %~dp0FTP\_AlegeusSourceFiles\

xcopy %~dp0AlegeusSourceFiles\*.* %~dp0FTP\_AlegeusSourceFiles\ /y
