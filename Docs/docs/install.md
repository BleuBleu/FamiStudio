# Installation

## System requirements

For all 3 desktop platforms, FamiStudio requires the following software/hardware environment:

* Windows 8 (64-bit) or newer (Windows 7 or 32-bit systems are no longer supported).
* MacOS 10.15 "Catalina" or newer
* .NET Runtime (5.0 on Windows, 6.0 on other platforms)
* OpenGL 3.3 support or newer

## Windows

### Installation

On Windows, it is highly recommended to use the installer and run `Setup.exe`. This should take care of installing all dependencies.

If you are running the portable EXE version, or are getting an error trying to start the application, be sure to install the .NET 5.0 Runtime.

* [.NET 5.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-5.0.17-windows-x64-installer)

If you are getting a VS2019 C++ Runtime error message, be sure to install the following package.

* [Visual Studio Runtime](https://aka.ms/vs/16/release/vc_redist.x86.exe)

If you are getting an XAudio 2 error, this installing old and clunky DirectX redist package. This should not be needed as we no longer support Windows 7, but leaving here just in case.

* [DirectX Redistributables](https://www.microsoft.com/en-us/download/confirmation.aspx?id=8109) 

### Warning on First Launch

On first launch, SmartScreen might say "Windows protected your PC".

![](images/SmartScreen1.png#center)

To bypass the warning, simply click "More Info" and then "Run Anyway".
 
![](images/SmartScreen2.png#center)

## MacOS

### Installation

On MacOS, you will need to install the .NET 6.0 Runtime. Here are some direct download link from Microsoft. Pick the architecture that matches your hardware. Installing the correct version for your CPU is important as it will ensure the app runs natively on your Mac.

* [.NET 6.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-6.0.19-macos-x64-installer) for x64 (Intel)
* [.NET 6.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-6.0.19-macos-arm64-installer) for ARM64 (M1/M2)

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

* [.NET 6.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)

### Launching

Then simply launch the application with this command:
```
dotnet FamiStudio.exe
```
If you run a very old version of Linux or if you are running an exotic architecture, you may have missing dependencies. If this is the case, you may have to compile some of the libraries. This is a rather manual process. Please follow the build steps on [GitHub](https://github.com/BleuBleu/FamiStudio). 
