using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DABApp.Droid
{
    [Activity(Label = "BranchActivity")]
    public class BranchActivity : Activity

    {
        private string logString = "";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            LogMessage("Branch initialization completed: ");

            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(Intent.GetStringExtra("BranchData"));
            foreach (var key in data.Keys)
            {
                LogMessage(key + " : " + data[key].ToString());
            }
        }

        #region Utils

        void LogMessage(string message)
        {
            Console.WriteLine(message);
            logString += DateTime.Now.ToLongTimeString() + "> " + message + "\n";
        }

        #endregion
    }
}