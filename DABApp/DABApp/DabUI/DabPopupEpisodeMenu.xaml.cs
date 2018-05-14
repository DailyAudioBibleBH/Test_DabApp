using Rg.Plugins.Popup.Contracts;
using Rg.Plugins.Popup.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DABApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DabPopupEpisodeMenu : PopupPage
    {
        public DabPopupEpisodeMenu()
        {
            InitializeComponent();
        }

        protected override bool OnBackButtonPressed() =>
            // Return true if you don't want to close this popup page when a back button is pressed
            false;

        // Invoked when background is clicked
        protected override bool OnBackgroundClicked() =>
            // Return false if you don't want to close this popup page when a background of the popup page is clicked
            true;
    }
}