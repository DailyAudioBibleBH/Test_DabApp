using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace DABApp.DabUI.BaseUI
{
    public class DabUserInteractionEvents
    {
        public delegate void WaitStart(object source, DabAppEventArgs e);

        public static event WaitStart WaitStartedWithoutMessageWithoutCancelEvent;

        public static void WaitStarted(object source, DabAppEventArgs e)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                WaitStartedWithoutMessageWithoutCancelEvent?.Invoke(source, e);
            });
        }
    }

    public class DabAppEventArgs : EventArgs
    {
        public DabAppEventArgs(string message, bool hasCancel)
        {
            this.message = message;
            this.hasCancel = hasCancel;
        }
        public string message { get; set; }
        public bool hasCancel { get; set; }
    }
}
