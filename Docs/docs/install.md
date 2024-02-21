# Installation

## System requirements

For all 3 desktop platforms, FamiStudio requires the following software/hardware environment:

* Windows 8 (64-bit) or newer
* MacOS 10.15 "Catalina" or newer
* .NET Runtime 7.0
* OpenGL 3.0 support or newer

## Windows

### Installation

On Windows, it is highly recommended to use the installer and run `Setup.exe`. This should take care of installing all dependencies.

If you are having problems running the portable application, be sure to install the .NET 7.0 Runtime.

* [.NET 7.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-7.0.14-windows-x64-installer)

If you are getting a VS2019 C++ Runtime error message, be sure to install the following package.

* [Visual Studio Runtime](https://aka.ms/vs/17/release/vc_redist.x64.exe)

### Warning on First Launch

On first launch, SmartScreen might say "Windows protected your PC".

![](images/SmartScreen1.png#center)

To bypass the warning, simply click "More Info" and then "Run Anyway".
 
![](images/SmartScreen2.png#center)

## MacOS

### Installation

On MacOS, you will need to install the .NET 7.0 Runtime. Here are some direct download links from Microsoft. Pick the architecture that matches your hardware. Installing the correct version for your CPU is important as it will ensure the app runs natively on your Mac.

* [.NET 6.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-7.0.14-macos-x64-installer) for x64 (Intel)
* [.NET 6.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-7.0.14-macos-arm64-installer) for ARM64 (M1/M2)

### Warning on First Launch

GateKeeper can be quite aggressive when first running the application. At first it will look like you simply cannot run it and it will give you the option to throw FamiStudio in the recycling bin.

![](images/GateKeeper1.png#center)

To bypass this warning, open the "Security and Privacy" settings and look for the warning saying that FamiStudio was blocked. 

![](images/GateKeeper2.png#center)

Click "Open Anyway" and then you will have the option to launch it.

![](images/GateKeeper3.png#center)

## Linux

### Installation

The Linux version should work on most x64 ditros. But given the very non-standard nature of the OS, your mileage may vary.

Please install the following dependencies before trying to run the Linux version:

* [.NET 6.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)

### Launching

Then simply launch the application with this command:
```
dotnet FamiStudio.dll
```
If you run a very old version of Linux or if you are running an exotic architecture, you may have missing dependencies. If this is the case, you may have to compile some of the libraries. This is a rather manual process. Please follow the build steps on [GitHub](https://github.com/BleuBleu/FamiStudio). 
