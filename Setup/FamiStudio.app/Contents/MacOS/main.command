#!/bin/sh

# Get the bundle's MacOS directory full path
DIR=$(cd "$(dirname "$0")"; pwd)

# Change these values to match your app
DLL_PATH="$DIR/FamiStudio.dll"
PROCESS_NAME=FamiStudio
APPNAME="FamiStudio"

# .NET version check
VERSION_TITLE="Cannot launch $APPNAME"
VERSION_MSG="$APPNAME requires the .NET 8.0 runtime."
DOWNLOAD_URL="https://famistudio.org/doc/install/#macos"

DOTNET_RUNTIMES="$(/usr/local/share/dotnet/dotnet --list-runtimes)"
DOTNET_RUNTIME_GREP="$(echo $DOTNET_RUNTIMES | grep 'Microsoft.NETCore.App 8.0')"

if [ -z "$DOTNET_RUNTIMES" ] || [ -z "$DOTNET_RUNTIME_GREP" ] 
then
    osascript \
    -e "set question to display dialog \"$VERSION_MSG\" with title \"$VERSION_TITLE\" buttons {\"Cancel\", \"Download...\"} default button 2" \
    -e "if button returned of question is equal to \"Download...\" then open location \"$DOWNLOAD_URL\""
    echo "$VERSION_TITLE"
    echo "$VERSION_MSG"
    exit 1
fi

# Command line
DOTNET_EXEC="exec -a \"$PROCESS_NAME\" /usr/local/share/dotnet/dotnet"

# Create log file directory if it doesn't exist
LOG_FILE="$HOME/Library/Application Support/$APPNAME/$APPNAME.log"
mkdir -p "`dirname \"$LOG_FILE\"`"

# Run app using dotnet
$DOTNET_EXEC $DOTNET_OPTIONS "$DLL_PATH" $* 2>&1 1> "$LOG_FILE"

