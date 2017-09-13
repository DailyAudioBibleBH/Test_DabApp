using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace DABApp
{
    public partial class DabForumTopicDetailsView : ContentView
    {
        public Button reply { get; set; }
        public ListView replies { get; set; }
        public Label last { get; set; }

        public DabForumTopicDetailsView()
        {
            InitializeComponent();
            BackgroundColor = (Color)App.Current.Resources["PageBackgroundColor"];
            if (Device.Idiom == TargetIdiom.Phone)
            {
                //Show the mini player on phones
                ControlTemplate = (ControlTemplate)App.Current.Resources["OtherPlayerPageTemplateWithoutScrolling"];
            }
            else
            {
                //Don't show the mini player on tablets
                ControlTemplate = (ControlTemplate)App.Current.Resources["NoPlayerPageTemplateWithoutScrolling"];

            }
            reply = rep;
            replies = reps;
            last = LastActivity;
            if (GuestStatus.Current.IsGuestLogin)
            {
                rep.Text = "Log in";
            }
        }
    }
}
