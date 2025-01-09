#!/bin/sh

# Build
dotnet clean -c:Release ../FamiStudio/FamiStudio.Linux.csproj
dotnet build -c:Release ../FamiStudio/FamiStudio.Linux.csproj

# Compress
version=`cat Version.txt`
filename=FamiStudio$version-LinuxAMD64.zip

rm $filename
zip -9 $filename Demo\ Songs/*.* Demo\ Instruments/*.* LinuxReadme.txt
cd ../FamiStudio
zip -u -9 ../Setup/$filename Localization/*.ini
cd bin/Release/net8.0
zip -u -9 ../../../../Setup/$filename *.so *.dll *.config *.json LICENSE FamiStudio.pdb FamiStudio
cd ../../../../Setup/

