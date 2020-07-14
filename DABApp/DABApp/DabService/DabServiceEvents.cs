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
    public delegate void GraphQlEpisodeUserDataChanged();



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

        //Episode User Data Changed Event
        public static event GraphQlEpisodeUserDataChanged EpisodeUserDataChangedEvent;
        public static void EpisodeUserDataChanged()
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                EpisodeUserDataChangedEvent?.Invoke();
            });
        }

        //Episode List Changed Event
        public static event GraphQlEpisodesChanged EpisodesChangedEvent;
        public static void EpisodesChanged()
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                EpisodesChangedEvent?.Invoke();
            });
        }
    }
}
