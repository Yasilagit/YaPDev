@echo off
"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe" "D:\\Yasila\\Project\\Project Management\\ADB\\Source\\SOURCE\\Solution\\Chakra.sln" /T:"DI_Chakra_Web" /P:DeployOnBuild=true /P:PublishProfile=Web.pubxml
set BUILD_STATUS=%ERRORLEVEL%
if %BUILD_STATUS%==0 echo Build success
if not %BUILD_STATUS%==0  echo Build failed
cmd /k


