@echo off
title Particle Engine installer
powershell.exe -NoProfile -File "%~dp0scripts\install-ribbon.ps1" -Configuration Release
echo.
pause
