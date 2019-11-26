@echo off
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe D:\Yasila\Personal\Timesheet\AnimatedProgressIndicator\AnimatedProgressIndicator.sln /t:Rebuild /p:Configuration=Debug
set BUILD_STATUS=%ERRORLEVEL%
if %BUILD_STATUS%==0 echo Build success
if not %BUILD_STATUS%==0  echo Build failed
cmd /k