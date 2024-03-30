using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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

using ActionBar = AndroidX.AppCompat.App.ActionBar;

namespace FamiStudio
{
    public class TutorialDialog
    {
        public TutorialDialog(FamiStudioWindow parent)
        {
        }

        public void ShowDialogAsync(Action<DialogResult> callback)
        {
            FamiStudioWindow.Instance.StartTutorialDialogActivity(callback, this);
        }
    }

    public class MaxHeightImageView : ImageView
    {
        float ratio;
        float maxScreenHeight;

        public MaxHeightImageView(Context context, float rat = 1.0f, float max = 0.5f) : base(context)
        {
            ratio = rat;
            maxScreenHeight = max;
        }

        public void UpdateSizeConstraints(float rat, float max)
        {
            ratio = rat;
            maxScreenHeight = max;
            Invalidate();
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            var width = MeasureSpec.GetSize(widthMeasureSpec);
            var height = (int)(width * ratio);

            var percentHeight = height / (float)Context.Resources.DisplayMetrics.HeightPixels;
            if (percentHeight > maxScreenHeight)
            {
                height = (int)(Context.Resources.DisplayMetrics.HeightPixels * maxScreenHeight);
                width = (int)(height / ratio);
            }

            SetMeasuredDimension(width, height);
        }
    }

    [Activity(Theme = "@style/AppTheme.NoActionBar", ResizeableActivity = false, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.UiMode | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden, ScreenOrientation = ScreenOrientation.Behind)]
    public class TutorialDialogActivity : AppCompatActivity
    {
        public LocalizedString[] TutorialMessages = new LocalizedString[12];

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

        public TutorialDialogActivity()
        {
            Localization.Localize(this);
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

            imageView = new MaxHeightImageView(this, 540.0f / 1100.0f, 0.5f);
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

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.Tutorials.{filename}"))
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
                var delay = System.Math.Max(0, Gif.GetFrameDelay(gif) - decodeTime);

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