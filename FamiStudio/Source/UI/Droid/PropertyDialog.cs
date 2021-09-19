using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Fragment.App;
using AndroidX.Core.Widget;
using AndroidX.DrawerLayout.Widget;
using AndroidX.CoordinatorLayout.Widget;
using Google.Android.Material.AppBar;

using Debug        = System.Diagnostics.Debug;
using DialogResult = System.Windows.Forms.DialogResult;
using ActionBar    = AndroidX.AppCompat.App.ActionBar;

namespace FamiStudio
{
    public class PropertyDialog
    {
        public const int RequestCode = 1001;

        private PropertyPage propertyPage = new PropertyPage(FamiStudioForm.Instance);
        public  PropertyPage Properties => propertyPage;

        public PropertyDialog(int width, bool canAccept = true, bool canCancel = true, object parent = null)
        {
        }

        public PropertyDialog(System.Drawing.Point pt, int width, bool leftAlign = false, bool topAlign = false)
        {
        }

        public void ShowDialog(FamiStudioForm parent, Action<DialogResult> callback)
        {
            FamiStudioForm.Instance.StartDialogActivity(typeof(PropertyDialogActivity), RequestCode, callback, this);
        }
    }

    [Activity(Theme = "@style/AppTheme.NoActionBar")]
    public class PropertyDialogActivity : AppCompatActivity
    {
        private CoordinatorLayout coordLayout;
        private AppBarLayout appBarLayout;
        private NestedScrollView scrollView;
        private FragmentContainerView fragmentView;
        private AndroidX.AppCompat.Widget.Toolbar toolbar;

        const int ApplyItemId = 321;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var dlg = FamiStudioForm.Instance.DialogUserData as PropertyDialog;

            var appBarLayoutParams = new AppBarLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, DroidUtils.GetSizeAttributeInPixel(this, Android.Resource.Attribute.ActionBarSize));
            appBarLayoutParams.ScrollFlags = 0;

            toolbar = new AndroidX.AppCompat.Widget.Toolbar(new ContextThemeWrapper(this, Resource.Style.ToolbarTheme));
            toolbar.LayoutParameters = appBarLayoutParams;
            SetSupportActionBar(toolbar);

            ActionBar actionBar = SupportActionBar;
            if (actionBar != null)
            {
                actionBar.SetDisplayHomeAsUpEnabled(true);
                actionBar.SetHomeButtonEnabled(true);
                actionBar.SetHomeAsUpIndicator(Android.Resource.Drawable.IcMenuCloseClearCancel);
            }

            appBarLayout = new AppBarLayout(this);
            appBarLayout.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            appBarLayout.AddView(toolbar);

            fragmentView = new FragmentContainerView(this);
            fragmentView.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            fragmentView.Id = 123;

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

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            SetResult(item != null && item.ItemId == ApplyItemId ? Result.Ok : Result.Canceled);
            Finish();

            return base.OnOptionsItemSelected(item);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            menu.Clear();
            var item = menu.Add(IMenu.None, ApplyItemId, IMenu.None, "Apply"); // DROIDTODO : Make the "verb" configurable.
            item.SetShowAsAction(ShowAsAction.Always);

            return true;
        }
    }
}