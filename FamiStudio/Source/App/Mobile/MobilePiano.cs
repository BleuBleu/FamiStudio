using System;
using System.Diagnostics;

using Color     = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;

namespace FamiStudio
{
    public class MobilePiano : Container
    {
        const int NumOctaves = 8;
        const int NumNotes   = NumOctaves * 12;

        const int DefaultButtonSize    = 135;
        const int DefaultIconPos       = 12;
        const int DefaultWhiteKeySizeX = 120;
        const int DefaultBlackKeySizeX = 96;

        const float MinZoom = 0.5f;
        const float MaxZoom = 2.0f;

        enum CaptureOperation
        {
            None,
            MobilePan,
            MobileZoom,
            PlayPiano
        }

        private int whiteKeySizeX;
        private int blackKeySizeX;
        private int octaveSizeX;
        private int virtualSizeX;

        private readonly Color LightGreyColor1Dark = Theme.Darken(Theme.LightGreyColor1);
        private readonly Color LightGreyColor2Dark = Theme.Darken(Theme.LightGreyColor2);
        private readonly Color DarkGreyColor4Light = Theme.Lighten(Theme.DarkGreyColor4);
        private readonly Color DarkGreyColor5Light = Theme.Lighten(Theme.DarkGreyColor5);

        private TextureAtlasRef bmpMobilePianoDrag;
        private TextureAtlasRef bmpMobilePianoRest;

        private int scrollX = -1;
        private int playAbsNote = -1;
        private int highlightAbsNote = Note.NoteInvalid;
        private int lastX;
        private int lastY;
        private float zoom = 1.0f;
        private float flingVelX;
        private bool canFling = false;
        private CaptureOperation captureOperation = CaptureOperation.None;
        
        public int LayoutSize
        {
            get
            {
                var screenSize = Platform.GetScreenResolution();
                return Math.Min(screenSize.Width, screenSize.Height) * Settings.MobilePianoHeight / 100;
            }
        }

        public MobilePiano()
        {
            SetTickEnabled(true);
        }

        protected override void OnAddedToContainer()
        {
            var g = ParentWindow.Graphics;
            bmpMobilePianoDrag = g.GetTextureAtlasRef("MobilePianoDrag");
            bmpMobilePianoRest = g.GetTextureAtlasRef("MobilePianoRest");
        }
        
        private void UpdateRenderCoords()
        {
            var screenSize = Platform.GetScreenResolution();
            var scale = Math.Min(screenSize.Width, screenSize.Height) / 1080.0f;

            whiteKeySizeX = DpiScaling.ScaleCustom(DefaultWhiteKeySizeX, scale * zoom);
            blackKeySizeX = DpiScaling.ScaleCustom(DefaultBlackKeySizeX, scale * zoom);
            octaveSizeX   = 7 * whiteKeySizeX;
            virtualSizeX  = octaveSizeX * NumOctaves;

            // Center the piano initially.
            if (scrollX < 0)
                scrollX = (virtualSizeX - Width) / 2;
        }

        protected override void OnResize(EventArgs e)
        {
            UpdateRenderCoords();
            ClampScroll();
        }

        private void TickFling(float delta)
        {
            if (flingVelX != 0.0f)
            {
                var deltaPixel = (int)Math.Round(flingVelX * delta);
                if (deltaPixel != 0 && DoScroll(-deltaPixel))
                    flingVelX *= (float)Math.Exp(delta * -4.5f);
                else
                    flingVelX = 0.0f;
            }
        }

        public override void Tick(float delta)
        {
            TickFling(delta);
        }

        public void HighlightPianoNote(int note)
        {
            if (note != highlightAbsNote)
            {
                highlightAbsNote = note;
                MarkDirty();
            }
        }

        private bool IsBlackKey(int key)
        {
            return key == 1 || key == 3 || key == 6 || key == 8 || key == 10;
        }

        private Rectangle GetKeyRectangle(int octave, int key)
        {
            var blackKey = IsBlackKey(key);

            key = key <= 4 ? key / 2 : 3 + (key - 5) / 2;

            if (blackKey)
                return new Rectangle(octaveSizeX * octave + key * whiteKeySizeX + whiteKeySizeX - blackKeySizeX / 2 - scrollX, 0, blackKeySizeX, Height / 2);
            else
                return new Rectangle(octaveSizeX * octave + key * whiteKeySizeX - scrollX, 0, whiteKeySizeX, Height);
        }

        private Rectangle GetPanRectangle(int octave, int idx)
        {
            if (octave == NumOctaves - 1 && idx == 1)
                return Rectangle.Empty;

            var r0 = GetKeyRectangle(octave,       idx == 0 ? 3 : 10);
            var r1 = GetKeyRectangle(octave + idx, idx == 0 ? 6 : 1);

            return new Rectangle(r0.Right, 0, r1.Left - r0.Right, Height / 2);
        }

        private bool GetDPCMKeyColor(int note, out Color color)
        {
            if (App.SelectedChannel.Type == ChannelType.Dpcm && App.SelectedInstrument != null && App.SelectedInstrument.HasAnyMappedSamples)
            {
                if (Settings.DpcmColorMode == Settings.ColorModeSample)
                {
                    var mapping = App.SelectedInstrument.GetDPCMMapping(note);
                    if (mapping != null)
                    {
                        color = mapping.Sample.Color;
                        return true;
                    }
                }
                else
                {
                    color = App.SelectedInstrument.Color;
                    return true;
                }
            }
            color = Color.Empty;
            return false;
        }

        protected void RenderDebug(Graphics g)
        {
#if DEBUG
            if (Platform.IsMobile)
            {
                g.OverlayCommandList.FillRectangle(lastX - 30, lastY - 30, lastX + 30, lastY + 30, Theme.WhiteColor);
            }
#endif
        }

        protected void RenderPiano(Graphics g)
        {
            var minVisibleOctave = Utils.Clamp((int)Math.Floor(scrollX / (float)octaveSizeX), 0, NumOctaves);
            var maxVisibleOctave = Utils.Clamp((int)Math.Ceiling((scrollX + Width) / (float)octaveSizeX), 0, NumOctaves);
            var layoutSize = LayoutSize;

            var b = g.BackgroundCommandList;
            var c = g.DefaultCommandList;

            // Background (white keys)
            b.FillRectangleGradient(0, 0, Width, Height, Theme.LightGreyColor1, Theme.LightGreyColor2, true, layoutSize);

            // Highlighted note.
            var playOctave = Note.IsMusicalNote(highlightAbsNote) ? (highlightAbsNote - 1) / 12 : -1;
            var playNote   = Note.IsMusicalNote(highlightAbsNote) ? (highlightAbsNote - 1) % 12 : -1;
            if (playNote >= 0 && !IsBlackKey(playNote))
                c.FillRectangleGradient(GetKeyRectangle(playOctave, playNote), LightGreyColor1Dark, LightGreyColor2Dark, true, layoutSize);

            // Early pass for DPCM white keys
            for (int i = minVisibleOctave; i < maxVisibleOctave; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    if (!IsBlackKey(j) && GetDPCMKeyColor(i * 12 + j + 1, out var color))
                        c.FillRectangleGradient(GetKeyRectangle(i, j), Theme.Darken(color, 20), color, true, Height);
                }
            }

            // Black keys
            for (int i = minVisibleOctave; i < maxVisibleOctave; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    if (IsBlackKey(j))
                    {
                        var color0 = Theme.DarkGreyColor4;
                        var color1 = Theme.DarkGreyColor5;
                        
                        if (GetDPCMKeyColor(i * 12 + j + 1, out var color))
                        { 
                            color0 = Theme.Darken(color, 40);
                            color1 = Theme.Darken(color, 20);
                        }
                        else if (playOctave == i && playNote == j)
                        {
                            color0 = DarkGreyColor4Light;
                            color1 = DarkGreyColor5Light;
                        }

                        c.FillRectangleGradient(GetKeyRectangle(i, j), color0, color1, true, Height / 2);
                    }
                }
            }

            // Lines between white keys
            for (int i = minVisibleOctave; i < maxVisibleOctave; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    if (!IsBlackKey(j))
                    {
                        var groupStart = j == 0 || j == 5;
                        var x = GetKeyRectangle(i, j).X;
                        var y = groupStart ? 0 : Height / 2;
                        var color = groupStart ? Theme.BlackColor: Theme.DarkGreyColor5;
                        c.DrawLine(x, y, x, Height, color);
                    }
                }
            }

            // Top line
            c.DrawLine(0, 0, Width, 0, Theme.BlackColor);

            // Octave labels
            for (int i = minVisibleOctave; i < maxVisibleOctave; i++)
            {
                var r = GetKeyRectangle(i, 0);
                c.DrawText("C" + i, Fonts.FontSmall, r.X, r.Y, Theme.BlackColor, TextFlags.BottomCenter, r.Width, r.Height - Fonts.FontSmall.Size);
            }

            // Drag images
            for (int i = minVisibleOctave; i < maxVisibleOctave; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    var r = GetPanRectangle(i, j);
                    if (!r.IsEmpty)
                    {
                        var size = bmpMobilePianoDrag.ElementSize;
                        var scale = Math.Min(r.Width, r.Height) / (float)Math.Min(size.Width, size.Height);
                        var posX = r.X + r.Width / 2 - (int)(size.Width * scale / 2);
                        var posY = r.Height / 2 - (int)(size.Height * scale / 2);
                        var bmp = App.IsRecording && j == 1 ? bmpMobilePianoRest : bmpMobilePianoDrag;
                        c.DrawTextureAtlas(bmp, posX, posY, scale, Color.Black.Transparent(0.25f));
                    }
                }
            }
        }

        protected override void OnRender(Graphics g)
        {
            RenderPiano(g); 
            RenderDebug(g);
        }

        private void ZoomAtLocation(int x, float scale)
        {
            if (scale == 1.0f)
                return;

            var absoluteX = x + scrollX;
            var prevNoteSizeX = whiteKeySizeX;

            zoom *= scale;
            zoom = Utils.Clamp(zoom, MinZoom, MaxZoom);

            // This will update the noteSizeX.
            UpdateRenderCoords();

            absoluteX = (int)Math.Round(absoluteX * (whiteKeySizeX / (double)prevNoteSizeX));
            scrollX = absoluteX - x;

            ClampScroll();
            MarkDirty();
        }

        private bool ClampScroll()
        {
            var minScrollX = 0;
            var maxScrollX = Math.Max(virtualSizeX - Width, 0);

            var scrolled = true;
            if (scrollX < minScrollX) { scrollX = minScrollX; scrolled = false; }
            if (scrollX > maxScrollX) { scrollX = maxScrollX; scrolled = false; }
            return scrolled;
        }

        private bool DoScroll(int deltaX)
        {
            scrollX += deltaX;
            MarkDirty();
            return ClampScroll();
        }

        protected int GetPianoNote(int x, int y)
        {
            for (int i = 0; i < NumOctaves; i++)
            {
                for (int j = 0; j < 12 && i * 12 + j < NumNotes; j++)
                {
                    if (IsBlackKey(j) && GetKeyRectangle(i, j).Contains(x, y))
                        return i * 12 + j + 1;
                }
                for (int j = 0; j < 12 && i * 12 + j < NumNotes; j++)
                {
                    if (!IsBlackKey(j) && GetKeyRectangle(i, j).Contains(x, y))
                        return i * 12 + j + 1;
                }
            }

            return -1;
        }

        protected void PlayPiano(int x, int y)
        {
            var note = GetPianoNote(x, Utils.Clamp(y, 0, height - 1));
            if (note >= 0)
            {
                if (note != playAbsNote)
                {
                    playAbsNote = note;
                    App.PlayInstrumentNote(playAbsNote, true, true);
                    Platform.VibrateTick();
                    MarkDirty();
                }
            }
        }

        private void EndPlayPiano()
        {
            App.StopOrReleaseIntrumentNote(false);
            playAbsNote = -1;
        }

        private void StartCaptureOperation(int x, int y, CaptureOperation op)
        {
            lastX = x;
            lastY = y;
            captureOperation = op;
            Capture = true;
            canFling = false;
        }

        private void UpdateCaptureOperation(int x, int y, float scale = 1.0f)
        {
            switch (captureOperation)
            {
                case CaptureOperation.MobilePan:
                    DoScroll(lastX - x);
                    break;
                case CaptureOperation.PlayPiano:
                    PlayPiano(x, y);
                    break;
                case CaptureOperation.MobileZoom:
                    ZoomAtLocation(x, scale);
                    DoScroll(lastX - x);
                    break;
            }
        }

        private void EndCaptureOperation(int x, int y)
        {
            switch (captureOperation)
            {
                case CaptureOperation.PlayPiano:
                    EndPlayPiano();
                    break;
                case CaptureOperation.MobilePan:
                case CaptureOperation.MobileZoom:
                    canFling = true;
                    break;
            }

            Capture = false;
            captureOperation = CaptureOperation.None;
            MarkDirty();
        }

        protected override void OnTouchUp(MouseEventArgs e)
        {
            EndCaptureOperation(e.X, e.Y);
        }

        private bool IsPointInPanRectangle(int x, int y)
        {
            for (int i = 0; i < NumOctaves; i++)
            {
                var maxIdx = App.IsRecording ? 1 : 2;
                for (int j = 0; j < maxIdx; j++)
                {
                    if (GetPanRectangle(i, j).Contains(x, y))
                        return true;
                }
            }

            return false;
        }

        private bool IsPointInRestRectangle(int x, int y)
        {
            if (App.IsRecording)
            {
                for (int i = 0; i < NumOctaves; i++)
                {
                    if (GetPanRectangle(i, 1).Contains(x, y))
                        return true;
                }
            }

            return false;
        }

        protected override void OnTouchDown(MouseEventArgs e)
        {
            var x = e.X;
            var y = e.Y;

            Debug.Assert(captureOperation == CaptureOperation.None);

            flingVelX = 0;
            lastX = x;
            lastY = y;

            if (IsPointInPanRectangle(x, y))
            { 
                StartCaptureOperation(x, y, CaptureOperation.MobilePan);
            }
            else if (IsPointInRestRectangle(x, y))
            { 
                App.AdvanceRecording();
            }
            else
            { 
                StartCaptureOperation(x, y, CaptureOperation.PlayPiano);
                PlayPiano(x, y);
            }
        }

        protected override void OnTouchFling(MouseEventArgs e)
        {
            if (IsPointInPanRectangle(lastX, lastY) && canFling)
            {
                EndCaptureOperation(e.X, e.Y);
                flingVelX = e.FlingVelocityX;
            }
        }

        protected override void OnTouchScaleBegin(MouseEventArgs e)
        {
            lastX = e.X;
            lastY = e.Y;

            if (captureOperation != CaptureOperation.None)
            {
                Debug.Assert(captureOperation != CaptureOperation.MobileZoom);
                EndCaptureOperation(e.X, e.Y);
            }

            StartCaptureOperation(e.X, e.Y, CaptureOperation.MobileZoom);
        }

        protected override void OnTouchScale(MouseEventArgs e)
        {
            UpdateCaptureOperation(e.X, e.Y, e.TouchScale);
            lastX = e.X;
            lastY = e.Y;
        }

        protected override void OnTouchScaleEnd(MouseEventArgs e)
        {
            EndCaptureOperation(e.X, e.Y);
            lastX = e.X;
            lastY = e.Y;
        }

        protected override void OnTouchMove(MouseEventArgs e)
        {
            UpdateCaptureOperation(e.X, e.Y);
            lastX = e.X;
            lastY = e.Y;
        }
    }
}
