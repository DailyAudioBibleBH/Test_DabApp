using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Plugin.FirebasePushNotification;
using Xamarin.Forms;

namespace DABApp.DabNotifications
{
    public class DabPushNotification
    {
        public string Title { get; set; }
        public string Message { get; set; }
        //private static Page page;
        //public static bool IsReady { get; private set; }

        /* Sample p key/values:
              google.c.a.c_l : Test 7
              google.c.a.e : 1
              aps.alert.title : Test 7
              aps.alert.body : Test message seven
              gcm.n.e : 1
              google.c.a.c_id : 6012590258670105844
              google.c.a.udt : 0
              gcm.message_id : 0:1557945955047639%bca610e0bca610e0
              google.c.a.ts : 1557945954
          */

        //public static void Init(Page p)
        //{
        //    page = p;
        //    IsReady = true;
        //}

        public DabPushNotification(FirebasePushNotificationDataEventArgs p)
        {
            //Store the push notification event args in a usable object
            LoadPushNotificationData(p.Data);
        }

        public DabPushNotification(FirebasePushNotificationResponseEventArgs p)
        {
            //Store the push notification event args in a usable object
            LoadPushNotificationData(p.Data);
        }

        private void LoadPushNotificationData(IDictionary<string, object> d)
        {
            //Load push notification data into usable fields
            foreach (var data in d)
            {
                switch (data.Key.ToLower())
                {
                    case "aps.alert.title":
                    case "title":
                        if (Title == null && data.Value != null)
                        {
                            Title = data.Value.ToString();
                        }
                        break;
                    case "aps.alert.body":
                    case "body":
                        if (Message == null && data.Value != null)
                        {
                            Message = data.Value.ToString();
                        }
                        break;
                    default:
                        //Do nothing
                        break;
                }
            }
        }

        public void DisplayAlert()
        {
            //if (DabPushNotification.IsReady)
            //{
            /* Display a simple alert with the push notification content */
            Device.BeginInvokeOnMainThread(() =>
            {
                Application.Current.MainPage.DisplayAlert(Title, Message, "OK");
            });
            //}
            //else
            //{
            //    Debug.WriteLine("DabNotifications has not been initialized");
            //}
        }
    }

}
