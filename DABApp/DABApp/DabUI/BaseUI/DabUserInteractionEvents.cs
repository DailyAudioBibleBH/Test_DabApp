using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace DABApp.DabUI.BaseUI
{
    public class DabUserInteractionEvents
    {
        public delegate void WaitStart(object source, DabAppEventArgs e);
        public delegate void WaitStop(object source, EventArgs e);

        public static event WaitStart WaitStartedEvent;
        public static event WaitStop WaitStoppedEvent;

        public static void WaitStarted(object source, DabAppEventArgs e)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                WaitStartedEvent?.Invoke(source, e);
            });
        }

        public static void WaitStopped(object source, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                WaitStoppedEvent?.Invoke(source, e);
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