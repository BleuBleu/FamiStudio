#!/bin/sh

# mdtool build -c:Release -t:Clean ../FamiStudio.Linux.sln
# mdtool build -c:Release -t:Build ../FamiStudio.Linux.sln

version=`cat Version.txt`
filename=FamiStudio$version-LinuxAMD64.zip

rm $filename
zip -9 $filename Demo\ Songs/*.* LinuxReadme.txt
cd ../FamiStudio/bin/Release/
zip -u -9 ../../../Setup/$filename *.so *.exe *.dll *.config LICENSE Resources/*.*
cd ../../../Setup/

