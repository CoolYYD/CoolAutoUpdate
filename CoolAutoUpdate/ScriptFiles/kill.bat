@echo off
cd /d %~dp0

:loop
tasklist|find /i "UIH.ClientProxy.Service.exe" 
if %errorlevel%== 1 (goto f)
else (goto e)
goto :loop 

:f
taskkill /f /im  UIH.ClientProxy.Service.exe
taskkill /f /im  UIH.Notice.exe
:e
taskkill /f /im UIH.Report.Service.exe
taskkill /f /im UIH.Report.Server.exe 
taskkill /f /im UIH.Report.Process.exe

taskkill /f /im UIH.Common.exe
taskkill /f /im UIH.Customize.exe 
taskkill /f /im UIH.Image.exe
taskkill /f /im UIH.HighSpeed.exe
taskkill /f /im UIH.BJCA.Service.exe
exit

