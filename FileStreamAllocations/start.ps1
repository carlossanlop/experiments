# This tells .NET Core to use the dotnet.exe passed as first argument to the script
$Env:DOTNET_ROOT="D:\runtime\artifacts\bin\testhost\net6.0-windows-Release-x64\"

# This tells .NET Core not to go looking for .NET Core in other places
$Env:DOTNET_MULTILEVEL_LOOKUP=0

# This determines which version of the new code to use:
# 0 = Same as 5.0 (legacy)
# 1 = New improved code
$Env:DOTNET_SYSTEM_IO_USELEGACYFILESTREAM=1

$Env:PATH=$Env:DOTNET_ROOT + ";" + $Env:PATH

Write-Output "PATH VARIABLE: $Env:PATH"

$devenv="C:\Program Files (x86)\Microsoft Visual Studio\2019\Preview\Common7\IDE\devenv.exe"
$sln="D:\testing\ConsoleCore\MySolution.sln"
&($devenv) $sln