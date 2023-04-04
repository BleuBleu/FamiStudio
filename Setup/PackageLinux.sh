#!/bin/sh

# Build
dotnet clean -c:Release ../FamiStudio/FamiStudio.Linux.csproj
dotnet build -c:Release ../FamiStudio/FamiStudio.Linux.csproj

# Compress
version=`cat Version.txt`
filename=FamiStudio$version-LinuxAMD64.zip

rm $filename
zip -9 $filename Demo\ Songs/*.* LinuxReadme.txt
cd ../FamiStudio
zip -u -9 $filename Localization/*.*
cd bin/Release/net6.0
zip -u -9 ../../../../Setup/$filename *.so *.dll *.config *.json LICENSE FamiStudio.pdb
cd ../../../../Setup/

