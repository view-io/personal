#!/bin/bash
set -e

APP_NAME="View Personal"
APP_BUNDLE_NAME="View Personal.app"
APP_VERSION=$(xmllint --xpath "string(//Version)" ../../src/View.Personal/View.Personal.csproj)
PUBLISH_DIR="publish-macos"
OUTPUT_DIR="Output"
ICON_PATH="Icon/glyph.icns"
ICON_PNG_FOR_DMG_ICON="Icon/glyph.png"
UPDATER_DIR="../../src/ViewPersonal.Updater"
UPDATER_OUTPUT_DIR="publish-updater"

echo "Cleaning..."
rm -rf "$PUBLISH_DIR" "$OUTPUT_DIR"
mkdir -p "$PUBLISH_DIR/x64" "$PUBLISH_DIR/arm64" "$OUTPUT_DIR"

build_app_bundle () {
    ARCH=$1
    RUNTIME=$2
    echo "Publishing $ARCH..."
    dotnet publish ../../src/View.Personal/View.Personal.csproj -c Release --self-contained true -r $RUNTIME -o "$PUBLISH_DIR/$ARCH"

    echo "Publishing updater ($ARCH)..."
    dotnet publish "$UPDATER_DIR/ViewPersonal.Updater.csproj" -c Release -r $RUNTIME --self-contained true -o "$UPDATER_DIR/$UPDATER_OUTPUT_DIR/$ARCH"

    STAGING_DIR="$OUTPUT_DIR/staging-$ARCH"
    mkdir -p "$STAGING_DIR"

    APP_BUNDLE="$STAGING_DIR/$APP_BUNDLE_NAME"
    APP_CONTENTS="$APP_BUNDLE/Contents"
    MACOS_DIR="$APP_CONTENTS/MacOS"
    RESOURCES_DIR="$APP_CONTENTS/Resources"
    mkdir -p "$MACOS_DIR/Updater" "$RESOURCES_DIR"

    cp -R "$PUBLISH_DIR/$ARCH/." "$MACOS_DIR/"
    cp -R "$UPDATER_DIR/$UPDATER_OUTPUT_DIR/$ARCH/." "$MACOS_DIR/Updater/"
    cp "$ICON_PATH" "$RESOURCES_DIR/glyph.icns"

    cat > "$APP_CONTENTS/Info.plist" <<EOF
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

    codesign --deep --force --sign - "$APP_BUNDLE"
    xattr -dr com.apple.quarantine "$APP_BUNDLE"
    codesign --verify --deep --strict --verbose=2 "$APP_BUNDLE"

    echo "Creating $ARCH DMG..."
    create-dmg \
      --volname "$APP_NAME Installer $ARCH" \
      --window-pos 200 120 \
      --window-size 520 320 \
      --icon-size 100 \
      --icon "$APP_BUNDLE_NAME" 200 140 \
      --app-drop-link 400 140 \
      --volicon "$ICON_PATH" \
      "$OUTPUT_DIR/ViewPersonalInstaller-$ARCH.dmg" \
      "$STAGING_DIR"

    if [[ -f "$ICON_PNG_FOR_DMG_ICON" ]]; then
        ICON_COPY="$OUTPUT_DIR/temp_icon.png"
        ICON_RSRC="$OUTPUT_DIR/dmg_icon.rsrc"
        cp "$ICON_PNG_FOR_DMG_ICON" "$ICON_COPY"
        sips -i "$ICON_COPY"
        DeRez -only icns "$ICON_COPY" > "$ICON_RSRC"
        Rez -append "$ICON_RSRC" -o "$OUTPUT_DIR/ViewPersonalInstaller-$ARCH.dmg"
        SetFile -a C "$OUTPUT_DIR/ViewPersonalInstaller-$ARCH.dmg" || echo "'SetFile' not available."
        rm -f "$ICON_COPY" "$ICON_RSRC"
    fi
}

build_app_bundle "x64" "osx-x64"
build_app_bundle "arm64" "osx-arm64"

echo "Cleaning up staging folders..."
rm -rf "$OUTPUT_DIR/staging-x64" "$OUTPUT_DIR/staging-arm64"

echo -e "\nFinal DMGs created:\n  $OUTPUT_DIR/ViewPersonalInstaller-x64.dmg\n  $OUTPUT_DIR/ViewPersonalInstaller-arm64.dmg"
