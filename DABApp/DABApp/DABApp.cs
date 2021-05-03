using System;
using System.Collections.Generic;
using BranchXamarinSDK;
using Xamarin.Forms;

namespace DABApp
{
    public class DABApp : Application, IBranchSessionInterface
    {

        public DABApp()
        {
        }

        #region IBranchSessionInterface implementation

        public void InitSessionComplete(Dictionary<string, object> data)
        {
        }

        public void CloseSessionComplete()
        {
        }

        public void SessionRequestError(BranchError error)
        {
        }

        #endregion
    }
}
