# Installation

Depending on how you download FamiStudio, you might get scary warnings the first time you try to install or run it.

## Windows

On Windows, if you get an error trying to start the application, but sure to install these 2 requirements:

* You will need the [Visual Studio runtime](https://aka.ms/vs/16/release/vc_redist.x86.exe)
* You also may need (especially on Windows 7) to install this rather old and cluncky [DirectX package](https://www.microsoft.com/en-us/download/confirmation.aspx?id=8109)

	1. It is going to ask you to extra the files to somewhere, extract to a temporary folder
	2. Run DXSETUP.exe inside that temporary folder to really install.
	3. Delete the temp folder afterwards to cleanup.

On Windows, SmartScreen might say "Windows protected your PC".

![](images/SmartScreen1.png#center)

To bypass the warning, simply click "More Info" and then "Run Anyway".
 
![](images/SmartScreen2.png#center)

## MacOS

On MacOS, you will need to install [Mono](https://www.mono-project.com/download/stable/#download-mac).

GateKeeper can be quite agressive when first running the application. At first it will look like you simply cannot run it and it will give you the option to throw FamiStudio in the recycling bin.

![](images/GateKeeper1.png#center)

To bypass this warning, open the "Security and Privacy" settings and look for the warning saying that FamiStudio was blocked. 

![](images/GateKeeper2.png#center)

Click "Open Anyway" and then you will have the option to launch it.

![](images/GateKeeper3.png#center)

## Linux

The Linux version should work on most x64 ditros. But given the very non-stardard nature of the OS, your mileage may vary.

Please install the following dependencies before trying ot run the Linux version.

1. Install [Mono](https://www.mono-project.com/download/stable/#download-lin)
2. Install gtk-sharp2, this is usually by typing this, but this might vary depending on your distro :

        sudo apt-get install gtk-sharp2

Then to run provided amd64 (basically any x64 processor), simply run:

    mono FamiStudio.exe

If you run a very old version of Linux or if you are running an exotic architecture, you may have missing dependencies. If this is the case, you may have to compile some of the libraries. This is a rather manual process. Please follow the build steps on [GitHub](https://github.com/BleuBleu/FamiStudio). 
