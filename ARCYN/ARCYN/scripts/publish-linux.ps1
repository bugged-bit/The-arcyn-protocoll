# Publish ARCYN.Avalonia for Linux (x64) without self-contained
dotnet publish .\\ARCYN.Avalonia\\ARCYN.Avalonia.csproj -c Release -r linux-x64 --self-contained false -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o .\\publish\\linux-x64
if ($LASTEXITCODE -ne 0) { Write-Host "Publish failed"; exit $LASTEXITCODE }

# Prepare AppDir structure for AppImage
$AppDir = "AppDir"
Remove-Item -Recurse -Force $AppDir -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path "$AppDir/usr/bin" -Force | Out-Null
New-Item -ItemType Directory -Path "$AppDir/usr/share/applications" -Force | Out-Null
New-Item -ItemType Directory -Path "$AppDir/usr/share/icons/hicolor/256x256/apps" -Force | Out-Null

# Copy executable
Copy-Item ".\\publish\\linux-x64\\ARCYN.Avalonia" "$AppDir/usr/bin/arcyn" -Force

# Placeholder for icon (replace with real PNG)
# Copy-Item ".\\ARCYN.Avalonia\\arcyn.png" "$AppDir/usr/share/icons/hicolor/256x256/apps/arcyn.png" -Force

# Create .desktop file
@"
[Desktop Entry]
Name=ARCYN
Comment=Workspace launcher
Exec=arcyn
Icon=arcyn
Terminal=false
Type=Application
Categories=Utility;
"@ | Set-Content -Encoding UTF8 "$AppDir/usr/share/applications/arcyn.desktop"

Write-Host "Linux publish assets prepared in $AppDir. Use appimagetool to generate an AppImage."
