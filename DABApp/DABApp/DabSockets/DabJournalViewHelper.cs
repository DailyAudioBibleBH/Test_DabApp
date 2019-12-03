using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Acr.DeviceInfo;
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
                Current.EntryHeight = DeviceInfo.Hardware.ScreenHeight * .8;
            }
            else
            {
                var modified = DeviceInfo.Hardware.ScreenHeight / GlobalResources.Instance.AndroidDensity;
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
