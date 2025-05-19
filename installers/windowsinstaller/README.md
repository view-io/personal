# View Personal Installer

This folder contains the Inno Setup script and build tools to create a Windows installer for the **View Personal** application.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/en-us/download) (required to build the application)
- [Inno Setup 6](https://jrsoftware.org/isdl.php) must be installed on your system and accessible at the default install path

## Files

- `ViewPersonal.iss` – The Inno Setup script file
- `BuildInstaller.bat` – Batch file that builds the application and generates the installer
- `Resource/icon.ico` – Application icon

## Building the Installer

Simply run the `BuildInstaller.bat` file. This batch file will:

- Check if InnoSetup is installed
- Build application in Release mode
- Compile the installer script
- Create the installer in the `Output` folder

## What the Installer Does

- Copies all application files from the Release build
- Creates desktop and start menu shortcuts
- Offers to launch the application after installation

## Notes

- On first run, the application will create the following files and directories under:
 `%LocalAppData%\ViewPersonal`
- This includes:
	- `data\appsettings.json` – Configuration file  
	- `data\view-personal.db` – SQLite database file  
	- `logs\` – Folder for log output files  
- These files are **not bundled with the installer** and will be automatically created when the application starts for the first time.