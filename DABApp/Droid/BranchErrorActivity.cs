using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DABApp.Droid
{
    [Activity(Label = "BranchErrorActivity")]
    public class BranchErrorActivity : Activity
    {
        private string logString = "";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            LogMessage("Branch initialization failed");
            LogMessage("Error code: " + Intent.Extras.GetInt("ErrorCode").ToString());
            LogMessage(Intent.Extras.GetString("ErrorMessage"));
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