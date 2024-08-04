using Android.Hardware.Camera2.Params;
using Android.Opengl;
using System;

namespace FamiStudio
{
    public class VideoPreviewDialog : TopBarDialog, IVideoEncoder, ILogOutput
    {
        private int margin = DpiScaling.ScaleForWindow(4);

        private LocalizedString VideoPreviewLabel;
        private LocalizedString VideoPreviewTooltip;

        private Label label;
        private ImageBox image;

        private VideoEncoderAndroidEglBase encoder;
        private Texture texture; // Owned by GL context of main app (not offscreen video encoder).

        private int previewResX;
        private int previewResY;
        private volatile bool abort;
        private float targetFrameTime;
        private double lastFrameTime = -1;
        private volatile byte[] lastFrameBitmap;

        public bool AbortOperation => abort;

        public VideoPreviewDialog(FamiStudioWindow win, int resX, int resY, float fps) : base(win)
        {
            Localization.Localize(this);

            // Will be overriden once we start.
            previewResX = resX / 2;
            previewResY = resY / 2; 

            CancelButtonImage = "Back";
            AcceptButtonVisible = false;

            label = new Label(VideoPreviewTooltip);
            label.Multiline = true;
            image = new ImageBox((Texture)null);
            image.StretchImageToFill = true;
            
            titleLabel.Text = VideoPreviewLabel;
            
            AddControl(label);
            AddControl(image);
            
            RepositionControls();
        }

        private void RepositionControls()
        {
            var ratio = previewResX / (float)previewResY;
            var actualWidth = clientRect.Width - margin * 2;

            var imgSizeX = (int)(actualWidth);
            var imgSizeY = (int)(actualWidth / ratio);

            var res = Platform.GetScreenResolution();
            var maxHeight = Math.Min(res.Width, res.Height) - topBarHeight * 5 / 2;

            if (imgSizeY > maxHeight)
            {
                ratio = maxHeight / (float)imgSizeY;
                imgSizeX = (int)Math.Round(imgSizeX * ratio);
                imgSizeY = (int)Math.Round(imgSizeY * ratio);
            }

            label.Move(clientRect.Left + margin, clientRect.Top + margin, actualWidth, 1);
            image.Move(clientRect.Left + margin + (actualWidth - imgSizeX) / 2, label.Bottom + margin, imgSizeX, imgSizeY);
        }

        public override void OnWindowResize(EventArgs e)
        {
            base.OnWindowResize(e);
            RepositionControls();
        }

        public void LogMessage(string msg)
        {
        }

        public void ReportProgress(float progress)
        {
        }

        // Called from the encoding thread, do not interact with the app directly.
        public bool BeginEncoding(int resX, int resY, int rateNumer, int rateDenom, int videoBitRate, int audioBitRate, bool stereo, string audioFile, string outputFile)
        {
            encoder = new VideoEncoderAndroidEglBase();

            if (!encoder.ElgInitialize(resX, resY))
            {
                return false;
            }

            lock (this)
            {
                previewResX = resX;
                previewResY = resY;
                targetFrameTime = rateDenom / (float)rateNumer;
                lastFrameTime = Platform.TimeSeconds();
            }

            return true;
        }

        // Called from the encoding thread, do not interact with the app directly.
        public bool AddFrame(OffscreenGraphics graphics)
        {
            encoder.SwapBuffers();

            var buffer = new byte[previewResX * previewResY * 4];
            graphics.GetBitmap(buffer);

            // TODO : There should be an easy way to share textures between the 2 GL contexts, but I 
            // need to look into this more. In the meantime, read back on CPU and update next frame.
            lock (this)
            {
                lastFrameBitmap = buffer;
            }
            
            // Throttle to mimic target FPS.
            var currentFrameTime = Platform.TimeSeconds();
            var delta = currentFrameTime - lastFrameTime;
            if (delta < targetFrameTime)
            {
                System.Threading.Thread.Sleep((int)((targetFrameTime - delta) * 1000));
            }

            lastFrameTime = currentFrameTime;

            return !abort;
        }

        // Called from the encoding thread, do not interact with the app directly.
        public void EndEncoding(bool abort)
        {
        }

        protected override void OnShowDialog()
        {
            base.OnShowDialog();
            SetTickEnabled(true);
            window.AppSuspended += Window_AppSuspended;
        }

        protected override void OnCloseDialog(DialogResult res)
        {
            base.OnCloseDialog(res);
            SetTickEnabled(false);
            window.AppSuspended -= Window_AppSuspended;
            abort = true;
            image.Image = null;
            Utils.DisposeAndNullify(ref texture);
        }

        private void Window_AppSuspended()
        {
            Close(DialogResult.Cancel);
        }

        public override void Tick(float delta)
        {
            MarkDirty(); 
        }

        protected override void OnRender(Graphics g)
        {
            var textureUpdateData = (byte[])null;

            lock (this)
            {
                if (texture == null || texture.Size.Width != previewResX || texture.Size.Height != previewResY)
                {
                    Utils.DisposeAndNullify(ref texture);
                    
                    texture = g.CreateEmptyTexture(previewResX, previewResY, TextureFormat.Rgba, true);
                    image.Image = texture;

                    var black = new byte[texture.Size.Width * texture.Size.Height * 4];
                        
                    for (var i = 3; i < black.Length; i += 4)
                    {
                        black[i] = 0xff;
                    }

                    textureUpdateData = black;
                }

                if (lastFrameBitmap != null && lastFrameBitmap.Length == texture.Size.Width * texture.Size.Height * 4)
                {
                    textureUpdateData = lastFrameBitmap;
                    lastFrameBitmap = null;
                }
            }

            if (textureUpdateData != null)
            {
                g.UpdateTexture(texture, 0, 0, texture.Size.Width, texture.Size.Height, textureUpdateData, TextureFormat.Rgba);
            }

            base.OnRender(g);
        }
    }
}
