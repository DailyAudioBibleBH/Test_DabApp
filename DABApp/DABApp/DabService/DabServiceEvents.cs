using System;
using System.Collections.Generic;
using DABApp.DabSockets;
using Xamarin.Forms;

namespace DABApp.Service
{
    public enum GraphQlTrafficDirection
    {
        Inbound,
        Outbound
    }


    public delegate void GraphQlTraffic(GraphQlTrafficDirection direction, string traffic);
    public delegate void GraphQlEventHandler(GraphQlUser user);

    public static class DabServiceEvents
    {
        //Traffic event
        public static event GraphQlTraffic TrafficOccuredEvent;
        public static void TrafficOccured(GraphQlTrafficDirection direction, string traffic)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                TrafficOccuredEvent?.Invoke(direction, traffic);
            });
        }

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
