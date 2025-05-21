# macOS Installer (.pkg) for View Personal

This directory contains scripts and resources for building a macOS installer (`.pkg`) for the **View Personal** Avalonia application.

---

## ✅ Prerequisites

Ensure the following tools are installed on your macOS system:

- **macOS machine** (required to build and sign `.pkg` installers)
- **.NET 9 SDK**  
  [Download .NET SDK](https://dotnet.microsoft.com/download)
- **Xcode Command Line Tools** (required for `pkgbuild`)
  
  Install via Terminal:
  ```sh 
  xcode-select --install

## How to Build a .pkg Installer for Avalonia App on macOS

### Automated Build
A build script is provided for convenience:

```sh
chmod +x build_mac_pkg.sh
./build_mac_pkg.sh 
```

This script will:
- Publishes the Avalonia app for macOS (x64 and arm64)
- Merges both into a universal binary
- Creates a .app bundle inside the correct macOS structure
- Builds the final .pkg installer using pkgbuild
- The resulting `.pkg` file will be created in this directory.

### Output
- After a successful build, you'll find the following inside the Output/ folder:
   - `ViewPersonalInstaller.pkg`
- You can double-click ViewPersonalInstaller.pkg to install the app into the /Applications directory.

## Notes
- All steps must be performed on a Mac.