#!/bin/sh

# Make the whole script fail if any of its commands fail 
set -e
# Print executed commands
set -x

# IMPORTANT : This does not update info.plist! Still needs to be done manually.

# Build
dotnet build -c:Debug -p:ExtraDefineConstants=WAIT_FOR_DEBUGGER ../FamiStudio/FamiStudio.Mac.csproj

# Copy binaries to package.
cp ../FamiStudio/bin/Debug/net8.0/*.dll ./FamiStudio.app/Contents/MacOS/
cp ../FamiStudio/bin/Debug/net8.0/*.json ./FamiStudio.app/Contents/MacOS/
cp ../FamiStudio/bin/Debug/net8.0/*.dylib ./FamiStudio.app/Contents/MacOS/

# Localization
rm -f ./FamiStudio.app/Contents/MacOS/Localization/*.ini
cp ../FamiStudio/Localization/*.ini ./FamiStudio.app/Contents/MacOS/Localization/

open FamiStudio.app
