using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace DABApp.DabSockets
{
    public class DabJournalViewHelper
    {
        private double _EntryHeight = 400;
        public static DabJournalViewHelper Current { get; private set; }
        DabJournalService service = new DabJournalService();

        public DabJournalViewHelper()
        {
        }

        static DabJournalViewHelper()
        {
            Current = new DabJournalViewHelper();
            if (Device.RuntimePlatform == Device.iOS)
            {
                Current.EntryHeight = DeviceDisplay.MainDisplayInfo.Height * .8;
            }
            else
            {
                var modified = DeviceDisplay.MainDisplayInfo.Height / GlobalResources.Instance.AndroidDensity;
                Current.EntryHeight = modified * .6;
            }
        }

        public double EntryHeight
        {
            get
            {
                return _EntryHeight;
            }
            set
            {
                _EntryHeight = value;
                OnPropertyChanged("EntryHeight"); 
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
