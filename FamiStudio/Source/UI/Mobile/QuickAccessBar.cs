using System;
using System.Diagnostics;
using System.Collections.Generic;

using Color     = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;

namespace FamiStudio
{
    public class QuickAccessBar : Container
    {
        // All of these were calibrated at 1080p and will scale up/down from there.
        const int DefaultNavButtonSize    = 120;
        const int DefaultButtonSize       = 144;
        const int DefaultIconSize         = 96;
        const int DefaultIconPos1         = 12;
        const int DefaultIconPos2         = 24;
        const int DefaultTextPosTop       = 108;
        const int DefaultScrollBarSizeX   = 16;

        const int DefaultListItemSize     = 120;
        const int DefaultListIconPos      = 12;

        private delegate TextureAtlasRef RenderInfoDelegate(out string text, out Color tint);
        private delegate void ListItemClickDelegate(int idx);
        private delegate bool EnabledDelegate();
        private delegate void EmptyDelegate();

        private class Button
        {
            public Rectangle Rect;
            public int IconX;
            public int IconY;
            public int TextX;
            public int TextY;
            public bool Visible = false;
            public bool IsNavButton = false;
            public RenderInfoDelegate GetRenderInfo;
            public EmptyDelegate Click;
            public EmptyDelegate LongPress;
            public ListItemClickDelegate ListItemLongPress;
            public ListItemClickDelegate ListItemClick;
        }

        private class ListItem
        {
            public Rectangle Rect;
            public int IconX;
            public int IconY;
            public int ExtraIconX;
            public int ExtraIconY;
            public int TextX;
            public int TextY;
            public Color Color;
            public TextureAtlasRef Image;
            public TextureAtlasRef ExtraImage;
            public Func<ListItem, float> GetImageOpacity;
            public Func<ListItem, float> GetExtraImageOpacity;
            public string Text;
            public Color  TextColor = Theme.BlackColor;
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
            Channel,
            Instrument,
            Envelope,
            Arpeggio,
            Snap,
            Effect,
            DPCMEffect,
            DPCMPlay,
            WaveformEffect,
            Count
        }

        TextureAtlasRef   bmpSequencer;
        TextureAtlasRef   bmpPianoRoll;
        TextureAtlasRef   bmpProjectExplorer;
        TextureAtlasRef   bmpSnapOn;
        TextureAtlasRef   bmpSnapOff;
        TextureAtlasRef   bmpCheckOff;
        TextureAtlasRef   bmpCheckOn;
        TextureAtlasRef   bmpRadio;
        TextureAtlasRef   bmpArpeggio;
        TextureAtlasRef   bmpGhostSmall;
        TextureAtlasRef   bmpPlay;
        TextureAtlasRef   bmpEffectNone;
        TextureAtlasRef   bmpEffectRepeat;
        TextureAtlasRef[] bmpEffects;
        TextureAtlasRef[] bmpEnvelopes;
        TextureAtlasRef[] bmpChannels;
        TextureAtlasRef[] bmpExpansions;

        Font buttonFont;
        Color scrollBarColor = Color.FromArgb(64, Color.Black);
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
        private int listItemSize;
        private int listIconPos;
        private int scrollBarSizeX;

        private float iconScaleFloat = 1.0f;

        public int   LayoutSize  => buttonSize;
        public float ExpandRatio => popupRatio;
        public bool  IsExpanded  => popupRatio > 0.001f;

        public override bool WantsFullScreenViewport => true;

        #region localization

        // Labels
        LocalizedString SnapToBeatLabel;
        LocalizedString SnapToBeatsLabel;
        LocalizedString SnapEnabledLabel;
        LocalizedString SnapEffectValuesLabel;
        LocalizedString NoneLabel;
        LocalizedString RepeatLabel;
        LocalizedString VolumeLabel;
        LocalizedString RepeatEnvelopeLabel;
        LocalizedString VolumeEnvelopeLabel;
        LocalizedString SnapOffLabel;

        // Context menus
        LocalizedString ReplaceSelectionInstContext;
        LocalizedString ReplaceSelectionArpContext;
        LocalizedString ToggleMuteContext;
        LocalizedString ToggleSoloContext;
        LocalizedString ToggleForceDisplayContext;

        #endregion

        public QuickAccessBar()
        {
            Localization.Localize(this);
            SetTickEnabled(true);
        }

        protected override void OnAddedToContainer()
        {
            var g= ParentWindow.Graphics;
            bmpSequencer = g.GetTextureAtlasRef("Sequencer");
            bmpPianoRoll = g.GetTextureAtlasRef("PianoRoll");
            bmpProjectExplorer = g.GetTextureAtlasRef("ProjectExplorer");
            bmpSnapOn = g.GetTextureAtlasRef("MobileSnapOn");
            bmpSnapOff = g.GetTextureAtlasRef("MobileSnapOff");
            bmpCheckOff = g.GetTextureAtlasRef("MobileCheckOff");
            bmpCheckOn = g.GetTextureAtlasRef("MobileCheckOn");
            bmpRadio = g.GetTextureAtlasRef("MobileRadio");
            bmpArpeggio = g.GetTextureAtlasRef("MobileArpeggio");
            bmpGhostSmall = g.GetTextureAtlasRef("GhostSmall");
            bmpPlay = g.GetTextureAtlasRef("Play");
            bmpEffectNone = g.GetTextureAtlasRef("MobileEffectNone");
            bmpEffectRepeat = g.GetTextureAtlasRef("MobileEffectRepeat");
            bmpEffects = g.GetTextureAtlasRefs(EffectType.Icons, "Mobile");
            bmpExpansions = g.GetTextureAtlasRefs(ExpansionType.Icons);
            bmpEnvelopes = g.GetTextureAtlasRefs(EnvelopeType.Icons);
            bmpChannels = g.GetTextureAtlasRefs(ChannelType.Icons);

            buttons[(int)ButtonType.Sequencer]      = new Button { GetRenderInfo = GetSequencerRenderInfo, Click = OnSequencer, IsNavButton = true };
            buttons[(int)ButtonType.PianoRoll]      = new Button { GetRenderInfo = GetPianoRollRenderInfo, Click = OnPianoRoll, IsNavButton = true };
            buttons[(int)ButtonType.Project]        = new Button { GetRenderInfo = GetProjectExplorerInfo, Click = OnProjectExplorer, IsNavButton = true };
            buttons[(int)ButtonType.Channel]        = new Button { GetRenderInfo = GetChannelRenderInfo, Click = OnChannel, ListItemClick = OnChannelItemClick, ListItemLongPress = OnChannelItemLongPress };
            buttons[(int)ButtonType.Instrument]     = new Button { GetRenderInfo = GetInstrumentRenderingInfo, Click = OnInstrument, LongPress = OnInstrumentLongPress, ListItemClick = OnInstrumentItemClick, ListItemLongPress = OnInstrumentItemLongPress };
            buttons[(int)ButtonType.Envelope]       = new Button { GetRenderInfo = GetEnvelopeRenderingInfo, Click = OnEnvelope, ListItemClick = OnEnvelopeItemClick };
            buttons[(int)ButtonType.Arpeggio]       = new Button { GetRenderInfo = GetArpeggioRenderInfo, Click = OnArpeggio, LongPress = OnArpeggioLongPress, ListItemClick = OnArpeggioItemClick, ListItemLongPress = OnArpeggioItemLongPress };
            buttons[(int)ButtonType.Snap]           = new Button { GetRenderInfo = GetSnapRenderInfo, Click = OnSnap, ListItemClick = OnSnapItemClick };
            buttons[(int)ButtonType.Effect]         = new Button { GetRenderInfo = GetEffectRenderInfo, Click = OnEffect, ListItemClick = OnEffectItemClick };
            buttons[(int)ButtonType.DPCMEffect]     = new Button { GetRenderInfo = GetDPCMEffectRenderInfo, Click = OnDPCMEffect, ListItemClick = OnDPCMEffectItemClick };
            buttons[(int)ButtonType.DPCMPlay]       = new Button { GetRenderInfo = GetDPCMPlayRenderInfo, Click = OnDPCMPlay, LongPress = OnDPCMPlayLongPress };
            buttons[(int)ButtonType.WaveformEffect] = new Button { GetRenderInfo = GetWaveformEffectRenderInfo, Click = OnWaveformEffect, ListItemClick = OnWaveformEffectItemClick };

            var screenSize = Platform.GetScreenResolution();
            var scale = Math.Min(screenSize.Width, screenSize.Height) / 1080.0f;

            buttonFont      = scale > 1.2f ? Fonts.FontSmall : Fonts.FontVerySmall;
            buttonSize      = DpiScaling.ScaleCustom(DefaultButtonSize, scale);
            buttonSizeNav   = DpiScaling.ScaleCustom(DefaultNavButtonSize, scale);
            buttonIconPos1  = DpiScaling.ScaleCustom(DefaultIconPos1, scale);
            buttonIconPos2  = DpiScaling.ScaleCustom(DefaultIconPos2, scale);
            textPosTop      = DpiScaling.ScaleCustom(DefaultTextPosTop, scale);
            listItemSize    = DpiScaling.ScaleCustom(DefaultListItemSize, scale);
            listIconPos     = DpiScaling.ScaleCustom(DefaultListIconPos, scale);
            scrollBarSizeX  = DpiScaling.ScaleCustom(DefaultScrollBarSizeX, scale);
            iconScaleFloat  = DpiScaling.ScaleCustomFloat(DefaultIconSize / (float)bmpSnapOn.ElementSize.Width, scale);
        }

        protected override void OnResize(EventArgs e)
        {
            UpdateButtonLayout();

            if (popupButtonIdx >= 0)
                StartExpandingList(popupButtonIdx, listItems);

            base.OnResize(e);
        }

        public override bool HitTest(int winX, int winY)
        {
            // Eat all the input when expanded.
            return IsExpanded || base.HitTest(winX, winY);
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

        private bool SetButtonVisible(ButtonType type, bool vis)
        {
            var btn = buttons[(int)type];

            if (btn.Visible != vis)
            {
                btn.Visible = vis;
                return true;
            }

            return false;
        }

        private void UpdateVisibleButtons()
        {
            if (ParentWindow == null)
                return;

            var needsLayout = false;

            needsLayout |= SetButtonVisible(ButtonType.Sequencer,      true);
            needsLayout |= SetButtonVisible(ButtonType.PianoRoll,      true);
            needsLayout |= SetButtonVisible(ButtonType.Project,        true);
            needsLayout |= SetButtonVisible(ButtonType.Channel,        true);
            needsLayout |= SetButtonVisible(ButtonType.Instrument,     true);
            needsLayout |= SetButtonVisible(ButtonType.Arpeggio,       true);
            needsLayout |= SetButtonVisible(ButtonType.Snap,           App.IsPianoRollActive && App.IsEditingChannel);
            needsLayout |= SetButtonVisible(ButtonType.Effect,         App.IsPianoRollActive && App.IsEditingChannel);
            needsLayout |= SetButtonVisible(ButtonType.DPCMPlay,       App.IsPianoRollActive && App.IsEditingDPCMSample);
            needsLayout |= SetButtonVisible(ButtonType.DPCMEffect,     App.IsPianoRollActive && App.IsEditingDPCMSample);
            needsLayout |= SetButtonVisible(ButtonType.Envelope,       App.IsPianoRollActive && App.IsEditingInstrument);
            needsLayout |= SetButtonVisible(ButtonType.WaveformEffect, App.IsPianoRollActive && App.IsEditingInstrument && Instrument.EnvelopeHasRepeat(App.EditEnvelopeType));

            if (needsLayout)
                UpdateButtonLayout();
        }

        public override void Tick(float delta)
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
            else
            {
                UpdateVisibleButtons();
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
            if (ParentWindow == null)
                return;

            var landscape = IsLandscape;
            var x = 0;

            for (int i = 0; i < (int)ButtonType.Count; i++)
            {
                var btn = buttons[i];
                var size = btn.IsNavButton ? buttonSizeNav : buttonSize;

                if (!btn.Visible)
                    continue;

                if (landscape)
                {
                    btn.Rect = new Rectangle(0, x, buttonSize, size);
                    btn.IconX = buttonIconPos2;
                    btn.IconY = x + buttonIconPos1;
                    btn.TextX = 0;
                    btn.TextY = x + textPosTop;
                }
                else
                {
                    btn.Rect = new Rectangle(x, 0, size, buttonSize);
                    btn.IconX = x + buttonIconPos2;
                    btn.IconY = buttonIconPos1;
                    btn.TextX = x;
                    btn.TextY = textPosTop;
                }

                x += size;
            }
        }

        private void StartExpandingList(int idx, ListItem[] items, int scrollItemIdx = -1)
        {
            var landscape = IsLandscape;

            var maxWidth  = landscape ? Math.Min(ParentWindowSize.Width, ParentWindowSize.Height) - buttonSize : Width;
            var maxHeight = landscape ? Height : listItemSize * 8;

            popupRect.X = 0;
            popupRect.Y = 0;
            popupRect.Width  = 0;
            popupRect.Height = items.Length * listItemSize + 1;

            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var size = textPosTop + Fonts.FontMediumBold.MeasureString(item.Text, false) * 5 / 4;

                if (item.ExtraImage != null)
                    size += DpiScaling.ScaleCustom(item.ExtraImage.ElementSize.Width, iconScaleFloat);

                popupRect.Width = Math.Max(popupRect.Width, size);
            }

            popupRect.Width  = Math.Min(popupRect.Width,  maxWidth);
            popupRect.Height = Math.Min(popupRect.Height, maxHeight);

            if (landscape)
            {
                if (popupRect.Height != Height)
                {
                    popupRect.Y = (buttons[idx].Rect.Top + buttons[idx].Rect.Bottom) / 2 - popupRect.Height / 2;

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

                if (item.ExtraImage != null)
                {
                    var extraIconSize = DpiScaling.ScaleCustom(item.ExtraImage.ElementSize.Width, iconScaleFloat);
                    item.ExtraIconX = popupRect.Width - listIconPos - extraIconSize;
                    item.ExtraIconY = y + (listItemSize - extraIconSize) / 2;
                }

                y += listItemSize;
            }

            minScrollY = 0;
            maxScrollY = y - popupRect.Height;

            popupButtonIdx = idx;
            popupRatio = 0.0f;
            popupOpening = true;
            popupClosing = false;
            listItems = items;
            flingVelY = 0.0f;

            // Try to center selected item.
            if (scrollItemIdx < 0)
                scrollItemIdx = popupSelectedIdx;

            scrollY = (int)((scrollItemIdx + 0.5f) * listItemSize - popupRect.Height * 0.5f);
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

        private void OnSequencer()
        {
            StartClosingList();
            App.SetActiveControl(App.Sequencer);
        }

        private void OnPianoRoll()
        {
            StartClosingList();
            if (!App.IsEditingChannel)
                App.StartEditChannel(App.SelectedChannelIndex);
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

        private void OnSnap()
        {
            if (CheckNeedsClosing((int)ButtonType.Snap))
                return;

            var items = new ListItem[SnapResolutionType.Max + 3];
            var disabledColor = Color.FromArgb(64, Theme.BlackColor);

            for (int i = 0; i < items.Length - 2; i++)
            {
                var item = new ListItem();
                item.Color = Theme.LightGreyColor1;
                item.Image = App.SnapResolution == i ? bmpRadio : null;
                item.Text = (SnapResolutionType.Factors[i] > 1.0 ? SnapToBeatsLabel : SnapToBeatLabel).Format(SnapResolutionType.Names[i]);
                item.TextColor = App.SnapEnabled ? Theme.BlackColor : disabledColor;
                item.GetImageOpacity = (l) => { return l.TextColor.A / 255.0f; };
                items[i] = item;
            }

            var snapEffectsItem = new ListItem();
            snapEffectsItem.Color = Theme.LightGreyColor1;
            snapEffectsItem.Image = App.SnapEffectEnabled ? bmpCheckOn : bmpCheckOff;
            snapEffectsItem.Text = SnapEffectValuesLabel;
            snapEffectsItem.TextColor = App.SnapEnabled ? Theme.BlackColor : disabledColor;
            snapEffectsItem.GetImageOpacity = (l) => { return l.TextColor.A / 255.0f; }; 
            items[items.Length - 2] = snapEffectsItem;

            var snapEnableItem = new ListItem();
            snapEnableItem.Color = Theme.LightGreyColor1;
            snapEnableItem.Image = App.SnapEnabled ? bmpCheckOn : bmpCheckOff;
            snapEnableItem.Text = SnapEnabledLabel;
            items[items.Length - 1] = snapEnableItem;

            popupSelectedIdx = App.SnapResolution;
            StartExpandingList((int)ButtonType.Snap, items, items.Length - 1);
        }

        private void OnEffect()
        {
            if (CheckNeedsClosing((int)ButtonType.Effect))
                return;

            popupSelectedIdx = 0;

            var channel = App.SelectedChannel;
            var selectedEffect = App.SelectedEffect;
            var effectPanelExpanded = App.EffectPanelExpanded;
            var count = 1;

            for (int i = 0;  i < Note.EffectCount; i++)
            {
                if (channel.ShouldDisplayEffect(i))
                    count++;
            }

            var items = new ListItem[count];

            var item = new ListItem();
            item.Color = Theme.LightGreyColor1;
            item.Image = bmpEffectNone;
            item.Text = "None";
            item.Data = -1;
            items[0] = item;

            for (int i = 0, j = 1; i < Note.EffectCount; i++)
            {
                if (channel.ShouldDisplayEffect(i))
                {
                    item = new ListItem();
                    item.Color = Theme.LightGreyColor1;
                    item.Image = bmpEffects[i];
                    item.Text = EffectType.LocalizedNames[i];
                    item.Data = i;
                    items[j] = item;

                    if (effectPanelExpanded && i == selectedEffect)
                        popupSelectedIdx = j;

                    j++;
                }
            }

            StartExpandingList((int)ButtonType.Effect, items);
        }

        private void OnDPCMEffect()
        {
            if (CheckNeedsClosing((int)ButtonType.DPCMEffect))
                return;

            popupSelectedIdx = App.EffectPanelExpanded ? 1 : 0;

            var items = new ListItem[2];

            items[0] = new ListItem();
            items[0].Color = Theme.LightGreyColor1;
            items[0].Image = bmpEffectNone;
            items[0].Text = NoneLabel;

            items[1] = new ListItem();
            items[1].Color = Theme.LightGreyColor1;
            items[1].Image = bmpEffects[Note.EffectVolume];
            items[1].Text = VolumeEnvelopeLabel;

            StartExpandingList((int)ButtonType.DPCMEffect, items);
        }

        private void OnDPCMPlay()
        {
            App.PreviewDPCMSample(App.EditSample, false);
        }

        private void OnDPCMPlayLongPress()
        {
            App.PreviewDPCMSample(App.EditSample, true);
        }

        private void OnWaveformEffect()
        {
            if (CheckNeedsClosing((int)ButtonType.WaveformEffect))
                return;

            popupSelectedIdx = App.EffectPanelExpanded ? 1 : 0;

            var items = new ListItem[2];

            items[0] = new ListItem();
            items[0].Color = Theme.LightGreyColor1;
            items[0].Image = bmpEffectNone;
            items[0].Text = NoneLabel;

            items[1] = new ListItem();
            items[1].Color = Theme.LightGreyColor1;
            items[1].Image = bmpEffectRepeat;
            items[1].Text = RepeatEnvelopeLabel;

            StartExpandingList((int)ButtonType.WaveformEffect, items);
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
                item.Color = Theme.LightGreyColor1;
                item.Image = bmpChannels[channelTypes[i]];
                item.GetImageOpacity = (l) => { return App.IsChannelActive((int)l.Data) ? 1.0f : 0.2f; };
                item.ExtraImage = bmpGhostSmall;
                item.GetExtraImageOpacity = (l) => { return App.IsChannelForceDisplay((int)l.Data) ? 1.0f : 0.2f; };
                item.Text = ChannelType.GetLocalizedNameWithExpansion(channelTypes[i]);
                item.Data = i;
                items[i] = item;
            }

            popupSelectedIdx = App.SelectedChannelIndex;

            StartExpandingList((int)ButtonType.Channel, items);
        }

        private void OnInstrument()
        {
            if (CheckNeedsClosing((int)ButtonType.Instrument))
                return;

            var editingChannel = App.IsEditingChannel;
            var project = App.Project;
            var channel = App.SelectedChannel;
            var items = new List<ListItem>();

            for (int i = 0; i < project.Instruments.Count; i++)
            {
                var inst = project.Instruments[i];

                if (!editingChannel || channel.SupportsInstrument(inst))
                {
                    var item = new ListItem();
                    item.Color = inst.Color;
                    item.Image = bmpExpansions[inst.Expansion];
                    item.Text = inst.Name;
                    item.Data = inst;
                    items.Add(item);
                }
            }

            if (items.Count == 0)
                return;

            popupSelectedIdx = items.FindIndex(i => i.Data == App.SelectedInstrument);

            StartExpandingList((int)ButtonType.Instrument, items.ToArray());
        }

        public void OnInstrumentLongPress()
        {
            if (App.IsEditingChannel && App.PianoRollHasSelection && App.SelectedChannel.SupportsInstrument(App.SelectedInstrument))
            {
                App.ShowContextMenu(new[]
                {
                    new ContextMenuOption("MenuReplaceSelection", ReplaceSelectionInstContext, () => { App.ReplacePianoRollSelectionInstrument(App.SelectedInstrument); MarkDirty(); })
                });
            }
        }

        private void OnEnvelope()
        {
            if (CheckNeedsClosing((int)ButtonType.Envelope))
                return;

            var inst = App.SelectedInstrument;

            if (inst == null)
                return;

            popupSelectedIdx = -1;

            var items = new ListItem[inst.NumVisibleEnvelopes];

            for (int i = 0, j = 0; i < EnvelopeType.Count; i++)
            {
                var env = inst.Envelopes[i];
                
                if (env != null && inst.IsEnvelopeVisible(i))
                {
                    var item = new ListItem();
                    item.Color = Theme.LightGreyColor1;
                    item.Image = bmpEnvelopes[i];
                    item.GetImageOpacity = (l) => { return env.IsEmpty(i) ? 0.2f : 1.0f; };
                    item.Text = EnvelopeType.LocalizedNames[i];
                    item.Data = i;
                    items[j] = item;

                    if (i == App.EditEnvelopeType)
                        popupSelectedIdx = j;

                    j++;
                }
            }

            StartExpandingList((int)ButtonType.Envelope, items);
        }

        private void OnArpeggio()
        {
            if (CheckNeedsClosing((int)ButtonType.Arpeggio))
                return;

            var project = App.Project;
            var items = new List<ListItem>();

            if (!App.IsEditingArpeggio)
            {
                var arpNoneItem = new ListItem();
                arpNoneItem.Color = Theme.LightGreyColor1;
                arpNoneItem.Image = bmpArpeggio;
                arpNoneItem.Text = NoneLabel;
                items.Add(arpNoneItem);
            }

            for (int i = 0; i < project.Arpeggios.Count; i++)
            {
                var arp = project.Arpeggios[i];
                var item = new ListItem();
                item.Color = arp.Color;
                item.Image = bmpArpeggio;
                item.Text = arp.Name;
                item.Data = arp;
                items.Add(item);
            }

            popupSelectedIdx = items.FindIndex(i => i.Data == App.SelectedArpeggio);

            StartExpandingList((int)ButtonType.Arpeggio, items.ToArray());
        }

        private void OnArpeggioLongPress()
        {
            if (App.IsEditingChannel && App.PianoRollHasSelection && App.SelectedChannel.SupportsArpeggios)
            {
                App.ShowContextMenu(new[]
                {
                    new ContextMenuOption("MenuReplaceSelection", ReplaceSelectionArpContext, () => { App.ReplacePianoRollSelectionArpeggio(App.SelectedArpeggio); MarkDirty(); }) 
                });
            }
        }

        private TextureAtlasRef GetSequencerRenderInfo(out string text, out Color tint)
        {
            text = null;
            tint = App.ActiveControl == App.Sequencer ? Theme.LightGreyColor1 : Theme.MediumGreyColor1;
            return bmpSequencer;
        }

        private TextureAtlasRef GetPianoRollRenderInfo(out string text, out Color tint)
        {
            text = null;
            tint = App.ActiveControl == App.PianoRoll && App.IsEditingChannel ? Theme.LightGreyColor1 : Theme.MediumGreyColor1;
            return bmpPianoRoll;
        }

        private TextureAtlasRef GetProjectExplorerInfo(out string text, out Color tint)
        {
            text = null;
            tint = App.ActiveControl == App.ProjectExplorer || App.ActiveControl == App.PianoRoll && !App.IsEditingChannel ? Theme.LightGreyColor1 : Theme.MediumGreyColor1;
            return bmpProjectExplorer;
        }

        private TextureAtlasRef GetSnapRenderInfo(out string text, out Color tint)
        {
            var snapEnabled = App.SnapEnabled;
            text = snapEnabled ? SnapResolutionType.Names[App.SnapResolution] + (App.SnapEffectEnabled ? " (FX)" : "") : SnapOffLabel;
            tint = App.IsRecording ? Theme.DarkRedColor : Theme.LightGreyColor1;
            return snapEnabled ? bmpSnapOn : bmpSnapOff;
        }

        private TextureAtlasRef GetEffectRenderInfo(out string text, out Color tint)
        {
            var validEffect = App.SelectedEffect >= 0 && App.EffectPanelExpanded;

            text = validEffect ? EffectType.LocalizedNames[App.SelectedEffect] : NoneLabel;
            tint = Theme.LightGreyColor1;
            return validEffect ? bmpEffects[App.SelectedEffect] : bmpEffectNone;
        }

        private TextureAtlasRef GetDPCMEffectRenderInfo(out string text, out Color tint)
        {
            text = App.EffectPanelExpanded ? VolumeLabel : NoneLabel;
            tint = Theme.LightGreyColor1;
            return App.EffectPanelExpanded ? bmpEffects[Note.EffectVolume] : bmpEffectNone;
        }

        private TextureAtlasRef GetDPCMPlayRenderInfo(out string text, out Color tint)
        {
            text = "Play";
            tint = App.EditSample.Color;
            return bmpPlay;
        }

        private TextureAtlasRef GetWaveformEffectRenderInfo(out string text, out Color tint)
        {
            text = App.EffectPanelExpanded ? RepeatLabel : NoneLabel;
            tint = Theme.LightGreyColor1;
            return App.EffectPanelExpanded ? bmpEffectRepeat : bmpEffectNone;
        }

        private TextureAtlasRef GetChannelRenderInfo(out string text, out Color tint)
        {
            text = App.SelectedChannel.NameWithExpansion;
            tint = Theme.LightGreyColor1;
            return bmpChannels[App.SelectedChannel.Type];
        }

        private TextureAtlasRef GetInstrumentRenderingInfo(out string text, out Color tint)
        {
            var inst = App.SelectedInstrument;
            text = inst != null ? inst.Name : "";
            tint = inst != null ? inst.Color : Theme.LightGreyColor1;
            var exp = inst != null ? inst.Expansion : ExpansionType.None;
            return bmpExpansions[exp];
        }

        private TextureAtlasRef GetEnvelopeRenderingInfo(out string text, out Color tint)
        {
            var envType = App.EditEnvelopeType;
            var inst = App.SelectedInstrument;
            text = EnvelopeType.LocalizedNames[envType];
            tint = inst != null ? inst.Color : Theme.LightGreyColor1;
            return bmpEnvelopes[envType];
        }

        private TextureAtlasRef GetArpeggioRenderInfo(out string text, out Color tint)
        {
            var arp = App.SelectedArpeggio;
            text = arp != null ? arp.Name  : NoneLabel;
            tint = arp != null ? arp.Color : Theme.LightGreyColor1;
            return bmpArpeggio;
        }

        private void OnSnapItemClick(int idx)
        {
            if (idx <= SnapResolutionType.Max)
            {
                App.SnapResolution = idx;
                App.SnapEnabled = true;
            }
            else if (idx == SnapResolutionType.Max + 1)
            {
                App.SnapEffectEnabled = !App.SnapEffectEnabled;
            }
            else if (idx == SnapResolutionType.Max + 2)
            {
                App.SnapEnabled = !App.SnapEnabled;
            }
        }

        private void OnEffectItemClick(int idx)
        {
            var effect = (int)listItems[idx].Data;
            if (effect >= 0)
            {
                App.SelectedEffect = effect;
                App.EffectPanelExpanded = true;
            }
            else
            {
                App.EffectPanelExpanded = false;
            }
        }

        private void OnDPCMEffectItemClick(int idx)
        {
            App.EffectPanelExpanded = idx == 1;
        }

        private void OnWaveformEffectItemClick(int idx)
        {
            App.EffectPanelExpanded = idx == 1;
        }

        private void OnChannelItemClick(int idx)
        {
            App.SelectedChannelIndex = idx;
        }

        private void OnInstrumentItemClick(int idx)
        {
            var instrument = listItems[idx].Data as Instrument;
            App.SelectedInstrument = instrument;
        }

        private void OnEnvelopeItemClick(int idx)
        {
            App.StartEditInstrument(App.SelectedInstrument, (int)listItems[idx].Data);
        }

        private void OnArpeggioItemClick(int idx)
        {
            var arpeggio = listItems[idx].Data as Arpeggio;
            App.SelectedArpeggio = arpeggio;
        }

        private void OnChannelItemLongPress(int idx)
        {
            App.ShowContextMenu(new[]
            {
                new ContextMenuOption("MenuMute", ToggleMuteContext, () => { App.ToggleChannelActive(idx); MarkDirty(); }),
                new ContextMenuOption("MenuSolo", ToggleSoloContext, () => { App.ToggleChannelSolo(idx); MarkDirty(); }),
                new ContextMenuOption("MenuForceDisplay", ToggleForceDisplayContext, () => { App.ToggleChannelForceDisplay(idx); MarkDirty(); })
            });
        }

        private void OnInstrumentItemLongPress(int idx)
        {
            var inst = listItems[idx].Data as Instrument;

            if (App.IsEditingChannel && App.PianoRollHasSelection && App.SelectedChannel.SupportsInstrument(inst))
            {
                App.ShowContextMenu(new[]
                {
                    new ContextMenuOption("MenuReplaceSelection", ReplaceSelectionInstContext, () => { App.ReplacePianoRollSelectionInstrument(inst); MarkDirty(); })
                });
            }
        }

        private void OnArpeggioItemLongPress(int idx)
        {
            var arp = listItems[idx].Data as Arpeggio;

            if (App.IsEditingChannel && App.PianoRollHasSelection && App.SelectedChannel.SupportsArpeggios)
            {
                App.ShowContextMenu(new[]
                {
                    new ContextMenuOption("MenuReplaceSelection", ReplaceSelectionArpContext, () => { App.ReplacePianoRollSelectionArpeggio(arp); MarkDirty(); })
                });
            }
        }

        private Rectangle GetScrollBarRect()
        {
            var visibleSizeY = popupRect.Height;
            var virtualSizeY = listItems.Length * listItemSize;

            if (visibleSizeY < virtualSizeY)
            {
                var sizeY = (int)Math.Round(visibleSizeY * (visibleSizeY / (float)virtualSizeY));
                var posY  = (int)Math.Round(visibleSizeY * (scrollY      / (float)virtualSizeY));

                return new Rectangle(popupRect.Width - scrollBarSizeX, posY, scrollBarSizeX, sizeY);
            }
            else
            {
                return Rectangle.Empty;
            }
        }

        protected override void OnRender(Graphics g)
        {
            var c = g.DefaultCommandList;
            var o = g.OverlayCommandList;

            c.Transform.GetOrigin(out var ox, out var oy);

            var listRect = GetExpandedListRect();
            var screenRect = new Rectangle(Point.Empty, ParentWindow.Size);
            screenRect.Offset(-(int)ox, -(int)oy);

            // Background shadow.
            if (IsExpanded)
            {
                var shadowColor = Color.FromArgb(popupRatio * 0.6f, Color.Black);

                if (IsLandscape)
                {
                    o.FillRectangle(screenRect.Left, screenRect.Top, listRect.Left, screenRect.Bottom, shadowColor);
                    o.FillRectangle(listRect.Left, screenRect.Top, 0, listRect.Top, shadowColor);
                    o.FillRectangle(listRect.Left, listRect.Bottom, 0, screenRect.Bottom, shadowColor);
                    o.FillRectangle(0, 0, width, height, shadowColor);
                }
                else
                {
                    o.FillRectangle(screenRect.Left, screenRect.Top, screenRect.Right, listRect.Top, shadowColor);
                    o.FillRectangle(screenRect.Left, listRect.Top, listRect.Left, 0, shadowColor);
                    o.FillRectangle(listRect.Right, listRect.Top, screenRect.Right, 0, shadowColor);
                    o.FillRectangle(0, 0, screenRect.Right, screenRect.Bottom, shadowColor);
                }
            }

            // Clear BG.
            var navBgRect = Rectangle.Empty;
            
            for (int i = 0; i < (int)ButtonType.Count; i++)
            {
                var btn = buttons[i];
                if (btn.Visible && btn.IsNavButton)
                    navBgRect = navBgRect.IsEmpty ? btn.Rect : Rectangle.Union(navBgRect, btn.Rect);
            }

            var bgRect = new Rectangle(0, 0, Width, Height);
            
            c.FillRectangleGradient(bgRect,    Theme.DarkGreyColor4, Theme.DarkGreyColor4.Scaled(0.8f), !IsLandscape, buttonSize);
            c.FillRectangleGradient(navBgRect, Theme.DarkGreyColor1, Theme.DarkGreyColor1.Scaled(0.8f), !IsLandscape, buttonSize);

            // Buttons
            for (int i = 0; i < (int)ButtonType.Count; i++)
            {
                var btn = buttons[i];

                if (btn.Visible)
                {
                    var bmp = btn.GetRenderInfo(out var text, out var tint);
                    c.DrawTextureAtlas(bmp, btn.IconX, btn.IconY, 1.0f, iconScaleFloat, tint);

                    if (!string.IsNullOrEmpty(text))
                        c.DrawText(text, buttonFont, btn.TextX, btn.TextY, Theme.LightGreyColor1, TextFlags.Center | TextFlags.Ellipsis, buttonSize, 0);
                }
            }

            // Dividing line.
            if (IsLandscape)
                c.DrawLine(0, 0, 0, Height, Theme.BlackColor);
            else
                c.DrawLine(0, 0, Width, 0, Theme.BlackColor);

            // List items.
            if (popupButtonIdx >= 0)
            {              
                if (IsLandscape)
                    screenRect.Width -= width;
                else
                    screenRect.Height -= (screenRect.Height - WindowRectangle.Top);

                screenRect = Rectangle.Intersect(screenRect, listRect);

                c.PushClipRegion(screenRect.Left, screenRect.Top, screenRect.Width, screenRect.Height, false);
                c.PushTranslation(listRect.Left, listRect.Top - scrollY);

                for (int i = 0; i < listItems.Length; i++)
                {
                    var item = listItems[i];
                    var opacity = item.GetImageOpacity != null ? item.GetImageOpacity(item) : 1.0f;

                    c.FillAndDrawRectangleGradient(item.Rect, item.Color, item.Color.Scaled(0.8f), Theme.BlackColor, true, listItemSize);
                    
                    if (item.Image != null)
                    { 
                        c.DrawTextureAtlas(item.Image, item.IconX, item.IconY, opacity, iconScaleFloat, Color.Black);
                    }

                    if (item.ExtraImage != null)
                    {
                        var extraOpacity = item.GetExtraImageOpacity != null ? item.GetExtraImageOpacity(item) : 1.0f;
                        c.DrawTextureAtlas(item.ExtraImage, item.ExtraIconX, item.ExtraIconY, extraOpacity, iconScaleFloat, Color.Black);
                    }

                    c.DrawText(item.Text, i == popupSelectedIdx ? Fonts.FontMediumBold : Fonts.FontMedium, item.TextX, item.TextY, item.TextColor, TextFlags.Middle, 0, listItemSize);
                }

                c.PopTransform();

                var scrollBarRect = GetScrollBarRect();

                if ((Math.Abs(flingVelY) > 0.0f || captureOperation == CaptureOperation.MobilePan) && !scrollBarRect.IsEmpty)
                {
                    c.PushTranslation(listRect.Left, listRect.Top);
                    c.FillRectangle(GetScrollBarRect(), scrollBarColor);
                    c.PopTransform();
                }

                if (IsLandscape)
                    listRect.Width  = -listRect.X;
                else
                    listRect.Height = -listRect.Y;

                listRect.Offset((int)Math.Round(ox), (int)Math.Round(oy));
                c.PopClipRegion();
            }
        }

        private void StartCaptureOperation(int x, int y, CaptureOperation op)
        {
            lastX = x;
            lastY = y;
            captureX = x;
            captureY = y;
            captureOperation = op;
            Capture = true;
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
            Capture = false;
            MarkDirty();
        }

        protected override void OnTouchClick(int x, int y)
        {
            foreach (var btn in buttons)
            {
                if (btn.Visible && btn.Rect.Contains(x, y))
                {
                    Platform.VibrateTick();
                    btn.Click();
                    return;
                }
            }

            if (popupRatio > 0.5f)
            {
                var rect = GetExpandedListRect();

                if (rect.Contains(x, y))
                {
                    var idx = (y - rect.Top + scrollY) / listItemSize;

                    if (idx >= 0 && idx < listItems.Length)
                    {
                        Platform.VibrateTick();
                        buttons[popupButtonIdx].ListItemClick?.Invoke(idx);
                        popupSelectedIdx = idx;
                    }
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

        protected override void OnTouchLongPress(int x, int y)
        {
            foreach (var btn in buttons)
            {
                if (btn.Visible && btn.Rect.Contains(x, y))
                {
                    if (btn.LongPress != null)
                        btn.LongPress();
                    return;
                }
            }

            if (popupRatio > 0.5f)
            {
                var rect = GetExpandedListRect();

                if (rect.Contains(x, y))
                {
                    var idx = (y - rect.Top + scrollY) / listItemSize;

                    if (idx >= 0 && idx < listItems.Length && buttons[popupButtonIdx].ListItemLongPress != null)
                        buttons[popupButtonIdx].ListItemLongPress?.Invoke(idx);
                }
            }

            lastX = x;
            lastY = y;
        }
    }
}
