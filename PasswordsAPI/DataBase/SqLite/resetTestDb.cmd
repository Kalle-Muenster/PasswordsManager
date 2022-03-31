@echo off
IF NOT EXIST "____db.db" (
copy /Y /B db.db ____db.db
)
del /f /q db.db
copy /Y /B test.db.db db.db
echo Test Database has ben Reset to defaults
