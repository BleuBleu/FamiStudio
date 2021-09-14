using System;
using System.Diagnostics;

using Color     = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;

using RenderBitmapAtlas = FamiStudio.GLBitmapAtlas;
using RenderControl     = FamiStudio.GLControl;
using RenderGraphics    = FamiStudio.GLGraphics;
using RenderBrush       = FamiStudio.GLBrush;
using RenderFont        = FamiStudio.GLFont;

namespace FamiStudio
{
    public class QuickAccessBar : RenderControl
    {
        // All of these were calibrated at 1080p and will scale up/down from there.
        const int DefaultNavButtonSize     = 120;
        const int DefaultButtonSize        = 144;
        const int DefaultIconSize          = 96;
        const int DefaultIconPos1          = 12;
        const int DefaultIconPos2          = 24;
        const int DefaultTextSize          = 24; // MATTT : Implement same font size solution everywhere.
        const int DefaultTextPosTop        = 108;
        const int DefaultExpandIconPosTop  = 0;
        const int DefaultExpandIconPosLeft = 56;
        const int DefaultExpandIconSize    = 32;

        const int DefaultListItemTextSize  = 36; // MATTT : Implement same font size solution everywhere.
        const int DefaultListItemSize      = 120;
        const int DefaultListIconPos       = 12;

        private delegate ButtonImageIndices RenderInfoDelegate(out string text, out Color tint);
        private delegate bool EnabledDelegate();

        private class Button
        {
            public Rectangle Rect;
            public int IconX;
            public int IconY;
            public int ExpandIconX;
            public int ExpandIconY;
            public int TextX;
            public int TextY;
            public bool IsNavButton = false;
            public Color Color;
            public RenderInfoDelegate GetRenderInfo;
            public EmptyDelegate Click;
        }

        private class ListItem
        {
            public Rectangle Rect;
            public int IconX;
            public int IconY;
            public int TextX;
            public int TextY;
            public Color Color;
            public int ImageIndex;
            public string Text;
            public object Data;
        }

        enum CaptureOperation
        {
            None,
            MobilePan
        }

        private enum ButtonType
        {
            Sequencer,
            PianoRoll,
            Project,
            Tool,
            Snap,
            Channel,
            Instrument,
            Arpeggio,
            Count
        }

        private enum ButtonImageIndices
        { 
            Sequencer,
            PianoRoll,
            ProjectExplorer,
            ToolAdd,
            ToolDelete,
            ToolSelect,
            MobileSnapOn,
            MobileSnapOff,
            MobileChannelDPCM,
            MobileChannelFM,
            MobileChannelNoise,
            MobileChannelSaw,
            MobileChannelSquare,
            MobileChannelTriangle,
            MobileChannelWaveTable,
            MobileInstrument,
            MobileInstrumentFds,
            MobileInstrumentNamco,
            MobileInstrumentSunsoft,
            MobileInstrumentVRC6,
            MobileInstrumentVRC7,
            MobileArpeggio,
            ExpandUp,
            ExpandDown,
            ExpandLeft,
            ExpandRight,
            Count
        };

        private readonly string[] ButtonImageNames = new string[]
        {
            "Sequencer",
            "PianoRoll",
            "ProjectExplorer",
            "ToolAdd",
            "ToolDelete",
            "ToolSelect",
            "MobileSnapOn",
            "MobileSnapOff",
            "ChannelDPCM",
            "ChannelFM",
            "ChannelNoise",
            "ChannelSaw",
            "ChannelSquare",
            "ChannelTriangle",
            "ChannelWaveTable",
            "Instrument",
            "InstrumentFds",
            "InstrumentNamco",
            "InstrumentSunsoft",
            "InstrumentVRC6",
            "InstrumentVRC7",
            "MobileArpeggio",
            "ExpandUp",
            "ExpandDown",
            "ExpandLeft",
            "ExpandRight",
        };

        public delegate void EmptyDelegate();

        public event EmptyDelegate SequencerClicked;
        public event EmptyDelegate PianoRollClicked;
        public event EmptyDelegate ProjectExplorerClicked;

        RenderFont buttonFont;
        RenderFont listFont;
        RenderBitmapAtlas bmpButtonAtlas;
        Button[] buttons = new Button[(int)ButtonType.Count];

        // These are only use for expandable button.
        private int        expandButtonIdx = -1;
        private int        expandVirtualSizeY = 0;
        private int        expandScrollY = 0;
        private int        expandMinScrollY = 0;
        private int        expandMaxScrollY = 0;
        private float      expandRatio = 0.0f;
        private bool       expanding;
        private Rectangle  expandRect;
        private bool       closing;
        private ListItem[] listItems;

        private int lastX;
        private int lastY;
        private int captureX;
        private int captureY;
        private CaptureOperation captureOperation = CaptureOperation.None;

        //private int maxExpandedSize;

        // Scaled layout variables.
        private int buttonSize;
        private int buttonSizeNav;
        private int buttonIconPos1;
        private int buttonIconPos2;
        private int textPosTop;
        private int expandIconPosTop;
        private int expandIconPosLeft;
        private int listItemSize;
        private int listIconPos;

        private float iconScaleFloat    = 1.0f;
        private float iconScaleExpFloat = 1.0f;

        public int   LayoutSize  => buttonSize;
        //public int   RenderSize  => buttonSize + (int)Math.Round(expandedSize * Utils.SmootherStep(expandRatio));
        public float ExpandRatio => expandRatio;
        public bool  IsExpanded  => expandRatio > 0.001f;

        public override bool WantsFullScreenViewport => true;

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            Debug.Assert((int)ButtonImageIndices.Count == ButtonImageNames.Length);

            bmpButtonAtlas = g.CreateBitmapAtlasFromResources(ButtonImageNames);

            buttons[(int)ButtonType.Sequencer]  = new Button { GetRenderInfo = GetSequencerRenderInfo, Click = OnSequencer, IsNavButton = true };
            buttons[(int)ButtonType.PianoRoll]  = new Button { GetRenderInfo = GetPianoRollRenderInfo, Click = OnPianoRoll, IsNavButton = true };
            buttons[(int)ButtonType.Project]    = new Button { GetRenderInfo = GetProjectExplorerInfo, Click = OnProjectExplorer, IsNavButton = true };
            buttons[(int)ButtonType.Tool]       = new Button { GetRenderInfo = GetToolRenderInfo, Click = OnTool };
            buttons[(int)ButtonType.Snap]       = new Button { GetRenderInfo = GetSnapRenderInfo, Click = OnSnap };
            buttons[(int)ButtonType.Channel]    = new Button { GetRenderInfo = GetChannelRenderInfo, Click = OnChannel };
            buttons[(int)ButtonType.Instrument] = new Button { GetRenderInfo = GetInstrumentRenderingInfo, Click = OnInstrument };
            buttons[(int)ButtonType.Arpeggio]   = new Button { GetRenderInfo = GetArpeggioRenderInfo, Click = OnArpeggio };

            // MATTT : Font scaling?
            var scale = Math.Min(ParentFormSize.Width, ParentFormSize.Height) / 1080.0f;

            buttonFont = ThemeResources.GetBestMatchingFont(g, ScaleCustom(DefaultTextSize, scale), false);
            listFont   = ThemeResources.GetBestMatchingFont(g, ScaleCustom(DefaultListItemTextSize, scale), false);

            buttonSize        = ScaleCustom(DefaultButtonSize, scale);
            buttonSizeNav     = ScaleCustom(DefaultNavButtonSize, scale);
            buttonIconPos1    = ScaleCustom(DefaultIconPos1, scale);
            buttonIconPos2    = ScaleCustom(DefaultIconPos2, scale);
            textPosTop        = ScaleCustom(DefaultTextPosTop, scale);
            expandIconPosTop  = ScaleCustom(DefaultExpandIconPosTop, scale);
            expandIconPosLeft = ScaleCustom(DefaultExpandIconPosLeft, scale);
            listItemSize      = ScaleCustom(DefaultListItemSize, scale);
            listIconPos       = ScaleCustom(DefaultListIconPos, scale);

            iconScaleFloat    = ScaleCustomFloat(DefaultIconSize / (float)bmpButtonAtlas.GetElementSize(0).Width, scale);
            iconScaleExpFloat = ScaleCustomFloat(DefaultExpandIconSize / (float)bmpButtonAtlas.GetElementSize((int)ButtonImageIndices.ExpandUp).Width, scale);
        }

        protected override void OnRenderTerminated()
        {
            Utils.DisposeAndNullify(ref bmpButtonAtlas);
        }

        protected override void OnResize(EventArgs e)
        {
            UpdateButtonLayout();

            if (expandButtonIdx >= 0)
                StartExpandingList(expandButtonIdx, listItems);

            base.OnResize(e);
        }

        public void Tick(float delta)
        {
            delta *= 6.0f;

            if (expandButtonIdx >= 0)
            {
                if (expanding && expandRatio != 1.0f)
                {
                    expandRatio = Math.Min(expandRatio + delta, 1.0f);
                    if (expandRatio == 1.0f)
                    {
                        expanding = false;
                    }
                    MarkDirty();
                }
                else if (closing && expandRatio != 0.0f)
                {
                    expandRatio = Math.Max(expandRatio - delta, 0.0f);
                    if (expandRatio == 0.0f)
                    {
                        listItems = null;
                        expandButtonIdx = -1;
                        closing = false;
                    }
                    MarkDirty();
                }
            }
        }

        private Rectangle GetExpandedListRect()
        {
            var rect = expandRect;

            if (IsLandscape)
                rect.X = -(int)Math.Round(rect.Width * Utils.SmootherStep(expandRatio));
            else
                rect.Y = -(int)Math.Round(rect.Height * Utils.SmootherStep(expandRatio));

            return rect;
        }

        private void UpdateButtonLayout()
        {
            if (!IsRenderInitialized)
                return;

            var landscape = IsLandscape;
            var x = 0;

            for (int i = 0; i < (int)ButtonType.Count; i++)
            {
                var btn = buttons[i];
                var size = btn.IsNavButton ? buttonSizeNav : buttonSize;

                if (landscape)
                {
                    btn.Rect = new Rectangle(0, x, buttonSize, size);
                    btn.IconX = buttonIconPos2;
                    btn.IconY = x + buttonIconPos1;
                    btn.ExpandIconX = 0;
                    btn.ExpandIconY = x + expandIconPosLeft;
                    btn.TextX = 0;
                    btn.TextY = x + textPosTop;
                }
                else
                {
                    btn.Rect = new Rectangle(x, 0, size, buttonSize);
                    btn.IconX = x + buttonIconPos2;
                    btn.IconY = buttonIconPos2;
                    btn.ExpandIconX = x + expandIconPosLeft;
                    btn.ExpandIconY = expandIconPosTop;
                    btn.TextX = x;
                    btn.TextY = textPosTop;
                }

                x += size;
            }
        }

        private void StartExpandingList(int idx, ListItem[] items)
        {
            var landscape = IsLandscape;

            var maxWidth  = landscape ? Math.Min(ParentFormSize.Width, ParentFormSize.Height) - buttonSize : Width;
            var maxHeight = landscape ? Height : listItemSize * 8;

            expandRect.X = 0;
            expandRect.Y = 0;
            expandRect.Width  = 0;
            expandRect.Height = items.Length * listItemSize + 1;

            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var size = textPosTop + listFont.MeasureString(item.Text) * 5 / 4;

                expandRect.Width = Math.Max(expandRect.Width, size);
            }

            expandRect.Width  = Math.Min(expandRect.Width,  maxWidth);
            expandRect.Height = Math.Min(expandRect.Height, maxHeight);

            if (landscape)
            {
                if (expandRect.Height != Height)
                {
                    expandRect.Y = (buttons[idx].Rect.Top + buttons[idx].Rect.Bottom) / 2 - expandRect.Height / 2;

                    // DROIDTODO : Test this (with arpeggios)
                    if (expandRect.Top < 0)
                        expandRect.Y -= expandRect.Top;
                    else if (expandRect.Bottom > Height)
                        expandRect.Y -= (expandRect.Bottom - Height);
                }
            }
            else
            {
                if (expandRect.Width != Width)
                {
                    expandRect.X = (buttons[idx].Rect.Left + buttons[idx].Rect.Right) / 2 - expandRect.Width / 2;

                    if (expandRect.Left < 0)
                        expandRect.X -= expandRect.Left;
                    else if (expandRect.Right > Width)
                        expandRect.X -= (expandRect.Right - Width);
                }
            }

            var y = 0;

            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];

                item.Rect = new Rectangle(0, y, expandRect.Width - 1, listItemSize);
                item.IconX = listIconPos;
                item.IconY = y + listIconPos;
                item.TextX = textPosTop;
                item.TextY = y;

                y += listItemSize;
            }

            // TODO : Scroll.

            //if (landscape)
            //{
            //    if (y <= Height)
            //    {
            //        expandMinScrollY = -(Height - y) / 2;
            //        expandMaxScrollY = expandMinScrollY;
            //    }
            //    else
            //    {
            //        expandMinScrollY = 0;
            //        expandMaxScrollY = y - Height;
            //    }
            //}
            //else
            //{
            //    if (y <= maxHeight)
            //    {
            //        expandMinScrollY = 0;
            //        expandMaxScrollY = 0;
            //    }
            //    else
            //    {
            //        expandMinScrollY = 0;
            //        expandMaxScrollY = y - maxHeight;
            //    }
            //}

            expandMinScrollY = 0;
            expandMaxScrollY = 0;

            expandScrollY = 0;
            expandVirtualSizeY = y;
            expandButtonIdx = idx;
            expandRatio = 0.0f;
            expanding = true;
            closing = false;
            listItems = items;
            
            ClampScroll();
        }
        
        private void StartClosingList(int idx)
        {
            expandButtonIdx = idx;
            expanding = false;
            closing = true;
        }

        private void AbortList()
        {
            expandButtonIdx = -1;
            expandRatio = 0.0f;
            expanding = false;
            closing = false;
            listItems = null;
        }

        private void OnSequencer()
        {
            SequencerClicked?.Invoke();
        }

        private void OnPianoRoll()
        {
            PianoRollClicked?.Invoke();
        }

        private void OnProjectExplorer()
        {
            ProjectExplorerClicked?.Invoke();
        }

        private void OnTool()
        {
            if (expandButtonIdx == (int)ButtonType.Tool)
            {
                StartClosingList(expandButtonIdx);
                return;
            }

            var items = new ListItem[]
            {
                new ListItem(),
                new ListItem(),
                new ListItem()
            };

            items[0].Color = Theme.LightGreyFillColor1;
            items[0].ImageIndex = (int)ButtonImageIndices.ToolAdd;
            items[0].Text = "Edit";
            items[1].Color = Theme.LightGreyFillColor1;
            items[1].ImageIndex = (int)ButtonImageIndices.ToolDelete;
            items[1].Text = "Delete";
            items[2].Color = Theme.LightGreyFillColor1;
            items[2].ImageIndex = (int)ButtonImageIndices.ToolSelect;
            items[2].Text = "Select";

            StartExpandingList((int)ButtonType.Tool, items);
        }

        private void OnSnap()
        {
            if (expandButtonIdx == (int)ButtonType.Snap)
            {
                StartClosingList(expandButtonIdx);
                return;
            }

            int minVal = SnapResolution.GetMinSnapValue(App.Project.UsesFamiTrackerTempo);
            int maxVal = SnapResolution.GetMaxSnapValue();

            var items = new ListItem[maxVal - minVal + 2];

            for (int i = 0; i < items.Length - 1; i++)
            {
                var item = new ListItem();
                item.Color = Theme.LightGreyFillColor1;
                item.ImageIndex = (int)ButtonImageIndices.MobileSnapOn;
                item.Text = $"Snap to {SnapResolution.Names[minVal + i]} notes";
                items[i] = item;
            }

            var turnOffItem = new ListItem();
            turnOffItem.Color = Theme.LightGreyFillColor1;
            turnOffItem.ImageIndex = (int)ButtonImageIndices.MobileSnapOff;
            turnOffItem.Text = $"Snap off";
            items[items.Length - 1] = turnOffItem;

            StartExpandingList((int)ButtonType.Snap, items);
        }

        private void OnChannel()
        {
            if (expandButtonIdx == (int)ButtonType.Channel)
            {
                StartClosingList(expandButtonIdx);
                return;
            }

            var channelTypes = App.Project.GetActiveChannelList();
            var items = new ListItem[channelTypes.Length];

            for (int i = 0; i < channelTypes.Length; i++)
            {
                var item = new ListItem();
                item.Color = Theme.LightGreyFillColor1;
                item.ImageIndex = Array.IndexOf(ButtonImageNames, ChannelType.Icons[i]);
                item.Text = ChannelType.GetNameWithExpansion(i);
                items[i] = item;
            }

            StartExpandingList((int)ButtonType.Channel, items);
        }

        private void OnInstrument()
        {
            if (expandButtonIdx == (int)ButtonType.Instrument)
            {
                StartClosingList(expandButtonIdx);
                return;
            }

            // DROIDTODO : Add "DPCM" (null) instrument.
            var project = App.Project;
            var items = new ListItem[project.Instruments.Count + 1];

            var dpcmItem = new ListItem();
            dpcmItem.Color = Theme.LightGreyFillColor1;
            dpcmItem.ImageIndex = (int)ButtonImageIndices.MobileInstrument;
            dpcmItem.Text = "DPCM";
            items[0] = dpcmItem;

            for (int i = 0; i < project.Instruments.Count; i++)
            {
                var inst = project.Instruments[i];
                var item = new ListItem();
                item.Color = inst.Color;
                item.ImageIndex = Array.IndexOf(ButtonImageNames, ExpansionType.Icons[inst.Expansion]);
                item.Text = inst.Name;
                item.Data = inst;
                items[i + 1] = item;
            }

            StartExpandingList((int)ButtonType.Instrument, items);
        }

        private void OnArpeggio()
        {
            if (expandButtonIdx == (int)ButtonType.Arpeggio)
            {
                StartClosingList(expandButtonIdx);
                return;
            }

            var project = App.Project;
            var items = new ListItem[project.Arpeggios.Count + 1];

            var arpNoneItem = new ListItem();
            arpNoneItem.Color = Theme.LightGreyFillColor1;
            arpNoneItem.ImageIndex = (int)ButtonImageIndices.MobileArpeggio;
            arpNoneItem.Text = "None";
            items[0] = arpNoneItem;

            for (int i = 0; i < project.Arpeggios.Count; i++)
            {
                var arp = project.Arpeggios[i];
                var item = new ListItem();
                item.Color = arp.Color;
                item.ImageIndex = (int)ButtonImageIndices.MobileArpeggio;
                item.Text = arp.Name;
                item.Data = arp;
                items[i + 1] = item;
            }

            StartExpandingList((int)ButtonType.Arpeggio, items);
        }

        private ButtonImageIndices GetSequencerRenderInfo(out string text, out Color tint)
        {
            text = null;
            tint = App.ActiveControl == App.Sequencer ? Theme.LightGreyFillColor1 : Theme.MediumGreyFillColor1;
            return ButtonImageIndices.Sequencer;
        }

        private ButtonImageIndices GetPianoRollRenderInfo(out string text, out Color tint)
        {
            text = null;
            tint = App.ActiveControl == App.PianoRoll ? Theme.LightGreyFillColor1 : Theme.MediumGreyFillColor1;
            return ButtonImageIndices.PianoRoll;
        }

        private ButtonImageIndices GetProjectExplorerInfo(out string text, out Color tint)
        {
            text = null;
            tint = App.ActiveControl == App.ProjectExplorer ? Theme.LightGreyFillColor1 : Theme.MediumGreyFillColor1;
            return ButtonImageIndices.ProjectExplorer;
        }

        private ButtonImageIndices GetToolRenderInfo(out string text, out Color tint)
        {
            text = "Edit"; // DROIDTODO : Tool!
            tint = Theme.LightGreyFillColor1;
            return ButtonImageIndices.ToolAdd;
        }

        private ButtonImageIndices GetSnapRenderInfo(out string text, out Color tint)
        {
            text = "1/2";
            tint = Theme.LightGreyFillColor1;
            return ButtonImageIndices.MobileSnapOn;
        }

        private ButtonImageIndices GetChannelRenderInfo(out string text, out Color tint)
        {
            text = App.SelectedChannel.NameWithExpansion;
            tint = Theme.LightGreyFillColor1;
            return (ButtonImageIndices)Array.IndexOf(ButtonImageNames, ChannelType.Icons[App.SelectedChannelIndex]);
        }

        private ButtonImageIndices GetInstrumentRenderingInfo(out string text, out Color tint)
        {
            var inst = App.SelectedInstrument;
            text = inst != null ? inst.Name  : "DPCM";
            tint = inst != null ? inst.Color : Theme.LightGreyFillColor1;
            return ButtonImageIndices.MobileInstrumentNamco;
        }

        private ButtonImageIndices GetArpeggioRenderInfo(out string text, out Color tint)
        {
            var arp = App.SelectedArpeggio;
            text = arp != null ? arp.Name  : "None";
            tint = arp != null ? arp.Color : Theme.LightGreyFillColor1;
            return ButtonImageIndices.MobileArpeggio;
        }

        protected override void OnRender(RenderGraphics g)
        {
            var c = g.CreateCommandList();

            c.Transform.GetOrigin(out var ox, out var oy);

            if (IsExpanded)
            {
                var fullscreenRect = new Rectangle(0, 0, ParentFormSize.Width, ParentFormSize.Height);
                fullscreenRect.Offset(-(int)ox, -(int)oy);
                c.FillRectangle(fullscreenRect, g.GetSolidBrush(Color.Black, 1.0f, expandRatio * 0.5f));
            }

            // Clear BG.
            var navBgRect = Rectangle.Empty;
            var bgRect = Rectangle.Empty;
            
            for (int i = 0; i < (int)ButtonType.Count; i++)
            {
                var btn = buttons[i];

                if (btn.IsNavButton)
                    navBgRect = navBgRect.IsEmpty ? btn.Rect : Rectangle.Union(navBgRect, btn.Rect);
                else
                    bgRect = bgRect.IsEmpty ? btn.Rect : Rectangle.Union(bgRect, btn.Rect);
            }

            var navBgBrush = IsLandscape ?
                g.GetHorizontalGradientBrush(Theme.DarkGreyLineColor1, buttonSize, 0.8f) :
                g.GetVerticalGradientBrush(Theme.DarkGreyFillColor1, buttonSize, 0.8f);
            var bgBrush = IsLandscape ?
                g.GetHorizontalGradientBrush(Theme.DarkGreyFillColor1, buttonSize, 0.8f) :
                g.GetVerticalGradientBrush(Theme.DarkGreyFillColor1, buttonSize, 0.8f);

            c.FillRectangle(navBgRect, navBgBrush);
            c.FillRectangle(bgRect, bgBrush);

            // Buttons
            for (int i = 0; i < (int)ButtonType.Count; i++)
            {
                var btn = buttons[i];

                var image = btn.GetRenderInfo(out var text, out var tint); // MATTT Return a color here, not brush.

                //var expImage = (int)ButtonImageIndices.ExpandLeft;
                //var expImage = IsLandscape ? (int)ButtonImageIndices.ExpandLeft : (int)ButtonImageIndices.ExpandUp;


                if (!btn.IsNavButton)
                {
                    c.DrawRectangle(btn.Rect, ThemeResources.BlackBrush);
                    //c.DrawBitmapAtlas(bmpButtonAtlas, (int)expImage, btn.ExpandIconX, btn.ExpandIconY, opacity, iconScaleExpFloat);
                }

                c.DrawBitmapAtlas(bmpButtonAtlas, (int)image, btn.IconX, btn.IconY, 1.0f, iconScaleFloat, tint);

                if (!string.IsNullOrEmpty(text))
                    c.DrawText(text, buttonFont, btn.TextX, btn.TextY, ThemeResources.LightGreyFillBrush1, RenderTextFlags.Center | RenderTextFlags.Ellipsis, buttonSize, 0);
            }

            g.DrawCommandList(c);

            // List items.
            if (expandButtonIdx >= 0)
            {
                c = g.CreateCommandList();

                var rect = GetExpandedListRect();
                c.PushTranslation(rect.Left, rect.Top - expandScrollY);

                for (int i = 0; i < listItems.Length; i++)
                {
                    var item = listItems[i];
                    var brush = g.GetVerticalGradientBrush(item.Color, listItemSize, 0.8f);
                    c.FillAndDrawRectangle(item.Rect, brush, ThemeResources.BlackBrush);
                    c.DrawBitmapAtlas(bmpButtonAtlas, item.ImageIndex, item.IconX, item.IconY, 1.0f, iconScaleFloat, Color.Black);
                    c.DrawText(item.Text, listFont, item.TextX, item.TextY, ThemeResources.BlackBrush, RenderTextFlags.Middle, 0, listItemSize);
                }

                c.PopTransform();

                if (IsLandscape)
                    rect.Width  = -rect.X;
                else
                    rect.Height = -rect.Y;

                //c.Transform.GetOrigin(out var ox, out var oy);
                rect.Offset((int)Math.Round(ox), (int)Math.Round(oy));
                g.DrawCommandList(c, rect);
            }
        }

        private void StartCaptureOperation(int x, int y, CaptureOperation op)
        {
            lastX = x;
            lastY = y;
            captureX = x;
            captureY = y;
            captureOperation = op;
        }

        private void DoScroll(int deltaY)
        {
            expandScrollY += deltaY;
            ClampScroll();
            MarkDirty();
        }

        private void ClampScroll()
        {
            expandScrollY = Utils.Clamp(expandScrollY, expandMinScrollY, expandMaxScrollY);
        }

        protected override void OnTouchClick(int x, int y, bool isLong)
        {
            foreach (var btn in buttons)
            {
                if (btn.Rect.Contains(x, y))
                {
                    btn.Click();
                    break;
                }
            }
        }

        private void UpdateCaptureOperation(int x, int y)
        {
            if (captureOperation == CaptureOperation.MobilePan)
                DoScroll(lastY - y);
        }

        protected override void OnTouchUp(int x, int y)
        {
            UpdateCaptureOperation(x, y);
            captureOperation = CaptureOperation.None;
        }

        protected override void OnTouchDown(int x, int y)
        {
            if (expandRatio == 1.0f)
            {
                var rect = GetExpandedListRect();
                if (rect.Contains(x, y))
                    StartCaptureOperation(x, y, CaptureOperation.MobilePan);
            }
        }

        protected override void OnTouchMove(int x, int y)
        {
            UpdateCaptureOperation(x, y);
            lastX = x;
            lastY = y;
        }
    }
}
