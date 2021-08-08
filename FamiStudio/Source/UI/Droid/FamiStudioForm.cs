using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using Android.Opengl;
using Javax.Microedition.Khronos.Opengles;
using static Android.Views.View;
using Java.Nio;

namespace FamiStudio
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class FamiStudioForm : AppCompatActivity, GLSurfaceView.IRenderer, IOnTouchListener
    {
        public static FamiStudioForm Instance { get; private set; }

        GLSurfaceView glSurfaceView;
        FamiStudio famistudio;

        private FamiStudioControls controls;

        public FamiStudio FamiStudio => famistudio;
        public Toolbar ToolBar => controls.ToolBar;
        public Sequencer Sequencer => controls.Sequencer;
        public PianoRoll PianoRoll => controls.PianoRoll;
        public ProjectExplorer ProjectExplorer => controls.ProjectExplorer;

        public string Text { get; set; }

        public System.Drawing.Rectangle Bounds
        {
            get
            {
                // DROIDTODO : Not sure about this.
                Android.Graphics.Rect rect = new Android.Graphics.Rect();
                glSurfaceView.GetDrawingRect(rect);
                return new System.Drawing.Rectangle(rect.Left, rect.Top, rect.Width(), rect.Height());
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            glSurfaceView = FindViewById<GLSurfaceView>(Resource.Id.surfaceview);
            glSurfaceView.PreserveEGLContextOnPause = true;
#if DEBUG
            glSurfaceView.DebugFlags = DebugFlags.CheckGlError;
#endif
            glSurfaceView.SetEGLContextClientVersion(1);
            glSurfaceView.SetEGLConfigChooser(8, 8, 8, 8, 0, 0);
            glSurfaceView.SetOnTouchListener(this);
            glSurfaceView.SetRenderer(this);

            controls = new FamiStudioControls(this);

            Instance = this;
            famistudio = new FamiStudio();
            famistudio.Initialize(null);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public void OnDrawFrame(IGL10 gl)
        {
            /*
            gl.GlViewport(0, 0, glSurfaceView.Width, glSurfaceView.Height);
            gl.GlClearColor(0.5f, 0.0f, 0.5f, 1.0f);
            gl.GlClear(GLES11.GlColorBufferBit);
            gl.GlDisable((int)2884); // Cull face?
            gl.GlDisable(GLES11.GlDepthTest);
            gl.GlDisable(GLES11.GlStencilTest);
            gl.GlMatrixMode(GLES11.GlProjection);
            gl.GlLoadIdentity();
            gl.GlOrthof(0, glSurfaceView.Width, glSurfaceView.Height, 0, -1, 1);
            gl.GlMatrixMode(GLES11.GlModelview);
            gl.GlLoadIdentity();
            gl.GlColor4f(0.0f, 1.0f, 0.0f, 1.0f);

            var buffer = Java.Nio.ByteBuffer.AllocateDirect(sizeof(float) * 6);
            buffer.Order(ByteOrder.NativeOrder());
            var vertices = buffer.AsFloatBuffer();

            vertices.Put(10.0f);
            vertices.Put(10.0f);
            vertices.Put(500.0f);
            vertices.Put(250.0f);
            vertices.Put(10.0f);
            vertices.Put(200.0f);
            vertices.Position(0);

            gl.GlEnableClientState(GLES11.GlVertexArray);
            gl.GlVertexPointer(2, GLES11.GlFloat, 0, vertices);
            gl.GlDrawArrays(GLES11.GlTriangles, 0, 3);
            gl.GlDisableClientState(GLES11.GlVertexArray);
            */

            controls.Redraw();
        }

        public void OnSurfaceChanged(IGL10 gl, int width, int height)
        {
            controls.Resize(width, height);
        }

        public void OnSurfaceCreated(IGL10 gl, Javax.Microedition.Khronos.Egl.EGLConfig config)
        {
            controls.InitializeGL(gl);
            controls.Resize(glSurfaceView.Width, glSurfaceView.Height);
            
            controls.PianoRoll.StartEditPattern(0, 0); // MATTT
        }

        public bool OnTouch(View v, MotionEvent e)
        {
            return false;
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
