#!/bin/sh

# Make the whole script fail if any of its commands fail 
set -e
# Print executed commands
set -x

# IMPORTANT : This does not update info.plist! Still needs to be done manually.

# Build
dotnet build -c:Debug -p:ExtraDefineConstants=WAIT_FOR_DEBUGGER ../FamiStudio/FamiStudio.Mac.csproj

# Copy binaries to package.
cp ../FamiStudio/bin/Debug/net6.0/*.dll ./FamiStudio.app/Contents/MacOS/
cp ../FamiStudio/bin/Debug/net6.0/*.json ./FamiStudio.app/Contents/MacOS/
cp ../FamiStudio/bin/Debug/net6.0/*.dylib ./FamiStudio.app/Contents/MacOS/

open FamiStudio.app
