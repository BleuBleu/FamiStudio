using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Android.Text;
using Android.Text.Style;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Content;
using Android.Content.PM;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.CoordinatorLayout.Widget;
using Google.Android.Material.AppBar;
using Java.Lang;
using Java.Util;

using Debug = System.Diagnostics.Debug;
using ActionBar = AndroidX.AppCompat.App.ActionBar;
using System.Threading;
using System.Threading.Tasks;

namespace FamiStudio
{
    public class TutorialDialog
    {
        public void ShowDialogAsync(FamiStudioWindow parent, Action<DialogResult> callback)
        {
            FamiStudioWindow.Instance.StartTutorialDialogActivity(callback, this);
        }
    }

    [Activity(Theme = "@style/AppTheme.NoActionBar", ResizeableActivity = false, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class TutorialDialogActivity : AppCompatActivity
    {
        public static readonly string[] TutorialMessages = new[]
        {
            "(1/11) Welcome to FamiStudio! Let's take a few seconds to review some of the basic controls to make sure you use the app to its fullest!",
            "(2/11) A QUICK TAP will usually ADD stuff, like patterns or notes.",
            "(3/11) A QUICK TAP will also toggle the HIGHLIGHT around certain objects. Object with a WHITE HIGHLIGHT can be moved and/or interacted with in a special way.",
            "(4/11) A SWIPE will usually PAN around. A SWIPE in the header will select stuff.",
            "(5/11) A SWIPE starting from the HIGHLIGHTED object may allow you to move it, or change some of its properties.",
            "(6/11) You can PINCH TO ZOOM in the Piano Roll and the Sequencer.",
            "(7/11) A LONG PRESS on some objects will reveal a CONTEXT MENU containing more advanced editing options. Try it everywhere and see what happens!",
            "(8/11) You can access the 3 main views (Sequencer, Piano Roll and Project Explorer) from the QUICK ACCESS BAR located on the right (or bottom in portrait mode).",
            "(9/11) For the complete DOCUMENTATION and a VIDEO TUTORIAL, please click on the big QUESTION MARK!",
            "(10/11) Using BLUETOOTH adds a lot of AUDIO LATENCY (delay in the audio). There is unfortunately nothing FamiStudio can do about this. Wired headphones or speakers will have much lower LATENCY.",
            "(11/11) Join us on DISCORD to meet other FamiStudio users and share your songs with them! Link in the documentation."
        };

        private const int NextMenuItemId = 1010;

        private CoordinatorLayout coordLayout;
        private TextView textView;
        private AppBarLayout appBarLayout;
        private AndroidX.AppCompat.Widget.Toolbar toolbar;
        private MaxHeightImageView imageView;

        private ManualResetEvent gifQuitEvent = new ManualResetEvent(false);
        private Task gifTask;
        private IntPtr gif;
        private Android.Graphics.Bitmap gifBmp;
        private int gifSizeX;
        private int gifSizeY;
        private byte[] gifData;
        private byte[] gifBuffer;
        private GCHandle gifHandle;

        private int pageIndex = 0;
        private bool stoppedByUser;

        private class MaxHeightImageView : ImageView
        {
            public MaxHeightImageView(Context context) : base(context)
            {
            }

            protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
            {
                var width = MeasureSpec.GetSize(widthMeasureSpec);
                var ratio = 540.0 / 1100.0;
                var height = (int)(width * ratio);

                const float MaxHeightScreen = 0.5f;

                var percentHeight = height / (float)Context.Resources.DisplayMetrics.HeightPixels;
                if (percentHeight > MaxHeightScreen)
                {
                    height = (int)(Context.Resources.DisplayMetrics.HeightPixels * MaxHeightScreen);
                    width = (int)(height / ratio);
                }

                SetMeasuredDimension(width, height);
            }
        }

        public TutorialDialogActivity()
        {
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var info = FamiStudioWindow.Instance != null ? FamiStudioWindow.Instance.ActiveDialog as TutorialDialogActivityInfo : null;

            if (savedInstanceState != null || info == null)
            {
                Finish();
                return;
            }

            var appBarLayoutParams = new AppBarLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, DroidUtils.GetSizeAttributeInPixel(this, Android.Resource.Attribute.ActionBarSize));
            appBarLayoutParams.ScrollFlags = 0;

            toolbar = new AndroidX.AppCompat.Widget.Toolbar(new ContextThemeWrapper(this, Resource.Style.ToolbarTheme));
            toolbar.LayoutParameters = appBarLayoutParams;
            toolbar.SetTitleTextAppearance(this, Resource.Style.LightGrayTextMediumBold);
            SetSupportActionBar(toolbar);

            ActionBar actionBar = SupportActionBar;
            if (actionBar != null)
                actionBar.Title = "Welcome to FamiStudio!";

            appBarLayout = new AppBarLayout(this);
            appBarLayout.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            appBarLayout.AddView(toolbar);

            var margin = DroidUtils.DpToPixels(10);

            var linearLayoutParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            linearLayoutParams.Gravity = GravityFlags.Center;
            linearLayoutParams.SetMargins(margin, margin, margin, margin);

            textView = new TextView(new ContextThemeWrapper(this, Resource.Style.LightGrayTextMedium));
            textView.Text = TutorialMessages[0];
            textView.LayoutParameters = linearLayoutParams;

            imageView = new MaxHeightImageView(this);
            imageView.LayoutParameters = linearLayoutParams;

            var coordLayoutParams = new CoordinatorLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent); ;
            coordLayoutParams.Behavior = new AppBarLayout.ScrollingViewBehavior(this, null);

            var linearLayout = new LinearLayout(new ContextThemeWrapper(this, Resource.Style.DarkBackgroundStyle));
            linearLayout.LayoutParameters = coordLayoutParams;
            linearLayout.Orientation = Android.Widget.Orientation.Vertical;
            linearLayout.AddView(textView);
            linearLayout.AddView(imageView);

            coordLayout = new CoordinatorLayout(this);
            coordLayout.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            coordLayout.AddView(appBarLayout);
            coordLayout.AddView(linearLayout);

            RefreshPage();
            SetContentView(coordLayout);
        }

        private void PrevPage()
        {
            if (pageIndex == 0)
            {
                stoppedByUser = true;
                SetResult(Result.Canceled);
                CloseGif();
                Finish();
            }
            else
            {
                pageIndex--;
                RefreshPage();
            }
        }

        private void NextPage()
        {
            if (pageIndex == TutorialMessages.Length - 1)
            {
                stoppedByUser = true;
                CloseGif();
                SetResult(Result.Ok);
                Finish();
            }
            else
            {
                pageIndex++;
                RefreshPage();
            }
        }

        private void OpenGif(string filename)
        {
            CloseGif();

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.{filename}"))
            {
                gifData = new byte[stream.Length];
                stream.Read(gifData, 0, (int)stream.Length);
                stream.Close();
            }

            gifHandle = GCHandle.Alloc(gifData, GCHandleType.Pinned);
            gif = Gif.Open(gifHandle.AddrOfPinnedObject(), 1);
            gifSizeX = Gif.GetWidth(gif);
            gifSizeY = Gif.GetHeight(gif);
            gifBmp = Android.Graphics.Bitmap.CreateBitmap(gifSizeX, gifSizeY, Android.Graphics.Bitmap.Config.Argb8888, false);
            gifBuffer = new byte[gifSizeX * gifSizeY * 4];
            imageView.SetImageBitmap(gifBmp);

            AdvanceFrame();
            UpdateImage();
            StartGifTask();
        }

        private void CloseGif()
        {
            if (gif != IntPtr.Zero)
            {
                StopGifTask();
                gifData = null;
                gifBuffer = null;
                Gif.Close(gif);
                gif = IntPtr.Zero;
                gifHandle.Free();
            }
        }
        private unsafe int AdvanceFrame()
        {
            var start = SystemClock.UptimeMillis();
            Gif.AdvanceFrame(gif);
            lock (gifBuffer)
            {
                fixed (byte* p = &gifBuffer[0])
                    Gif.RenderFrame(gif, (IntPtr)p, gifSizeX * 4, 4);
            }
            return (int)(SystemClock.UptimeMillis() - start);
        }

        private void UpdateImage()
        {
            if (gifBuffer != null)
            {
                lock (gifBuffer)
                {
                    var pixels = gifBmp.LockPixels();
                    Marshal.Copy(gifBuffer, 0, pixels, gifBuffer.Length);
                    gifBmp.UnlockPixels();
                }

                imageView.Invalidate();
            }
        }

        private void StartGifTask()
        {
            Debug.Assert(gifTask == null);

            gifQuitEvent.Reset();
            gifTask = Task.Factory.StartNew(GifTask, TaskCreationOptions.LongRunning);
        }

        private void StopGifTask()
        {
            gifQuitEvent.Set();
            gifTask.Wait();
            gifTask = null;
        }

        private void RefreshPage()
        {
            textView.Text = TutorialMessages[pageIndex];

            OpenGif($"MobileTutorial{pageIndex + 1}.gif");
        }

        public override void OnBackPressed()
        {
            if (pageIndex > 0)
            {
                PrevPage();
            }
            else
            {
                stoppedByUser = true;
                base.OnBackPressed();
            }
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            NextPage();
            return base.OnOptionsItemSelected(item);
        }

        private ICharSequence SetMenuItemFont(string text, int resId)
        {
            SpannableStringBuilder sb = new SpannableStringBuilder(text);
            sb.SetSpan(new TextAppearanceSpan(this, resId), 0, text.Length, 0);
            return sb;
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            menu.Clear();
            var item = menu.Add(IMenu.None, NextMenuItemId, IMenu.None, SetMenuItemFont("NEXT", Resource.Style.LightGrayTextMedium));
            item.SetShowAsAction(ShowAsAction.Always);
            return true;
        }

        protected override void OnPause()
        {
            // If we are being stopped, but not by the user closing the dialog,
            // it is likely that the user switched app. If the main activity isnt
            // running, lets suspend FamiStudio.
            if (!stoppedByUser && !FamiStudioWindow.ActivityRunning)
                FamiStudio.StaticInstance.Suspend();
            base.OnPause();
        }

        private unsafe void GifTask()
        {
            // The first frame is always decoded on the main thread, so wait here.
            if (gifQuitEvent.WaitOne(Gif.GetFrameDelay(gif)))
            {
                return;
            }

            while (true)
            {
                var decodeTime = AdvanceFrame();
                var delay = Gif.GetFrameDelay(gif) - decodeTime;

                Debug.WriteLine($"Decode time {decodeTime}");

                RunOnUiThread(() => { UpdateImage(); });

                if (gifQuitEvent.WaitOne(delay))
                {
                    break;
                }
            }
        }
    }
}