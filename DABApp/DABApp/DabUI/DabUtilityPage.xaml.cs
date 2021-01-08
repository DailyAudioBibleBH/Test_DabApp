using System;
using System.Collections.Generic;
using DABApp.DabUI.BaseUI;
using Xamarin.Forms;

namespace DABApp
{
    public partial class DabUtilityPage : DabBaseContentPage
    {
        public DabUtilityPage()
        {
            InitializeComponent();
        }


        async void btnClose_Clicked(System.Object sender, System.EventArgs e)
        {
            await Navigation.PopAsync();
        }

        async void btnFakeEpisode_Clicked(System.Object sender, System.EventArgs e)
        {
            await DisplayAlert("Not Implemented", "This isn't implemented just yet.", "OK");
        }
    }
}
