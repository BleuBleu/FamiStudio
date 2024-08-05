using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FamiStudio
{
    public class DropDownOptionsDialog : TopBarDialog
    {
        private TouchScrollContainer scrollContainer;
        private int selectedIndex = -1;

        public int SelectedIndex => selectedIndex;

        public DropDownOptionsDialog(FamiStudioWindow win, string title, string[] items, int idx) : base(win, title, false)
        {
            selectedIndex = idx;

            var buttonHeight = DpiScaling.ScaleForWindow((int)Utils.Lerp(20, 14, Utils.Saturate((items.Length - 4) / 20.0f)));
            var virtualSizeY = buttonHeight * items.Length;

            scrollContainer = new TouchScrollContainer();
            scrollContainer.Move(clientRect.Left, clientRect.Top, clientRect.Width, Math.Min(virtualSizeY, clientRect.Height));
            scrollContainer.VirtualSizeY = virtualSizeY;
            scrollContainer.ScrollY = (int)Math.Round((selectedIndex + 0.5f) * buttonHeight - scrollContainer.Height * 0.5f);
            scrollContainer.ClampScroll();
            AddControl(scrollContainer);

            for (var i = 0; i < items.Length; i++)
            {
                var j = i; // Important, local copy for lambda below.
                var button = new Button(selectedIndex == i ? "RadioButtonOn" : "RadioButtonOff", items[i]);
                button.UserData = i;
                button.ImageScale = DpiScaling.Window * 0.25f;
                button.Transparent = true;
                button.Click += (s) =>
                {
                    (scrollContainer.GetControl(selectedIndex) as Button).ImageName = "RadioButtonOff";
                    selectedIndex = j;
                    (scrollContainer.GetControl(selectedIndex) as Button).ImageName = "RadioButtonOn";
                    Close(DialogResult.OK);
                };
                button.Move(0, i * buttonHeight, scrollContainer.Width, buttonHeight);
                scrollContainer.AddControl(button);
            }
        }

        public override void OnWindowResize(EventArgs e)
        {
            Close(DialogResult.Cancel);
        }

        protected override void OnShowDialog()
        {
            Resize(scrollContainer.Width, topBarHeight + scrollContainer.Height);
            base.OnShowDialog();
        }
    }
}
