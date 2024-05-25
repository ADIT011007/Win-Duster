@echo off

:: Created by: Shawn Brink
:: Created on: October 1, 2015
:: Updated on: March 8, 2021
:: Tutorial: https://www.tenforums.com/tutorials/24742-reset-windows-update-windows-10-a.html

:: Prompt to Run as administrator
Set "Variable=0" & if exist "%temp%\getadmin.vbs" del "%temp%\getadmin.vbs"
fsutil dirty query %systemdrive%  >nul 2>&1 && goto :(Privileges_got)
If "%1"=="%Variable%" (echo. &echo. Please right-click on the file and select &echo. "Run as administrator". &echo. Press any key to exit. &pause>nul 2>&1& exit)
cmd /u /c echo Set UAC = CreateObject^("Shell.Application"^) : UAC.ShellExecute "%~0", "%Variable%", "", "runas", 1 > "%temp%\getadmin.vbs"&cscript //nologo "%temp%\getadmin.vbs" & exit
:(Privileges_got)

:: Checking and Stopping the Windows Update services
echo Stopping BITS service...
net start bits >nul 2>&1
net stop bits >nul 2>&1
echo Checking the BITS service status.
sc query bits | findstr /I /C:"STOPPED" 
if not %errorlevel%==0 ( 
    goto :bits 
) 

echo Stopping Windows Update (wuauserv) service...
net start wuauserv >nul 2>&1
net stop wuauserv >nul 2>&1
echo Checking the wuauserv service status.
sc query wuauserv | findstr /I /C:"STOPPED" 
if not %errorlevel%==0 ( 
    goto :loop3 
) 

echo Stopping Application Identity (appidsvc) service...
net start appidsvc >nul 2>&1
net stop appidsvc >nul 2>&1
echo Checking the appidsvc service status.
sc query appidsvc | findstr /I /C:"STOPPED" 
if not %errorlevel%==0 ( 
    goto :loop4 
) 

echo Stopping Cryptographic Services (cryptsvc) service...
taskkill /f /im cryptsvc.exe >nul 2>&1

:: Wait for the service to stop
ping 127.0.0.1 -n 6 >nul 2>&1

echo Checking the cryptsvc service status.
sc query cryptsvc | findstr /I /C:"STOPPED" 
if not %errorlevel%==0 ( 
    goto :Reset 
) else (
    echo Cryptographic Services did not stop. Proceeding...
)

:Reset
echo Flushing DNS resolver cache...
Ipconfig /flushdns

echo Deleting QMgr database...
del /s /q /f "%ALLUSERSPROFILE%\Application Data\Microsoft\Network\Downloader\qmgr*.dat" 
del /s /q /f "%ALLUSERSPROFILE%\Microsoft\Network\Downloader\qmgr*.dat"

echo Deleting Windows Update log files...
del /s /q /f "%SYSTEMROOT%\Logs\WindowsUpdate\*"

echo Handling pending.xml if exists...
if exist "%SYSTEMROOT%\winsxs\pending.xml.bak" del /s /q /f "%SYSTEMROOT%\winsxs\pending.xml.bak" 
if exist "%SYSTEMROOT%\winsxs\pending.xml" ( 
    takeown /f "%SYSTEMROOT%\winsxs\pending.xml" 
    attrib -r -s -h /s /d "%SYSTEMROOT%\winsxs\pending.xml" 
    ren "%SYSTEMROOT%\winsxs\pending.xml" pending.xml.bak 
) 

echo Renaming SoftwareDistribution folder...
if exist "%SYSTEMROOT%\SoftwareDistribution.bak" rmdir /s /q "%SYSTEMROOT%\SoftwareDistribution.bak"
if exist "%SYSTEMROOT%\SoftwareDistribution" ( 
    attrib -r -s -h /s /d "%SYSTEMROOT%\SoftwareDistribution" 
    ren "%SYSTEMROOT%\SoftwareDistribution" SoftwareDistribution.bak 
) 

echo Renaming Catroot2 folder...
if exist "%SYSTEMROOT%\system32\Catroot2.bak" rmdir /s /q "%SYSTEMROOT%\system32\Catroot2.bak" 
if exist "%SYSTEMROOT%\system32\Catroot2" ( 
    attrib -r -s -h /s /d "%SYSTEMROOT%\system32\Catroot2" 
    ren "%SYSTEMROOT%\system32\Catroot2" Catroot2.bak 
) 

:: Reset Windows Update policies
echo Resetting Windows Update policies...
reg delete "HKCU\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate" /f
reg delete "HKCU\SOFTWARE\Microsoft```