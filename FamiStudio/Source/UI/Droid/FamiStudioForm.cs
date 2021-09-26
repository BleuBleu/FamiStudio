using System;
using System.IO;
using System.Reflection;
using Android.Widget;
using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Opengl;
using Android.Runtime;
using Android.Util;
using Android.Views;
using AndroidX.Core.View;
using AndroidX.AppCompat.App;
using Javax.Microedition.Khronos.Opengles;
using Google.Android.Material.BottomSheet;

using Debug        = System.Diagnostics.Debug;
using DialogResult = System.Windows.Forms.DialogResult;

namespace FamiStudio
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true, ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize)]
    public class FamiStudioForm : AppCompatActivity, GLSurfaceView.IRenderer, GestureDetector.IOnGestureListener, ScaleGestureDetector.IOnScaleGestureListener, Choreographer.IFrameCallback
    {
        private LinearLayout linearLayout;
        private GLSurfaceView glSurfaceView;

        private FamiStudio famistudio;
        private FamiStudioControls controls;

        // For context menus.
        BottomSheetDialog contextMenuDialog;

        // For property or multi-property dialogs.
        private int dialogRequestCode = -1;
        private bool glThreadIsRunning;
        private bool glStopRequested;
        private bool glStartRequested;
        private long lastFrameTime = -1;
        private object dialogUserData = null;
        private object renderLock = new object();
        private Action<DialogResult> dialogCallback;
        private GLControl captureControl;

        public static FamiStudioForm Instance { get; private set; }
        public object DialogUserData => dialogUserData;
        public bool IsAsyncDialogInProgress => dialogCallback != null || contextMenuDialog != null; // DROIDTODO : Add lots of validation with that.

        public FamiStudio      FamiStudio      => famistudio;
        public Toolbar         ToolBar         => controls.ToolBar;
        public Sequencer       Sequencer       => controls.Sequencer;
        public PianoRoll       PianoRoll       => controls.PianoRoll;
        public ProjectExplorer ProjectExplorer => controls.ProjectExplorer;
        public QuickAccessBar  QuickAccessBar  => controls.QuickAccessBar;
        public GLControl       ActiveControl   => controls.ActiveControl;

        public System.Drawing.Size Size => new System.Drawing.Size(glSurfaceView.Width, glSurfaceView.Height);
        public bool IsLandscape => glSurfaceView.Width > glSurfaceView.Height;
        public string Text { get; set; }

        private GestureDetectorCompat detector;
        private ScaleGestureDetector  scaleDetector;

        private BottomSheetDialog bottomSheetDialog;

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

        public void SetActiveControl(GLControl ctrl, bool animate = true)
        {
            controls.SetActiveControl(ctrl, animate);
        }

        private void EnableFullscreenMode(Window win)
        {
            // Fullscreen mode.
            win.AddFlags(WindowManagerFlags.Fullscreen);

            int uiOptions = (int)win.DecorView.SystemUiVisibility;

            uiOptions |= (int)SystemUiFlags.LowProfile;
            uiOptions |= (int)SystemUiFlags.Fullscreen;
            uiOptions |= (int)SystemUiFlags.HideNavigation;
            uiOptions |= (int)SystemUiFlags.ImmersiveSticky;

            win.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
#if DEBUG
            Debug.Listeners.Add(new DebuggerBreakListener());
#endif

            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            EnableFullscreenMode(Window);

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
            glSurfaceView.SetRenderer(this);
            glSurfaceView.RenderMode = Rendermode.WhenDirty;
            glThreadIsRunning = true;

            linearLayout = new LinearLayout(this);
            linearLayout.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            linearLayout.AddView(glSurfaceView);

            SetContentView(linearLayout);

            controls = new FamiStudioControls(this);

            Instance = this;
            famistudio = new FamiStudio();
            famistudio.Initialize(null);

            detector = new GestureDetectorCompat(this, this);
            detector.IsLongpressEnabled = true;
            scaleDetector = new ScaleGestureDetector(this, this);
            scaleDetector.QuickScaleEnabled = false;

            Choreographer.Instance.PostFrameCallback(this);
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            EnableFullscreenMode(Window);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void StartFileActivity()
        {
            //dialogRequestCode = FILE_RESULT_CODE;
            //dialogCallback = FileActivityCallback;

            //Intent chooseFile = new Intent(Intent.ActionGetContent);
            //chooseFile.AddCategory(Intent.CategoryOpenable);
            //chooseFile.SetType("text/plain");
            //StartActivityForResult(Intent.CreateChooser(chooseFile, "Choose a file"), FILE_RESULT_CODE);

            //Xamarin.Essentials.FilePicker.PickAsync();
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
            Debug.Assert(!glStartRequested && !glStopRequested);
            glStartRequested = true;
        }

        private void StopGLThread()
        {
            Debug.Assert(!glStartRequested && !glStopRequested);
            glStopRequested = true;
        }

        // For debugging property pages.
        private void DialogTest()
        {
#if DEBUG
            var dlg = new PropertyDialog("Test Dialog", 200);

            dlg.Properties.AddTextBox("TextBox", "Hello1", 0, "This is a long tooltip explaining what this property is all about");
            dlg.Properties.AddColorPicker(System.Drawing.Color.Pink);
            dlg.Properties.AddButton("Hey", "This is a button", "Button tooltip");
            dlg.Properties.AddNumericUpDown("Integer", 10, 2, 50, "Integer Tooltip");
            dlg.Properties.AddDropDownList("Hey", new[] { "Option1 QQQ", "Option2 QQQ", "Option3 QQQ", "Option4 QQQ" }, "Option3 QQQ", "Dropdown tooltip");
            dlg.Properties.AddRadioButton("This is a radio", "Radio 123", false);
            dlg.Properties.AddRadioButton("This is a radio", "Radio 435", true);
            dlg.Properties.AddRadioButton("This is a radio", "Radio 888", false);
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

            if (glStartRequested || glStopRequested)
            {
                Debug.Assert(glStartRequested ^ glStopRequested);

                Choreographer.Instance.RemoveFrameCallback(this);

                if (glStartRequested)
                    glSurfaceView.OnResume();
                else
                    glSurfaceView.OnPause();

                lock (renderLock) { }; // Extra safety.

                glThreadIsRunning = glStartRequested;

                if (glThreadIsRunning)
                    MarkDirty();

                glStartRequested = false;
                glStopRequested  = false;
            }

            if (glThreadIsRunning)
            {
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
            }

            Choreographer.Instance.PostFrameCallback(this);
            lastFrameTime = frameTimeNanos;
        }

        // GL thread.
        public void OnDrawFrame(IGL10 gl)
        {
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

        public void ShowContextMenu(ContextMenuOption[] options)
        {
            Debug.Assert(contextMenuDialog == null);

            var bgColor = DroidUtils.ToAndroidColor(global::FamiStudio.Theme.DarkGreyFillColor1);

            var linearLayout = new LinearLayout(this);
            linearLayout.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            linearLayout.Orientation = Orientation.Vertical;
            linearLayout.SetBackgroundColor(bgColor);

            var imagePad  = DroidUtils.DpToPixels(2);
            var imageSize = DroidUtils.DpToPixels(32);

            for (int i = 0; i < options.Length; i++)
            {
                var opt = options[i];

                var bmp = new BitmapDrawable(Resources, PlatformUtils.LoadBitmapFromResource($"FamiStudio.Resources.{opt.Image}.png", true)); // $"FamiStudio.Resources.{opt.Image}.png"
                bmp.SetBounds(0, 0, imageSize, imageSize);

                var textView = new TextView(new ContextThemeWrapper(this, Resource.Style.LightGrayTextMedium));
                textView.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
                textView.SetCompoundDrawables(bmp, null, null, null);
                textView.CompoundDrawablePadding = imagePad;
                textView.SetPadding(imagePad, imagePad, imagePad, imagePad);
                textView.Gravity = GravityFlags.CenterVertical;
                textView.Text = opt.Text;
                textView.Tag = new ContextMenuTag(opt);
                textView.Click += ContextMenuDialog_Click;

                linearLayout.AddView(textView);
            }

            DisplayMetrics metrics = new DisplayMetrics();
            Window.WindowManager.DefaultDisplay.GetMetrics(metrics);

            contextMenuDialog = new BottomSheetDialog(this);
            EnableFullscreenMode(contextMenuDialog.Window);
            contextMenuDialog.Window.AddFlags(WindowManagerFlags.NotFocusable); // Prevents nav bar from appearing.
            contextMenuDialog.SetContentView(linearLayout);
            contextMenuDialog.Behavior.MaxWidth = Math.Min(metrics.HeightPixels, metrics.WidthPixels);
            contextMenuDialog.Behavior.State = BottomSheetBehavior.StateExpanded;
            contextMenuDialog.DismissEvent += ContextMenuDialog_DismissEvent;

            // In portrait mode, add a bit of padding to cover the navigation bar.
            if (metrics.HeightPixels != glSurfaceView.Height)
            {
                // DROIDTODO : When using gesture navigation, in landscape more, we draw a full-width bar.
                // Needs to match the dialog.
                GradientDrawable invisible = new GradientDrawable();
                GradientDrawable navBar = new GradientDrawable();
                navBar.SetColor(bgColor);

                LayerDrawable windowBackground = new LayerDrawable(new Drawable[] { invisible, navBar });
                windowBackground.SetLayerInsetTop(1, metrics.HeightPixels);
                contextMenuDialog.Window.SetBackgroundDrawable(windowBackground);
            }

            contextMenuDialog.Show();

            PlatformUtils.VibrateClick();
        }

        private void ContextMenuDialog_DismissEvent(object sender, EventArgs e)
        {
            contextMenuDialog = null;
            MarkDirty();
        }

        private void ContextMenuDialog_Click(object sender, EventArgs e)
        {
            var tag = (sender as TextView).Tag as ContextMenuTag;

            tag.opt.Callback();
            contextMenuDialog.Dismiss();
            MarkDirty();

            PlatformUtils.VibrateTick();
        }

        int c = 0;

        private GLControl GetCapturedControlAtCoord(int formX, int formY, out int ctrlX, out int ctrlY)
        {
            if (captureControl != null)
            {
                Debug.Assert(controls.CanAcceptInput);

                ctrlX = formX - captureControl.Left;
                ctrlY = formY - captureControl.Top;
                return captureControl;
            }
            else if (controls.CanAcceptInput)
            {
                return controls.GetControlAtCoord(formX, formY, out ctrlX, out ctrlY);
            }
            else
            {
                ctrlX = 0;
                ctrlY = 0;
                return null;
            }
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (!IsAsyncDialogInProgress)
            {
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
            else
            {
                return false;
            }
        }

        public bool OnDown(MotionEvent e)
        {
            if (!IsAsyncDialogInProgress)
            {
                Debug.WriteLine($"{c++} OnDown {e.PointerCount} ({e.GetX()}, {e.GetY()})");
                lock (renderLock)
                    GetCapturedControlAtCoord((int)e.GetX(), (int)e.GetY(), out var x, out var y)?.TouchDown(x, y);
            }
            return false;
        }

        public bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
        {
            if (!IsAsyncDialogInProgress)
            {

                Debug.WriteLine($"{c++} OnFling {e1.PointerCount} ({e1.GetX()}, {e1.GetY()}) ({velocityX}, {velocityY})");
                lock (renderLock)
                    GetCapturedControlAtCoord((int)e1.GetX(), (int)e1.GetY(), out var x, out var y)?.TouchFling(x, y, velocityX, velocityY);
            }
            return false;
        }

        public void OnLongPress(MotionEvent e)
        {
            if (!IsAsyncDialogInProgress)
            {
                Debug.WriteLine($"{c++} OnLongPress {e.PointerCount} ({e.GetX()}, {e.GetY()})");
                lock (renderLock)
                    GetCapturedControlAtCoord((int)e.GetX(), (int)e.GetY(), out var x, out var y)?.TouchLongPress(x, y);
            }
        }

        public bool OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY)
        {
            return false;
        }

        public void OnShowPress(MotionEvent e)
        {
            if (!IsAsyncDialogInProgress)
            {
                Debug.WriteLine($"{c++} {e.PointerCount} OnShowPress ({e.GetX()}, {e.GetY()})");
            }
        }

        public bool OnSingleTapUp(MotionEvent e)
        {
            if (!IsAsyncDialogInProgress)
            {
                //DialogTest();
                //StartFileActivity();

                Debug.WriteLine($"{c++} {e.PointerCount} OnSingleTapUp ({e.GetX()}, {e.GetY()})");
                lock (renderLock)
                    GetCapturedControlAtCoord((int)e.GetX(), (int)e.GetY(), out var x, out var y)?.TouchClick(x, y);
            }
            return false;
        }

        public bool OnScale(ScaleGestureDetector detector)
        {
            if (!IsAsyncDialogInProgress)
            {
                Debug.WriteLine($"{c++} OnScale ({detector.FocusX}, {detector.FocusY}) {detector.ScaleFactor}");
                lock (renderLock)
                    GetCapturedControlAtCoord((int)detector.FocusX, (int)detector.FocusY, out var x, out var y)?.TouchScale(x, y, detector.ScaleFactor, TouchScalePhase.Scale);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool OnScaleBegin(ScaleGestureDetector detector)
        {
            if (!IsAsyncDialogInProgress)
            {
                Debug.WriteLine($"{c++} OnScaleBegin ({detector.FocusX}, {detector.FocusY})");
                lock (renderLock)
                    GetCapturedControlAtCoord((int)detector.FocusX, (int)detector.FocusY, out var x, out var y)?.TouchScale(x, y, detector.ScaleFactor, TouchScalePhase.Begin);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void OnScaleEnd(ScaleGestureDetector detector)
        {
            if (!IsAsyncDialogInProgress)
            {
                Debug.WriteLine($"{c++} OnScaleEnd ({detector.FocusX}, {detector.FocusY})");
                lock (renderLock)
                    GetCapturedControlAtCoord((int)detector.FocusX, (int)detector.FocusY, out var x, out var y)?.TouchScale(x, y, detector.ScaleFactor, TouchScalePhase.End);
            }
        }

        private class ContextMenuTag : Java.Lang.Object
        {
            public ContextMenuOption opt;
            public ContextMenuTag(ContextMenuOption o)
            {
                opt = o;
            }
        };
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
