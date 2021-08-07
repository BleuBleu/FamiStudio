using OpenTK.Graphics.ES30;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace FamiStudio
{
    public partial class FamiStudioForm : ContentPage
    {
        OpenGLView  glView;
        StackLayout stackLayout;

        public FamiStudioForm()
        {
            glView = new OpenGLView();
            glView.HasRenderLoop = true;
            glView.WidthRequest  = 300;
            glView.HeightRequest = 200;
            glView.OnDisplay = OnRender;

            stackLayout = new StackLayout();
            stackLayout.Children.Add(glView);

            Content = stackLayout;
        }

        void OnRender(Rectangle rect)
        {
            GL.ClearColor(1.0f, 0.0f, 0.5f, 1.0f); 
            GL.Clear((ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
        }

        public System.Windows.Forms.Keys GetModifierKeys()
        {
            return System.Windows.Forms.Keys.None;
        }

        public System.Drawing.Point GetCursorPosition()
        {
            return System.Drawing.Point.Empty;
        }

        public System.Drawing.Point PointToClient(System.Drawing.Point p)
        {
            return System.Drawing.Point.Empty;
        }

        public System.Drawing.Point PointToScreen(System.Drawing.Point p)
        {
            return System.Drawing.Point.Empty;
        }

        public bool IsKeyDown(System.Windows.Forms.Keys key)
        {
            return false;
        }

        public void RefreshCursor()
        {
        }
    }
}
