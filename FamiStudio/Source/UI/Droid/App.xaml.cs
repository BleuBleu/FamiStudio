using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace FamiStudio
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new FamiStudioForm(null);
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
