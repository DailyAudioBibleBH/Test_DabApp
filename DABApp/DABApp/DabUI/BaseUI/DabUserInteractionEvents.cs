using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace DABApp.DabUI.BaseUI
{
    public class DabUserInteractionEvents
    {
        public delegate void WaitStartWithoutMessageWithCancel(object source, EventArgs e);
        public delegate void WaitStartWithMessageWithCancel(object source, EventArgs e);
        public delegate void WaitStartWithoutMessageWithoutCancel(object source, EventArgs e);
        public delegate void WaitStartWithMessageWithoutCancel(object source, EventArgs e);

        public static event WaitStartWithoutMessageWithCancel WaitStartedWithoutMessageWithCancelEvent;

        public static event WaitStartWithMessageWithCancel WaitStartedWithMessageWithCancelEvent;

        public static event WaitStartWithoutMessageWithoutCancel WaitStartedWithoutMessageWithoutCancelEvent;

        public static void WaitStartedWithoutMessageWithoutCancel(object source, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                WaitStartedWithoutMessageWithoutCancelEvent?.Invoke(source, EventArgs.Empty);
            });
        }
    }

    public class DabAppEventArgs : EventArgs
    {
        public DabAppEventArgs()
        {
            this.message = message;
            this.hasCancel = hasCancel;
        }
        public string message { get; set; }
        public bool hasCancel { get; set; }
    }
}
