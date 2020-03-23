using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace DABApp.DabUI
{
    public partial class AchievementsProgressPopup
    {
        public AchievementsProgressPopup()
        {
            InitializeComponent();
        }

        async void OnShare(object o, EventArgs e)
        {
            Xamarin.Forms.DependencyService.Get<IShareable>().OpenShareIntent("achievement", "achievement");
        }

        async void OnContinue(object o, EventArgs e)
        {


        }
    }
}
