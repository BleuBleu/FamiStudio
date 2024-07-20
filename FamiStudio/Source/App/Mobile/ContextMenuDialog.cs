using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FamiStudio
{
    public class ContextMenuDialog : Dialog
    {
        private TouchScrollContainer scrollContainer;

        public ContextMenuDialog(FamiStudioWindow win, ContextMenuOption[] options) : base(win, "")
        {
            fullscreen = false;

            // MATTT : Review this. How much should be done here vs. OnShowDialog?
            var dialogWidth = Math.Min(window.Width, window.Height);
            var dialogHeight = dialogWidth * 6 / 10;

            var contextMenu = new ContextMenu();
            contextMenu.Initialize(options);
            contextMenu.Visible = true;

            scrollContainer = new TouchScrollContainer();
            AddControl(scrollContainer);

            scrollContainer.AddControl(contextMenu);
            scrollContainer.Move(0, 0, dialogWidth, Math.Min(dialogHeight, contextMenu.Height));
            scrollContainer.VirtualSizeY = contextMenu.Height;
            contextMenu.Resize(dialogWidth, contextMenu.Height);
        }

        protected override void OnShowDialog()
        {
            Resize(scrollContainer.Width, scrollContainer.Height);
        }
    }
}
