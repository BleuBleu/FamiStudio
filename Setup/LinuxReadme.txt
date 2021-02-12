These are AMD64 binaries for Linux, so it should work on most 64-bit ditros. 

Installation procedure:

    1) Install Mono (https://www.mono-project.com/download/stable/#download-lin). Usually "mono-core" is sufficient.
    2) Install gtk-sharp2, on Debian-based distros, this is done with: "sudo apt-get install gtk-sharp2". If you installed "mono-complete" in step one, you may already have it.
    3) Run with: "mono FamiStudio.exe"

If you run a very old version of Linux or if you are running an exotic architecture, you may have missing dependencies. If this is the case, you may have to compile some of the libraries. This is a rather manual process. Please follow the build steps on GitHub.

https://github.com/BleuBleu/FamiStudio

A much easier way to install FamiStudio on Linux is through FlatHub. 

https://flathub.org/apps/details/org.famistudio.FamiStudio

