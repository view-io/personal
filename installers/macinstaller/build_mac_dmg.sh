#!/bin/bash
set -e
 
APP_NAME="View Personal"
APP_BUNDLE_NAME="View Personal.app"
APP_VERSION="1.1.0"
PUBLISH_DIR="publish-macos"
UNIVERSAL_DIR="$PUBLISH_DIR/universal"
OUTPUT_DIR="Output"
ICON_PATH="Icon/glyph.icns"
ICON_PNG_FOR_DMG_ICON="Icon/glyph.png"
DMG_NAME="ViewPersonalInstaller.dmg"
 
echo "Cleaning..."
rm -rf "$PUBLISH_DIR" "$OUTPUT_DIR"
mkdir -p "$PUBLISH_DIR/x64" "$PUBLISH_DIR/arm64" "$UNIVERSAL_DIR" "$OUTPUT_DIR"
 
echo "Publishing Avalonia app..."
dotnet publish ../../src/View.Personal/View.Personal.csproj -c Release --self-contained true -r osx-x64 -o "$PUBLISH_DIR/x64"
dotnet publish ../../src/View.Personal/View.Personal.csproj -c Release --self-contained true -r osx-arm64 -o "$PUBLISH_DIR/arm64"
 
echo "Creating universal binary..."
lipo -create -output "$UNIVERSAL_DIR/View Personal" \
  "$PUBLISH_DIR/x64/View.Personal" \
  "$PUBLISH_DIR/arm64/View.Personal"
chmod +x "$UNIVERSAL_DIR/View Personal"
 
echo "Creating .app bundle..."
APP_BUNDLE="$OUTPUT_DIR/$APP_BUNDLE_NAME"
APP_CONTENTS="$APP_BUNDLE/Contents"
mkdir -p "$APP_CONTENTS/MacOS" "$APP_CONTENTS/Resources"
cp -R "$PUBLISH_DIR/x64/." "$APP_CONTENTS/MacOS/"
cp "$UNIVERSAL_DIR/View Personal" "$APP_CONTENTS/MacOS/View Personal"
cp "$ICON_PATH" "$APP_CONTENTS/Resources/glyph.icns"
 
cat > "$APP_CONTENTS/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN"
"http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key><string>$APP_NAME</string>
    <key>CFBundleDisplayName</key><string>$APP_NAME</string>
    <key>CFBundleExecutable</key><string>View Personal</string>
    <key>CFBundleIdentifier</key><string>com.view.personal</string>
    <key>CFBundleVersion</key><string>$APP_VERSION</string>
    <key>CFBundlePackageType</key><string>APPL</string>
    <key>CFBundleIconFile</key><string>glyph</string>
    <key>LSMinimumSystemVersion</key><string>10.13</string>
    <key>NSHighResolutionCapable</key><true/>
    <key>LSApplicationCategoryType</key><string>public.app-category.productivity</string>
</dict>
</plist>
EOF
 
echo "Creating .dmg with create-dmg..."
if ! command -v create-dmg &> /dev/null; then
    echo "'create-dmg' is not installed. Run: brew install create-dmg"
    exit 1
fi
 
create-dmg \
  --volname "$APP_NAME Installer" \
  --window-pos 200 120 \
  --window-size 520 320 \
  --icon-size 100 \
  --icon "$APP_BUNDLE_NAME" 200 140 \
  --app-drop-link 400 140 \
  --volicon "$ICON_PATH" \
  "$OUTPUT_DIR/$DMG_NAME" \
  "$OUTPUT_DIR"
 
echo "Embedding custom icon to .dmg file itself..."
ICON_COPY="$OUTPUT_DIR/temp_icon.png"
ICON_RSRC="$OUTPUT_DIR/dmg_icon.rsrc"
 
if [[ ! -f "$ICON_PNG_FOR_DMG_ICON" ]]; then
  echo "PNG icon for .dmg file not found at $ICON_PNG_FOR_DMG_ICON"
  exit 1
fi
 
cp "$ICON_PNG_FOR_DMG_ICON" "$ICON_COPY"
sips -i "$ICON_COPY"
DeRez -only icns "$ICON_COPY" > "$ICON_RSRC"
Rez -append "$ICON_RSRC" -o "$OUTPUT_DIR/$DMG_NAME"
SetFile -a C "$OUTPUT_DIR/$DMG_NAME" || echo "⚠️ 'SetFile' not available. Run: xcode-select --install"
 
# Cleanup
echo "Removing temporary files..."
rm -f "$ICON_COPY" "$ICON_RSRC"
rm -rf "$APP_BUNDLE"
 
echo -e "\nFinal DMG created at: $OUTPUT_DIR/$DMG_NAME (cleaned up temporary files)"