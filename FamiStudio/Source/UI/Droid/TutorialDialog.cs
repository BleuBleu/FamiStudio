using System;
using Android.Text;
using Android.Text.Style;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Runtime;
using Android.Content;
using Android.Content.Res;
using Android.Content.PM;
using AndroidX.AppCompat.App;
using AndroidX.Fragment.App;
using AndroidX.Core.Widget;
using AndroidX.CoordinatorLayout.Widget;
using Google.Android.Material.AppBar;
using Java.Lang;

using Debug        = System.Diagnostics.Debug;
using DialogResult = System.Windows.Forms.DialogResult;
using ActionBar    = AndroidX.AppCompat.App.ActionBar;
using Android.Widget;
using Android.Webkit;
using AndroidX.ConstraintLayout.Widget;
using Android.Util;

namespace FamiStudio
{
    public class TutorialDialog
    {
        public void ShowDialogAsync(FamiStudioForm parent, Action<DialogResult> callback)
        {
            FamiStudioForm.Instance.StartTutorialDialogActivity(callback, this);
        }
    }

    [Activity(Theme = "@style/AppTheme.NoActionBar", ResizeableActivity = false, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class TutorialDialogActivity : AppCompatActivity
    {
        public static readonly string[] TutorialMessages = new[]
        {
            "(1/10) Welcome to FamiStudio! Let's take a few seconds to review some of the basic controls to make sure you use the app to its fullest!",
            "(2/10) A QUICK TAP will usually ADD stuff, like patterns or notes.",
            "(3/10) A QUICK TAP will also toggle the HIGHLIGHT around certain objects. Object with a WHITE HIGHLIGHT can be moved and/or interacted with in a special way.",
            "(4/10) A SWIPE will usually PAN around. A SWIPE in the header will select stuff.",
            "(5/10) A SWIPE starting from the HIGHLIGHTED object may allow you to move it, or change some of its properties.",
            "(6/10) You can PINCH TO ZOOM in the Piano Roll and the Sequencer.",
            "(7/10) A LONG PRESS on some objects will reveal a CONTEXT MENU containing more advanced editing options. Try it everywhere and see what happens!",
            "(8/10) You can access the 3 main views (Sequencer, Piano Roll and Project Explorer) from the QUICK ACCESS BAR located on the right (or bottom in portrait mode).",
            "(9/10) For the complete DOCUMENTATION and a VIDEO TUTORIAL, please click on the big QUESTION MARK!",
            "(10/10) Join us on DISCORD to meet other FamiStudio users and share your songs with them! Link in the documentation."
        };

        private const int NextMenuItemId = 1010;

        private CoordinatorLayout coordLayout;
        private TutorialDialog dlg;
        private TextView textView;
        private AppBarLayout appBarLayout;
        private AndroidX.AppCompat.Widget.Toolbar toolbar;
        private MaxHeightWebView webView;
        private int pageIndex = 0;

        private class MaxHeightWebView : WebView
        {
            public MaxHeightWebView(Context context) : base(context)
            {
            }

            protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
            {
                var width  = MeasureSpec.GetSize(widthMeasureSpec);
                var ratio  = 540.0 / 1170; // MATTT;
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

            var info = FamiStudioForm.Instance != null ? FamiStudioForm.Instance.ActiveDialog as TutorialDialogActivityInfo : null;

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

            webView = new MaxHeightWebView(this);
            webView.LayoutParameters = linearLayoutParams;

            var coordLayoutParams = new CoordinatorLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent); ;
            coordLayoutParams.Behavior = new AppBarLayout.ScrollingViewBehavior(this, null);

            var linearLayout = new LinearLayout(new ContextThemeWrapper(this, Resource.Style.DarkBackgroundStyle));
            linearLayout.LayoutParameters = coordLayoutParams;
            linearLayout.Orientation = Android.Widget.Orientation.Vertical;
            linearLayout.AddView(textView);
            linearLayout.AddView(webView);

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
                SetResult(Result.Canceled);
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
                Finish();
            }
            else
            {
                pageIndex++;
                RefreshPage();
            }
        }

        private void RefreshPage()
        {
            textView.Text = TutorialMessages[pageIndex];
            webView.LoadUrl($"file:///android_asset/Source/UI/Droid/Assets/tutorial{pageIndex + 1}.html");
        }

        public override void OnBackPressed()
        {
            if (pageIndex > 0)
            {
                PrevPage();
            }
            else
            {
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
            //// If we are being stopped, but not by the user closing the dialog,
            //// it is likely that the user switched app. If the main activity isnt
            //// running, lets suspend FamiStudio.
            //if (!stoppedByUser && !FamiStudioForm.ActivityRunning)
            //    FamiStudio.StaticInstance.Suspend();
            base.OnPause();
        }
    }
}