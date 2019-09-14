using Gdk;
using Gtk;
using System;
using System.Collections.Generic;

namespace FamiStudio
{
    public class CheckBoxList : VBox
    {
        List<CheckButton> checkButtons = new List<CheckButton>();

        public CheckBoxList(string[] values, bool[] selected)
        {
            var vbox = new VBox();
            vbox.Show();

            for (int i = 0; i < values.Length; i++)
            {
                var chk = new CheckButton(values[i]);
                chk.Active = selected == null ? true : selected[i];
                chk.Show();
                vbox.PackStart(chk, false, false, 0);
                checkButtons.Add(chk);
            }

            var scroll = new ScrolledWindow();
            scroll.SetPolicy(PolicyType.Never, PolicyType.Automatic);
            scroll.AddWithViewport(vbox);
            scroll.Show();
            scroll.Child.HeightRequest = 200;

            Add(scroll);
        }

        public bool[] GetSelected()
        {
            var selected = new bool[checkButtons.Count];

            for (int i = 0; i < checkButtons.Count; i++)
            {
                selected[i] = checkButtons[i].Active;
            }

            return selected;
        }
    }
}
