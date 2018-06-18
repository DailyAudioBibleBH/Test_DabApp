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
        public event EventHandler ChangedRequested;
        public Resource Resource { get; private set; }
        public List<EpisodeViewModel> Episodes { get; private set; } 

        public DabPopupEpisodeMenu(Resource resource, List<EpisodeViewModel> episodes)
        {
            InitializeComponent();
            Resource = resource;
            Offline.On = Resource.availableOffline;
            Episodes = episodes;
            SortOld.IsVisible = Resource.AscendingSort;
            SortNew.IsVisible = !Resource.AscendingSort;
        }

        protected override bool OnBackButtonPressed()
        {
            // Return true if you don't want to close this popup page when a back button is pressed
            return false;
        }

        // Invoked when background is clicked
        protected override bool OnBackgroundClicked()
        {
            // Return false if you don't want to close this popup page when a background of the popup page is clicked
            return true;
        }

        private void OnBackground(object sender, EventArgs e)
        {
            Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopAsync();
        }

        void OnFavorited(object sender, EventArgs e)
        {
            Episodes = Episodes.Where(x => x.favoriteVisible == true).ToList();
        }

        public void OnOffline(object o, ToggledEventArgs e)
        {
            Resource.availableOffline = e.Value;
            ContentAPI.UpdateOffline(e.Value, Resource.id);
            if (e.Value)
            {
                Task.Run(async () => { await PlayerFeedAPI.DownloadEpisodes(); });
            }
            else
            {
                Task.Run(async () => {
                    await PlayerFeedAPI.DeleteChannelEpisodes(Resource);
                    Device.BeginInvokeOnMainThread(() => {
                        var handler = ChangedRequested;
                        handler(this, new EventArgs());
                    });
                });
            }
        }

        void OnNewest(object o, EventArgs e)
        {
            Resource.AscendingSort = false;
            SortNew.IsVisible = true;
            SortOld.IsVisible = false;
            var handler = ChangedRequested;
            handler(this, new EventArgs());
        }

        void OnOldest(object o, EventArgs e)
        {
            SortNew.IsVisible = false;
            SortOld.IsVisible = true;
            Resource.AscendingSort = true;
            var handler = ChangedRequested;
            handler(this, new EventArgs());
        }
    }
}