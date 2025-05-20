#!/bin/bash
# Fixed version: Build and package Avalonia app as a macOS .pkg installer

set -e

APP_NAME="View Personal"
APP_BUNDLE_NAME="View Personal.app"
PUBLISH_DIR="publish-macos"
PKG_ROOT="pkg-root"
OUTPUT_DIR="Output"
UNIVERSAL_DIR="$PUBLISH_DIR/universal"
ICON_PATH="Icon/glyph.icns"
PKG_NAME="$OUTPUT_DIR/ViewPersonalInstaller.pkg"
IDENTIFIER="com.example.viewpersonal"
VERSION="1.0.0"

# 1. Clean previous builds
rm -rf "$PUBLISH_DIR" "$PKG_ROOT" "$PKG_NAME"
mkdir -p "$PUBLISH_DIR/x64" "$PUBLISH_DIR/arm64" "$UNIVERSAL_DIR" "$OUTPUT_DIR"

# 2. Publish Avalonia app for x64 and arm64
dotnet publish ../../src/View.Personal/View.Personal.csproj -c Release --self-contained true -r osx-x64 -o "$PUBLISH_DIR/x64"
dotnet publish ../../src/View.Personal/View.Personal.csproj -c Release --self-contained true -r osx-arm64 -o "$PUBLISH_DIR/arm64"

# 3. Create universal binary
lipo -create \
  -output "$UNIVERSAL_DIR/View.Personal" \
  "$PUBLISH_DIR/x64/View.Personal" \
  "$PUBLISH_DIR/arm64/View.Personal"

# 4. Copy all contents from x64 publish (assuming it's representative)
cp -R "$PUBLISH_DIR/x64/." "$UNIVERSAL_DIR/"

# 5. Replace single-arch binary with universal one
cp "$UNIVERSAL_DIR/View.Personal" "$UNIVERSAL_DIR/View Personal"
chmod +x "$UNIVERSAL_DIR/View Personal"

# 6. Create .app bundle inside pkg-root/Applications/
APP_BUNDLE="$PKG_ROOT/Applications/$APP_BUNDLE_NAME"
APP_CONTENTS="$APP_BUNDLE/Contents"
mkdir -p "$APP_CONTENTS/MacOS" "$APP_CONTENTS/Resources"


# 7. Copy all runtime files into MacOS folder
cp -R "$UNIVERSAL_DIR/." "$APP_CONTENTS/MacOS/"

# 8. Copy icon
cp "$ICON_PATH" "$APP_CONTENTS/Resources/glyph.icns"

# 9. Create Info.plist
cat > "$APP_CONTENTS/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN"
 "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>$APP_NAME</string>
    <key>CFBundleDisplayName</key>
    <string>$APP_NAME</string>
    <key>CFBundleExecutable</key>
    <string>View Personal</string>
    <key>CFBundleIdentifier</key>
    <string>$IDENTIFIER</string>
    <key>CFBundleVersion</key>
    <string>$VERSION</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleIconFile</key>
    <string>glyph.icns</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.13</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>LSApplicationCategoryType</key>
    <string>public.app-category.productivity</string>
</dict>
</plist>
EOF

# 10. Build the .pkg installer
pkgbuild \
  --root "$PKG_ROOT/Applications" \
  --identifier "$IDENTIFIER" \
  --version "$VERSION" \
  --install-location "/Applications" \
  "$PKG_NAME"



echo -e "\n✅ Installer created at: $PKG_NAME"
