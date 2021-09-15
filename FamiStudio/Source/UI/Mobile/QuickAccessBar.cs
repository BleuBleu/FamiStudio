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
        private delegate void ListItemClickDelegate(int idx);
        private delegate bool EnabledDelegate();
        private delegate void EmptyDelegate();

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
            public ListItemClickDelegate ListItemClick;
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

        RenderFont buttonFont;
        RenderFont listFont;
        RenderFont listFontBold;
        RenderBitmapAtlas bmpButtonAtlas;
        Button[] buttons = new Button[(int)ButtonType.Count];

        // These are only use for popup menu.
        private int        popupButtonIdx = -1;
        private int        popupButtonNextIdx = -1;
        private int        popupSelectedIdx = -1;
        private Rectangle  popupRect;
        private float      popupRatio = 0.0f;
        private bool       popupOpening;
        private bool       popupClosing;
        private ListItem[] listItems;

        // Popup-list scrollong.
        private int scrollY = 0;
        private int minScrollY = 0;
        private int maxScrollY = 0;

        // Mouse tracking.
        private int lastX;
        private int lastY;
        private int captureX;
        private int captureY;
        private float flingVelY;
        private CaptureOperation captureOperation = CaptureOperation.None;

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
        public float ExpandRatio => popupRatio;
        public bool  IsExpanded  => popupRatio > 0.001f;

        public override bool WantsFullScreenViewport => true;

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            Debug.Assert((int)ButtonImageIndices.Count == ButtonImageNames.Length);

            bmpButtonAtlas = g.CreateBitmapAtlasFromResources(ButtonImageNames);

            buttons[(int)ButtonType.Sequencer]  = new Button { GetRenderInfo = GetSequencerRenderInfo, Click = OnSequencer, IsNavButton = true };
            buttons[(int)ButtonType.PianoRoll]  = new Button { GetRenderInfo = GetPianoRollRenderInfo, Click = OnPianoRoll, IsNavButton = true };
            buttons[(int)ButtonType.Project]    = new Button { GetRenderInfo = GetProjectExplorerInfo, Click = OnProjectExplorer, IsNavButton = true };
            buttons[(int)ButtonType.Tool]       = new Button { GetRenderInfo = GetToolRenderInfo, Click = OnTool, ListItemClick = OnToolChange };
            buttons[(int)ButtonType.Snap]       = new Button { GetRenderInfo = GetSnapRenderInfo, Click = OnSnap, ListItemClick = OnSnapChange };
            buttons[(int)ButtonType.Channel]    = new Button { GetRenderInfo = GetChannelRenderInfo, Click = OnChannel, ListItemClick = OnChannelChange };
            buttons[(int)ButtonType.Instrument] = new Button { GetRenderInfo = GetInstrumentRenderingInfo, Click = OnInstrument, ListItemClick = OnInstrumentChange };
            buttons[(int)ButtonType.Arpeggio]   = new Button { GetRenderInfo = GetArpeggioRenderInfo, Click = OnArpeggio, ListItemClick = OnArpeggioChange };

            // MATTT : Font scaling?
            var scale = Math.Min(ParentFormSize.Width, ParentFormSize.Height) / 1080.0f;

            buttonFont   = ThemeResources.GetBestMatchingFont(g, ScaleCustom(DefaultTextSize, scale), false);
            listFont     = ThemeResources.GetBestMatchingFont(g, ScaleCustom(DefaultListItemTextSize, scale), false);
            listFontBold = ThemeResources.GetBestMatchingFont(g, ScaleCustom(DefaultListItemTextSize, scale), true);

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

            if (popupButtonIdx >= 0)
                StartExpandingList(popupButtonIdx, listItems);

            base.OnResize(e);
        }

        private void TickFling(float delta)
        {
            if (flingVelY != 0.0f)
            {
                var deltaPixel = (int)Math.Round(flingVelY * delta);
                if (deltaPixel != 0 && DoScroll(-deltaPixel))
                    flingVelY *= (float)Math.Exp(delta * -4.5f);
                else
                    flingVelY = 0.0f;
            }
        }

        public void Tick(float delta)
        {
            TickFling(delta);

            if (popupButtonIdx >= 0)
            {
                if (popupOpening && popupRatio != 1.0f)
                {
                    delta *= 6.0f;
                    popupRatio = Math.Min(popupRatio + delta, 1.0f);
                    if (popupRatio == 1.0f)
                    {
                        popupOpening = false;
                    }
                    MarkDirty();
                }
                else if (popupClosing && popupRatio != 0.0f)
                {
                    delta *= 10.0f;
                    popupRatio = Math.Max(popupRatio - delta, 0.0f);
                    if (popupRatio == 0.0f)
                    {
                        listItems = null;
                        popupButtonIdx = -1;
                        popupClosing = false;

                        if (popupButtonNextIdx >= 0)
                        {
                            var btn = buttons[popupButtonNextIdx];
                            popupButtonNextIdx = -1;
                            btn.Click();
                        }
                    }
                    MarkDirty();
                }
            }
        }

        private Rectangle GetExpandedListRect()
        {
            var rect = popupRect;

            if (IsLandscape)
                rect.X = -(int)Math.Round(rect.Width * Utils.SmootherStep(popupRatio));
            else
                rect.Y = -(int)Math.Round(rect.Height * Utils.SmootherStep(popupRatio));

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

            popupRect.X = 0;
            popupRect.Y = 0;
            popupRect.Width  = 0;
            popupRect.Height = items.Length * listItemSize + 1;

            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var size = textPosTop + listFontBold.MeasureString(item.Text) * 5 / 4;

                popupRect.Width = Math.Max(popupRect.Width, size);
            }

            popupRect.Width  = Math.Min(popupRect.Width,  maxWidth);
            popupRect.Height = Math.Min(popupRect.Height, maxHeight);

            if (landscape)
            {
                if (popupRect.Height != Height)
                {
                    popupRect.Y = (buttons[idx].Rect.Top + buttons[idx].Rect.Bottom) / 2 - popupRect.Height / 2;

                    // DROIDTODO : Test this (with arpeggios)
                    if (popupRect.Top < 0)
                        popupRect.Y -= popupRect.Top;
                    else if (popupRect.Bottom > Height)
                        popupRect.Y -= (popupRect.Bottom - Height);
                }
            }
            else
            {
                if (popupRect.Width != Width)
                {
                    popupRect.X = (buttons[idx].Rect.Left + buttons[idx].Rect.Right) / 2 - popupRect.Width / 2;

                    if (popupRect.Left < 0)
                        popupRect.X -= popupRect.Left;
                    else if (popupRect.Right > Width)
                        popupRect.X -= (popupRect.Right - Width);
                }
            }

            var y = 0;

            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];

                item.Rect = new Rectangle(0, y, popupRect.Width - 1, listItemSize);
                item.IconX = listIconPos;
                item.IconY = y + listIconPos;
                item.TextX = textPosTop;
                item.TextY = y;

                y += listItemSize;
            }

            minScrollY = 0;
            maxScrollY = y - popupRect.Height;
            scrollY = 0;

            popupButtonIdx = idx;
            popupRatio = 0.0f;
            popupOpening = true;
            popupClosing = false;
            listItems = items;
            flingVelY = 0.0f;

            ClampScroll();
        }
        
        private void StartClosingList()
        {
            if (popupButtonIdx >= 0)
            {
                popupOpening = false;
                popupClosing = popupRatio > 0.0f ? true : false;
                flingVelY = 0.0f;
            }
        }

        private void AbortList()
        {
            popupButtonIdx = -1;
            popupButtonNextIdx = -1;
            popupRatio = 0.0f;
            popupOpening = false;
            popupClosing = false;
            listItems = null;
            flingVelY = 0.0f;
        }

        private void OnSequencer()
        {
            StartClosingList();
            App.SetActiveControl(App.Sequencer);
        }

        private void OnPianoRoll()
        {
            StartClosingList();
            App.SetActiveControl(App.PianoRoll);
        }

        private void OnProjectExplorer()
        {
            StartClosingList();
            App.SetActiveControl(App.ProjectExplorer);
        }

        private bool CheckNeedsClosing(int idx)
        {
            if (popupButtonIdx >= 0)
            {
                StartClosingList();
                if (popupButtonIdx != idx)
                {
                    Debug.Assert(popupRatio > 0.0f);
                    popupButtonNextIdx = idx;
                }
                return true;
            }

            return false;
        }

        private void OnTool()
        {
            if (CheckNeedsClosing((int)ButtonType.Tool))
                return;

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

            popupSelectedIdx = 0; // MATTT

            StartExpandingList((int)ButtonType.Tool, items);
        }

        private void OnSnap()
        {
            if (CheckNeedsClosing((int)ButtonType.Snap))
                return;

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

            popupSelectedIdx = 0; // MATTT

            StartExpandingList((int)ButtonType.Snap, items);
        }

        private void OnChannel()
        {
            if (CheckNeedsClosing((int)ButtonType.Channel))
                return;

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

            popupSelectedIdx = App.SelectedChannelIndex;

            StartExpandingList((int)ButtonType.Channel, items);
        }

        private void OnInstrument()
        {
            if (CheckNeedsClosing((int)ButtonType.Instrument))
                return;

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

            popupSelectedIdx = Array.FindIndex(items, i => i.Data == App.SelectedInstrument);

            StartExpandingList((int)ButtonType.Instrument, items);
        }

        private void OnArpeggio()
        {
            if (CheckNeedsClosing((int)ButtonType.Arpeggio))
                return;

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

            popupSelectedIdx = Array.FindIndex(items, i => i.Data == App.SelectedArpeggio);

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

        private void OnToolChange(int idx)
        {

        }

        private void OnSnapChange(int idx)
        {

        }

        private void OnChannelChange(int idx)
        {
            App.SelectedChannelIndex = idx;
        }

        private void OnInstrumentChange(int idx)
        {
            var instrument = listItems[idx].Data as Instrument;
            // App.SelectedInstrument = instrument;
        }

        private void OnArpeggioChange(int idx)
        {
            var arpeggio = listItems[idx].Data as Arpeggio;
            // App.SelectedArpeggio = arpeggio;
        }

        protected override void OnRender(RenderGraphics g)
        {
            var c = g.CreateCommandList();

            c.Transform.GetOrigin(out var ox, out var oy);

            // Background shadow.
            if (IsExpanded)
            {
                var fullscreenRect = new Rectangle(0, 0, ParentFormSize.Width, ParentFormSize.Height);
                fullscreenRect.Offset(-(int)ox, -(int)oy);
                c.FillRectangle(fullscreenRect, g.GetSolidBrush(Color.Black, 1.0f, popupRatio * 0.5f));
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
                var image = btn.GetRenderInfo(out var text, out var tint);

                c.DrawBitmapAtlas(bmpButtonAtlas, (int)image, btn.IconX, btn.IconY, 1.0f, iconScaleFloat, tint);

                if (!string.IsNullOrEmpty(text))
                    c.DrawText(text, buttonFont, btn.TextX, btn.TextY, ThemeResources.LightGreyFillBrush1, RenderTextFlags.Center | RenderTextFlags.Ellipsis, buttonSize, 0);
            }

            // Dividing line.
            if (IsLandscape)
                c.DrawLine(0, 0, 0, Height, ThemeResources.BlackBrush);
            else
                c.DrawLine(0, 0, Width, 0, ThemeResources.BlackBrush);

            g.DrawCommandList(c);

            // List items.
            if (popupButtonIdx >= 0)
            {
                c = g.CreateCommandList();

                var rect = GetExpandedListRect();
                c.PushTranslation(rect.Left, rect.Top - scrollY);

                for (int i = 0; i < listItems.Length; i++)
                {
                    var item = listItems[i];
                    var brush = g.GetVerticalGradientBrush(item.Color, listItemSize, 0.8f);

                    c.FillAndDrawRectangle(item.Rect, brush, ThemeResources.BlackBrush);
                    c.DrawBitmapAtlas(bmpButtonAtlas, item.ImageIndex, item.IconX, item.IconY, 1.0f, iconScaleFloat, Color.Black);
                    c.DrawText(item.Text, i == popupSelectedIdx ? listFontBold : listFont, item.TextX, item.TextY, ThemeResources.BlackBrush, RenderTextFlags.Middle, 0, listItemSize);
                }

                c.PopTransform();

                if (IsLandscape)
                    rect.Width  = -rect.X;
                else
                    rect.Height = -rect.Y;

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

        private bool ClampScroll()
        {
            var scrolled = true;
            if (scrollY < minScrollY) { scrollY = minScrollY; scrolled = false; }
            if (scrollY > maxScrollY) { scrollY = maxScrollY; scrolled = false; }
            return scrolled;
        }

        private bool DoScroll(int deltaY)
        {
            scrollY += deltaY;
            MarkDirty();
            return ClampScroll();
        }

        private void UpdateCaptureOperation(int x, int y)
        {
            if (captureOperation == CaptureOperation.MobilePan)
                DoScroll(lastY - y);
        }

        private void EndCaptureOperation(int x, int y)
        {
            captureOperation = CaptureOperation.None;
        }

        protected override void OnTouchClick(int x, int y, bool isLong)
        {
            foreach (var btn in buttons)
            {
                if (btn.Rect.Contains(x, y))
                {
                    btn.Click();
                    return;
                }
            }

            if (popupRatio > 0.5f)
            {
                var idx = (y - popupRect.Top + scrollY) / listItemSize;

                if (idx >= 0 && idx < listItems.Length)
                {
                    buttons[popupButtonIdx].ListItemClick?.Invoke(idx);
                    popupSelectedIdx = idx;
                }

                StartClosingList();
            }
        }

        protected override void OnTouchUp(int x, int y)
        {
            EndCaptureOperation(x, y);
        }

        protected override void OnTouchDown(int x, int y)
        {
            flingVelY = 0;

            if (popupRatio == 1.0f)
            {
                var rect = GetExpandedListRect();
                if (rect.Contains(x, y))
                    StartCaptureOperation(x, y, CaptureOperation.MobilePan);
            }
        }

        protected override void OnTouchFling(int x, int y, float velX, float velY)
        {
            EndCaptureOperation(x, y);
            flingVelY = velY;
        }

        protected override void OnTouchMove(int x, int y)
        {
            UpdateCaptureOperation(x, y);
            lastX = x;
            lastY = y;
        }
    }
}
