@echo off
if "%~1" == "" goto FALSCH

set _source_=%~dp0
set _dest_=%~1

tokken --u-1 --o-"%TEMP%\PasswordsAPI_GuiClient_installation.tmp"
echo @echo off > "%TEMP%\PasswordsAPI_GuiClient_installation.bat"
echo reg add HKEY_CURRENT_USER\Software\ThePasswords\TheGUI\TheClient /v TheAgent /t REG_SZ /d ^^>> "%TEMP%\PasswordsAPI_GuiClient_installation.bat"
type %TEMP%\PasswordsAPI_GuiClient_installation.tmp >> "%TEMP%\PasswordsAPI_GuiClient_installation.bat"
echo reg add HKEY_CURRENT_USER\Software\ThePasswords\TheGUI\TheClient /v TheName /t REG_SZ /d %COMPUTERNAME% >> "%TEMP%\PasswordsAPI_GuiClient_installation.bat"
echo reg add HKEY_CURRENT_USER\Software\ThePasswords\TheGUI\TheNetwork
echo reg add HKEY_CURRENT_USER\Software\ThePasswords\TheGUI\TheNetwork /v TheHost /t REG_SZ /d localhost
echo del /s /q "%TEMP%\PasswordsAPI_GuiClient_installation.tmp" >> "%TEMP%\PasswordsAPI_GuiClient_installation.bat"
echo. >> "%TEMP%\PasswordsAPI_GuiClient_installation.bat"
echo echo ok! PasswordsGUI succsessfully registered and can be used now :^^) >> "%TEMP%\PasswordsAPI_GuiClient_installation.bat"
echo. >> "%TEMP%\PasswordsAPI_GuiClient_installation.bat"
echo del /s /q "%TEMP%\PasswordsAPI_GuiClient_installation.bat"

md "%_dest_%"
copy /B /Y "%_source_%*.exe" "%_dest_%">nul
copy /B /Y "%_source_%*.dll" "%_dest_%">nul
copy /B /Y "%_source_%*.json" "%_dest_%">nul
copy /B /Y "%_source_%*.bat" "%_dest_%">nul
cd "%_dest_%"
call "%TEMP%\PasswordsAPI_GuiClient_installation.bat"
goto END

:FALSCH
echo.
echo parameter 1 must be given targetpath where to install
echo.

:END
set _dest_=
set _source_=
