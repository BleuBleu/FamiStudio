#!/bin/sh

# Make the whole script fail if any of its commands fail 
set -e
# Print executed commands
set -x

# IMPORTANT : This does not update info.plist! Still needs to be done manually.

# Build
msbuild ../FamiStudio/FamiStudio.Mac.csproj /t:Rebuild /p:Configuration=Release /verbosity:quiet

# Copy binaries to package.
cp ../FamiStudio/bin/Release/*.exe ./FamiStudio.app/Contents/MacOS/
cp ../FamiStudio/bin/Release/*.dylib ./FamiStudio.app/Contents/MacOS/
cp ../FamiStudio/bin/Release/FamiStudio.pdb ./FamiStudio.app/Contents/MacOS/

version=`cat Version.txt`
filename=FamiStudio$version-MacOS.zip

# Prepare for package creation
[ -f "$filename" ] && rm "$filename"
[ -d tmp ] && rm -r tmp
mkdir tmp

# Create a package
cp -R "Demo Songs" tmp/
cp -R FamiStudio.app tmp/
cd tmp
# Using ditto to preserve permissions and xattr.
ditto -c -k -X --rsrc . ../$filename
cd ..

# Clean up after package creation
rm -r tmp
