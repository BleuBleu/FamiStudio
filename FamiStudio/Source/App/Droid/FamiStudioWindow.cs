﻿using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Opengl;
using Android.OS;
using Android.Renderscripts;
using Android.Runtime;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Core.Content;
using AndroidX.Core.Graphics;
using AndroidX.Core.View;
using Javax.Microedition.Khronos.Opengles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Xamarin.Essentials;
using static Android.Views.View;
using static Android.Views.ViewGroup;

namespace FamiStudio
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true, ResizeableActivity = false, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.UiMode | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden)]
    public class FamiStudioWindow : 
        AppCompatActivity, 
        GLSurfaceView.IRenderer, 
        GestureDetector.IOnGestureListener,
        GestureDetector.IOnDoubleTapListener, 
        ScaleGestureDetector.IOnScaleGestureListener, 
        Choreographer.IFrameCallback,
        AndroidX.Core.View.IOnApplyWindowInsetsListener,
        IOnSystemUiVisibilityChangeListener
    {
        private LinearLayout linearLayout;
        private GLSurfaceView glSurfaceView;

        private FamiStudio famistudio;
        private FamiStudioContainer container;
        private Graphics graphics;
        private Fonts fonts;
        private bool dirty = true;

        // For property or multi-property dialogs.
        private bool appWasAlreadyRunning;
        private bool glThreadIsRunning;
        private static bool activityRunning;
        private long lastFrameTime = -1;
        private object renderLock = new object();
        private BaseFileActivity currentFileActivity;
        private BaseFileActivity pendingFinishFileActivity;
        private int captureCookie;
        private Control captureControl;
        private ModifierKeys modifiers = new ModifierKeys();
        private Rectangle cachedViewRect;
        private Point glViewPosition;

        private long doubleTapStartTime;
        private Point doubleTapLocation;
        private Control doubleTapControl;
        private bool doubleTapReceived;
        private bool doubleTapCanSendLongpress;

        // Android 15 edge-to-edge hack-fest.
        private bool navigationBarVisible;
        private int cutoutInsetLeft;
        private int cutoutInsetRight;
        private int cutoutInsetBottom;
        private int cutoutInsetTop;
        private int navInsetLeft;
        private int navInsetRight;
        private int navInsetBottom;
        private int navInsetTop;

        private string delayedMessage = null;
        private string delayedMessageTitle = null;
        private List<string> deferDeleteFiles = new List<string>();

        public static bool ActivityRunning => activityRunning;
        public static FamiStudioWindow Instance { get; private set; }
        public BaseFileActivity CurrentFileActivity => currentFileActivity;
        public bool IsAsyncFileActivityInProgress => currentFileActivity != null;
        public bool IsAsyncDialogInProgress => IsAsyncFileActivityInProgress || (container != null && container.IsDialogActive);

        public FamiStudio FamiStudio => famistudio;
        public Toolbar ToolBar => container.ToolBar;
        public Sequencer Sequencer => container.Sequencer;
        public PianoRoll PianoRoll => container.PianoRoll;
        public ProjectExplorer ProjectExplorer => container.ProjectExplorer;
        public QuickAccessBar QuickAccessBar => container.QuickAccessBar;
        public MobilePiano MobilePiano => container.MobilePiano;
        public Control ActiveControl => container.ActiveControl;
        public Graphics Graphics => graphics;
        public Fonts Fonts => fonts;

        public int Width  => cachedViewRect.Width;
        public int Height => cachedViewRect.Height;
        public Size Size => cachedViewRect.Size;
        public Rectangle Bounds => cachedViewRect;
        public Point LastMousePosition => Point.Empty;
        public Point LastContextMenuPosition => Point.Empty;
        public bool IsLandscape => cachedViewRect.Width > cachedViewRect.Height;
        public bool MobilePianoVisible { get => container.MobilePianoVisible; set => container.MobilePianoVisible = value; }
        public string Text { get; set; }

        private GestureDetectorCompat detector;
        private ScaleGestureDetector  scaleDetector;

        public delegate void AppSuspendedDelegate();
        public event AppSuspendedDelegate AppSuspended;

        private LocalizedString EnterTextLabel;
        private LocalizedString YesButton;
        private LocalizedString NoButton;
        private LocalizedString CancelButton;
        private LocalizedString OKButton;

        public FamiStudioWindow()
        {
            Localization.Localize(this);
        }

        public FamiStudioWindow(FamiStudio f)
        {
            Debug.Assert(false);
        }


        public void SetActiveControl(Control ctrl, bool animate = true)
        {
            container.SwitchToControl(ctrl, animate);
        }

        private void SetFullscreenMode()
        {
            // Fullscreen mode.
            Window.AddFlags(WindowManagerFlags.Fullscreen);

            int uiOptions = (int)Window.DecorView.SystemUiVisibility;

            uiOptions |= (int)SystemUiFlags.LowProfile;
            uiOptions |= (int)SystemUiFlags.Fullscreen;
            uiOptions |= (int)SystemUiFlags.ImmersiveSticky;
            uiOptions |= (int)SystemUiFlags.HideNavigation;

            Window.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            SetFullscreenMode();
            ForceScreenOn(false);

            Init.InitializeBaseSystems();

            // Only create the app once.
            appWasAlreadyRunning = FamiStudio.StaticInstance != null;

            if (appWasAlreadyRunning)
                famistudio = FamiStudio.StaticInstance;
            else
                famistudio = new FamiStudio();

            lock (renderLock)
            {
                glSurfaceView = new GLSurfaceView(this);
                glSurfaceView.PreserveEGLContextOnPause = true;
            #if DEBUG
                glSurfaceView.DebugFlags = DebugFlags.CheckGlError;
            #endif
                glSurfaceView.SetEGLContextClientVersion(2);
                glSurfaceView.SetEGLConfigChooser(8, 8, 8, 8, 16, 0);
                glSurfaceView.SetRenderer(this);
                glSurfaceView.RenderMode = Rendermode.WhenDirty;
                glThreadIsRunning = true;

                linearLayout = new LinearLayout(this);
                linearLayout.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
                linearLayout.AddView(glSurfaceView);
                linearLayout.SetBackgroundColor(Android.Graphics.Color.Black);

                // Starting at android 15, edge-to-edge is forced ON by default and we must
                // set padding manually to get the old behavior.
                var androidVersion = DeviceInfo.Version.Major;
                if (androidVersion >= 15)
                {
                    ViewCompat.SetOnApplyWindowInsetsListener(linearLayout, this);
                    Window.DecorView.SetOnSystemUiVisibilityChangeListener(this);
                }

                SetContentView(linearLayout);
                Instance = this;

                detector = new GestureDetectorCompat(this, this);
                detector.IsLongpressEnabled = true;
                detector.SetOnDoubleTapListener(this);
                scaleDetector = new ScaleGestureDetector(this, this);
                scaleDetector.QuickScaleEnabled = false;

                Choreographer.Instance.PostFrameCallback(this);

                UpdateForceLandscape();
                StartCleanCacheFolder();
            }
        }

        private void StartCleanCacheFolder()
        {
            new Thread(() =>
            {
                var files = Directory.GetFiles(Path.GetTempPath());
                foreach (var f in files)
                    File.Delete(f);
            }).Start();
        }

        public void DeferDeleteFile(string file)
        {
            if (IsAsyncDialogInProgress)
                deferDeleteFiles.Add(file);
            else
                File.Delete(file);
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            SetFullscreenMode();
        }

        public void StartLoadFileActivityAsync(string[] extensions, Action<string> callback)
        {
            Debug.Assert(currentFileActivity == null && pendingFinishFileActivity == null);
            currentFileActivity = new LoadActivity(callback);

            Intent intent = new Intent(Intent.ActionOpenDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("*/*");
            StartActivityForResult(intent, currentFileActivity.RequestCode);
        }

        public void StartSaveFileActivityAsync(string filename, Action<string> callback)
        {
            Debug.Assert(currentFileActivity == null && pendingFinishFileActivity == null);
            currentFileActivity = new SaveActivity(callback);

            var ext = Path.GetExtension(filename).ToLower();
            var mime = "*/*";

            if (ext == ".mp4")
            {
                mime = "video/mp4";
            }
            else
            {
                var idx = Array.FindIndex(AudioFormatType.Extensions, (e) => $".{e}" == ext);
                if (idx >= 0)
                {
                    mime = AudioFormatType.MimeTypes[idx];
                }
            }

            Intent intent = new Intent(Intent.ActionCreateDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType(mime);
            intent.PutExtra(Intent.ExtraTitle, filename);
            StartActivityForResult(intent, currentFileActivity.RequestCode);
            ForceScreenOn(true);
        }

        public void StartFileSharingActivity(string filename, Action callback)
        {
            Debug.Assert(currentFileActivity == null && pendingFinishFileActivity == null);
            currentFileActivity = new ShareActivity(callback);

            var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(this, "org.famistudio.fileprovider", new Java.IO.File(filename), filename); 

            Intent shareIntent = new Intent(Intent.ActionSend);
            shareIntent.SetType("*/*");
            shareIntent.PutExtra(Intent.ExtraStream, uri);
            StartActivityForResult(Intent.CreateChooser(shareIntent, "Share File"), currentFileActivity.RequestCode);
        }

        public void QueueDelayedMessageBox(string msg, string title)
        {
            delayedMessage      = msg;
            delayedMessageTitle = title;
        }

        private void ConditionalShowDelayedMessageBox()
        {
            if (delayedMessage != null)
            {
                Platform.MessageBoxAsync(this, delayedMessage, delayedMessageTitle, MessageBoxButtons.OK);

                delayedMessage      = null;
                delayedMessageTitle = null;
            }
        }

        private void ConditionalProcessDeferDelete()
        {
            if (!IsAsyncDialogInProgress && deferDeleteFiles.Count > 0)
            {
                File.Delete(deferDeleteFiles[0]);
                deferDeleteFiles.RemoveAt(0);
            }
        }

        private void ResumeGLThread()
        {
            Console.WriteLine("ResumeGLThread");

            if (!glThreadIsRunning)
            {
                MarkDirty();
                glSurfaceView.OnResume();
                glThreadIsRunning = true;
            }
        }

        private void PauseGLThread()
        {
            Console.WriteLine("PauseGLThread");

            if (glThreadIsRunning)
            {
                glSurfaceView.OnPause();
                lock (renderLock) { }; // Extra safety.
                glThreadIsRunning = false;
            }
        }

        public void InvokeOnMainThread(System.Action action)
        {
            MainThread.InvokeOnMainThreadAsync(() => 
            {
                lock (renderLock)
                {
                    action();
                }
            });
        }

        // For debugging property pages.
        private void DialogTest()
        {
#if DEBUG
            var dlg = new PropertyDialog(this, "Test Dialog", 200);

            dlg.Properties.AddTextBox("TextBox", "Hello1", 0, false, "This is a long tooltip explaining what this property is all about");
            dlg.Properties.AddColorPicker(Color.Pink);
            dlg.Properties.AddButton("Hey", "This is a button", "Button tooltip");
            dlg.Properties.AddNumericUpDown("Integer", 10, 2, 50, 1, "Integer Tooltip");
            dlg.Properties.AddDropDownList("Hey", new[] { "Option1 QQQ", "Option2 QQQ", "Option3 QQQ", "Option4 QQQ" }, "Option3 QQQ", "Dropdown tooltip");
            dlg.Properties.AddRadioButton("This is a radio", "Radio 123", false);
            dlg.Properties.AddRadioButton("This is a radio", "Radio 435", true);
            dlg.Properties.AddRadioButton("This is a radio", "Radio 888", false);
            dlg.Properties.BeginAdvancedProperties();
            dlg.Properties.AddCheckBoxList("Check box list", new[] { "Check1", "Check2", "Check3", "Check4" }, new[] { false, true, true, false });
            dlg.Properties.AddCheckBox("CheckBox1", true, "Checkbox tooltip!");
            dlg.Properties.AddSlider("Slider", 50, 0, 100, (v) => $"Allo {0} XXX", "Tooltip for slider");

            dlg.ShowDialogAsync((r) =>
            {
                if (r == DialogResult.OK)
                {
                    Debug.WriteLine("Hello!");
                }
            });
#endif
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            // This happens if the activity was destroyed while inside a dialog. The main activity will 
            // have been re-created and we wont remember what we were doing. Simply ignore. 
            if (currentFileActivity == null)
                return;

            var dialog = currentFileActivity;

            if (!currentFileActivity.IsDialogDone(resultCode))
                pendingFinishFileActivity = currentFileActivity;
            currentFileActivity = null;

            dialog.OnResult(this, resultCode, data);

            base.OnActivityResult(requestCode, resultCode, data);
        }

        public void FinishSaveFileActivityAsync(bool commit, Action callback)
        {
            Debug.Assert(pendingFinishFileActivity != null);
            var saveInfo = pendingFinishFileActivity as SaveActivity;
            Debug.Assert(saveInfo != null);
            pendingFinishFileActivity = null;
            saveInfo.Finish(commit, callback);
            ForceScreenOn(false);
        }

        public void ForceScreenOn(bool on)
        {
            if (on)
                Window.AddFlags(WindowManagerFlags.KeepScreenOn);
            else
                Window.ClearFlags(WindowManagerFlags.KeepScreenOn);
        }

        private void UpdateForceLandscape()
        {
            var settingsWantLandscape = Settings.ForceLandscape;
            var appIsInLandscape = RequestedOrientation == ScreenOrientation.SensorLandscape;

            if (settingsWantLandscape != appIsInLandscape)
            {
                RequestedOrientation = settingsWantLandscape ? ScreenOrientation.SensorLandscape : ScreenOrientation.Unspecified;
            }
        }

        public void DoFrame(long frameTimeNanos)
        {
            if (lastFrameTime < 0)
                lastFrameTime = frameTimeNanos;

            if (glThreadIsRunning && container != null && !IsAsyncFileActivityInProgress)
            {
                var deltaTime = (float)Math.Min(0.25f, (float)((frameTimeNanos - lastFrameTime) / 1000000000.0));

                UpdateForceLandscape();

                lock (renderLock)
                {
                    CacheViewRect();

                    if (!IsAsyncDialogInProgress)
                    {
                        famistudio.Tick(deltaTime);
                    }
                    else
                    {
                        famistudio.TickAsyncDialog(deltaTime);
                    }

                    container.TickWithChildren(deltaTime);
                }

                ConditionalShowDelayedMessageBox();
                ConditionalProcessDeferDelete();

                if (dirty)
                {
                    glSurfaceView.RequestRender();
                    dirty = false;
                }
            }

            Choreographer.Instance.PostFrameCallback(this);
            lastFrameTime = frameTimeNanos;
        }

        // GL thread.
        public void OnDrawFrame(IGL10 gl)
        {
            lock (renderLock)
            {
                var rect = new Rectangle(Point.Empty, Size);
                var clearColor = global::FamiStudio.Theme.DarkGreyColor2;

                graphics.BeginDrawFrame(rect, true, clearColor);
                container.Render(graphics);
                graphics.EndDrawFrame();
            }
        }

        // GL thread.
        public void OnSurfaceChanged(IGL10 gl, int width, int height)
        {
            lock (renderLock)
            {
                CacheViewRect();

                if (container != null)
                {
                    captureControl?.SendPointerUp(new PointerEventArgs(0, 0)); // We dont store the last position, should be good enough.
                    ReleasePointer();
                    container.Resize(cachedViewRect.Width, cachedViewRect.Height);
                }
            }
        }

        // GL thread.
        public void OnSurfaceCreated(IGL10 gl, Javax.Microedition.Khronos.Egl.EGLConfig config)
        {
            lock (renderLock)
            {
                Console.WriteLine("OnSurfaceCreated");

                CacheViewRect();

                graphics = new Graphics();
                graphics.SetLineBias(2);
                fonts = new Fonts(graphics);

                if (container == null)
                {
                    Console.WriteLine("OnSurfaceCreated : Creating container.");

                    container = new FamiStudioContainer(this);
                    container.Resize(cachedViewRect.Width, cachedViewRect.Height);

                    // Simply update the form if the app already exists.
                    if (appWasAlreadyRunning)
                        famistudio.SetWindow(this, true);
                    else
                        famistudio.Initialize(this, null);

                    RefreshLayout();
                }
            }
        }

        public ModifierKeys GetModifierKeys()
        {
            return modifiers;
        }

        public Point GetCursorPosition()
        {
            return Point.Empty;
        }

        public Point ScreenToWindow(Point p)
        {
            return Point.Empty;
        }

        public Point WindowToScreen(Point p)
        {
            return Point.Empty;
        }

        //public Point PointToClient(Control ctrl, Point p)
        //{
        //    return Point.Empty;
        //}

        //public Point PointToScreen(Control ctrl, Point p)
        //{
        //    return Point.Empty;
        //}

        public void RunEventLoop(bool allowSleep = false)
        {
            Debug.Assert(false);
        }

        public bool IsKeyDown(Keys key)
        {
            return false;
        }

        public void InitDialog(Dialog dlg)
        {
            container.InitDialog(dlg);
        }

        public void PushDialog(Dialog dlg)
        {
            ReleasePointer();
            container.PushDialog(dlg);
        }

        public void PopDialog(Dialog dlg, int numLevels = 1)
        {
            container.PopDialog(dlg, numLevels);
        }

        public void ShowToast(string text, bool longDuration = false, Action click = null)
        {
            container.ShowToast(text, longDuration, click);
        }

        public void Quit()
        {
            Debug.Assert(false);
        }

        public Dialog TopDialog => null;

        public static FamiStudioWindow CreateWindow(FamiStudio fs)
        {
            Debug.Assert(false);
            return null;
        }

        public int CapturePointer(Control ctrl)
        {
            captureControl = ctrl;
            return ++captureCookie;
        }

        public void ReleasePointer()
        {
            captureControl = null;
        }
        
        public bool CheckCaptureCookie(int cookie)
        {
            return captureCookie == cookie;
        }

        public void RefreshLayout()
        {
            lock (renderLock)
            {
                CacheViewRect();
                container.Resize(cachedViewRect.Size);
            }
        }

        private void CacheViewRect()
        {
            var rect = new Android.Graphics.Rect();
            glSurfaceView.GetDrawingRect(rect);
            cachedViewRect = new Rectangle(rect.Left, rect.Top, rect.Width(), rect.Height());

            int[] location = new int[2];
            glSurfaceView.GetLocationInWindow(location);
            glViewPosition = new Point(location[0], location[1]);
        }

        public void MarkDirty()
        {
            dirty = true;
        }

        public void Refresh()
        {
            MarkDirty();
        }

        public void RefreshCursor()
        {
        }

        public void Run()
        {
        }

        protected override void OnStop()
        {
            Debug.WriteLine("FamiStudioForm.OnStop");

            base.OnStop();
        }

        protected override void OnStart()
        {
            Debug.WriteLine("FamiStudioForm.OnStart");

            base.OnStart();

        }

        protected override void OnDestroy()
        {
            Debug.WriteLine("FamiStudioForm.OnDestroy");

            lock (renderLock)
            {
                // This will stop all audio and prevent issues.
                famistudio.Suspend();
                AppSuspended?.Invoke();
            }

            base.OnDestroy();
        }

        protected override void OnPause()
        {
            Debug.WriteLine("FamiStudioForm.OnPause");

            Debug.Assert(activityRunning);
            activityRunning = false;

            lock (renderLock)
            {
                // If we are begin stopped, but not because we are opening a dialog,
                // this likely mean the user is switching app. Let's suspend.
                // The property dialogs will handle this themselves.
                if (currentFileActivity == null)
                {
                    famistudio.Suspend();
                }
                else
                {
                    famistudio.SaveWorkInProgress();
                }

                AppSuspended?.Invoke();
            }

            PauseGLThread();

            base.OnPause();
        }

        protected override void OnResume()
        {
            Debug.WriteLine("FamiStudioForm.OnResume");

            Debug.Assert(!activityRunning);
            activityRunning = true;

            famistudio.Resume();

            ResumeGLThread();

            base.OnResume();
        }

        public void ShowContextMenuAsync(ContextMenuOption[] options)
        {
            container.ShowContextMenuAsync(options);
        }

        public void HideContextMenu()
        {
            container.HideContextMenu();
        }

        public void MessageBoxAsync(string text, string title, MessageBoxButtons buttons, Action<DialogResult> callback = null)
        {
            var dialog = new Android.App.AlertDialog.Builder(Xamarin.Essentials.Platform.CurrentActivity);
            var alert = dialog.Create();

            alert.SetTitle(title);
            alert.SetMessage(text);

            if (buttons == MessageBoxButtons.YesNo ||
                buttons == MessageBoxButtons.YesNoCancel)
            {
                alert.SetButton(YesButton, (c, ev) => { lock (renderLock) { callback?.Invoke(DialogResult.Yes); } });
                alert.SetButton2(NoButton, (c, ev) => { lock (renderLock) { callback?.Invoke(DialogResult.No);  } });
                if (buttons == MessageBoxButtons.YesNoCancel)
                {
                    alert.SetButton3(CancelButton, (c, ev) => { lock (renderLock) { callback?.Invoke(DialogResult.Cancel); } });
                    alert.CancelEvent += (s, e) => { lock (renderLock) { callback.Invoke(DialogResult.Cancel); } };
                }
                else
                {
                    alert.CancelEvent += (s, e) => { lock (renderLock) { callback.Invoke(DialogResult.No); } };
                }
            }
            else
            {
                alert.SetButton(OKButton, (c, ev) => { lock (renderLock) { callback?.Invoke(DialogResult.OK); } });
            }

            alert.SetCanceledOnTouchOutside(false);
            alert.SetCancelable(false);

            alert.Show();
        }

        public class KeyboardFocusEditText : EditText
        {
            public KeyboardFocusEditText(Context context) : base(context)
            {
            }

            public override void OnWindowFocusChanged(bool hasWindowFocus)
            {
                if (hasWindowFocus)
                {
                    RequestFocus();
                    SelectAll();
                    Post(() =>
                    {
                        InputMethodManager inputMethodManager = (InputMethodManager)Application.Context.GetSystemService(Context.InputMethodService);
                        inputMethodManager.ShowSoftInput(this, 0);
                    });
                }
            }
        }

        public void EditTextAsync(string prompt, string text, Action<string> callback)
        {
            var content = new KeyboardFocusEditText(new ContextThemeWrapper(this, Resource.Style.LightGrayTextMedium));
            content.Text = text;
            content.Gravity = Android.Views.GravityFlags.Center;
            content.ImeOptions = ImeAction.Go;
            content.SetTextColor(Application.Context.GetColorStateList(Resource.Color.light_grey));
            content.Background.SetColorFilter(BlendModeColorFilterCompat.CreateBlendModeColorFilterCompat(DroidUtils.GetColorFromResources(this, Resource.Color.LightGreyColor1), BlendModeCompat.SrcAtop));
            content.SetSingleLine(true);
            content.RequestFocus();
            var builder = new Android.App.AlertDialog.Builder(new ContextThemeWrapper(this, Resource.Style.TextInputDialogStyle));
            var dialog = builder.Create();
            dialog.SetTitle(string.IsNullOrEmpty(prompt) ? EnterTextLabel : prompt);
            dialog.SetView(content);
            var action = () => { lock (renderLock) { dialog.Dismiss(); callback?.Invoke(content.Text); } };
            content.EditorAction += (s, e) => { if (e.ActionId == ImeAction.Go) action();  };
            dialog.SetButton((int)DialogButtonType.Positive, "OK", (c, ev) => action());
            dialog.Show();
        }

        private Control GetCapturedControlAtCoord(int formX, int formY, out int ctrlX, out int ctrlY)
        {
            if (captureControl != null)
            {
                Debug.Assert(container.CanAcceptInput);
                ctrlX = formX - captureControl.WindowPosition.X;
                ctrlY = formY - captureControl.WindowPosition.Y;
                return captureControl;
            }
            else if (container.CanAcceptInput)
            {
                return container.GetControlAt(formX, formY, out ctrlX, out ctrlY);
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
            if (!IsAsyncFileActivityInProgress)
            {
                var px = (int)e.GetX() - glViewPosition.X;
                var py = (int)e.GetY() - glViewPosition.Y;

                //Debug.WriteLine($"OnTouchEvent {e.Action.ToString()} ({px}, {py})");

                if (e.Action == MotionEventActions.Up || e.Action == MotionEventActions.Cancel)
                {
                    lock (renderLock)
                    {
                        var ctrl = GetCapturedControlAtCoord(px, py, out var x, out var y);
                        if (ctrl != null)
                        {
                            if (captureControl == ctrl)
                                ReleasePointer();
                            ctrl.SendPointerUp(new PointerEventArgs(x, y));
                        }
                    }

                    doubleTapReceived = false;
                }
                else if (e.Action == MotionEventActions.Move && !scaleDetector.IsInProgress)
                {
                    lock (renderLock)
                    {
                        var ctrl = GetCapturedControlAtCoord(px, py, out var x, out var y);
                        ctrl?.SendPointerMove(new PointerEventArgs(x, y));
                    }

                    if (doubleTapReceived && ((int.Abs(doubleTapLocation.X - px) > 32 || int.Abs(doubleTapLocation.Y - py) > 32)))
                    {
                        doubleTapCanSendLongpress = false;
                    }
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
            if (!IsAsyncFileActivityInProgress)
            {
                var px = (int)e.GetX() - glViewPosition.X;
                var py = (int)e.GetY() - glViewPosition.Y;
                
                Debug.WriteLine($"OnDown {e.PointerCount} ({px}, {py})");

                lock (renderLock)
                {
                    var ctrl = GetCapturedControlAtCoord(px, py, out var x, out var y);
                    ctrl?.SendPointerDown(new PointerEventArgs(x, y));
                }
            }
            return false;
        }

        public bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
        {
            if (!IsAsyncFileActivityInProgress)
            {
                var px = (int)e1.GetX() - glViewPosition.X;
                var py = (int)e1.GetY() - glViewPosition.Y;

                //Debug.WriteLine($"OnFling {e1.PointerCount} ({px}, {py}) ({velocityX}, {velocityY})");

                lock (renderLock)
                {
                    var ctrl = GetCapturedControlAtCoord(px, py, out var x, out var y);
                    ctrl?.SendTouchFling(new PointerEventArgs(x, y, velocityX, velocityY));
                }
            }
            return false;
        }

        public void OnLongPress(MotionEvent e)
        {
            if (!IsAsyncFileActivityInProgress)
            {
                var px = (int)e.GetX() - glViewPosition.X;
                var py = (int)e.GetY() - glViewPosition.Y;

                Debug.WriteLine($"OnLongPress {e.PointerCount} ({px}, {py})");

                lock (renderLock)
                {
                    if (!doubleTapReceived || doubleTapCanSendLongpress)
                    {
                        var ctrl = GetCapturedControlAtCoord(px, py, out var x, out var y);
                        ctrl?.SendTouchLongPress(new PointerEventArgs(x, y) { IsDoubleTapLongPress = doubleTapReceived });
                    }
                }
            }
        }

        public bool OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY)
        {
            return false;
        }

        public void OnShowPress(MotionEvent e)
        {
            if (!IsAsyncFileActivityInProgress)
            {
                //Debug.WriteLine($"{e.PointerCount} OnShowPress ({e.GetX()}, {e.GetY()})");
            }
        }

        public bool OnSingleTapUp(MotionEvent e)
        {
            if (!IsAsyncFileActivityInProgress)
            {
                //DialogTest();
                //StartSaveFileActivity("audio/mpeg", "Toto.mp3");

                var px = (int)e.GetX() - glViewPosition.X;
                var py = (int)e.GetY() - glViewPosition.Y;

                Debug.WriteLine($"OnSingleTapUp ({px}, {py})");

                lock (renderLock)
                {
                    var ctrl = GetCapturedControlAtCoord(px, py, out var x, out var y);
                    ctrl?.SendTouchClick(new PointerEventArgs(x, y));
                }
            }
            return false;
        }

        public bool OnScale(ScaleGestureDetector detector)
        {
            if (!IsAsyncFileActivityInProgress)
            {
                var px = (int)detector.FocusX - glViewPosition.X;
                var py = (int)detector.FocusY - glViewPosition.Y;

                Debug.WriteLine($"OnScale ({px}, {py}) {detector.ScaleFactor}");

                lock (renderLock)
                {
                    var ctrl = GetCapturedControlAtCoord(px, py, out var x, out var y);
                    ctrl?.SendTouchScale(new PointerEventArgs(x, y, detector.ScaleFactor));
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool OnScaleBegin(ScaleGestureDetector detector)
        {
            if (!IsAsyncFileActivityInProgress)
            {
                var px = (int)detector.FocusX - glViewPosition.X;
                var py = (int)detector.FocusY - glViewPosition.Y;

                Debug.WriteLine($"OnScaleBegin ({px}, {py})");

                lock (renderLock)
                {
                    var ctrl = GetCapturedControlAtCoord(px, py, out var x, out var y);
                    ctrl?.SendTouchScaleBegin(new PointerEventArgs(x, y));
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public void OnScaleEnd(ScaleGestureDetector detector)
        {
            if (!IsAsyncFileActivityInProgress)
            {
                Debug.WriteLine($"OnScaleEnd ({detector.FocusX}, {detector.FocusY})");
                lock (renderLock)
                {
                    var ctrl = GetCapturedControlAtCoord((int)detector.FocusX, (int)detector.FocusY, out var x, out var y);
                    ctrl?.SendTouchScaleEnd(new PointerEventArgs(x, y));
                }
            }
        }

        public bool OnDoubleTap(MotionEvent e)
        {
            if (!IsAsyncFileActivityInProgress)
            {
                var px = (int)e.GetX() - glViewPosition.X;
                var py = (int)e.GetY() - glViewPosition.Y;

                Debug.WriteLine($"OnDoubleTap ({px}, {py})");

                lock (renderLock)
                {
                    doubleTapStartTime = e.EventTime;
                    doubleTapLocation = new Point(px, py);
                    doubleTapControl = GetCapturedControlAtCoord(doubleTapLocation.X, doubleTapLocation.Y, out var x, out var y);
                    doubleTapReceived = true;
                    doubleTapCanSendLongpress = true;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool OnDoubleTapEvent(MotionEvent e)
        {
            if (!IsAsyncFileActivityInProgress)
            {
                lock (renderLock)
                {
                    var px = (int)e.GetX() - glViewPosition.X;
                    var py = (int)e.GetY() - glViewPosition.Y;

                    Debug.WriteLine($"OnDoubleTapEvent ({px}, {py}) (Action={e.Action})");

                    if (e.Action == MotionEventActions.Up && (e.EventTime - doubleTapStartTime) < 350)
                    {
                        // "OnDoubleTap" returns the coordinate of the first tap, so it you tap
                        // at 2 different locations very quickly, the second tap will still be at
                        // the first location. This is fine for controls that supports double-tap
                        // but not for the ones that dont support it. "OnDoubleTapEvent + Down"
                        // returns the correct new location, so we will use that for controls
                        // that dont want to deal with double-clicks. 
                        //
                        // Also, we enforce that we double tap on the same control as the first tap.
                        // Tapping fast between to buttons triggers double taps which feels super weird.
                        var ctrl = GetCapturedControlAtCoord(px, py, out var x, out var y);

                        if (ctrl != null)
                        {
                            if (!ctrl.SupportsDoubleClick || (ctrl != doubleTapControl && doubleTapControl != null))
                            {
                                ctrl.SendTouchClick(new PointerEventArgs(x, y));
                            }
                            else
                            {
                                ctrl.SendTouchDoubleClick(new PointerEventArgs(x, y));
                            }
                        }
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool OnSingleTapConfirmed(MotionEvent e)
        {
            var px = (int)e.GetX() - glViewPosition.X;
            var py = (int)e.GetY() - glViewPosition.Y;

            Debug.WriteLine($"OnSingleTapConfirmed ({px}, {py})");

            return false;
        }

        public WindowInsetsCompat OnApplyWindowInsets(View v, WindowInsetsCompat windowInsets)
        {
            var cutoutInsets = windowInsets.GetInsetsIgnoringVisibility(WindowInsetsCompat.Type.DisplayCutout());
            var navInsets    = windowInsets.GetInsetsIgnoringVisibility(WindowInsetsCompat.Type.DisplayCutout() | WindowInsetsCompat.Type.NavigationBars());

            // We just want to apply the navigation insets when the navigation bar is visible. This
            // only happens when dialogs (such as EditText) pop up. "OnSystemUiVisibilityChange" is sometimes
            // called before/after "OnApplyWindowInsets" so we cant rely on that. We store both insets and
            // decide which one to pick in ApplyInsets.
            cutoutInsetLeft   = cutoutInsets.Left;
            cutoutInsetTop    = cutoutInsets.Top;
            cutoutInsetRight  = cutoutInsets.Right;
            cutoutInsetBottom = cutoutInsets.Bottom;

            navInsetLeft   = navInsets.Left;
            navInsetTop    = navInsets.Top;
            navInsetRight  = navInsets.Right;
            navInsetBottom = navInsets.Bottom;

            //v.SetPadding(insets.Left, insets.Top, insets.Right, insets.Bottom);
            //v.SetPadding(123, 456, 221, 50); // For debugging, set weird padding.

            ApplyInsets();

            return WindowInsetsCompat.Consumed;
        }

        public void OnSystemUiVisibilityChange([GeneratedEnum] StatusBarVisibility visibility)
        {
            var flags = (SystemUiFlags)visibility;
            navigationBarVisible = !flags.HasFlag(SystemUiFlags.HideNavigation);
            ApplyInsets();
        }

        private void ApplyInsets()
        {
            // See comment in OnApplyWindowInsets.
            var insetLeft   = navigationBarVisible ? navInsetLeft   : cutoutInsetLeft;
            var insetRight  = navigationBarVisible ? navInsetRight  : cutoutInsetRight;
            var insetBottom = navigationBarVisible ? navInsetBottom : cutoutInsetBottom;
            var insetTop    = navigationBarVisible ? navInsetTop    : cutoutInsetTop;

            if (insetLeft   != linearLayout.PaddingLeft ||
                insetRight  != linearLayout.PaddingRight ||
                insetBottom != linearLayout.PaddingBottom ||
                insetTop    != linearLayout.PaddingTop)
            {
                linearLayout.SetPadding(insetLeft, insetTop, insetRight, insetBottom);
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

    public abstract class BaseFileActivity
    {
        protected int requestCode;
        public int RequestCode => requestCode;
        public virtual bool IsDialogDone(Result result) => true;
        public abstract void OnResult(FamiStudioWindow main, Result code, Intent data);
    };

    public class LoadActivity : BaseFileActivity
    {
        protected Action<string> callback;

        public LoadActivity(Action<string> cb)
        {
            requestCode = 1000;
            callback = cb;
        }

        public override void OnResult(FamiStudioWindow main, Result code, Intent data)
        {
            if (code == Result.Ok)
            {
                var filename = (string)null;

                var c = main.ContentResolver.Query(data.Data, null, null, null);
                if (c != null && c.MoveToFirst())
                {
                    int id = c.GetColumnIndex(Android.Provider.IOpenableColumns.DisplayName);
                    if (id != -1)
                        filename = c.GetString(id);
                }

                if (filename != null)
                {
                    var tempFile = Path.Combine(Path.GetTempPath(), filename);
                    var buffer = new byte[256 * 1024];

                    using (var streamIn = main.ContentResolver.OpenInputStream(data.Data))
                    {
                        using (var streamOut = File.Open(tempFile, FileMode.Create))
                        {
                            while (true)
                            {
                                var len = streamIn.Read(buffer, 0, buffer.Length);
                                if (len == 0)
                                    break;
                                streamOut.Write(buffer, 0, len);
                            }

                            streamOut.Close();
                        }

                        streamIn.Close();
                    }

                    callback(tempFile);

                    // Some things like NSF import may open further dialogs which 
                    // will open the file again. So we cant delete it until all the 
                    // dialogs are closed.
                    FamiStudioWindow.Instance.DeferDeleteFile(tempFile);
                }
            }
        }
    }

    public class SaveActivity : BaseFileActivity
    {
        protected Action<string> callback;
        protected string lastSaveTempFile;
        protected Android.Net.Uri lastSaveFileUri;

        public SaveActivity(Action<string> cb)
        {
            requestCode = 1001;
            callback = cb;
        }

        public override bool IsDialogDone(Result result) => result != Result.Ok;

        public override void OnResult(FamiStudioWindow main, Result code, Intent data)
        {
            if (code == Result.Ok)
            {
                lastSaveFileUri  = data.Data;
                lastSaveTempFile = Path.GetTempFileName();

                // Save to temporary file and copy.
                callback(lastSaveTempFile);
            }
        }

        public void Finish(bool commit, Action callback)
        {
            if (commit)
            {
                if (lastSaveTempFile != null && File.Exists(lastSaveTempFile))
                {
                    Log.ReportProgress(1.0f);
                    Log.LogMessage(LogSeverity.Info, "Copying the file to storage...");

                    var buffer = new byte[256 * 1024];

                    using (var streamIn = File.OpenRead(lastSaveTempFile))
                    {
                        using (var streamOut = FamiStudioWindow.Instance.ContentResolver.OpenOutputStream(lastSaveFileUri))
                        {
                            while (true)
                            {
                                var len = streamIn.Read(buffer, 0, buffer.Length);
                                if (len == 0)
                                    break;
                                streamOut.Write(buffer, 0, len);
                            }

                            streamOut.Close();
                        }

                        streamIn.Close();
                    }

                    File.Delete(lastSaveTempFile);
                }
            }
            else if (lastSaveTempFile != null && File.Exists(lastSaveTempFile))
            {
                File.Delete(lastSaveTempFile);
            }

            callback();
        }
    }

    public class ShareActivity : BaseFileActivity
    {
        protected Action callback;

        public ShareActivity(Action cb)
        {
            requestCode = 2002;
            callback = cb;
        }

        public override void OnResult(FamiStudioWindow main, Result code, Intent data)
        {
            if (code == Result.Ok)
                callback();
        }
    }
}
