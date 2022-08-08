@echo off
setlocal on
set _arch_=%~1
set _conf_=%~2
set _here_=%~dp0

del /q "%_here_%Ijwhost.dll"
copy /Y /B "%ConsolaBinRoot%\%_arch_%\%_conf_%\Ijwhost.dll" "%_here_%Ijwhost.dll"


