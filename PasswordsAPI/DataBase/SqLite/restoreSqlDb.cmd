@echo off
if exist "____db.db" (
del /f /q db.db
rename ____db.db db.db
echo Successfully restored the SqLite Db file!
echo.
)
