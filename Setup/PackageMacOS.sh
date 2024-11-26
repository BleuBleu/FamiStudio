#!/bin/sh

# Make the whole script fail if any of its commands fail 
set -e
# Print executed commands
set -x

# IMPORTANT : This does not update info.plist! Still needs to be done manually.

# Build
dotnet clean -c:Release ../FamiStudio/FamiStudio.Mac.csproj
dotnet build -c:Release ../FamiStudio/FamiStudio.Mac.csproj

# Copy binaries to package.
cp ../FamiStudio/bin/Release/net8.0/*.dll ./FamiStudio.app/Contents/MacOS/
cp ../FamiStudio/bin/Release/net8.0/*.json ./FamiStudio.app/Contents/MacOS/
cp ../FamiStudio/bin/Release/net8.0/*.dylib ./FamiStudio.app/Contents/MacOS/
cp ../FamiStudio/bin/Release/net8.0/FamiStudio.pdb ./FamiStudio.app/Contents/MacOS/

# Localization
rm -f ./FamiStudio.app/Contents/MacOS/Localization/*.ini
cp ../FamiStudio/Localization/*.ini ./FamiStudio.app/Contents/MacOS/Localization/

version=`cat Version.txt`
filename=FamiStudio$version-MacOS.zip

# Prepare for package creation
[ -f "$filename" ] && rm "$filename"
[ -d tmp ] && rm -r tmp
mkdir tmp

# Create a package
cp -R "Demo Songs" tmp/
cp -R "Demo Instruments" tmp/
cp -R FamiStudio.app tmp/
cd tmp
# Using ditto to preserve permissions and xattr.
ditto -c -k -X --rsrc . ../$filename
cd ..

# Clean up after package creation
rm -r tmp
