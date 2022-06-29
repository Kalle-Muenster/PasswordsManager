@if "%ECHO_STATE%"=="" (@echo off ) else (@echo %ECHO_STATE% )

:: Prepare locations
set _name_=PasswordsAPI
set _call_=%CD%
cd %~dp0
set _here_=%CD%
set _root_=%CD%

:: Set VersionNumber
set PasswordsAPIVersionNumber=00000001
set PasswordsAPIVersionString=0.0.0.1
set DotNetVersionString=core5

:: Set Dependencies
if "%ConsolaBinRoot%"=="" (
set ConsolaBinRoot=%_root_%\..\Consola\bin\%DotNetVersionString%
)
if "%Int24TypesBinRoot%"=="" (
set Int24TypesBinRoot=%_root_%\..\Int24Types\bin\%DotNetVersionString%
)
if "%YpsCryptBinRoot%"=="" (
set YpsCryptBinRoot=%_root_%\..\YpsCrypt\bin\%DotNetVersionString%
)

:: Set parameters and solution files
call %_root_%\Args "%~1" "%~2" "%~3" "%~4" PasswordsAPI.sln PasswordsGUI\Passwords.GUI.csproj

:: Do the Build
cd %_here_%
call MsBuild %_target_% %_args_%
cd %_call_%

:: Cleanup Environment
call %_root_%\Args ParameterCleanUp

