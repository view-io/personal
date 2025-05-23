# macOS DMG Installer for View Personal

This directory contains the script and resources for building a polished `.dmg` installer for the **View Personal** Avalonia desktop application on macOS.

---

## ✅ Prerequisites

Ensure the following tools are installed on your macOS system:

- **macOS system** (required for building `.dmg`)
- **.NET 9 SDK**  
  👉 [Download .NET SDK](https://dotnet.microsoft.com/download)
- **Xcode Command Line Tools** (for `SetFile`, `Rez`)
  ```sh
  xcode-select --install
  ```
- **create-dmg** (to generate `.dmg` from the `.app`)
  ```sh
  brew install create-dmg
  ```

---

## 💿 Building the `.dmg` Installer (Drag & Drop)

### Script:
```sh
chmod +x build_mac_dmg.sh
./build_mac_dmg.sh
```

### What it does:
- Publishes the Avalonia app for macOS (x64 and arm64)
- Creates a universal binary
- Builds a `.app` bundle with custom icon
- Generates a clean `.dmg` with:
  - Custom **app icon**
  - Proper Finder layout
- Cleans up all intermediate files after build

---

## 📁 Output Folder

| File                        | Description                                 |
|-----------------------------|---------------------------------------------|
| `ViewPersonalInstaller.dmg` | Final DMG with custom icon and branding     |

You can distribute this `.dmg` file directly. Users can drag the app into `/Applications` from the mounted volume.

---

## ⚠️ Notes

- All steps **must be performed on a Mac**

