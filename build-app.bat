@echo off
rem ===========================================================================
rem  PhotoVault - compilare + creare folder de distributie (publish\PhotoVault)
rem
rem  Cerinte: Windows 10/11 cu .NET 10 SDK  (https://dotnet.microsoft.com/download/dotnet/10.0)
rem  Rulare:  dublu-click pe build-app.bat  sau, din linia de comanda:
rem           build-app.bat            compilare + teste + publicare
rem           build-app.bat notest     fara testele unitare (mai rapid)
rem
rem  Rezultat:
rem    publish\PhotoVault\PhotoVault.exe            aplicatia portabila (self-contained, un singur .exe)
rem    publish\PhotoVault\README.txt                instructiuni scurte
rem    publish\PhotoVault-<versiune>-win-x64.zip    arhiva pentru distributie
rem ===========================================================================
setlocal EnableExtensions
cd /d "%~dp0"
title PhotoVault - build

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [EROARE] Nu a fost gasit .NET SDK ^(comanda "dotnet"^).
    echo          Instaleaza .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0
    goto :fail
)

for /f "usebackq delims=" %%v in (`powershell -NoProfile -Command "([xml](Get-Content 'Directory.Build.props')).Project.PropertyGroup.Version"`) do set "VERSION=%%v"
if "%VERSION%"=="" set "VERSION=dev"

echo.
echo === PhotoVault %VERSION% ===
echo.
echo [1/4] Compilare solutie (Release)...
dotnet build PhotoVault.sln -c Release -nologo -v:minimal
if errorlevel 1 goto :fail

if /i "%~1"=="notest" (
    echo [2/4] Teste unitare: sarite ^(parametrul notest^).
) else (
    echo [2/4] Teste unitare...
    dotnet test PhotoVault.Tests -c Release --no-build -nologo -v:minimal --filter "Category!=RealRaw"
    if errorlevel 1 goto :fail
)

echo [3/4] Publicare versiune portabila ^(self-contained, un singur .exe^)...
if exist "publish" rmdir /s /q "publish"
dotnet publish PhotoVault.App -p:PublishProfile=Portabil -nologo -v:minimal
if errorlevel 1 goto :fail
if not exist "publish\PhotoVault\PhotoVault.exe" (
    echo [EROARE] publish\PhotoVault\PhotoVault.exe nu a fost creat.
    goto :fail
)

echo [4/4] Arhiva .zip pentru distributie...
powershell -NoProfile -Command "Compress-Archive -Path 'publish\PhotoVault' -DestinationPath 'publish\PhotoVault-%VERSION%-win-x64.zip' -Force"
if errorlevel 1 goto :fail

echo.
echo === Gata ===
for %%f in ("publish\PhotoVault\PhotoVault.exe") do echo   Aplicatie: %%~ff  ^(%%~zf bytes^)
echo   Arhiva:    %cd%\publish\PhotoVault-%VERSION%-win-x64.zip
echo.
echo Folderul publish\PhotoVault poate fi copiat oriunde ^(alt disc, stick USB^) si pornit cu PhotoVault.exe.
echo.
if "%~1"=="" pause
endlocal
exit /b 0

:fail
echo.
echo === Build ESUAT - vezi mesajele de mai sus ===
echo.
if "%~1"=="" pause
endlocal
exit /b 1
