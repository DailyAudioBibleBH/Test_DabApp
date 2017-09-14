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
            //Don't show the mini player since this is just a view. the containing page will take care of it.
            ControlTemplate = (ControlTemplate)App.Current.Resources["NoPlayerPageTemplateWithoutScrolling"];
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
