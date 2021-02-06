if not defined DevEnvDir (
	@call "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Auxiliary\Build\vcvars32.bat"
)	

:: Not building those since visual studio builds arent deterministic (they contain timestamps). 
REM msbuild ..\ThirdParty\NotSoFatso\NotSoFatso.vcxproj /t:Rebuild /p:Configuration=Release /p:Platform="Win32" /p:SolutionDir="%~dp0../" /verbosity:quiet
REM msbuild ..\ThirdParty\NesSndEmu\SndEmu.vcxproj /t:Rebuild /p:Configuration=Release /p:Platform="Win32" /p:SolutionDir="%~dp0../" /verbosity:quiet
REM msbuild ..\ThirdParty\ShineMp3\ShineMp3.vcxproj /t:Rebuild /p:Configuration=Release /p:Platform="Win32" /p:SolutionDir="%~dp0../" /verbosity:quiet

:: Build main app.
msbuild ..\FamiStudio\FamiStudio.csproj /t:Rebuild /p:Configuration=Release /p:Platform="AnyCPU" /p:SolutionDir="%~dp0../" /verbosity:quiet

:: This is needed to build setup project, well know visual studio issue.
cd "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\IDE\CommonExtensions\Microsoft\VSI\DisableOutOfProcBuild"
@call DisableOutOfProcBuild.exe

:: Build setup.
cd "%~dp0"
devenv ..\FamiStudio.sln /Project Setup /rebuild Release

cd "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\IDE\CommonExtensions\Microsoft\VSI\DisableOutOfProcBuild"
@call DisableOutOfProcBuild.exe undo

:: Zip and create 3 versions (zipped installer, installer, portable exe)
cd "%~dp0"

set /p Version=<Version.txt

cd Release
tar -a -c -f ..\FamiStudio%Version%-WinInstaller.zip Setup.msi
copy /y Setup.msi ..\FamiStudio%Version%-WinInstaller.msi
cd ..

tar -a -c -f FamiStudio%Version%-WinPortableExe.zip "Demo Songs\*.*" -C ..\FamiStudio\bin\Release\ *.exe *.dll *.config
