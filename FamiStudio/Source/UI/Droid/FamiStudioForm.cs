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
using AndroidX.Core.View;

namespace FamiStudio
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true, ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class FamiStudioForm : AppCompatActivity, GLSurfaceView.IRenderer, /*IOnTouchListener,*/ GestureDetector.IOnGestureListener, ScaleGestureDetector.IOnScaleGestureListener, Choreographer.IFrameCallback
    {
        public static FamiStudioForm Instance { get; private set; }
        public object DialogUserData => dialogUserData;
        public bool IsAsyncDialogInProgress => dialogRequestCode >= 0;

        LinearLayout linearLayout;
        GLSurfaceView glSurfaceView;
        FamiStudio famistudio;

        private bool glThreadIsRunning;
        private int dialogRequestCode = -1;
        private object dialogUserData = null;
        private Action<DialogResult> dialogCallback;
        private object renderLock = new object();
        private FamiStudioControls controls;
        private GLControl captureControl;

        public FamiStudio FamiStudio => famistudio;
        public Toolbar ToolBar => controls.ToolBar;
        public Sequencer Sequencer => controls.Sequencer;
        public PianoRoll PianoRoll => controls.PianoRoll;
        public ProjectExplorer ProjectExplorer => controls.ProjectExplorer;
        public System.Drawing.Size Size => new System.Drawing.Size(glSurfaceView.Width, glSurfaceView.Height);
        public bool IsLandscape => glSurfaceView.Width > glSurfaceView.Height;
        public string Text { get; set; }
        public long lastFrameTime = -1;

        private GestureDetectorCompat detector;
        private ScaleGestureDetector scaleDetector;

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
#if DEBUG
            Debug.Listeners.Add(new DebuggerBreakListener());
#endif

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
            //glSurfaceView.DebugFlags = DebugFlags.CheckGlError;
            glSurfaceView.DebugFlags = DebugFlags.CheckGlError;
#endif
            glSurfaceView.SetEGLContextClientVersion(1);
            glSurfaceView.SetEGLConfigChooser(8, 8, 8, 8, 0, 0);
            glSurfaceView.SetRenderer(this);
            glSurfaceView.RenderMode = Rendermode.WhenDirty;
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

            detector = new GestureDetectorCompat(this, this);
            detector.IsLongpressEnabled = true; // MATTT
            scaleDetector = new ScaleGestureDetector(this, this);
            scaleDetector.QuickScaleEnabled = true;

            Choreographer.Instance.PostFrameCallback(this);
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
                Choreographer.Instance.PostFrameCallback(this);
                glSurfaceView.OnResume();
                lock (renderLock) { }; // Extra safety.
                glThreadIsRunning = true;
            }
        }

        private void StopGLThread()
        {
            if (glThreadIsRunning)
            {
                Choreographer.Instance.RemoveFrameCallback(this);
                glSurfaceView.OnPause();
                lock (renderLock) { }; // Extra safety.
                glThreadIsRunning = false;
            }
        }

        // For debugging property pages.
        private void DialogTest()
        {
#if DEBUG
            var dlg = new PropertyDialog(200);

            dlg.Properties.AddTextBox("TextBox", "Hello1", 0, "This is a long tooltip explaining what this property is all about");
            dlg.Properties.AddColorPicker(System.Drawing.Color.Pink);
            dlg.Properties.AddButton("Hey", "This is a button", "Button tooltip");
            dlg.Properties.AddNumericUpDown("Integer", 10, 2, 50, "Integer Tooltip");
            dlg.Properties.AddDropDownList("Hey", new[] { "Option1 QQQ", "Option2 QQQ", "Option3 QQQ", "Option4 QQQ" }, "Option3 QQQ", "Dropdown tooltip");
            dlg.Properties.BeginAdvancedProperties();
            dlg.Properties.AddCheckBoxList("Check box list", new[] { "Check1", "Check2", "Check3", "Check4" }, new[] { false, true, true, false });
            dlg.Properties.AddCheckBox("CheckBox1", true, "Checkbox tooltip!");
            dlg.Properties.AddSlider("Slider", 50, 0, 100, 1.0f, 2, "Allo {0} XXX", "Tooltip for slider");

            dlg.ShowDialog(this, (r) =>
            {
                if (r == DialogResult.OK)
                {
                    Debug.WriteLine("Hello!");
                }
            });
#endif
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
            Debug.Assert(
                (dialogRequestCode == PropertyDialog.RequestCode      && dialogUserData is PropertyDialog) ||
                (dialogRequestCode == MultiPropertyDialog.RequestCode && dialogUserData is MultiPropertyDialog));

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

        public void DoFrame(long frameTimeNanos)
        {
            if (lastFrameTime < 0)
                lastFrameTime = frameTimeNanos;

            float deltaTime = (float)((frameTimeNanos - lastFrameTime) / 1000000000.0);

            if (deltaTime > 0.03)
                Console.WriteLine($"FRAME SKIP!!!!!!!!!!!!!!!!!!!!!! {deltaTime}");

            lock (renderLock)
            {
                famistudio.Tick(deltaTime);
                controls.Tick(deltaTime);
            }

            if (controls.NeedsRedraw())
                glSurfaceView.RequestRender();

            Choreographer.Instance.PostFrameCallback(this);
            lastFrameTime = frameTimeNanos;
        }

        // GL thread.
        public void OnDrawFrame(IGL10 gl)
        {
            lock (renderLock)
            {
                Debug.Assert(!IsAsyncDialogInProgress);
                controls.Redraw();
            }
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
            controls.InitializeGL();

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
            Debug.Assert(captureControl == null);
            captureControl = ctrl;
        }

        public void ReleaseMouse()
        {
            captureControl = null;
        }

        public void RefreshLayout()
        {
        }

        public void MarkDirty()
        {
            controls.MarkDirty();
        }

        public void Refresh()
        {
        }

        public void RefreshCursor()
        {
        }

        public void Run()
        {
        }

        int c = 0;

        private GLControl GetCapturedControlAtCoord(int formX, int formY, out int ctrlX, out int ctrlY)
        {
            if (captureControl != null)
            {
                ctrlX = formX - captureControl.Left;
                ctrlY = formY - captureControl.Top;
                return captureControl;
            }
            else
            {
                return controls.GetControlAtCoord(formX, formY, out ctrlX, out ctrlY);
            }
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            //if (detector.OnTouchEvent(e))
            //    return true;
            //if (scaleDetector.OnTouchEvent(e))
            //    return true;

            if (e.Action == MotionEventActions.Up)
            {
                Debug.WriteLine($"{c++} Up {e.PointerCount} ({e.GetX()}, {e.GetY()})");
                lock (renderLock)
                    GetCapturedControlAtCoord((int)e.GetX(), (int)e.GetY(), out var x, out var y)?.TouchUp(x, y);
            }
            else if (e.Action == MotionEventActions.Move && !scaleDetector.IsInProgress)
            {
                Debug.WriteLine($"{c++} Move {e.PointerCount} ({e.GetX()}, {e.GetY()})");
                lock (renderLock)
                    GetCapturedControlAtCoord((int)e.GetX(), (int)e.GetY(), out var x, out var y)?.TouchMove(x, y);
            }

            detector.OnTouchEvent(e);
            scaleDetector.OnTouchEvent(e);

            return base.OnTouchEvent(e);
        }

        public bool OnDown(MotionEvent e)
        {
            // DialogTest();

            Debug.WriteLine($"{c++} OnDown {e.PointerCount} ({e.GetX()}, {e.GetY()})");
            lock (renderLock)
                GetCapturedControlAtCoord((int)e.GetX(), (int)e.GetY(), out var x, out var y)?.TouchDown(x, y);
            return false;
        }

        public bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
        {
            Debug.WriteLine($"{c++} OnFling {e1.PointerCount} ({e1.GetX()}, {e1.GetY()}) ({e2.GetX()}, {e2.GetY()})");
            lock (renderLock)
                GetCapturedControlAtCoord((int)e1.GetX(), (int)e1.GetY(), out var x, out var y)?.TouchFling(x, y, velocityX, velocityY);
            return false;
        }

        public void OnLongPress(MotionEvent e)
        {
            Debug.WriteLine($"{c++} OnLongPress {e.PointerCount} ({e.GetX()}, {e.GetY()})");
            lock (renderLock)
                GetCapturedControlAtCoord((int)e.GetX(), (int)e.GetY(), out var x, out var y)?.TouchClick(x, y, true);
        }

        public bool OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY)
        {
            return false;
        }

        public void OnShowPress(MotionEvent e)
        {
            Debug.WriteLine($"{c++} {e.PointerCount} OnShowPress ({e.GetX()}, {e.GetY()})");
        }

        public bool OnSingleTapUp(MotionEvent e)
        {
            Debug.WriteLine($"{c++} {e.PointerCount} OnSingleTapUp ({e.GetX()}, {e.GetY()})");
            lock (renderLock)
                GetCapturedControlAtCoord((int)e.GetX(), (int)e.GetY(), out var x, out var y)?.TouchClick(x, y, false);
            return false;
        }

        public bool OnScale(ScaleGestureDetector detector)
        {
            Debug.WriteLine($"{c++} OnScale ({detector.FocusX}, {detector.FocusY}) {detector.ScaleFactor}");
            lock (renderLock)
                GetCapturedControlAtCoord((int)detector.FocusX, (int)detector.FocusY, out var x, out var y)?.TouchScale(x, y, detector.ScaleFactor, TouchScalePhase.Scale);
            return true;
        }

        public bool OnScaleBegin(ScaleGestureDetector detector)
        {
            Debug.WriteLine($"{c++} OnScaleBegin ({detector.FocusX}, {detector.FocusY})");
            lock (renderLock)
                GetCapturedControlAtCoord((int)detector.FocusX, (int)detector.FocusY, out var x, out var y)?.TouchScale(x, y, detector.ScaleFactor, TouchScalePhase.Begin);
            return true;
        }

        public void OnScaleEnd(ScaleGestureDetector detector)
        {
            Debug.WriteLine($"{c++} OnScaleEnd ({detector.FocusX}, {detector.FocusY})");
            lock (renderLock)
                GetCapturedControlAtCoord((int)detector.FocusX, (int)detector.FocusY, out var x, out var y)?.TouchScale(x, y, detector.ScaleFactor, TouchScalePhase.End);
        }
    }

#if DEBUG
    // By default Debug.Assert() doesnt break in the debugger on Android. This does that.
    public class DebuggerBreakListener : System.Diagnostics.TraceListener
    {
        bool breakOnFail = true;

        public override void Write(string message)
        {
        }

        public override void WriteLine(string message)
        {
        }

        public override void Fail(string message, string detailMessage)
        {
            base.Fail(message, detailMessage);
            
            if (breakOnFail)
                System.Diagnostics.Debugger.Break();
        }
    }
#endif
}
