@echo off
echo Building View Personal Installer...
set "PUBLISH_DIR=..\..\src\_publish"
:: Step 1: Publish the main .NET application
echo.
echo Publishing main .NET project...
dotnet publish ../../src/View.Personal/View.Personal.csproj -c Release --self-contained true -r win-x64 -o "%PUBLISH_DIR%\View.Personal"
if %ERRORLEVEL% NEQ 0 (
    echo Error: View.Personal publish failed. Please check the build output.
    pause
    exit /b %ERRORLEVEL%
)

:: Step 2: Publish the updater .NET application
echo.
echo Publishing updater .NET project...
dotnet publish ../../src/ViewPersonal.Updater/ViewPersonal.Updater.csproj -c Release --self-contained true -r win-x64 -o "%PUBLISH_DIR%\ViewPersonal.Updater"

if %ERRORLEVEL% NEQ 0 (
    echo Error: ViewPersonal.Updater publish failed.
    pause
    exit /b %ERRORLEVEL%
)

:: Step 3: Check if Inno Setup is installed
echo.
echo Checking for Inno Setup...
if exist "%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" (
    set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
) else if exist "%ProgramFiles%\Inno Setup 6\ISCC.exe" (
    set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
) else (
    echo Error: Inno Setup not found. Please install Inno Setup from https://jrsoftware.org/isdl.php
    pause
    exit /b 1
)

:: Step 4: Create Output directory if it doesn't exist
if not exist "Output" mkdir Output

:: Step 5: Confirm that the applications were published
if not exist "%PUBLISH_DIR%\View.Personal\View.Personal.exe" (
    echo Error: View.Personal publish output not found.
    pause
    exit /b 1
)

if not exist "%PUBLISH_DIR%\ViewPersonal.Updater\ViewPersonal.Updater.exe" (
    echo Error: ViewPersonal.Updater publish output not found.
    pause
    exit /b 1
)

:: Step 6: Run the Inno Setup Compiler
echo.
echo Running Inno Setup Compiler...
"%ISCC%" ViewPersonal.iss

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Installer built successfully! You can find it in the Output folder.
) else (
    echo.
    echo Error building installer. Please check the output above for details.
)

pause
