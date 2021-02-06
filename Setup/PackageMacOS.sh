#!/bin/sh

msbuild ../FamiStudio/FamiStudio.Mac.csproj /t:Rebuild /p:Configuration=Release /verbosity:quiet

cp ../FamiStudio/bin/Release/*.exe ./FamiStudio.app/Contents/MacOS/
cp ../FamiStudio/bin/Release/*.dll ./FamiStudio.app/Contents/MacOS/
cp ../FamiStudio/bin/Release/*.dylib ./FamiStudio.app/Contents/MacOS/

version=`cat Version.txt`
filename=FamiStudio$version-MacOS.zip

rm $filename
zip -9 -r $filename FamiStudio.app Demo\ Songs

