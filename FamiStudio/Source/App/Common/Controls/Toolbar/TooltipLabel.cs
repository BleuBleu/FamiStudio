using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace FamiStudio
{
    public class TooltipLabel : Control
    {
        class TooltipSpecialCharacter
        {
            public TextureAtlasRef Texture;
            public int Width;
            public int Height;
            public float OffsetY;
        };

        private int tooltipSingleLinePosY   =  DpiScaling.ScaleForWindow(12);
        private int tooltipMultiLinePosY    =  DpiScaling.ScaleForWindow(4);
        private int tooltipLineSizeY        =  DpiScaling.ScaleForWindow(17);
        private int tooltipSpecialCharSizeX =  DpiScaling.ScaleForWindow(16);
        private int tooltipSpecialCharSizeY =  DpiScaling.ScaleForWindow(15);

        private Dictionary<string, TooltipSpecialCharacter> specialCharacters = new Dictionary<string, TooltipSpecialCharacter>();

        public TooltipLabel()
        {
        }

        protected override void OnAddedToContainer()
        {
            specialCharacters["Shift"]      = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(32) };
            specialCharacters["Space"]      = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(38) };
            specialCharacters["Home"]       = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(38) };
            specialCharacters["Ctrl"]       = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(28) };
            specialCharacters["ForceCtrl"]  = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(28) };
            specialCharacters["Alt"]        = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(24) };
            specialCharacters["Tab"]        = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(24) };
            specialCharacters["Enter"]      = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(38) };
            specialCharacters["Esc"]        = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(24) };
            specialCharacters["Del"]        = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(24) };
            specialCharacters["F1"]         = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(18) };
            specialCharacters["F2"]         = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(18) };
            specialCharacters["F3"]         = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(18) };
            specialCharacters["F4"]         = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(18) };
            specialCharacters["F5"]         = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(18) };
            specialCharacters["F6"]         = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(18) };
            specialCharacters["F7"]         = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(18) };
            specialCharacters["F8"]         = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(18) };
            specialCharacters["F9"]         = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(18) };
            specialCharacters["F10"]        = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(24) };
            specialCharacters["F11"]        = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(24) };
            specialCharacters["F12"]        = new TooltipSpecialCharacter { Width = DpiScaling.ScaleForWindow(24) };
            specialCharacters["Drag"]       = new TooltipSpecialCharacter { Texture = graphics.GetTextureAtlasRef("Drag"),       OffsetY = DpiScaling.ScaleForWindow(2) };
            specialCharacters["MouseLeft"]  = new TooltipSpecialCharacter { Texture = graphics.GetTextureAtlasRef("MouseLeft"), OffsetY = DpiScaling.ScaleForWindow(2) };
            specialCharacters["MouseRight"] = new TooltipSpecialCharacter { Texture = graphics.GetTextureAtlasRef("MouseRight"), OffsetY = DpiScaling.ScaleForWindow(2) };
            specialCharacters["MouseWheel"] = new TooltipSpecialCharacter { Texture = graphics.GetTextureAtlasRef("MouseWheel"), OffsetY = DpiScaling.ScaleForWindow(2) };
            specialCharacters["Warning"]    = new TooltipSpecialCharacter { Texture = graphics.GetTextureAtlasRef("Warning") };

            for (char i = 'A'; i <= 'Z'; i++)
                specialCharacters[i.ToString()] = new TooltipSpecialCharacter { Width = tooltipSpecialCharSizeX };
            for (char i = '0'; i <= '9'; i++)
                specialCharacters[i.ToString()] = new TooltipSpecialCharacter { Width = tooltipSpecialCharSizeX };

            specialCharacters["~"] = new TooltipSpecialCharacter { Width = tooltipSpecialCharSizeX };

            foreach (var c in specialCharacters.Values)
            {
                if (c.Texture != null)
                    c.Width = c.Texture.ElementSize.Width;
                c.Height = tooltipSpecialCharSizeY;
            }
        }

        protected override void OnRender(Graphics g)
        {
            var scaling = DpiScaling.Window;
            var message = tooltip;
            var messageColor = Theme.LightGreyColor2;
            var messageFont = Fonts.FontMedium;

            // Tooltip
            if (!string.IsNullOrEmpty(message) && width > 1)
            {
                var c = g.DefaultCommandList;

                c.PushClipRegion(0, 0, width, height);
                c.FillRectangleGradient(0, 0, Width, Height, Theme.DarkGreyColor5, Theme.DarkGreyColor4, true, Height); // Same as toolbar.

                var lines = message.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var posY = lines.Length == 1 ? tooltipSingleLinePosY : tooltipMultiLinePosY;

                for (int j = 0; j < lines.Length; j++)
                {
                    var splits = lines[j].Split(new char[] { '<', '>' }, StringSplitOptions.RemoveEmptyEntries);
                    var posX = (float)Width;

                    for (int i = splits.Length - 1; i >= 0; i--)
                    {
                        var str = splits[i];

                        if (specialCharacters.TryGetValue(str, out var specialCharacter))
                        {
                            posX -= specialCharacter.Width;

                            if (specialCharacter.Texture != null)
                            {
                                c.DrawTextureAtlas(specialCharacter.Texture, posX, posY + specialCharacter.OffsetY, 1.0f, Theme.LightGreyColor1);
                            }
                            else
                            {
                                if (Platform.IsMacOS && str == "Ctrl") str = "Cmd";

                                c.DrawRectangle(posX, posY + specialCharacter.OffsetY, posX + specialCharacter.Width - (int)scaling, posY + specialCharacter.Height + specialCharacter.OffsetY, messageColor);
                                c.DrawText(str, messageFont, posX, posY, messageColor, TextFlags.Center, specialCharacter.Width);
                            }
                        }
                        else
                        {
                            posX -= c.Graphics.MeasureString(splits[i], messageFont);
                            c.DrawText(str, messageFont, posX, posY, messageColor);
                        }
                    }

                    posY += tooltipLineSizeY;
                }

                c.PopClipRegion();
            }
        }
    }
}
