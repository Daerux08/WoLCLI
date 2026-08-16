#!/usr/bin/env bash
set -e

APP_NAME="wolcli"
APP_DISPLAY_NAME="WolCLI"
APP_VERSION="1.1.0"
ARCH="amd64"

# Project file
PROJECT_PATH="./WoLCLI.csproj"

# Output directories
PUBLISH_DIR="./publish"
DEB_DIR="./Package"
DEBIAN_DIR="$DEB_DIR/DEBIAN"
USR_BIN_DIR="$DEB_DIR/usr/bin"
USR_SHARE_DIR="$DEB_DIR/usr/share/$APP_NAME"
ICON_DIR="$DEB_DIR/usr/share/icons/hicolor/256x256/apps"

echo "==> Cleaning old builds"
rm -rf "$PUBLISH_DIR"
rm -rf "$DEB_DIR"
rm -f *.deb

echo "==> Publishing the .NET application"
dotnet publish "$PROJECT_PATH" -c Release -r linux-x64 --self-contained true -o "$PUBLISH_DIR"

echo "==> Creating Debian package structure"
mkdir -p "$DEBIAN_DIR"
mkdir -p "$USR_BIN_DIR"
mkdir -p "$USR_SHARE_DIR"
mkdir -p "$DEB_DIR/usr/share/applications"
mkdir -p "$ICON_DIR"

echo "==> Copying published files"
cp -r "$PUBLISH_DIR"/* "$USR_SHARE_DIR/"

echo "==> Creating launcher script and command aliases"
cat <<EOF > "$USR_BIN_DIR/$APP_NAME"
#!/bin/bash
exec /usr/share/$APP_NAME/WoLCLI "\$@"
EOF
chmod +x "$USR_BIN_DIR/$APP_NAME"

# Create symlink for the second command
ln -sf "$APP_NAME" "$USR_BIN_DIR/summon-boss"

echo "==> Creating .desktop file"
cat <<EOF > "$DEB_DIR/usr/share/applications/$APP_NAME.desktop"
[Desktop Entry]
Name=$APP_DISPLAY_NAME
GenericName=Wake-on-LAN Tool
Comment=Send Wake-on-LAN magic packets from the command line
Exec=$APP_NAME
Icon=$app_name
Type=Application
Categories=Utility;Network;
Terminal=true
StartupWMClass=$APP_DISPLAY_NAME
Keywords=Network;WakeOnLAN;WoL;CLI;SummonBoss;
EOF

echo "==> Creating control file"
DEPENDS="net-tools"
cat <<EOF > "$DEBIAN_DIR/control"
Package: $APP_NAME
Version: $APP_VERSION
Section: net
Priority: optional
Architecture: $ARCH
Maintainer: Alexander
Depends: $DEPENDS
Description: Wake-on-LAN command line utility
 A lightweight tool to send Wake-on-LAN (WoL) magic packets to network devices.
EOF

echo "==> Creating post-installation scripts"
cat <<EOF > "$DEBIAN_DIR/postinst"
#!/bin/sh
set -e
if [ "\$1" = "configure" ]; then
    update-desktop-database -q /usr/share/applications || true
    gtk-update-icon-cache -q -t -f /usr/share/icons/hicolor || true
fi
EOF

cat <<EOF > "$DEBIAN_DIR/postrm"
#!/bin/sh
set -e
if [ "\$1" = "remove" ] || [ "\$1" = "purge" ]; then
    update-desktop-database -q /usr/share/applications || true
    gtk-update-icon-cache -q -t -f /usr/share/icons/hicolor || true
fi
EOF

chmod 555 "$DEBIAN_DIR/postinst"
chmod 555 "$DEBIAN_DIR/postrm"

echo "==> Finalizing permissions"
# Set standard permissions: 755 for dirs, 644 for files
find "$DEB_DIR" -type d -exec chmod 755 {} +
find "$DEB_DIR" -type f -exec chmod 644 {} +
# Restore execution bits for binaries and launcher scripts
chmod 755 "$USR_BIN_DIR/$APP_NAME"
chmod 755 "$USR_SHARE_DIR/WoLCLI"
chmod 755 "$DEBIAN_DIR/postinst" "$DEBIAN_DIR/postrm"

echo "==> Building .deb package"
OUTPUT_FILE="${APP_NAME}_${APP_VERSION}_${ARCH}.deb"
dpkg-deb --root-owner-group --build "$DEB_DIR" "$OUTPUT_FILE"

echo "==> Done!"
echo "Created package: $OUTPUT_FILE"