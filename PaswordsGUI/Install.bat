@echo off
if "%~1" == "" goto FALSCH
set _source_=%~dp0
set _dest_=%~1


:: creating temporary files :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

:: generate a client key string wich the application will use for authentication when connecting to a password server
tokomako --u-1 --o-"%TEMP%\PasswordsAPI_GuiClient_installation.tmp"

:: generate a registration script which registers the application on the local machine with the current desktop sessions user account 
echo @echo off > "%TEMP%\PasswordsAPI_GuiClient_installation.bat"

:: add commands to the script which add reg entries 
echo reg add HKEY_CURRENT_USER\Software\ThePasswords\TheGUI\TheClient /v TheAgent /t REG_SZ /d ^^>> "%TEMP%\PasswordsAPI_GuiClient_installation.bat"
type %TEMP%\PasswordsAPI_GuiClient_installation.tmp >> "%TEMP%\PasswordsAPI_GuiClient_installation.bat"
echo reg add HKEY_CURRENT_USER\Software\ThePasswords\TheGUI\TheClient /v TheName /t REG_SZ /d %COMPUTERNAME%>> "%TEMP%\PasswordsAPI_GuiClient_installation.bat"
echo reg add HKEY_CURRENT_USER\Software\ThePasswords\TheGUI\TheNetwork /v TheHost /t REG_SZ /d localhost>> "%TEMP%\PasswordsAPI_GuiClient_installation.bat"
echo reg add HKEY_CURRENT_USER\Software\ThePasswords\TheGUI\TheNetwork\localhost /v ThePort /t REG_DWORD /d 0x00001388>> "%TEMP%\PasswordsAPI_GuiClient_installation.bat"
echo reg add HKEY_CURRENT_USER\Software\ThePasswords\TheGUI\TheNetwork\localhost /v TheServer /t REG_SZ /d localhost>> "%TEMP%\PasswordsAPI_GuiClient_installation.bat"

:: make the script deleting the temporarly generated client key file after adding it to the registry 
echo del /s /q "%TEMP%\PasswordsAPI_GuiClient_installation.tmp" >> "%TEMP%\PasswordsAPI_GuiClient_installation.bat"

:: make the script printing output which tells if registration was successfull or if it has failed for some reason maybe
echo. >> "%TEMP%\PasswordsAPI_GuiClient_installation.bat"
echo if "%%ERRORLEVEL%%"=="0" ^( >>"%TEMP%\PasswordsAPI_GuiClient_installation.bat"
echo echo ok! PasswordsGUI succsessfully registered and can be used now >> "%TEMP%\PasswordsAPI_GuiClient_installation.bat"
echo ^) else ^( >> "%TEMP%\PasswordsAPI_GuiClient_installation.bat"
echo echo ERROR ERROR ERROR >> "%TEMP%\PasswordsAPI_GuiClient_installation.bat"
echo ^) >> "%TEMP%\PasswordsAPI_GuiClient_installation.bat"
echo. >> "%TEMP%\PasswordsAPI_GuiClient_installation.bat"

:: add command which makes the script deleting itself after running to completion
echo del /s /q "%TEMP%\PasswordsAPI_GuiClient_installation.bat"



:: executing the installation  :::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

:: create the destination directory where to install the application into
md "%_dest_%"

:: copy over all application files from installer to installation destination  
copy /B /Y "%_source_%*.exe" "%_dest_%">nul
copy /B /Y "%_source_%*.dll" "%_dest_%">nul
copy /B /Y "%_source_%*.json" "%_dest_%">nul

:: in the destination folder delete installation leftovers not used by the application itself 
cd "%_dest_%"
del /s /q tokomako.exe

:: then execute that registration script which before was generated in the temp folder 
call "%TEMP%\PasswordsAPI_GuiClient_installation.bat"
goto END

:FALSCH
echo.
echo FALSCH!
echo parameter 1 must be given path to destination directory application should be installed into
echo.

:END
set _dest_=
set _source_=
