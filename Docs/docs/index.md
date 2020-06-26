# Welcome

Welcome to the FamiStudio documentation. Please use the __User Guide__ menu above to navigate the the various sections of the documentation or use the __Search__ button to search for a specific topic.

# Tutorials

Here is a short YouTube video tutorial series to get you started.

## Part 1 - Your First Song

The quickest way of getting started with FamiStudio is simply to follow this tutorial video step by step. It will walk you through the creation a simple song, from beginning to end.

<div style="position:relative;margin-left: auto;margin-right: auto;width:80%;height:0;padding-bottom:45%;">
	<iframe style="position:absolute;top:0;left:0;width:100%;height:100%" src="https://www.youtube.com/embed/-5GovfMwF90" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
</div>

## Part 2 - Advanced Edition

This second part will cover some more advanced topics that could not be included in the first tutorial.

<div style="position:relative;margin-left: auto;margin-right: auto;width:80%;height:0;padding-bottom:45%;">
	<iframe style="position:absolute;top:0;left:0;width:100%;height:100%" src="https://www.youtube.com/embed/-KqJe9EWTPQ" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
</div>

## Part 3 - Advanced Tempo

This third part will show you the 2 tempo modes that are supported by FamiStudio.

<div style="position:relative;margin-left: auto;margin-right: auto;width:80%;height:0;padding-bottom:45%;">
	<iframe style="position:absolute;top:0;left:0;width:100%;height:100%" src="https://www.youtube.com/embed/NA-eb7Tt9Gc" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>
</div>

# Installation

Depending on how you download FamiStudio, you might get scary warnings the first time you try to install or run it.

## Windows

On Windows, if you get an error trying to start the application, but sure to install these 2 requirements:

* You will need the [Visual Studio 2015 runtime](https://www.microsoft.com/en-pk/download/details.aspx?id=48145) (x86 version)
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
