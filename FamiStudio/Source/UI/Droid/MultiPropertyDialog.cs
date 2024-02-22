using System;
using System.Collections.Generic;
using Android.App;
using Android.OS;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using Android.Content.PM;
using AndroidX.AppCompat.App;
using AndroidX.Fragment.App;
using AndroidX.Core.Widget;
using AndroidX.CoordinatorLayout.Widget;
using Google.Android.Material.AppBar;
using Java.Lang;

using ActionBar = AndroidX.AppCompat.App.ActionBar;
using AndroidX.Core.Graphics;

namespace FamiStudio
{
    public class MultiPropertyDialog
    {
        public class PropertyPageTab
        {
            public string text;
            public string image;
            public LinearLayout button;
            public PropertyPage properties;
            public string customVerb;
            public bool visible = true;
            public int backTab = -1;
        }

        private string title;
        private string verb = "Apply";
        private bool showVerbOnTabPage = false;
        private int selectedIndex = -1;
        private List<PropertyPageTab> tabs = new List<PropertyPageTab>();

        public string Title => title;
        public string Verb => verb;
        public bool ShowVerbOnTabPage => showVerbOnTabPage;
        public int PageCount => tabs.Count;
        public int SelectedIndex => selectedIndex;
        public FamiStudioWindow ParentWindow => FamiStudioWindow.Instance;

        public delegate void PageChangingDelegate(int oldPage, int newPage);
        public delegate void CustomVerbActivatedDelegate();
        public event PageChangingDelegate PageChanging;
        public event CustomVerbActivatedDelegate CustomVerbActivated;

        public MultiPropertyDialog(FamiStudioWindow win, string text, int width, int tabsWidth = 150)
        {
            title = text;
        }

        public void SetVerb(string text, bool showOnTabPage = false)
        {
            verb = text;
            showVerbOnTabPage = showOnTabPage;
        }

        public PropertyPage AddPropertyPage(string text, string image)
        {
            var tab = new PropertyPageTab();
            tab.text = text;
            tab.image = image;
            tab.properties = new PropertyPage(FamiStudioWindow.Instance);
            tabs.Add(tab);

            return tab.properties;
        }

        public void SetPageVisible(int idx, bool visible)
        {
            tabs[idx].visible = visible;
        }

        public void SetSelectedIndex(int idx)
        {
            if (selectedIndex != idx)
            {
                PageChanging?.Invoke(selectedIndex, idx);
                selectedIndex = idx;
            }
        }

        public PropertyPage GetPropertyPage(int idx)
        {
            return tabs[idx].properties;
        }

        public PropertyPageTab GetPropertyPageTab(int idx)
        {
            return tabs[idx];
        }

        public void SwitchToPage(int idx)
        {
            var multiDialogActivity = Xamarin.Essentials.Platform.CurrentActivity as MultiPropertyDialogActivity;
            if (multiDialogActivity != null && multiDialogActivity.Dialog == this)
            {
                multiDialogActivity.SwitchToPage(idx);
            }
        }

        public void SetPageBackPage(int idx, int backIdx)
        {
            tabs[idx].backTab = backIdx;
        }

        public void AddPageCustomVerb(int idx, string verb)
        {
            tabs[idx].customVerb = verb;
        }

        public void ActivateCustomVerb()
        {
            Debug.Assert(tabs[selectedIndex].customVerb != null);
            CustomVerbActivated?.Invoke();
        }

        public void ShowDialogAsync(Action<DialogResult> callback)
        {
            FamiStudioWindow.Instance.StartMultiPropertyDialogActivity(callback, this);
        }
    }

    [Activity(Theme = "@style/AppTheme.NoActionBar", ResizeableActivity = false, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.UiMode | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden, ScreenOrientation = ScreenOrientation.Behind)]
    public class MultiPropertyDialogActivity : AppCompatActivity, View.IOnClickListener
    {
        private const int FragmentViewId       = 1008;
        private const int ApplyMenuItemId      = 1009;
        private const int CustomVerbMenuItemId = 1010;

        private CoordinatorLayout coordLayout;
        private AppBarLayout appBarLayout;
        private NestedScrollView scrollView;
        private FragmentContainerView fragmentView;
        private MultiPropertyTabFragment tabsFragment;
        private AndroidX.AppCompat.Widget.Toolbar toolbar;
        private MultiPropertyDialog dlg;
        private IMenuItem applyMenuItem;
        private bool stoppedByUser;

        private int selectedTabIndex = -1; // -1 means in the tab page.

        public MultiPropertyDialog Dialog => dlg;

        public MultiPropertyDialogActivity()
        {
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var info = FamiStudioWindow.Instance != null ? FamiStudioWindow.Instance.ActiveDialog as MultiPropertyDialogActivityInfo : null;

            if (savedInstanceState != null || info == null)
            {
                Finish();
                return;
            }

            dlg = info.Dialog;
            tabsFragment = new MultiPropertyTabFragment(dlg);
            
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
                actionBar.SetHomeAsUpIndicator(global::FamiStudio.Resource.Drawable.cross);
                actionBar.Title = dlg.Title;
            }

            appBarLayout = new AppBarLayout(this);
            appBarLayout.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            appBarLayout.AddView(toolbar);

            var fragmentViewLayoutParams = new NestedScrollView.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            fragmentViewLayoutParams.Gravity = GravityFlags.Fill;

            fragmentView = new FragmentContainerView(this);
            fragmentView.LayoutParameters = fragmentViewLayoutParams;
            fragmentView.Id = FragmentViewId;

            var scrollViewLayoutParams = new CoordinatorLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            scrollViewLayoutParams.Behavior = new AppBarLayout.ScrollingViewBehavior(this, null);

            scrollView = new NestedScrollView(new ContextThemeWrapper(this, Resource.Style.DarkBackgroundStyle));
            scrollView.LayoutParameters = scrollViewLayoutParams;
            scrollView.FillViewport = true;
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

        protected override void OnPause()
        {
            // If we are being stopped, but not by the user closing the dialog,
            // it is likely that the user switched app. If the main activity isnt
            // running, lets suspend FamiStudio.
            if (!stoppedByUser && !FamiStudioWindow.ActivityRunning)
                FamiStudio.StaticInstance.Suspend();
            base.OnPause();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            var customVerbPressed   = item.ItemId == CustomVerbMenuItemId;
            var applyPressed        = item.ItemId == ApplyMenuItemId;
            var backOrClosedPressed = !customVerbPressed && !applyPressed;

            if (customVerbPressed)
            {
                dlg.ActivateCustomVerb();
            }
            else
            {
                var finish = true;

                if (backOrClosedPressed && selectedTabIndex >= 0)
                {
                    var tab = dlg.GetPropertyPageTab(selectedTabIndex);
                    if (tab.backTab >= 0)
                    {
                        SwitchToPage(tab.backTab);
                        finish = false;
                    }
                }

                if (finish)
                {
                    stoppedByUser = true;
                    SetResult(item != null && applyPressed ? Result.Ok : Result.Canceled);
                    Finish();
                }
            }
            
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
            applyMenuItem = menu.Add(IMenu.None, ApplyMenuItemId, IMenu.None, SetMenuItemFont(dlg.Verb, Resource.Style.LightGrayTextMedium));

            var showApplyButton = false;

            if (selectedTabIndex >= 0)
            {
                var tab = dlg.GetPropertyPageTab(selectedTabIndex);
                if (tab.customVerb != null)
                    menu.Add(IMenu.None, CustomVerbMenuItemId, IMenu.None, SetMenuItemFont(tab.customVerb, Resource.Style.LightGrayTextMedium));
                showApplyButton = tab.backTab < 0;
            }
            else
            {
                showApplyButton = dlg.ShowVerbOnTabPage;
            }

            applyMenuItem.SetShowAsAction(showApplyButton ? ShowAsAction.Always : ShowAsAction.Never);
            applyMenuItem.SetVisible(showApplyButton);

            return true;
        }

        public void OnClick(View v)
        {
            if (selectedTabIndex == -1)
            {
                for (int i = 0; i < dlg.PageCount; i++)
                {
                    var tab = dlg.GetPropertyPageTab(i);
                    if (tab.button == v)
                    {
                        SwitchToPage(i);
                        break;
                    }
                }
            }
        }

        public override void OnBackPressed()
        {
            if (selectedTabIndex >= 0)
            {
                var tab = dlg.GetPropertyPageTab(selectedTabIndex);
                SwitchToPage(tab.backTab);
            }
            else
            {
                stoppedByUser = true;
                base.OnBackPressed();
            }
        }

        public void SwitchToPage(int idx)
        {
            var tab = idx >= 0 ? dlg.GetPropertyPageTab(idx) : null;
            var fragments = tab == null ? tabsFragment as AndroidX.Fragment.App.Fragment : tab.properties as AndroidX.Fragment.App.Fragment;

            if (SupportActionBar != null)
            {
                var icon = tab != null && tab.backTab >= 0 ? global::FamiStudio.Resource.Drawable.arrow_back : global::FamiStudio.Resource.Drawable.cross;
                SupportActionBar.SetHomeAsUpIndicator(icon);
            }

            selectedTabIndex = idx;
            dlg.SetSelectedIndex(idx);
            InvalidateOptionsMenu();
            SupportFragmentManager.BeginTransaction()
                .SetTransition(AndroidX.Fragment.App.FragmentTransaction.TransitFragmentFade)
                .Replace(fragmentView.Id, fragments, "MultiPropertyDialog")
                .Commit();
        }

        private class MultiPropertyTabFragment : AndroidX.Fragment.App.Fragment
        {
            private MultiPropertyDialog dialog;

            public MultiPropertyTabFragment()
            {
            }

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
                linearLayout.SetBackgroundColor(DroidUtils.GetColorFromResources(container.Context, Resource.Color.DarkGreyColor4));

                var first = true;

                for (int i = 0; i < dialog.PageCount; i++)
                {
                    var tab = dialog.GetPropertyPageTab(i);

                    if (!tab.visible)
                        continue;

                    if (!first)
                    {
                        var spacer = new View(container.Context);
                        spacer.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 1);
                        spacer.SetBackgroundColor(DroidUtils.GetColorFromResources(container.Context, Resource.Color.LightGreyColor1));
                        linearLayout.AddView(spacer);
                    }

                    first = false;

                    var imageView = new ImageView(container.Context);
                    imageView.LayoutParameters = new LinearLayout.LayoutParams(dp36, dp36);
                    imageView.SetImageBitmap(DroidUtils.LoadTgaBitmapFromResource($"FamiStudio.Resources.Atlas.{tab.image}@2x.tga"));
                    imageView.SetColorFilter(BlendModeColorFilterCompat.CreateBlendModeColorFilterCompat(DroidUtils.GetColorFromResources(container.Context, Resource.Color.LightGreyColor1), BlendModeCompat.SrcAtop));

                    var textViewLayoutParams = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
                    textViewLayoutParams.Gravity = GravityFlags.Left | GravityFlags.CenterVertical;

                    var textView = new TextView(new ContextThemeWrapper(container.Context, Resource.Style.LightGrayTextMedium));
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