using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using Android.Widget;
using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Opengl;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Content.PM;
using AndroidX.Core.Graphics;
using AndroidX.Core.View;
using AndroidX.AppCompat.App;
using AndroidX.Core.Content;
using Javax.Microedition.Khronos.Opengles;
using Google.Android.Material.BottomSheet;

namespace FamiStudio
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true, ResizeableActivity = false, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.UiMode | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden)]
    public class FamiStudioWindow : AppCompatActivity, GLSurfaceView.IRenderer, GestureDetector.IOnGestureListener, GestureDetector.IOnDoubleTapListener, ScaleGestureDetector.IOnScaleGestureListener, Choreographer.IFrameCallback
    {
        private LinearLayout linearLayout;
        private GLSurfaceView glSurfaceView;

        private FamiStudio famistudio;
        private FamiStudioContainer container;
        private Graphics graphics;
        private Fonts fonts;
        private bool dirty = true;

        // For context menus.
        BottomSheetDialog contextMenuDialog;

        // For property or multi-property dialogs.
        private bool appWasAlreadyRunning;
        private bool glThreadIsRunning;
        private static bool activityRunning;
        private long lastFrameTime = -1;
        private object renderLock = new object();
        private BaseDialogActivityInfo activeDialog;
        private BaseDialogActivityInfo pendingFinishDialog;
        private Control captureControl;
        private ModifierKeys modifiers = new ModifierKeys();

        private string delayedMessage = null;
        private string delayedMessageTitle = null;
        private List<string> deferDeleteFiles = new List<string>();

        public static bool ActivityRunning => activityRunning;
        public static FamiStudioWindow Instance { get; private set; }
        public BaseDialogActivityInfo ActiveDialog => activeDialog;
        public bool IsAsyncDialogInProgress => activeDialog != null;

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

        public int Width  => glSurfaceView.Width;
        public int Height => glSurfaceView.Height;
        public Size Size => new Size(glSurfaceView.Width, glSurfaceView.Height);
        public Point LastMousePosition => new Point(0, 0); // MATTT!
        public Point LastContextMenuPosition => new Point(0, 0); // MATTT!
        public bool IsLandscape => glSurfaceView.Width > glSurfaceView.Height;
        public bool MobilePianoVisible { get => container.MobilePianoVisible; set => container.MobilePianoVisible = value; }
        public string Text { get; set; }

        private GestureDetectorCompat detector;
        private ScaleGestureDetector  scaleDetector;

        public FamiStudioWindow()
        {
        }

        public FamiStudioWindow(FamiStudio f)
        {
            Debug.Assert(false);
        }

        public Rectangle Bounds
        {
            get
            {
                var rect = new Android.Graphics.Rect();
                glSurfaceView.GetDrawingRect(rect);
                return new Rectangle(rect.Left, rect.Top, rect.Width(), rect.Height());
            }
        }

        public void SetActiveControl(Control ctrl, bool animate = true)
        {
            container.SetActiveControl(ctrl, animate);
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
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            EnableFullscreenMode(Window);
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
            EnableFullscreenMode(Window);
        }

        public void StartLoadFileActivityAsync(string mimeType, Action<string> callback)
        {
            Debug.Assert(activeDialog == null && pendingFinishDialog == null);
            activeDialog = new LoadDialogActivityInfo(callback);

            Intent intent = new Intent(Intent.ActionOpenDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType(mimeType);
            StartActivityForResult(intent, activeDialog.RequestCode);
        }

        public void StartSaveFileActivityAsync(string mimeType, string filename, Action<string> callback)
        {
            Debug.Assert(activeDialog == null && pendingFinishDialog == null);
            activeDialog = new SaveDialogActivityInfo(callback);

            Intent intent = new Intent(Intent.ActionCreateDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType(mimeType);
            intent.PutExtra(Intent.ExtraTitle, filename);
            StartActivityForResult(intent, activeDialog.RequestCode);
            ForceScreenOn(true);
        }

        public void StartPropertyDialogActivity(Action<DialogResult> callback, PropertyDialog dlg)
        {
            Debug.Assert(activeDialog == null);
            activeDialog = new PropertyDialogActivityInfo(dlg, callback);
            StartActivityForResult(new Intent(this, typeof(PropertyDialogActivity)), activeDialog.RequestCode);
        }

        public void StartMultiPropertyDialogActivity(Action<DialogResult> callback, MultiPropertyDialog dlg)
        {
            Debug.Assert(activeDialog == null && pendingFinishDialog == null);
            activeDialog = new MultiPropertyDialogActivityInfo(dlg, callback);
            StartActivityForResult(new Intent(this, typeof(MultiPropertyDialogActivity)), activeDialog.RequestCode);
        }

        public void StartTutorialDialogActivity(Action<DialogResult> callback, TutorialDialog dlg)
        {
            Debug.Assert(activeDialog == null);
            activeDialog = new TutorialDialogActivityInfo(dlg, callback);
            StartActivityForResult(new Intent(this, typeof(TutorialDialogActivity)), activeDialog.RequestCode);
        }

        public void StartFileSharingActivity(string filename, Action callback)
        {
            Debug.Assert(activeDialog == null && pendingFinishDialog == null);
            activeDialog = new ShareActivityInfo(callback);

            var uri = FileProvider.GetUriForFile(this, "org.famistudio.fileprovider", new Java.IO.File(filename), filename); 

            Intent shareIntent = new Intent(Intent.ActionSend);
            shareIntent.SetType("*/*");
            shareIntent.PutExtra(Intent.ExtraStream, uri);
            StartActivityForResult(Intent.CreateChooser(shareIntent, "Share File"), activeDialog.RequestCode);
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
            dlg.Properties.AddSlider("Slider", 50, 0, 100, 1.0f, 2, "Allo {0} XXX", "Tooltip for slider");

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
            if (activeDialog == null)
                return;

            var dialog = activeDialog;

            if (!activeDialog.IsDialogDone(resultCode))
                pendingFinishDialog = activeDialog;
            activeDialog = null;

            dialog.OnResult(this, resultCode, data);

            base.OnActivityResult(requestCode, resultCode, data);
        }

        public void FinishSaveFileActivityAsync(bool commit, Action callback)
        {
            Debug.Assert(pendingFinishDialog != null);
            var saveInfo = pendingFinishDialog as SaveDialogActivityInfo;
            Debug.Assert(saveInfo != null);
            pendingFinishDialog = null;
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
            var appIsInLandscape = RequestedOrientation == ScreenOrientation.Landscape;

            if (settingsWantLandscape != appIsInLandscape)
            {
                RequestedOrientation = settingsWantLandscape ? ScreenOrientation.Landscape : ScreenOrientation.Unspecified;
            }
        }

        public void DoFrame(long frameTimeNanos)
        {
            if (lastFrameTime < 0)
                lastFrameTime = frameTimeNanos;

            if (glThreadIsRunning && container != null && !IsAsyncDialogInProgress)
            {
                var deltaTime = (float)Math.Min(0.25f, (float)((frameTimeNanos - lastFrameTime) / 1000000000.0));

                UpdateForceLandscape();

                lock (renderLock)
                {
                    famistudio.Tick(deltaTime);
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
                Platform.AcquireGLContext();

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
                Platform.AcquireGLContext();

                if (container != null)
                { 
                    container.Resize(width, height);
                }
            }
        }

        // GL thread.
        public void OnSurfaceCreated(IGL10 gl, Javax.Microedition.Khronos.Egl.EGLConfig config)
        {
            lock (renderLock)
            {
                Console.WriteLine("OnSurfaceCreated");

                Platform.AcquireGLContext();

                graphics = new Graphics();
                graphics.SetLineBias(2);
                fonts = new Fonts(graphics);

                if (container == null)
                {
                    Console.WriteLine("OnSurfaceCreated : Creating container.");

                    container = new FamiStudioContainer(this);
                    container.Resize(glSurfaceView.Width, glSurfaceView.Height);

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
            Debug.Assert(false);
        }

        public void PushDialog(Dialog dlg)
        {
            Debug.Assert(false);
        }

        public void PopDialog(Dialog dlg)
        {
            Debug.Assert(false);
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

        public void CaptureMouse(Control ctrl)
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
            lock (renderLock)
            { 
                container.Resize(glSurfaceView.Width, glSurfaceView.Height);
            }
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
                if (activeDialog == null || activeDialog.ShouldSuspend)
                    famistudio.Suspend();
                else
                    famistudio.SaveWorkInProgress();
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

        public void ShowContextMenu(int x, int y, ContextMenuOption[] options)
        {
            if (options == null || options.Length == 0)
                return;
            
            Debug.Assert(contextMenuDialog == null);

            var bgColor = DroidUtils.ToAndroidColor(global::FamiStudio.Theme.DarkGreyColor4);

            var linearLayout = new LinearLayout(this);
            linearLayout.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            linearLayout.Orientation = Orientation.Vertical;
            linearLayout.SetBackgroundColor(bgColor);

            var prevWantedSeparator = false;

            var imagePad  = DroidUtils.DpToPixels(2);
            var imageSize = DroidUtils.DpToPixels(32);

            for (int i = 0; i < options.Length; i++)
            {
                var opt = options[i];

                if (i > 0 && (opt.Separator.HasFlag(ContextMenuSeparator.MobileBefore) || prevWantedSeparator))
                {
                    var lineView = new View(this);
                    lineView.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 1);
                    lineView.SetBackgroundColor(DroidUtils.GetColorFromResources(this, Resource.Color.LightGreyColor1));
                    linearLayout.AddView(lineView);
                    prevWantedSeparator = false;
                }

                var imageName = opt.Image;

                if (string.IsNullOrEmpty(imageName))
                {
                    var checkState = opt.CheckState();
                    switch (checkState)
                    {
                        case ContextMenuCheckState.Checked:   imageName = "MenuCheckOn";  break;
                        case ContextMenuCheckState.Unchecked: imageName = "MenuCheckOff"; break;
                        case ContextMenuCheckState.Radio:     imageName = "MenuRadio";    break;
                    }
                }

                if (string.IsNullOrEmpty(imageName))
                    imageName = "MenuBlank";

                var resName1 = $"FamiStudio.Resources.Mobile.Mobile{imageName}.tga";
                var resName2 = $"FamiStudio.Resources.Atlas.{imageName}@4x.tga";
                var resName = Utils.ResourceExists(resName1) ? resName1 : resName2;

                var bmp = new BitmapDrawable(Resources, DroidUtils.LoadTgaBitmapFromResource(resName));
                bmp.SetBounds(0, 0, imageSize, imageSize);
                bmp.SetColorFilter(BlendModeColorFilterCompat.CreateBlendModeColorFilterCompat(DroidUtils.GetColorFromResources(this, Resource.Color.LightGreyColor1), BlendModeCompat.SrcAtop));

                var textView = new TextView(new ContextThemeWrapper(this, Resource.Style.LightGrayTextMedium));
                textView.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
                textView.CompoundDrawablePadding = imagePad;
                textView.SetPadding(imagePad, imagePad, imagePad, imagePad);
                textView.Gravity = GravityFlags.CenterVertical;
                textView.Text = opt.Text;
                textView.Tag = new ContextMenuTag(opt);
                textView.Click += ContextMenuDialog_Click;
                textView.SetCompoundDrawables(bmp, null, null, null);

                linearLayout.AddView(textView);

                prevWantedSeparator = opt.Separator.HasFlag(ContextMenuSeparator.MobileAfter);
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
                // Needs to match the dialog.
                GradientDrawable invisible = new GradientDrawable();
                GradientDrawable navBar = new GradientDrawable();
                navBar.SetColor(bgColor);

                LayerDrawable windowBackground = new LayerDrawable(new Drawable[] { invisible, navBar });
                windowBackground.SetLayerInsetTop(1, metrics.HeightPixels);
                windowBackground.SetLayerWidth(1, contextMenuDialog.Behavior.MaxWidth);
                windowBackground.SetLayerGravity(1, GravityFlags.Center);
                contextMenuDialog.Window.SetBackgroundDrawable(windowBackground);
            }

            contextMenuDialog.Show();

            Platform.VibrateClick();
        }

        public void HideContextMenu()
        {
            // Only used on desktop.
            Debug.Assert(false);
        }

        private void ContextMenuDialog_DismissEvent(object sender, EventArgs e)
        {
            contextMenuDialog = null;
            MarkDirty();
        }

        private void ContextMenuDialog_Click(object sender, EventArgs e)
        {
            // HACK : We have a weird NULL crash here. No clue how to repro this.
            var tag = (sender as TextView)?.Tag as ContextMenuTag;

            lock (renderLock)
            {
                tag?.opt?.Callback();
                contextMenuDialog?.Dismiss();
                MarkDirty();
            }

            Platform.VibrateTick();
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
                alert.SetButton("Yes", (c, ev) => { lock (renderLock) { callback?.Invoke(DialogResult.Yes); } });
                alert.SetButton2("No", (c, ev) => { lock (renderLock) { callback?.Invoke(DialogResult.No);  } });
                if (buttons == MessageBoxButtons.YesNoCancel)
                    alert.SetButton3("Cancel", (c, ev) => { lock (renderLock) { callback?.Invoke(DialogResult.Cancel); } });
            }
            else
            {
                alert.SetButton("OK", (c, ev) => { lock (renderLock) { callback?.Invoke(DialogResult.OK); } });
            }

            alert.Show();
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
            if (!IsAsyncDialogInProgress)
            {
                if (e.Action == MotionEventActions.Up)
                {
                    //Debug.WriteLine($"Up {e.PointerCount} ({e.GetX()}, {e.GetY()})");
                    lock (renderLock)
                    {
                        var ctrl = GetCapturedControlAtCoord((int)e.GetX(), (int)e.GetY(), out var x, out var y);
                        if (ctrl != null)
                        {
                            if (captureControl == ctrl)
                                ReleaseMouse();
                            ctrl.SendTouchUp(new MouseEventArgs(x, y));
                        }
                    }
                }
                else if (e.Action == MotionEventActions.Move && !scaleDetector.IsInProgress)
                {
                    //Debug.WriteLine($"Move {e.PointerCount} ({e.GetX()}, {e.GetY()})");
                    lock (renderLock)
                        GetCapturedControlAtCoord((int)e.GetX(), (int)e.GetY(), out var x, out var y)?.SendTouchMove(new MouseEventArgs(x, y));
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
                //Debug.WriteLine($"OnDown {e.PointerCount} ({e.GetX()}, {e.GetY()})");
                lock (renderLock)
                    GetCapturedControlAtCoord((int)e.GetX(), (int)e.GetY(), out var x, out var y)?.SendTouchDown(new MouseEventArgs(x, y));
            }
            return false;
        }

        public bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
        {
            if (!IsAsyncDialogInProgress)
            {
                //Debug.WriteLine($"OnFling {e1.PointerCount} ({e1.GetX()}, {e1.GetY()}) ({velocityX}, {velocityY})");
                lock (renderLock)
                    GetCapturedControlAtCoord((int)e1.GetX(), (int)e1.GetY(), out var x, out var y)?.SendTouchFling(new MouseEventArgs(x, y, velocityX, velocityY));
            }
            return false;
        }

        public void OnLongPress(MotionEvent e)
        {
            if (!IsAsyncDialogInProgress)
            {
                //Debug.WriteLine($"OnLongPress {e.PointerCount} ({e.GetX()}, {e.GetY()})");
                lock (renderLock)
                    GetCapturedControlAtCoord((int)e.GetX(), (int)e.GetY(), out var x, out var y)?.SendTouchLongPress(new MouseEventArgs(x, y));
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
                //Debug.WriteLine($"{e.PointerCount} OnShowPress ({e.GetX()}, {e.GetY()})");
            }
        }

        public bool OnSingleTapUp(MotionEvent e)
        {
            if (!IsAsyncDialogInProgress)
            {
                //DialogTest();
                //StartSaveFileActivity("audio/mpeg", "Toto.mp3");

                //Debug.WriteLine($"{e.PointerCount} OnSingleTapUp ({e.GetX()}, {e.GetY()})");
                lock (renderLock)
                    GetCapturedControlAtCoord((int)e.GetX(), (int)e.GetY(), out var x, out var y)?.SendTouchClick(new MouseEventArgs(x, y));
            }
            return false;
        }

        public bool OnScale(ScaleGestureDetector detector)
        {
            if (!IsAsyncDialogInProgress)
            {
                //Debug.WriteLine($"OnScale ({detector.FocusX}, {detector.FocusY}) {detector.ScaleFactor}");
                lock (renderLock)
                    GetCapturedControlAtCoord((int)detector.FocusX, (int)detector.FocusY, out var x, out var y)?.SendTouchScale(new MouseEventArgs(x, y, detector.ScaleFactor));
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
                //Debug.WriteLine($"OnScaleBegin ({detector.FocusX}, {detector.FocusY})");
                lock (renderLock)
                    GetCapturedControlAtCoord((int)detector.FocusX, (int)detector.FocusY, out var x, out var y)?.SendTouchScaleBegin(new MouseEventArgs(x, y));
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
                //Debug.WriteLine($"OnScaleEnd ({detector.FocusX}, {detector.FocusY})");
                lock (renderLock)
                    GetCapturedControlAtCoord((int)detector.FocusX, (int)detector.FocusY, out var x, out var y)?.SendTouchScaleEnd(new MouseEventArgs(x, y));
            }
        }

        public bool OnDoubleTap(MotionEvent e)
        {
            if (!IsAsyncDialogInProgress)
            {
                //Debug.WriteLine($"OnDoubleTap ({e.GetX()}, {e.GetY()})");
                lock (renderLock)
                    GetCapturedControlAtCoord((int)e.GetX(), (int)e.GetY(), out var x, out var y)?.SendTouchDoubleClick(new MouseEventArgs(x, y));
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool OnDoubleTapEvent(MotionEvent e)
        {
            //Debug.WriteLine($"OnDoubleTapEvent ({e.GetX()}, {e.GetY()})");
            return false;
        }

        public bool OnSingleTapConfirmed(MotionEvent e)
        {
            //Debug.WriteLine($"OnSingleTapConfirmed ({e.GetX()}, {e.GetY()})");
            return false;
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

    public abstract class BaseDialogActivityInfo
    {
        protected int requestCode;
        public int  RequestCode => requestCode;
        public virtual bool ShouldSuspend => true;
        public virtual bool IsDialogDone(Result result) => true;
        public abstract void OnResult(FamiStudioWindow main, Result code, Intent data);
    };

    public class LoadDialogActivityInfo : BaseDialogActivityInfo
    {
        protected Action<string> callback;

        public LoadDialogActivityInfo(Action<string> cb)
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

    public class SaveDialogActivityInfo : BaseDialogActivityInfo
    {
        protected Action<string> callback;
        protected string lastSaveTempFile;
        protected Android.Net.Uri lastSaveFileUri;

        public SaveDialogActivityInfo(Action<string> cb)
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
                    Log.LogMessage(LogSeverity.Info, "Copying file to storage...");

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

    public class TutorialDialogActivityInfo : BaseDialogActivityInfo
    {
        protected TutorialDialog dialog;
        protected Action<DialogResult> callback;
        public TutorialDialog Dialog => dialog;
        public override bool ShouldSuspend => false;

        public TutorialDialogActivityInfo(TutorialDialog dlg, Action<DialogResult> cb)
        {
            requestCode = 2000;
            dialog = dlg;
            callback = cb;
        }

        public override void OnResult(FamiStudioWindow main, Result code, Intent data)
        {
            callback(code == Result.Ok ? DialogResult.OK : DialogResult.Cancel);
        }
    }

    public class PropertyDialogActivityInfo : BaseDialogActivityInfo
    {
        protected PropertyDialog dialog;
        protected Action<DialogResult> callback;
        public PropertyDialog Dialog => dialog;
        public override bool ShouldSuspend => false;

        public PropertyDialogActivityInfo(PropertyDialog dlg, Action<DialogResult> cb)
        {
            requestCode = 2001;
            dialog = dlg;
            callback = cb;
        }

        public override void OnResult(FamiStudioWindow main, Result code, Intent data)
        {
            callback(code == Result.Ok ? DialogResult.OK : DialogResult.Cancel);
        }
    }

    public class MultiPropertyDialogActivityInfo : BaseDialogActivityInfo
    {
        protected MultiPropertyDialog dialog;
        protected Action<DialogResult> callback;
        public MultiPropertyDialog Dialog => dialog;
        public override bool ShouldSuspend => false;

        public MultiPropertyDialogActivityInfo(MultiPropertyDialog dlg, Action<DialogResult> cb)
        {
            requestCode = 2002;
            dialog = dlg;
            callback = cb;
        }

        public override void OnResult(FamiStudioWindow main, Result code, Intent data)
        {
            callback(code == Result.Ok ? DialogResult.OK : DialogResult.Cancel);
        }
    }

    public class ShareActivityInfo : BaseDialogActivityInfo
    {
        protected Action callback;

        public ShareActivityInfo(Action cb)
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
