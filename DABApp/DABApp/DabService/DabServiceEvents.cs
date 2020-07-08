using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public delegate void GraphQlProfileChanged(GraphQlUser user);
    public delegate void GraphQlEpisodesChanged();
    

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
        public static event GraphQlProfileChanged UserProfileChangedEvent;
        public static void UserProfileChanged(GraphQlUser user)
        {
            Device.BeginInvokeOnMainThread(async () =>
           {
               UserProfileChangedEvent?.Invoke(user);
           });
        }

        //Episode Property Changed Event
        public static event GraphQlEpisodesChanged EpisodesChangedEvent;
        public static void EpisodesChanged()
        {
            Debug.WriteLine($"EpisodesChanged Fired");
            Device.BeginInvokeOnMainThread(async () =>
            {
                EpisodesChangedEvent?.Invoke();
            });
        }
    }
}
