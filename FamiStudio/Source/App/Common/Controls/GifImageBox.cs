using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FamiStudio
{
    public class GifImageBox : ImageBox, IDisposable
    {
        private IntPtr gif;
        private int sizeX;
        private int sizeY;
        private byte[] imageData;
        private byte[][] decodeBuffer = new byte[2][];
        private GCHandle gcHandle;

        private Task decodeTask;
        private int decodeFrameIndex;
        private int renderFrameIndex;
        private volatile bool frameReady;
        private Semaphore frameSemaphore;
        private AutoResetEvent quitEvent = new AutoResetEvent(false);

        public GifImageBox() : base((Texture)null)
        {
            SetTickEnabled(true);
        }

        private unsafe void DecodeFrame()
        {
            Gif.AdvanceFrame(gif);
            fixed (byte* p = decodeBuffer[decodeFrameIndex & 1])
                Gif.RenderFrame(gif, new IntPtr(p), sizeX * 3, 3);
            decodeFrameIndex++;
            frameReady = true;
        }

        public override void Tick(float delta)
        {
            if (gif != IntPtr.Zero)
            {
                MarkDirty();
            }
        }

        public void LoadGifInternal(byte[] bytes)
        {
            UnloadGif();

            imageData = bytes;
            gcHandle = GCHandle.Alloc(imageData, GCHandleType.Pinned);
            gif = Gif.Open(gcHandle.AddrOfPinnedObject(), Platform.IsDesktop ? 1 : 0);
            sizeX = Gif.GetWidth(gif);
            sizeY = Gif.GetHeight(gif);
            decodeBuffer[0] = new byte[sizeX * sizeY * 3];
            decodeBuffer[1] = new byte[sizeX * sizeY * 3];
            decodeFrameIndex = 0;
            renderFrameIndex = 0;
            DecodeFrame();
            StartDecodeThread();
            MarkDirty();
        }

        public void LoadGifFromResource(string name)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                byte[] bytes = new byte[stream.Length];
                stream.Read(bytes, 0, (int)stream.Length);
                stream.Close();

                LoadGifInternal(bytes);
            }
        }

        public void LoadGifFromFile(string filename)
        {
            LoadGifInternal(System.IO.File.ReadAllBytes(filename));
        }

        public void UnloadGif()
        {
            if (gif != IntPtr.Zero)
            {
                StopDecodeThread();

                imageData = null;
                decodeBuffer[0] = null;
                decodeBuffer[1] = null;
                Gif.Close(gif);
                gif = IntPtr.Zero;
                gcHandle.Free();
                Utils.DisposeAndNullify(ref bmp);
            }
        }

        private void StartDecodeThread()
        {
            Debug.Assert(decodeTask == null);

            quitEvent.Reset();
            frameSemaphore = new Semaphore(1, 2);
            decodeTask = Task.Factory.StartNew(GifDecodeThread, TaskCreationOptions.LongRunning);
        }

        private void StopDecodeThread()
        {
            quitEvent.Set();
            decodeTask.Wait();
            decodeTask = null;
        }

        private void GifDecodeThread()
        {
            var waitHandles = new WaitHandle[] { quitEvent, frameSemaphore };

            while (true)
            {
                var frameDelay = Gif.GetFrameDelay(gif) / 1000.0;
                var decodeStart = Platform.TimeSeconds();

                if (WaitHandle.WaitAny(waitHandles) == 0)
                {
                    break;
                }

                DecodeFrame();
                
                var decodeTime = (float)(Platform.TimeSeconds() - decodeStart);
                var waitTime = (int)(Math.Max(0, frameDelay - decodeTime) * 1000);

                if (quitEvent.WaitOne(waitTime))
                {
                    break;
                }
            }
        }

        public void Dispose()
        {
            UnloadGif();
            Utils.DisposeAndNullify(ref bmp);
        }

        protected override void OnRender(Graphics g)
        {
            var timeSeconds = Platform.TimeSeconds(); ;

            if (gif != IntPtr.Zero)
            {
                if (bmp == null || bmp.Size.Width != sizeX || bmp.Size.Height != sizeY)
                {
                    Utils.DisposeAndNullify(ref bmp); 
                    bmp = g.CreateEmptyTexture(sizeX, sizeY, TextureFormat.Rgb, DpiScaling.Window > 1.0f);
                }

                if (frameReady)
                {
                    var updateStart = Platform.TimeSeconds();
                    g.UpdateTexture(bmp, 0, 0, sizeX, sizeY, decodeBuffer[renderFrameIndex & 1], TextureFormat.Rgb);
                    var updateTime = (float)(Platform.TimeSeconds() - updateStart);
                    frameReady = false;
                    renderFrameIndex++;
                    frameSemaphore.Release();
                }
            }

            base.OnRender(g);
        }
    }
}
