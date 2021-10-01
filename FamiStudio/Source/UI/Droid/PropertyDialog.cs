using System;
using Android.Text;
using Android.Text.Style;
using Android.App;
using Android.OS;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.Fragment.App;
using AndroidX.Core.Widget;
using AndroidX.CoordinatorLayout.Widget;
using Google.Android.Material.AppBar;
using Java.Lang;

using Debug        = System.Diagnostics.Debug;
using DialogResult = System.Windows.Forms.DialogResult;
using ActionBar    = AndroidX.AppCompat.App.ActionBar;
using Android.Runtime;
using Android.Content;

namespace FamiStudio
{
    public class PropertyDialog
    {
        public const int RequestCode = 1001;

        private string title = "";
        private string verb = "Apply";
        private bool canAccept = true;
        private bool canCancel = true;
        private PropertyPage propertyPage = new PropertyPage(FamiStudioForm.Instance);

        public PropertyPage Properties => propertyPage;
        public string Title => title;
        public string Verb  => verb;
        public bool CanAccept => canAccept;
        public bool CanCancel => canCancel;

        public delegate void CloseRequestDelegate(DialogResult result);
        public event CloseRequestDelegate CloseRequested;

        public PropertyDialog(string text, int width, bool canAccept = true, bool canCancel = true, object parent = null)
        {
            this.title = text;
            this.canAccept = canAccept;
            this.canCancel = canCancel;
        }

        public PropertyDialog(string text, System.Drawing.Point pt, int width, bool leftAlign = false, bool topAlign = false)
        {
            title = text;
        }

        public void SetVerb(string text)
        {
            verb = text;
        }

        public void CloseWithResult(DialogResult result)
        {
            CloseRequested?.Invoke(result);
        }

        public void ShowDialogAsync(FamiStudioForm parent, Action<DialogResult> callback)
        {
            FamiStudioForm.Instance.StartPropertyDialogActivity(callback, this);
        }
    }

    [Activity(Theme = "@style/AppTheme.NoActionBar")]
    public class PropertyDialogActivity : AppCompatActivity
    {
        private const int FragmentViewId  = 1008;
        private const int ApplyMenuItemId = 1009;

        private CoordinatorLayout coordLayout;
        private AppBarLayout appBarLayout;
        private NestedScrollView scrollView;
        private FragmentContainerView fragmentView;
        private AndroidX.AppCompat.Widget.Toolbar toolbar;
        private PropertyDialog dlg;
        private bool stoppedByUser;

        public PropertyDialogActivity()
        {
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var info = FamiStudioForm.Instance != null ? FamiStudioForm.Instance.ActiveDialog as PropertyDialogActivityInfo : null;

            if (savedInstanceState != null || info == null)
            {
                Finish();
                return;
            }

            dlg = info.Dialog;
            dlg.CloseRequested += Dlg_CloseRequested;

            var appBarLayoutParams = new AppBarLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, DroidUtils.GetSizeAttributeInPixel(this, Android.Resource.Attribute.ActionBarSize));
            appBarLayoutParams.ScrollFlags = 0;

            toolbar = new AndroidX.AppCompat.Widget.Toolbar(new ContextThemeWrapper(this, Resource.Style.ToolbarTheme));
            toolbar.LayoutParameters = appBarLayoutParams;
            toolbar.SetTitleTextAppearance(this, Resource.Style.LightGrayTextMediumBold);
            SetSupportActionBar(toolbar);

            ActionBar actionBar = SupportActionBar;
            if (actionBar != null)
            {
                actionBar.SetDisplayHomeAsUpEnabled(true);
                actionBar.SetHomeButtonEnabled(true);
                actionBar.SetHomeAsUpIndicator(Android.Resource.Drawable.IcMenuCloseClearCancel);
                actionBar.Title = dlg.Title;
            }

            appBarLayout = new AppBarLayout(this);
            appBarLayout.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            appBarLayout.AddView(toolbar);

            fragmentView = new FragmentContainerView(this);
            fragmentView.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            fragmentView.Id = FragmentViewId;

            var scrollViewLayoutParams = new CoordinatorLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            scrollViewLayoutParams.Behavior = new AppBarLayout.ScrollingViewBehavior(this, null);

            scrollView = new NestedScrollView(new ContextThemeWrapper(this, Resource.Style.DarkBackgroundStyle));
            scrollView.LayoutParameters = scrollViewLayoutParams;
            scrollView.AddView(fragmentView);

            coordLayout = new CoordinatorLayout(this);
            coordLayout.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            coordLayout.AddView(appBarLayout);
            coordLayout.AddView(scrollView);

            SetContentView(coordLayout);

            SupportFragmentManager.BeginTransaction().SetReorderingAllowed(true).Add(fragmentView.Id, dlg.Properties, "PropertyDialog").Commit();
        }

        public override void OnBackPressed()
        {
            stoppedByUser = true;
            base.OnBackPressed();
        }

        private void Dlg_CloseRequested(DialogResult result)
        {
            stoppedByUser = true;
            SetResult(result == DialogResult.OK ? Result.Ok : Result.Canceled);
            Finish();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            dlg.CloseRequested -= Dlg_CloseRequested;
        }

        protected override void OnPause()
        {
            // If we are being stopped, but not by the user closing the dialog,
            // it is likely that the user switched app. If the main activity isnt
            // running, lets suspend FamiStudio.
            if (!stoppedByUser && !FamiStudioForm.ActivityRunning)
                FamiStudio.StaticInstance.Suspend();
            base.OnPause();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            stoppedByUser = true;
            SetResult(item != null && item.ItemId == ApplyMenuItemId ? Result.Ok : Result.Canceled);
            Finish();

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

            if (dlg.CanAccept)
            {
                var item = menu.Add(IMenu.None, ApplyMenuItemId, IMenu.None, SetMenuItemFont(dlg.Verb, Resource.Style.LightGrayTextMedium));
                item.SetShowAsAction(ShowAsAction.Always);
            }

            return true;
        }
    }
}