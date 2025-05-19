@echo off
echo Building View Personal Installer...

:: Step 1: Build the .NET application in Release mode
echo.
echo Building .NET project...
dotnet build ..\..\src\View.Personal\View.Personal.csproj -c Release

if %ERRORLEVEL% NEQ 0 (
    echo Error: .NET build failed. Please check the build output.
    pause
    exit /b %ERRORLEVEL%
)

:: Step 2: Check if Inno Setup is installed
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

:: Step 3: Create Output directory if it doesn't exist
if not exist "Output" mkdir Output

:: Step 4: Confirm that the application was built
if not exist "..\..\src\View.Personal\bin\Release\net9.0\View.Personal.exe" (
    echo Error: Build output not found. Ensure the application builds successfully.
    pause
    exit /b 1
)

:: Step 5: Run the Inno Setup Compiler
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
