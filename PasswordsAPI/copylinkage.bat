@echo off
setlocal
set _arch_=%~1
set _conf_=%~2
set _here_=%~dp0
set _bins_=%_here_%%~3
set _test_=%_here_%..\PasswordsAPI.Tests\bin\%_arch_%\%_conf_%\net5.0


del /q "%_here_%Ijwhost.dll"
copy /Y /B "%ConsolaBinRoot%\%_arch_%\%_conf_%\Ijwhost.dll" "%_here_%Ijwhost.dll"
copy /Y /B "%YpsCryptBinRoot%\%_arch_%\%_conf_%\YpsTests.dll" "%_bins_%"
copy /Y /B "%Int24TypesBinRoot%\%_arch_%\%_conf_%\Int24Tests.dll" "%_bins_%"
copy /Y /B "%ConsolaBinRoot%\%_arch_%\%_conf_%\Consola.Test.dll" "%_bins_%"

copy /Y /B "%YpsCryptBinRoot%\%_arch_%\%_conf_%\*.json" "%_test_%"
copy /Y /B "%Int24TypesBinRoot%\%_arch_%\%_conf_%\*.json" "%_test_%"
copy /Y /B "%_test_%\*.json" "%_bins_%"
set errorlevel=0