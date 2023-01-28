#!/bin/sh

#get the bundle's MacOS directory full path
DIR=$(cd "$(dirname "$0")"; pwd)

#change these values to match your app
DLL_PATH="$DIR/FamiStudio.dll"
PROCESS_NAME=FamiStudio
APPNAME="FamiStudio"

#set up environment
#DOTNET_FRAMEWORK_PATH=/Library/Frameworks/Mono.framework/Versions/Current
#export DYLD_FALLBACK_LIBRARY_PATH="$DIR:$DOTNET_FRAMEWORK_PATH/lib:/lib:/usr/lib"
#export PATH="$DOTNET_FRAMEWORK_PATH/bin:$PATH"

#mono version check
REQUIRED_MAJOR=6
REQUIRED_MINOR=0

VERSION_TITLE="Cannot launch $APPNAME"
VERSION_MSG="$APPNAME requires the .NET version $REQUIRED_MAJOR.$REQUIRED_MINOR or later."
DOWNLOAD_URL="https://learn.microsoft.com/en-us/dotnet/core/install/macos"

DOTNET_VERSION="$(dotnet --version | cut -f5 -d\ )"
DOTNET_VERSION_MAJOR="$(echo $DOTNET_VERSION | cut -f1 -d.)"
DOTNET_VERSION_MINOR="$(echo $DOTNET_VERSION | cut -f2 -d.)"
if [ -z "$DOTNET_VERSION" ] \
    || [ $DOTNET_VERSION_MAJOR -lt $REQUIRED_MAJOR ] \
    || [ $DOTNET_VERSION_MAJOR -eq $REQUIRED_MAJOR -a $DOTNET_VERSION_MINOR -lt $REQUIRED_MINOR ]
then
    osascript \
    -e "set question to display dialog \"$VERSION_MSG\" with title \"$VERSION_TITLE\" buttons {\"Cancel\", \"Download...\"} default button 2" \
    -e "if button returned of question is equal to \"Download...\" then open location \"$DOWNLOAD_URL\""
    echo "$VERSION_TITLE"
    echo "$VERSION_MSG"
    exit 1
fi

#Mono command line
DOTNET_EXEC="exec -a \"$PROCESS_NAME\" dotnet"

#create log file directory if it doesn't exist
LOG_FILE="$HOME/Library/Application Support/$APPNAME/$APPNAME.log"
mkdir -p "`dirname \"$LOG_FILE\"`"

#run app using mono
$DOTNET_EXEC $DOTNET_OPTIONS "$DLL_PATH" $* 2>&1 1> "$LOG_FILE"

