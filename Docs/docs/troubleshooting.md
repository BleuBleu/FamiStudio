# Troubleshooting

This section will contain solutions to problems that have been encountered by FamiStudio users. This section is still work in progress.

## Keyboard issues on MacOS

If the keyboards does not seems to work on MacOS, you may have to grant a few more permissions to the app.

In the **Security & Privacy** settings, make sure **Input Monitoring** is allowed for both **sh** and **Terminal**. 

![](images/InputMacOs.png#center)

## Audio issues on Linux

If the app outputs no audio and/or freezes when pressing New or opening a file on Linux, this procedure may be helpful. 

While the Linux version of FamiStudio comes with a pre-compiled version of OpenAL Soft, it is not garanteed that it will work correctly on your system. You can force FamiStudio to use the existing one you already have.

1. Make sure you have OpenAL Soft installed on your system and you know where the dynamic library is. Will typically be called something like "libopenal.so.1".
2. In the FamiStudio folder, delete (or rename) libopenal32.so
3. Download <a href='https://famistudio.org/troubleshooting/OpenTK.dll.config'>this file</a> and put it in the FamiStudio folder.
4. Edit this file so that it points to your open AL soft version you have on your machine already (i assumed it would be "libopenal.so.1" but might be slightly different).
5. Run FamiStudio!
