using System;
using System.IO;
using System.Reflection;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;
using Android.Opengl;
using Javax.Microedition.Khronos.Opengles;
using Android.Content.Res;
using static Android.Views.View;
using Android.Content;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using Debug = System.Diagnostics.Debug;
using DialogResult = System.Windows.Forms.DialogResult;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Android.Widget;

namespace FamiStudio
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true, ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class FamiStudioForm : AppCompatActivity, GLSurfaceView.IRenderer, IOnTouchListener
    {
        public static FamiStudioForm Instance { get; private set; }
        public object DialogUserData => dialogUserData;

        LinearLayout linearLayout;
        GLSurfaceView glSurfaceView;
        FamiStudio famistudio;

        private bool glThreadIsRunning;
        private int dialogRequestCode = -1;
        private object dialogUserData = null;
        private Action<DialogResult> dialogCallback;
        private object renderLock = new object();
        private FamiStudioControls controls;

        public FamiStudio FamiStudio => famistudio;
        public Toolbar ToolBar => controls.ToolBar;
        public Sequencer Sequencer => controls.Sequencer;
        public PianoRoll PianoRoll => controls.PianoRoll;
        public ProjectExplorer ProjectExplorer => controls.ProjectExplorer;
        public System.Drawing.Size Size => new System.Drawing.Size(glSurfaceView.Width, glSurfaceView.Height);
        public bool IsLandscape => glSurfaceView.Width > glSurfaceView.Height;
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

        private void EnableFullscreenMode()
        {
            // Fullscreen mode.
            Window.AddFlags(WindowManagerFlags.Fullscreen);

            int uiOptions = (int)Window.DecorView.SystemUiVisibility;

            uiOptions |= (int)SystemUiFlags.LowProfile;
            uiOptions |= (int)SystemUiFlags.Fullscreen;
            uiOptions |= (int)SystemUiFlags.HideNavigation;
            uiOptions |= (int)SystemUiFlags.ImmersiveSticky;

            Window.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            EnableFullscreenMode();

            // DROIDTODO : Move this to a function!
            //Settings.Load(); // DROIDTODO : Settings.
            DpiScaling.Initialize();
            Utils.Initialize();
            PlatformUtils.Initialize();
            global::FamiStudio.Theme.Initialize();
            NesApu.InitializeNoteTables();

            glSurfaceView = new GLSurfaceView(this);
            glSurfaceView.PreserveEGLContextOnPause = true;
#if DEBUG
            glSurfaceView.DebugFlags = DebugFlags.CheckGlError;
#endif
            glSurfaceView.SetEGLContextClientVersion(1);
            glSurfaceView.SetEGLConfigChooser(8, 8, 8, 8, 0, 0);
            glSurfaceView.SetOnTouchListener(this);
            glSurfaceView.SetRenderer(this);
            glThreadIsRunning = true;

            linearLayout = new LinearLayout(this);
            linearLayout.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            linearLayout.AddView(glSurfaceView);

            SetContentView(linearLayout);

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

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void StartFileActivity(Action<DialogResult> callback)
        {
            //dialogRequestCode = FILE_RESULT_CODE;
            //dialogCallback = FileActivityCallback;

            //Intent chooseFile = new Intent(Intent.ActionGetContent);
            //chooseFile.AddCategory(Intent.CategoryOpenable);
            //chooseFile.SetType("text/plain");
            //StartActivityForResult(Intent.CreateChooser(chooseFile, "Choose a file"), FILE_RESULT_CODE);
        }

        public void StartDialogActivity(Type type, int resultCode, Action<DialogResult> callback, object userData)
        {
            // No support for nested dialog at the moment.
            Debug.Assert(dialogCallback == null && dialogRequestCode == -1 && dialogUserData == null);

            dialogRequestCode = resultCode;
            dialogCallback = callback;
            dialogUserData = userData;

            StopGLThread();
            StartActivityForResult(new Intent(this, type), resultCode);
        }

        private void StartGLThread()
        {
            if (!glThreadIsRunning)
            {
                glSurfaceView.OnResume();
                lock (renderLock) { }; // Extra safety.
                glThreadIsRunning = true;
            }
        }

        private void StopGLThread()
        {
            if (glThreadIsRunning)
            {
                glSurfaceView.OnPause();
                lock (renderLock) { }; // Extra safety.
                glThreadIsRunning = false;
            }
        }

        int idx = 0;

        // Main thread.
        public bool OnTouch(View v, MotionEvent e)
        {
            if (e.Action == MotionEventActions.Down)
            {
                var dlg = new PropertyDialog();

                dlg.Properties.AddTextBox("TextBox", "Hello1", 0, "This is a long tooltip explaining what this property is all about");
                dlg.Properties.AddButton("Hey", "This is a button");
                dlg.Properties.AddCheckBoxList("Check box list", new[] { "Check1", "Check2", "Check3", "Check4" }, new[] { false, true, true, false });
                dlg.Properties.BeginAdvancedProperties();
                dlg.Properties.AddColorPicker(System.Drawing.Color.Pink);
                dlg.Properties.AddCheckBox("CheckBox1", true);
                dlg.Properties.AddSlider("Slider", 50, 0, 100, 1.0f, 2, "Allo {0} XXX");

                dlg.ShowDialog((r) =>
                {
                    if (r == DialogResult.OK)
                    {
                        Debug.WriteLine("Hello!");
                    }
                });
            }

            return false;
        }

        private DialogResult ToWinFormsResult([GeneratedEnum] Result resultCode)
        {
            if (resultCode == Result.Ok)
                return DialogResult.OK;
            else
                return DialogResult.Cancel;
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            Debug.Assert(dialogRequestCode == requestCode);
            var callback = dialogCallback;

            dialogRequestCode = -1;
            dialogCallback = null;
            dialogUserData = null;

            callback(ToWinFormsResult(resultCode));

            // If not more dialog are needed, restart GL thread.
            if (dialogCallback == null)
                StartGLThread();

            base.OnActivityResult(requestCode, resultCode, data);
        }

        private void Tick()
        {
            famistudio.Tick();
        }

        // GL thread.
        public void OnDrawFrame(IGL10 gl)
        {
            RunOnUiThread(() => { Tick(); });

            lock (renderLock)
                controls.Redraw();
        }

        // GL thread.
        public void OnSurfaceChanged(IGL10 gl, int width, int height)
        {
            controls.Resize(width, height);
        }

        // GL thread.
        public void OnSurfaceCreated(IGL10 gl, Javax.Microedition.Khronos.Egl.EGLConfig config)
        {
            controls.Resize(glSurfaceView.Width, glSurfaceView.Height);
            controls.InitializeGL(gl);

            controls.PianoRoll.StartEditPattern(0, 0);
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
