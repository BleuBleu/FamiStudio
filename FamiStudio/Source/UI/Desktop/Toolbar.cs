using System;
using System.Collections.Generic;
using System.Media;
using System.Windows.Forms;
using System.Diagnostics;

using RenderBitmap      = FamiStudio.GLBitmap;
using RenderBitmapAtlas = FamiStudio.GLBitmapAtlas;
using RenderBrush       = FamiStudio.GLBrush;
using RenderGeometry    = FamiStudio.GLGeometry;
using RenderControl     = FamiStudio.GLControl;
using RenderGraphics    = FamiStudio.GLGraphics;
using RenderTheme       = FamiStudio.GLTheme;
using RenderCommandList = FamiStudio.GLCommandList;
using RenderTransform   = FamiStudio.GLTransform;

namespace FamiStudio
{
    public partial class Toolbar : RenderControl
    {
        const int DefaultTimecodeOffsetX = 38; // Offset from config button.
        const int DefaultTimecodePosY = 4;
        const int DefaultTimecodeSizeX = 140;
        const int DefaultTooltipSingleLinePosY = 12;
        const int DefaultTooltipMultiLinePosY = 4;
        const int DefaultTooltipLineSizeY = 17;
        const int DefaultTooltipSpecialCharSizeX = 16;
        const int DefaultTooltipSpecialCharSizeY = 14;
        const int DefaultButtonPosX = 4;
        const int DefaultButtonPosY = 4;
        const int DefaultButtonSizeX = 32;
        const int DefaultButtonSpacingX = 34;
        const int DefaultButtonTimecodeSpacingX = 4; // Spacing before/after timecode.

        int timecodeOffsetX;
        int timecodePosX;
        int timecodePosY;
        int timecodeSizeX;
        int oscilloscopePosX;
        int tooltipSingleLinePosY;
        int tooltipMultiLinePosY;
        int tooltipLineSizeY;
        int tooltipSpecialCharSizeX;
        int tooltipSpecialCharSizeY;
        int buttonPosX;
        int buttonPosY;
        int buttonSizeX;
        int buttonSpacingX;
        int buttonTimecodeSpacingX;

        class TooltipSpecialCharacter
        {
            public SpecialCharImageIndices BmpIndex = SpecialCharImageIndices.Count;
            public int Width;
            public int Height;
            public float OffsetY;
        };

        DateTime warningTime;
        string warning = "";

        int lastButtonX = 500;
        bool redTooltip = false;
        string tooltip = "";
        RenderBitmapAtlas bmpSpecialCharAtlas;
        Dictionary<string, TooltipSpecialCharacter> specialCharacters = new Dictionary<string, TooltipSpecialCharacter>();

        enum SpecialCharImageIndices
        {
            Drag,
            MouseLeft,
            MouseRight,
            MouseWheel,
            Warning,
            Count
        };

        readonly string[] SpecialCharImageNames = new string[]
        {
            "Drag",
            "MouseLeft",
            "MouseRight",
            "MouseWheel",
            "Warning"
        };

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            OnRenderInitializedCommon(g);

            Debug.Assert((int)SpecialCharImageIndices.Count == SpecialCharImageNames.Length);

            bmpSpecialCharAtlas = g.CreateBitmapAtlasFromResources(SpecialCharImageNames);

            var scaling = RenderTheme.MainWindowScaling;

            timecodeOffsetX         = (int)(DefaultTimecodeOffsetX * scaling);
            timecodePosY            = (int)(DefaultTimecodePosY * scaling);
            timecodeSizeX           = (int)(DefaultTimecodeSizeX * scaling);
            tooltipSingleLinePosY   = (int)(DefaultTooltipSingleLinePosY * scaling);
            tooltipMultiLinePosY    = (int)(DefaultTooltipMultiLinePosY * scaling);
            tooltipLineSizeY        = (int)(DefaultTooltipLineSizeY * scaling);
            tooltipSpecialCharSizeX = (int)(DefaultTooltipSpecialCharSizeX * scaling);
            tooltipSpecialCharSizeY = (int)(DefaultTooltipSpecialCharSizeY * scaling);
            buttonPosX              = (int)(DefaultButtonPosX * scaling);
            buttonPosY              = (int)(DefaultButtonPosY * scaling);
            buttonSizeX             = (int)(DefaultButtonSizeX * scaling);
            buttonSpacingX          = (int)(DefaultButtonSpacingX * scaling);
            buttonTimecodeSpacingX  = (int)(DefaultButtonTimecodeSpacingX * scaling);

            UpdateButtonLayout();

            specialCharacters["Shift"]      = new TooltipSpecialCharacter { Width = (int)(32 * scaling) };
            specialCharacters["Space"]      = new TooltipSpecialCharacter { Width = (int)(38 * scaling) };
            specialCharacters["Home"]       = new TooltipSpecialCharacter { Width = (int)(38 * scaling) };
            specialCharacters["Ctrl"]       = new TooltipSpecialCharacter { Width = (int)(28 * scaling) };
            specialCharacters["Alt"]        = new TooltipSpecialCharacter { Width = (int)(24 * scaling) };
            specialCharacters["Enter"]      = new TooltipSpecialCharacter { Width = (int)(38 * scaling) };
            specialCharacters["Esc"]        = new TooltipSpecialCharacter { Width = (int)(24 * scaling) };
            specialCharacters["Del"]        = new TooltipSpecialCharacter { Width = (int)(24 * scaling) };
            specialCharacters["Drag"]       = new TooltipSpecialCharacter { BmpIndex = SpecialCharImageIndices.Drag, OffsetY = 2 * scaling };
            specialCharacters["MouseLeft"]  = new TooltipSpecialCharacter { BmpIndex = SpecialCharImageIndices.MouseLeft, OffsetY = 2 * scaling };
            specialCharacters["MouseRight"] = new TooltipSpecialCharacter { BmpIndex = SpecialCharImageIndices.MouseRight, OffsetY = 2 * scaling };
            specialCharacters["MouseWheel"] = new TooltipSpecialCharacter { BmpIndex = SpecialCharImageIndices.MouseWheel, OffsetY = 2 * scaling };
            specialCharacters["Warning"]    = new TooltipSpecialCharacter { BmpIndex = SpecialCharImageIndices.Warning };

            for (char i = 'A'; i <= 'Z'; i++)
                specialCharacters[i.ToString()] = new TooltipSpecialCharacter { Width = tooltipSpecialCharSizeX };
            for (char i = '0'; i <= '9'; i++)
                specialCharacters[i.ToString()] = new TooltipSpecialCharacter { Width = tooltipSpecialCharSizeX };

            specialCharacters["~"] = new TooltipSpecialCharacter { Width = tooltipSpecialCharSizeX };

            foreach (var c in specialCharacters.Values)
            {
                if (c.BmpIndex != SpecialCharImageIndices.Count)
                    c.Width = bmpSpecialCharAtlas.GetElementSize((int)c.BmpIndex).Width;
                c.Height = tooltipSpecialCharSizeY;
            }
        }

        protected override void OnRenderTerminated()
        {
            theme.Terminate();

            Utils.DisposeAndNullify(ref toolbarBrush);
            Utils.DisposeAndNullify(ref warningBrush);
            Utils.DisposeAndNullify(ref bmpButtonAtlas);
            Utils.DisposeAndNullify(ref bmpSpecialCharAtlas);

            specialCharacters.Clear();
        }


        private void UpdateButtonLayout()
        {
            if (theme == null)
                return;

            // Hide a few buttons if the window is too small (out min "usable" resolution is ~1280x720).
            bool hideLessImportantButtons = Width < 1420 * RenderTheme.MainWindowScaling;
            bool hideOscilloscope = Width < 1250 * RenderTheme.MainWindowScaling;

            var posX = buttonPosX;

            for (int i = 0; i < (int)ButtonType.Count; i++)
            {
                var btn = buttons[i];

                if (i == (int)ButtonType.Help)
                {
                    btn.X = buttonSizeX;
                }
                else
                {
                    btn.X = posX;
                    lastButtonX = posX + buttonSizeX;
                }

                btn.Y = buttonPosY;
                btn.Size = buttonSizeX;
                btn.Visible = !hideLessImportantButtons || i < (int)ButtonType.Copy || i > (int)ButtonType.Redo;

                if (i == (int)ButtonType.Config)
                {
                    posX += buttonSpacingX + timecodeSizeX + buttonTimecodeSpacingX * 2;

                    oscilloscopeVisible = Settings.ShowOscilloscope && !hideOscilloscope;
                    if (oscilloscopeVisible)
                        posX += timecodeSizeX + buttonTimecodeSpacingX * 2;
                }
                else if (btn.Visible)
                {
                    posX += buttonSpacingX;
                }
            }

            timecodePosX = buttons[(int)ButtonType.Config].X + timecodeOffsetX;
            oscilloscopePosX = timecodePosX + timecodeSizeX + buttonTimecodeSpacingX * 2;
        }

        public void SetToolTip(string msg, bool red = false)
        {
            if (tooltip != msg || red != redTooltip)
            {
                tooltip = msg;
                redTooltip = red;
                ConditionalInvalidate();
            }
        }

        public void DisplayWarning(string msg, bool beep)
        {
            warningTime = DateTime.Now;
            warning = "{Warning} " + msg;
            if (beep)
                SystemSounds.Beep.Play();
        }

        public void Tick()
        {
            if (!string.IsNullOrEmpty(warning))
                ConditionalInvalidate();
        }

        public void Reset()
        {
            tooltip = "";
            redTooltip = false;
        }

        private void RenderButtons(RenderGraphics g, RenderCommandList c)
        {
            var pt = PointToClient(Cursor.Position);

            // Clear
            c.FillRectangle(0, 0, Width, Height, toolbarBrush);

            // Buttons
            foreach (var btn in buttons)
            {
                if (!btn.Visible)
                    continue;

                var hover = btn.IsPointIn(pt.X, pt.Y, Width);
                var bmpIndex = btn.GetBitmap != null ? btn.GetBitmap() : btn.BmpAtlasIndex;
                var status = btn.Enabled == null ? ButtonStatus.Enabled : btn.Enabled();
                var opacity = status == ButtonStatus.Enabled ? 1.0f : 0.25f;

                if (status != ButtonStatus.Disabled && hover)
                    opacity *= 0.75f;

                int x = btn.RightAligned ? Width - btn.X : btn.X;
                c.DrawBitmapAtlas(bmpButtonAtlas, (int)bmpIndex, x, btn.Y, opacity);
            }
        }

        private void RenderWarningAndTooltip(RenderGraphics g, RenderCommandList c)
        {
            var scaling = RenderTheme.MainWindowScaling;
            var message = tooltip;
            var messageBrush = redTooltip ? warningBrush : theme.LightGreyFillBrush2;
            var messageFont = ThemeBase.FontMedium;
            var messageFontCenter = ThemeBase.FontMediumCenter;

            if (!string.IsNullOrEmpty(warning))
            {
                var span = DateTime.Now - warningTime;

                if (span.TotalMilliseconds >= 2000)
                {
                    warning = "";
                }
                else
                {
                    message = (((((long)span.TotalMilliseconds) / 250) & 1) != 0) ? warning : "";
                    messageBrush = warningBrush;
                    messageFont = ThemeBase.FontMediumBold;
                    messageFontCenter = ThemeBase.FontMediumBoldCenter;
                }
            }

            // Tooltip
            if (!string.IsNullOrEmpty(message))
            {
                var lines = message.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var posY = lines.Length == 1 ? tooltipSingleLinePosY : tooltipMultiLinePosY;

                for (int j = 0; j < lines.Length; j++)
                {
                    var splits = lines[j].Split(new char[] { '{', '}' }, StringSplitOptions.RemoveEmptyEntries);
                    var posX = Width - 40 * scaling;

                    for (int i = splits.Length - 1; i >= 0; i--)
                    {
                        var str = splits[i];

                        if (specialCharacters.TryGetValue(str, out var specialCharacter))
                        {
                            posX -= specialCharacter.Width;

                            if (specialCharacter.BmpIndex != SpecialCharImageIndices.Count)
                            {
                                c.DrawBitmapAtlas(bmpSpecialCharAtlas, (int)specialCharacter.BmpIndex, posX, posY + specialCharacter.OffsetY);
                            }
                            else
                            {
#if FAMISTUDIO_MACOS
                                if (str == "Ctrl") str = "Cmd";
#endif

#if !FAMISTUDIO_WINDOWS
                                // HACK: The way we handle fonts in OpenGL is so different, i cant be bothered to debug this.
                                posX -= (int)scaling;
#endif

                                c.DrawRectangle(posX, posY + specialCharacter.OffsetY, posX + specialCharacter.Width, posY + specialCharacter.Height + specialCharacter.OffsetY, messageBrush);
                                c.DrawText(str, messageFontCenter, posX, posY, messageBrush, specialCharacter.Width);

#if !FAMISTUDIO_WINDOWS
                                // HACK: The way we handle fonts in OpenGL is so different, i cant be bothered to debug this.
                                posX -= (int)scaling;
#endif
                            }
                        }
                        else
                        {
                            posX -= g.MeasureString(splits[i], messageFont);
                            c.DrawText(str, messageFont, posX, posY, messageBrush);
                        }
                    }

                    posY += tooltipLineSizeY;
                }
            }
        }

        protected override void OnRender(RenderGraphics g)
        {
            var cm = g.CreateCommandList(); // Main
            var ct = g.CreateCommandList(); // Tooltip (clipped)

            // Prepare the batches.
            RenderButtons(g, cm);
            RenderTimecode(g, cm);
            RenderOscilloscope(g, cm, oscilloscopePosX, timecodePosY, timecodeSizeX, Height - timecodePosY * 2);
            RenderWarningAndTooltip(g, ct);

            // Draw everything.
            g.DrawCommandList(cm);
            g.DrawCommandList(ct, new System.Drawing.Rectangle(lastButtonX, 0, Width, Height));
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            ConditionalInvalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            ConditionalInvalidate();

            foreach (var btn in buttons)
            {
                if (btn.Visible && btn.IsPointIn(e.X, e.Y, Width))
                {
                    SetToolTip(btn.ToolTip);
                    return;
                }
            }

            SetToolTip("");
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            foreach (var btn in buttons)
            {
                if (btn != null && btn.Visible && btn.IsPointIn(e.X, e.Y, Width) && (btn.Enabled == null || btn.Enabled() != ButtonStatus.Disabled))
                {
                    btn.MouseWheel?.Invoke(e.Delta);
                    break;
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            bool left = e.Button.HasFlag(MouseButtons.Left);
            bool right = e.Button.HasFlag(MouseButtons.Right);

            if (left || right)
            {
                if (e.X > timecodePosX && e.X < timecodePosX + timecodeSizeX &&
                    e.Y > timecodePosY && e.Y < Height - timecodePosY)
                {
                    Settings.TimeFormat = Settings.TimeFormat == 0 ? 1 : 0;
                    ConditionalInvalidate();
                }
                else
                {
                    foreach (var btn in buttons)
                    {
                        if (btn != null && btn.Visible && btn.IsPointIn(e.X, e.Y, Width) && (btn.Enabled == null || btn.Enabled() != ButtonStatus.Disabled))
                        {
                            if (left)
                                btn.Click?.Invoke();
                            else
                                btn.RightClick?.Invoke();
                            break;
                        }
                    }
                }
            }
        }
    }
}
