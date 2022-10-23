@echo off
cd /d %~dp0
set _data_=%CD%\PasswordsAPIInstallData
set _dest_=c:\SERVER\PasswordsAPI

:: generate application key which will be registered
tokomako --3-32 --g-8~8~8~8 --o-passwordsapi_setup.key

:: generate template which add registry keys branch 
type regentries.inst>passwordsapi_setup.reg
echo "TheHostName"=sz:%COMPUTERNAME%>>passwordsapi_setup.reg

:: generate batch script which applies these registry changes
echo @echo off>passwordsapi_setup.bat
echo reg import passwordsapi_setup.reg>>passwordsapi_setup.bat
echo reg add HKEY_LOCAL_MACHINE\SOFTWARE\ThePasswords\TheAPI\TheService /v TheKeyString /t REG_SZ /d ^^>>passwordsapi_setup.bat
type passwordsapi_setup.key>>passwordsapi_setup.bat
echo call servicescript.bat>>passwordsapi_setup.bat

:: call generated batch script. then remove generated files 
call passwordsapi_setup.bat
del /s /q passwordsapi_setup.reg
del /s /q passwordsapi_setup.key
del /s /q passwordsapi_setup.bat
