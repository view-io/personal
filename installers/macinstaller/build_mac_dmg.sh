#!/bin/bash
set -e
 
APP_NAME="View Personal"
APP_BUNDLE_NAME="View Personal.app"
APP_VERSION="1.1.0"
PUBLISH_DIR="publish-macos"
OUTPUT_DIR="Output"
ICON_PATH="Icon/glyph.icns"
ICON_PNG_FOR_DMG_ICON="Icon/glyph.png"
 
echo "Cleaning..."
rm -rf "$PUBLISH_DIR" "$OUTPUT_DIR"
mkdir -p "$PUBLISH_DIR/x64" "$PUBLISH_DIR/arm64" "$OUTPUT_DIR"
 
# -------------------------------------------------------
# BUILD x64
# -------------------------------------------------------
echo "Publishing x64..."
dotnet publish ../../src/View.Personal/View.Personal.csproj -c Release --self-contained true -r osx-x64 -o "$PUBLISH_DIR/x64"
 
STAGING_X64="$OUTPUT_DIR/staging-x64"
mkdir -p "$STAGING_X64"
 
APP_BUNDLE_X64="$STAGING_X64/$APP_BUNDLE_NAME"
APP_CONTENTS_X64="$APP_BUNDLE_X64/Contents"
MACOS_DIR_X64="$APP_CONTENTS_X64/MacOS"
RESOURCES_DIR_X64="$APP_CONTENTS_X64/Resources"
 
mkdir -p "$MACOS_DIR_X64" "$RESOURCES_DIR_X64"
cp -R "$PUBLISH_DIR/x64/." "$MACOS_DIR_X64/"
cp "$ICON_PATH" "$RESOURCES_DIR_X64/glyph.icns"
 
cat > "$APP_CONTENTS_X64/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN"
"http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key><string>$APP_NAME</string>
    <key>CFBundleDisplayName</key><string>$APP_NAME</string>
    <key>CFBundleExecutable</key><string>View.Personal</string>
    <key>CFBundleIdentifier</key><string>com.view.personal</string>
    <key>CFBundleVersion</key><string>$APP_VERSION</string>
    <key>CFBundlePackageType</key><string>APPL</string>
    <key>CFBundleIconFile</key><string>glyph</string>
    <key>LSMinimumSystemVersion</key><string>10.13</string>
</dict>
</plist>
EOF
 
codesign --deep --force --sign - "$APP_BUNDLE_X64"
xattr -dr com.apple.quarantine "$APP_BUNDLE_X64"
codesign --verify --deep --strict --verbose=2 "$APP_BUNDLE_X64"
 
echo "Creating x64 DMG..."
create-dmg \
  --volname "$APP_NAME Installer x64" \
  --window-pos 200 120 \
  --window-size 520 320 \
  --icon-size 100 \
  --icon "$APP_BUNDLE_NAME" 200 140 \
  --app-drop-link 400 140 \
  --volicon "$ICON_PATH" \
  "$OUTPUT_DIR/ViewPersonalInstaller-x64.dmg" \
  "$STAGING_X64"
 
# embed custom disk icon
if [[ -f "$ICON_PNG_FOR_DMG_ICON" ]]; then
    ICON_COPY="$OUTPUT_DIR/temp_icon.png"
    ICON_RSRC="$OUTPUT_DIR/dmg_icon.rsrc"
    cp "$ICON_PNG_FOR_DMG_ICON" "$ICON_COPY"
    sips -i "$ICON_COPY"
    DeRez -only icns "$ICON_COPY" > "$ICON_RSRC"
    Rez -append "$ICON_RSRC" -o "$OUTPUT_DIR/ViewPersonalInstaller-x64.dmg"
    SetFile -a C "$OUTPUT_DIR/ViewPersonalInstaller-x64.dmg" || echo "'SetFile' not available."
    rm -f "$ICON_COPY" "$ICON_RSRC"
fi
 
# -------------------------------------------------------
# BUILD arm64
# -------------------------------------------------------
echo "Publishing arm64..."
dotnet publish ../../src/View.Personal/View.Personal.csproj -c Release --self-contained true -r osx-arm64 -o "$PUBLISH_DIR/arm64"
 
STAGING_ARM64="$OUTPUT_DIR/staging-arm64"
mkdir -p "$STAGING_ARM64"
 
APP_BUNDLE_ARM64="$STAGING_ARM64/$APP_BUNDLE_NAME"
APP_CONTENTS_ARM64="$APP_BUNDLE_ARM64/Contents"
MACOS_DIR_ARM64="$APP_CONTENTS_ARM64/MacOS"
RESOURCES_DIR_ARM64="$APP_CONTENTS_ARM64/Resources"
 
mkdir -p "$MACOS_DIR_ARM64" "$RESOURCES_DIR_ARM64"
cp -R "$PUBLISH_DIR/arm64/." "$MACOS_DIR_ARM64/"
cp "$ICON_PATH" "$RESOURCES_DIR_ARM64/glyph.icns"
 
cat > "$APP_CONTENTS_ARM64/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN"
"http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key><string>$APP_NAME</string>
    <key>CFBundleDisplayName</key><string>$APP_NAME</string>
    <key>CFBundleExecutable</key><string>View.Personal</string>
    <key>CFBundleIdentifier</key><string>com.view.personal</string>
    <key>CFBundleVersion</key><string>$APP_VERSION</string>
    <key>CFBundlePackageType</key><string>APPL</string>
    <key>CFBundleIconFile</key><string>glyph</string>
    <key>LSMinimumSystemVersion</key><string>10.13</string>
</dict>
</plist>
EOF
 
codesign --deep --force --sign - "$APP_BUNDLE_ARM64"
xattr -dr com.apple.quarantine "$APP_BUNDLE_ARM64"
codesign --verify --deep --strict --verbose=2 "$APP_BUNDLE_ARM64"
 
echo "Creating arm64 DMG..."
create-dmg \
  --volname "$APP_NAME Installer arm64" \
  --window-pos 200 120 \
  --window-size 520 320 \
  --icon-size 100 \
  --icon "$APP_BUNDLE_NAME" 200 140 \
  --app-drop-link 400 140 \
  --volicon "$ICON_PATH" \
  "$OUTPUT_DIR/ViewPersonalInstaller-arm64.dmg" \
  "$STAGING_ARM64"
 
# embed custom disk icon
if [[ -f "$ICON_PNG_FOR_DMG_ICON" ]]; then
    ICON_COPY="$OUTPUT_DIR/temp_icon.png"
    ICON_RSRC="$OUTPUT_DIR/dmg_icon.rsrc"
    cp "$ICON_PNG_FOR_DMG_ICON" "$ICON_COPY"
    sips -i "$ICON_COPY"
    DeRez -only icns "$ICON_COPY" > "$ICON_RSRC"
    Rez -append "$ICON_RSRC" -o "$OUTPUT_DIR/ViewPersonalInstaller-arm64.dmg"
    SetFile -a C "$OUTPUT_DIR/ViewPersonalInstaller-arm64.dmg" || echo "'SetFile' not available."
    rm -f "$ICON_COPY" "$ICON_RSRC"
fi
 
# -------------------------------------------------------
# CLEANUP
# -------------------------------------------------------
echo "Cleaning up staging folders..."
rm -rf "$OUTPUT_DIR/staging-x64" "$OUTPUT_DIR/staging-arm64"
 
echo -e "\nFinal DMGs created:\n  $OUTPUT_DIR/ViewPersonalInstaller-x64.dmg\n  $OUTPUT_DIR/ViewPersonalInstaller-arm64.dmg"