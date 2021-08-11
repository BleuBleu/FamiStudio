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
using Android.Content.Res;

namespace FamiStudio
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true, ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
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

        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
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

            var i0 = famistudio.Project.CreateInstrument(0, "i0");
            var i1 = famistudio.Project.CreateInstrument(0, "i1");
            var p0 = famistudio.Song.Channels[0].CreatePatternAndInstance(0, "toto1");

            p0.GetOrCreateNoteAt(10).Value = 52;
            p0.GetOrCreateNoteAt(10).Instrument = i0;
            p0.GetOrCreateNoteAt(10).Duration = 20;
            p0.GetOrCreateNoteAt(50).Value = 20;
            p0.GetOrCreateNoteAt(50).Instrument = i1;
            p0.GetOrCreateNoteAt(50).Duration = 20;
            p0.GetOrCreateNoteAt(70).Value = 60;
            p0.GetOrCreateNoteAt(70).Instrument = i0;
            p0.GetOrCreateNoteAt(70).Duration = 20;
            p0.GetOrCreateNoteAt(90).Value = 30;
            p0.GetOrCreateNoteAt(90).Instrument = i1;
            p0.GetOrCreateNoteAt(90).Duration = 10;
            p0.GetOrCreateNoteAt(110).Value = 40;
            p0.GetOrCreateNoteAt(110).Instrument = i1;
            p0.GetOrCreateNoteAt(110).Duration = 40;

            famistudio.Song.Channels[0].PatternInstances[3] = p0;
            famistudio.Song.Channels[0].PatternInstances[5] = p0;

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
