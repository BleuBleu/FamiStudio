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
using RenderCommandList = FamiStudio.GLCommandList;
using RenderTransform   = FamiStudio.GLTransform;

namespace FamiStudio
{
    public class Toolbar : ToolbarBase
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
            base.OnRenderInitialized(g);

            buttons[(int)ButtonType.Help].RightAligned = true;
            buttons[(int)ButtonType.More] = null;

            buttons[(int)ButtonType.New].ToolTip       = "{MouseLeft} New Project {Ctrl} {N}";
            buttons[(int)ButtonType.Open].ToolTip      = "{MouseLeft} Open Project {Ctrl} {O}";
            buttons[(int)ButtonType.Save].ToolTip      = "{MouseLeft} Save Project {Ctrl} {S}\n{MouseRight} Save As...";
            buttons[(int)ButtonType.Export].ToolTip    = "{MouseLeft} Export to various formats {Ctrl} {E}\n{MouseRight} Repeat last export {Ctrl} {Shift} {E}";
            buttons[(int)ButtonType.Copy].ToolTip      = "{MouseLeft} Copy selection {Ctrl} {C}";
            buttons[(int)ButtonType.Cut].ToolTip       = "{MouseLeft} Cut selection {Ctrl} {X}";
            buttons[(int)ButtonType.Paste].ToolTip     = "{MouseLeft} Paste {Ctrl} {V}\n{MouseRight} Paste Special... {Ctrl} {Shift} {V}";
            buttons[(int)ButtonType.Undo].ToolTip      = "{MouseLeft} Undo {Ctrl} {Z}";
            buttons[(int)ButtonType.Redo].ToolTip      = "{MouseLeft} Redo {Ctrl} {Y}";
            buttons[(int)ButtonType.Transform].ToolTip = "{MouseLeft} Perform cleanup and various operations";
            buttons[(int)ButtonType.Config].ToolTip    = "{MouseLeft} Edit Application Settings";
            buttons[(int)ButtonType.Play].ToolTip      = "{MouseLeft} Play/Pause {Space} - {MouseWheel} Change play rate - Play from start of pattern {Ctrl} {Space}\nPlay from start of song {Shift} {Space} - Play from loop point {Ctrl} {Shift} {Space}";
            buttons[(int)ButtonType.Rewind].ToolTip    = "{MouseLeft} Rewind {Home}\nRewind to beginning of current pattern {Ctrl} {Home}";
            buttons[(int)ButtonType.Rec].ToolTip       = "{MouseLeft} Toggles recording mode {Enter}\nAbort recording {Esc}";
            buttons[(int)ButtonType.Loop].ToolTip      = "{MouseLeft} Toggle Loop Mode (Song, Pattern/Selection)";
            buttons[(int)ButtonType.Qwerty].ToolTip    = "{MouseLeft} Toggle QWERTY keyboard piano input {Shift} {Q}";
            buttons[(int)ButtonType.Metronome].ToolTip = "{MouseLeft} Toggle metronome while song is playing";
            buttons[(int)ButtonType.Machine].ToolTip   = "{MouseLeft} Toggle between NTSC/PAL playback mode";
            buttons[(int)ButtonType.Follow].ToolTip    = "{MouseLeft} Toggle follow mode {Shift} {F}";
            buttons[(int)ButtonType.Help].ToolTip      = "{MouseLeft} Online documentation";

            Debug.Assert((int)SpecialCharImageIndices.Count == SpecialCharImageNames.Length);

            bmpSpecialCharAtlas = g.CreateBitmapAtlasFromResources(SpecialCharImageNames);

            timecodeOffsetX         = ScaleForMainWindow(DefaultTimecodeOffsetX);
            timecodePosY            = ScaleForMainWindow(DefaultTimecodePosY);
            oscilloscopePosY        = ScaleForMainWindow(DefaultTimecodePosY);
            timecodeOscSizeX        = ScaleForMainWindow(DefaultTimecodeSizeX);
            tooltipSingleLinePosY   = ScaleForMainWindow(DefaultTooltipSingleLinePosY);
            tooltipMultiLinePosY    = ScaleForMainWindow(DefaultTooltipMultiLinePosY);
            tooltipLineSizeY        = ScaleForMainWindow(DefaultTooltipLineSizeY);
            tooltipSpecialCharSizeX = ScaleForMainWindow(DefaultTooltipSpecialCharSizeX);
            tooltipSpecialCharSizeY = ScaleForMainWindow(DefaultTooltipSpecialCharSizeY);
            buttonPosX              = ScaleForMainWindow(DefaultButtonPosX);
            buttonPosY              = ScaleForMainWindow(DefaultButtonPosY);
            buttonSizeX             = ScaleForMainWindow(DefaultButtonSizeX);
            buttonSpacingX          = ScaleForMainWindow(DefaultButtonSpacingX);
            buttonTimecodeSpacingX  = ScaleForMainWindow(DefaultButtonTimecodeSpacingX);

            UpdateButtonLayout();

            specialCharacters["Shift"]      = new TooltipSpecialCharacter { Width = ScaleForMainWindow(32) };
            specialCharacters["Space"]      = new TooltipSpecialCharacter { Width = ScaleForMainWindow(38) };
            specialCharacters["Home"]       = new TooltipSpecialCharacter { Width = ScaleForMainWindow(38) };
            specialCharacters["Ctrl"]       = new TooltipSpecialCharacter { Width = ScaleForMainWindow(28) };
            specialCharacters["Alt"]        = new TooltipSpecialCharacter { Width = ScaleForMainWindow(24) };
            specialCharacters["Enter"]      = new TooltipSpecialCharacter { Width = ScaleForMainWindow(38) };
            specialCharacters["Esc"]        = new TooltipSpecialCharacter { Width = ScaleForMainWindow(24) };
            specialCharacters["Del"]        = new TooltipSpecialCharacter { Width = ScaleForMainWindow(24) };
            specialCharacters["Drag"]       = new TooltipSpecialCharacter { BmpIndex = SpecialCharImageIndices.Drag,       OffsetY = ScaleForMainWindow(2) };
            specialCharacters["MouseLeft"]  = new TooltipSpecialCharacter { BmpIndex = SpecialCharImageIndices.MouseLeft,  OffsetY = ScaleForMainWindow(2) };
            specialCharacters["MouseRight"] = new TooltipSpecialCharacter { BmpIndex = SpecialCharImageIndices.MouseRight, OffsetY = ScaleForMainWindow(2) };
            specialCharacters["MouseWheel"] = new TooltipSpecialCharacter { BmpIndex = SpecialCharImageIndices.MouseWheel, OffsetY = ScaleForMainWindow(2) };
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
            Utils.DisposeAndNullify(ref toolbarBrush);
            Utils.DisposeAndNullify(ref warningBrush);
            Utils.DisposeAndNullify(ref bmpButtonAtlas);
            Utils.DisposeAndNullify(ref bmpSpecialCharAtlas);

            specialCharacters.Clear();
        }

        protected override void UpdateButtonLayout()
        {
            if (!IsRenderInitialized)
                return;

            // Hide a few buttons if the window is too small (out min "usable" resolution is ~1280x720).
            bool hideLessImportantButtons = Width < 1420 * MainWindowScaling;
            bool hideOscilloscope         = Width < 1250 * MainWindowScaling;

            var posX = buttonPosX;

            for (int i = 0; i < (int)ButtonType.Count; i++)
            {
                var btn = buttons[i];

                if (btn == null)
                    continue;

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
                    posX += buttonSpacingX + timecodeOscSizeX + buttonTimecodeSpacingX * 2;

                    oscilloscopeVisible = Settings.ShowOscilloscope && !hideOscilloscope;
                    if (oscilloscopeVisible)
                        posX += timecodeOscSizeX + buttonTimecodeSpacingX * 2;
                }
                else if (btn.Visible)
                {
                    posX += buttonSpacingX;
                }
            }

            timecodePosX = buttons[(int)ButtonType.Config].X + timecodeOffsetX;
            oscilloscopePosX = timecodePosX + timecodeOscSizeX + buttonTimecodeSpacingX * 2;
            timecodeOscSizeY = Height - timecodePosY * 2;
        }

        public override void SetToolTip(string msg, bool red = false)
        {
            if (tooltip != msg || red != redTooltip)
            {
                tooltip = msg;
                redTooltip = red;
                ConditionalInvalidate();
            }
        }

        public override void DisplayWarning(string msg, bool beep)
        {
            warningTime = DateTime.Now;
            warning = "{Warning} " + msg;
            if (beep)
                SystemSounds.Beep.Play();
        }

        public void Tick(float delta)
        {
            if (!string.IsNullOrEmpty(warning))
                ConditionalInvalidate();
        }

        public void Reset()
        {
            tooltip = "";
            redTooltip = false;
        }

        private void RenderWarningAndTooltip(RenderGraphics g, RenderCommandList c)
        {
            var scaling = MainWindowScaling;
            var message = tooltip;
            var messageBrush = redTooltip ? warningBrush : ThemeResources.LightGreyFillBrush2;
            var messageFont = ThemeResources.FontMedium;

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
                    messageFont = ThemeResources.FontMediumBold;
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
                                if (PlatformUtils.IsMacOS && str == "Ctrl") str = "Cmd";
                                posX -= (int)scaling; // HACK: The way we handle fonts in OpenGL is so different, i cant be bothered to debug this.
                                c.DrawRectangle(posX, posY + specialCharacter.OffsetY, posX + specialCharacter.Width, posY + specialCharacter.Height + specialCharacter.OffsetY, messageBrush);
                                c.DrawText(str, messageFont, posX, posY, messageBrush, RenderTextFlags.Center, specialCharacter.Width);
                                posX -= (int)scaling; // HACK: The way we handle fonts in OpenGL is so different, i cant be bothered to debug this.
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
            base.OnRender(g);

            var ct = g.CreateCommandList(); // Tooltip (clipped)
            RenderWarningAndTooltip(g, ct);
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
                if (btn != null && btn.Visible && btn.IsPointIn(e.X, e.Y, Width))
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
                if (e.X > timecodePosX && e.X < timecodePosX + timecodeOscSizeX &&
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
