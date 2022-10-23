@echo off
set _dest_=C:\SERVER\PasswordsAPI
pushd %_dest_%
sc create PasswordsAPI binPath= "%CD%\Passwords.API.exe" DisplayName= PasswordsAPI
ping localhost>nul
sc query PasswordsAPI
echo.
echo Done!
echo.
popd