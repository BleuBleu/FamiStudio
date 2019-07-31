using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.DirectWrite;

namespace FamiStudio
{
    public partial class ResourceFontLoader : CallbackBase, FontCollectionLoader, FontFileLoader
    {
        private readonly List<ResourceFontFileStream> _fontStreams = new List<ResourceFontFileStream>();
        private readonly List<ResourceFontFileEnumerator> _enumerators = new List<ResourceFontFileEnumerator>();
        private readonly DataStream _keyStream;
        private readonly Factory _factory;

        public ResourceFontLoader(Factory factory)
        {
            _factory = factory;
            foreach (var name in typeof(ResourceFontLoader).Assembly.GetManifestResourceNames())
            {
                if (name.EndsWith(".ttf"))
                {
                    var fontBytes = Utilities.ReadStream(typeof(ResourceFontLoader).Assembly.GetManifestResourceStream(name));
                    var stream = new DataStream(fontBytes.Length, true, true);
                    stream.Write(fontBytes, 0, fontBytes.Length);
                    stream.Position = 0;
                    _fontStreams.Add(new ResourceFontFileStream(stream));
                }
            }

            // Build a Key storage that stores the index of the font
            _keyStream = new DataStream(sizeof(int) * _fontStreams.Count, true, true);
            for (int i = 0; i < _fontStreams.Count; i++)
                _keyStream.Write((int)i);
            _keyStream.Position = 0;

            // Register the 
            _factory.RegisterFontFileLoader(this);
            _factory.RegisterFontCollectionLoader(this);
        }

        public DataStream Key
        {
            get
            {
                return _keyStream;
            }
        }

        FontFileEnumerator FontCollectionLoader.CreateEnumeratorFromKey(Factory factory, DataPointer collectionKey)
        {
            var enumerator = new ResourceFontFileEnumerator(factory, this, collectionKey);
            _enumerators.Add(enumerator);

            return enumerator;
        }

        FontFileStream FontFileLoader.CreateStreamFromKey(DataPointer fontFileReferenceKey)
        {
            var index = Utilities.Read<int>(fontFileReferenceKey.Pointer);
            return new ResourceFontFileStream(_fontStreams[index]._stream);
        }

        protected override void Dispose(bool disposing)
        {
            // Dispose native resources 
            var callback = ((ICallbackable)this);
            if (callback.Shadow != null)
            {
                callback.Shadow.Dispose();
                callback.Shadow = null;
            }
        }
    }

    public class ResourceFontFileStream : CallbackBase, FontFileStream
    {
        public readonly DataStream _stream;

        public ResourceFontFileStream(DataStream stream)
        {
            this._stream = stream;
        }

        void FontFileStream.ReadFileFragment(out IntPtr fragmentStart, long fileOffset, long fragmentSize, out IntPtr fragmentContext)
        {
            lock (this)
            {
                fragmentContext = IntPtr.Zero;
                _stream.Position = fileOffset;
                fragmentStart = _stream.PositionPointer;
            }
        }

        protected override void Dispose(bool disposing)
        {
            // Dispose native resources 
            var callback = ((ICallbackable)this);
            if (callback.Shadow != null)
            {
                callback.Shadow.Dispose();
                callback.Shadow = null;
            }
        }

        void FontFileStream.ReleaseFileFragment(IntPtr fragmentContext)
        {
            // Nothing to release. No context are used
        }

        long FontFileStream.GetFileSize()
        {
            return _stream.Length;
        }

        long FontFileStream.GetLastWriteTime()
        {
            return 0;
        }
    }

    public class ResourceFontFileEnumerator : CallbackBase, FontFileEnumerator
    {
        private Factory _factory;
        private FontFileLoader _loader;
        private DataStream keyStream;
        private FontFile _currentFontFile;

        public ResourceFontFileEnumerator(Factory factory, FontFileLoader loader, DataPointer key)
        {
            _factory = factory;
            _loader = loader;
            keyStream = new DataStream(key.Pointer, key.Size, true, false);
        }

        bool FontFileEnumerator.MoveNext()
        {
            bool moveNext = keyStream.RemainingLength != 0;
            if (moveNext)
            {
                if (_currentFontFile != null)
                    _currentFontFile.Dispose();

                _currentFontFile = new FontFile(_factory, keyStream.PositionPointer, 4, _loader);
                keyStream.Position += 4;
            }
            return moveNext;
        }

        FontFile FontFileEnumerator.CurrentFontFile
        {
            get
            {
                ((IUnknown)_currentFontFile).AddReference();
                return _currentFontFile;
            }
        }
    }
}
