@echo off
cd /d %~dp0
call addtopath -pre "C:\Program Files\7-Zip"
set _data_=%CD%\PasswordsAPIInstallData
set _dest_=c:\SERVER\PasswordsAPI
md "%_dest_%"
7z x -y PasswordsAPIInstallData.zip
ping localhost>null
del /q PasswordsAPIInstallData.zip
xcopy "%_data_%\*.*" "%_dest_%" /E /Y
