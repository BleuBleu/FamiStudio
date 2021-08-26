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

using Debug = System.Diagnostics.Debug;
using DialogResult = System.Windows.Forms.DialogResult;
using ActionBar = AndroidX.AppCompat.App.ActionBar;
using Android.Graphics.Drawables;

namespace FamiStudio
{
    public class MultiPropertyDialog
    {
        public const int RequestCode = 1002;

        public class PropertyPageTab
        {
            public string text;
            public string image;
            public LinearLayout button;
            public PropertyPage properties;
            public bool visible = true;
        }

        private int selectedIndex = 0;
        private List<PropertyPageTab> tabs = new List<PropertyPageTab>();

        public int PageCount => tabs.Count;
        public int SelectedIndex => selectedIndex;
        
        public MultiPropertyDialog(int width, int height, int tabsWidth = 150)
        {

        }

        public PropertyPage AddPropertyPage(string text, string image)
        {
            var tab = new PropertyPageTab();
            tab.text = text;
            tab.image = image;
            tab.properties = new PropertyPage(FamiStudioForm.Instance);
            tabs.Add(tab);

            return tab.properties;
        }

        public void SetPageVisible(int idx, bool visible)
        {
            tabs[idx].visible = visible;
        }

        public PropertyPage GetPropertyPage(int idx)
        {
            return tabs[idx].properties;
        }

        public PropertyPageTab GetPropertyPageTab(int idx)
        {
            return tabs[idx];
        }

        public void ShowDialog(FamiStudioForm parent, Action<DialogResult> callback)
        {
            FamiStudioForm.Instance.StartDialogActivity(typeof(MultiPropertyDialogActivity), RequestCode, callback, this);
        }
    }

    [Activity(Theme = "@style/AppTheme.NoActionBar")]
    public class MultiPropertyDialogActivity : AppCompatActivity, View.IOnClickListener
    {
        private CoordinatorLayout coordLayout;
        private AppBarLayout appBarLayout;
        private NestedScrollView scrollView;
        private FragmentContainerView fragmentView;
        private MultiPropertyTabFragment tabsFragment;
        private AndroidX.AppCompat.Widget.Toolbar toolbar;
        private MultiPropertyDialog dialog;
        private IMenuItem applyMenuItem;
        private UpdateToolbarRunnable updateToolbarRunnable;

        private int selectedTabIndex = -1; // -1 means in the tab page.

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            dialog = FamiStudioForm.Instance.DialogUserData as MultiPropertyDialog;
            updateToolbarRunnable = new UpdateToolbarRunnable(this);
            tabsFragment = new MultiPropertyTabFragment(dialog);
            
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

            SupportFragmentManager.BeginTransaction()
                .Add(fragmentView.Id, tabsFragment, "MultiPropertyDialogTabs")
                .Commit();
        }

        // MATTT : WHy does this cause problem when changing fragment.
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            if (applyMenuItem == null)
            {
                menu.Clear();
                applyMenuItem = menu.Add(IMenu.None, 123, IMenu.None, "Apply"); // MATTT : Label + ID.
                UpdateToolbar(false);
            }

            return true;
        }

        public void UpdateToolbar(bool showApplyButton)
        {
            applyMenuItem.SetShowAsAction(showApplyButton ? ShowAsAction.Always : ShowAsAction.Never);
            applyMenuItem.SetVisible(showApplyButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        public void OnClick(View v)
        {
            if (selectedTabIndex == -1)
            {
                for (int i = 0; i < dialog.PageCount; i++)
                {
                    var tab = dialog.GetPropertyPageTab(i);
                    if (tab.button == v)
                    {
                        selectedTabIndex = i;
                        UpdateToolbar(false);
                        SupportFragmentManager.BeginTransaction()
                            .SetCustomAnimations(Resource.Animation.slide_in_right, Resource.Animation.fade_out)
                            .Replace(fragmentView.Id, tab.properties, "MultiPropertyDialog")
                            .RunOnCommit(updateToolbarRunnable)
                            .Commit();
                        break;
                    }
                }
            }
        }

        public override void OnBackPressed()
        {
            if (selectedTabIndex >= 0)
            {
                selectedTabIndex = -1;
                UpdateToolbar(false);
                SupportFragmentManager.BeginTransaction()
                    .SetCustomAnimations(Resource.Animation.fade_in, Resource.Animation.slide_out_right)
                    .Replace(fragmentView.Id, tabsFragment, "MultiPropertyDialogTabs")
                    .Commit();
            }
            else
            {
                base.OnBackPressed();
            }
        }

        private class UpdateToolbarRunnable : Java.Lang.Object, Java.Lang.IRunnable
        {
            MultiPropertyDialogActivity activity;

            public UpdateToolbarRunnable(MultiPropertyDialogActivity act)
            {
                activity = act;
            }

            public void Run()
            {
                activity.UpdateToolbar(true);
            }
        }

        private class MultiPropertyTabFragment : AndroidX.Fragment.App.Fragment
        {
            private MultiPropertyDialog dialog;

            public MultiPropertyTabFragment(MultiPropertyDialog dlg)
            {
                dialog = dlg;
            }

            public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
            {
                var acticity = container.Context as MultiPropertyDialogActivity;

                var dp2  = DroidUtils.DpToPixels(2);
                var dp10 = DroidUtils.DpToPixels(10);
                var dp36 = DroidUtils.DpToPixels(36);

                var linearLayoutParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
                linearLayoutParams.SetMargins(dp2, dp2, dp2, dp2);

                var linearLayout = new LinearLayout(container.Context);
                linearLayout.Orientation = Orientation.Vertical;
                linearLayout.LayoutParameters = linearLayoutParams;

                for (int i = 0; i < dialog.PageCount; i++)
                {
                    var tab = dialog.GetPropertyPageTab(i);

                    if (!tab.visible)
                        continue;

                    if (i > 0)
                    {
                        var spacer = new View(container.Context);
                        spacer.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 1);
                        spacer.SetBackgroundColor(Android.Graphics.Color.LightGray); // MATTT color.
                        linearLayout.AddView(spacer);
                    }

                    var imageView = new ImageView(container.Context);
                    imageView.LayoutParameters = new LinearLayout.LayoutParams(dp36, dp36);
                    imageView.SetImageBitmap(PlatformUtils.LoadBitmapFromResource($"FamiStudio.Resources.{tab.image}@2x.png", true));

                    var textViewLayoutParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
                    textViewLayoutParams.Gravity = GravityFlags.Left | GravityFlags.CenterVertical;

                    var textView = new TextView(new ContextThemeWrapper(container.Context, Resource.Style.LightGrayLargeBold));
                    textView.Text = tab.text;
                    textView.LayoutParameters = textViewLayoutParams;
                    textView.SetPadding(dp10, 0, 0, 0);

                    var buttonLayoutParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
                    buttonLayoutParams.SetMargins(dp10, dp10, dp10, dp10);

                    var buttonLayout = new LinearLayout(container.Context);
                    buttonLayout.LayoutParameters = buttonLayoutParams;
                    buttonLayout.AddView(imageView);
                    buttonLayout.AddView(textView);
                    buttonLayout.SetOnClickListener(acticity);
                    
                    linearLayout.AddView(buttonLayout);

                    tab.button = buttonLayout;
                }

                return linearLayout;
            }
        }
    }
}