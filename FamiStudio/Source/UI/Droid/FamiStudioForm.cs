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
        FamiStudio  famistudio;

        private FamiStudioControls controls;

        public FamiStudio FamiStudio => famistudio;
        public Toolbar ToolBar => controls.ToolBar;
        public Sequencer Sequencer => controls.Sequencer;
        public PianoRoll PianoRoll => controls.PianoRoll;
        public ProjectExplorer ProjectExplorer => controls.ProjectExplorer;
        public string Text { get; set; }

        public FamiStudioForm(FamiStudio famistudio) // DROIDTODO
        {
            this.famistudio = famistudio;

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

        public System.Drawing.Point PointToClient(GLControl ctrl, System.Drawing.Point p)
        {
            return System.Drawing.Point.Empty;
        }

        public System.Drawing.Point PointToScreen(GLControl ctrl, System.Drawing.Point p)
        {
            return System.Drawing.Point.Empty;
        }

        public static bool IsKeyDown(System.Windows.Forms.Keys key)
        {
            return false;
        }

        public void CaptureMouse(GLControl ctrl)
        {
        }

        public void ReleaseMouse()
        {
        }

        public void RefreshLayout()
        {
        }

        public void Refresh()
        {
        }

        public void Invalidate()
        {
        }

        public void RefreshCursor()
        {
        }

        public void Run()
        {
        }
    }
}
