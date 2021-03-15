@ECHO OFF
SETLOCAL

:: This tells .NET Core to use the dotnet.exe passed as first argument to the script
SET DOTNET_ROOT=D:\runtime\artifacts\bin\testhost\net6.0-windows-Release-x64\

:: This tells .NET Core not to go looking for .NET Core in other places
SET DOTNET_MULTILEVEL_LOOKUP=0

:: This determines which version of the new code to use:
:: 0 = Same as 5.0 (legacy)
:: 1 = New improved code
SET DOTNET_SYSTEM_IO_USELEGACYFILESTREAM=1

:: Put our local dotnet.exe on PATH first so Visual Studio knows which one to use
SET PATH=%DOTNET_ROOT%;%PATH%

echo PATH VARIABLE: %PATH%

:: This starts VS
"C:\Program Files (x86)\Microsoft Visual Studio\2019\Preview\Common7\IDE\devenv.exe" "D:\testing\ConsoleCore\MySolution.sln"