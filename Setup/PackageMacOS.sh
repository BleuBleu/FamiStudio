#!/bin/sh

msbuild ../FamiStudio/FamiStudio.Mac.csproj /t:Rebuild /p:Configuration=Release /verbosity:quiet

version=`cat Version.txt`
filename=FamiStudio$version-MacOS.zip

rm $filename
zip -9 -r $filename FamiStudio.app Demo\ Songs

