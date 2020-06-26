using System;
using System.Collections.Generic;
using DABApp.DabSockets;
using Xamarin.Forms;

namespace DABApp.Service
{

    public delegate void GraphQlEventHandler(GraphQlUser user);

    public static class DabServiceEvents
    {
        //User Profile Changed Event
        public static event GraphQlEventHandler UserProfileChangedEvent;
        public static void UserProfileChanged(GraphQlUser user)
        {
            Device.BeginInvokeOnMainThread(async () =>
           {
               UserProfileChangedEvent?.Invoke(user);
           });
        }
    }
}
