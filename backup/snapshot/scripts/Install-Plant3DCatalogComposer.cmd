@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-Plant3DCatalogComposer.ps1" %*
if errorlevel 1 exit /b 1
