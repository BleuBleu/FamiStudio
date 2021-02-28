#!/bin/sh

# IMPORTANT : This does not update info.plist! Still needs to be done manually.

# Build
msbuild ../FamiStudio/FamiStudio.Mac.csproj /t:Rebuild /p:Configuration=Release /verbosity:quiet

# Copy binaries to package.
cp ../FamiStudio/bin/Release/*.exe ./FamiStudio.app/Contents/MacOS/
cp ../FamiStudio/bin/Release/*.dll ./FamiStudio.app/Contents/MacOS/
cp ../FamiStudio/bin/Release/*.dylib ./FamiStudio.app/Contents/MacOS/

version=`cat Version.txt`
filename=FamiStudio$version-MacOS.zip

# Using ditto to preserve permissions and xattr.
rm $filename
rm -r tmp
mkdir tmp
cp -R Demo\ Songs tmp/
cp -R FamiStudio.app tmp/
cd tmp
ditto -c -k -X --rsrc . ../$filename
cd ..
rm -r tmp
