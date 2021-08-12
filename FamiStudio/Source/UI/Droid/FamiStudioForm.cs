using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using Android.Opengl;
using Javax.Microedition.Khronos.Opengles;
using static Android.Views.View;
using Android.Content.Res;

using RenderTheme = FamiStudio.GLTheme;
using static Android.Views.Choreographer;
using System.IO;
using System.Reflection;

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

            //Settings.Load(); // DROIDTODO : Settings.
            Utils.Initialize();
            PlatformUtils.Initialize();
            RenderTheme.Initialize();
            NesApu.InitializeNoteTables();

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

            var filename = Path.Combine(Path.GetTempPath(), "Silius.fms");

            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("FamiStudio.Silius.fms"))
            {
                var buffer = new byte[(int)s.Length];
                s.Read(buffer, 0, (int)s.Length);
                File.WriteAllBytes(filename, buffer);
            }

            Instance = this;
            famistudio = new FamiStudio();
            famistudio.Initialize(filename);
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

        private void Test()
        {
            famistudio.Tick();
        }

        public void OnDrawFrame(IGL10 gl)
        {
            RunOnUiThread(Test);
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
