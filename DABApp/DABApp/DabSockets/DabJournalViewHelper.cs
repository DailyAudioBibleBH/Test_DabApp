using System;
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
                Current.EntryHeight = DeviceInfo.Hardware.ScreenHeight * .5;
            }
            else
            {
                var modified = DeviceInfo.Hardware.ScreenHeight / GlobalResources.Instance.AndroidDensity;
                Current.EntryHeight = modified * .5;
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
                service.OnPropertyChanged("EntryHeight"); 
            }
        }
    }
}
