#!/bin/sh

echo "This launcher MUST be compiled on MacOS Sierra 10.12.6 (fully patched Sierra) to match the AppKit version used by GTK#"

gcc FamiStudio.c -L/Library/Frameworks/Mono.framework/Libraries -lmono-2.0 -liconv -lpthread -I/Library/Frameworks/Mono.framework/Headers/mono-2.0 -D_THREAD_SAFE -O2 -o FamiStudio

cp FamiStudio ../FamiStudio.app/Contents/MacOS/

