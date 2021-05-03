using System;
using BranchXamarinSDK;
using Xamarin.Forms;

namespace DABApp
{
    public class DABAppBUO : Application, IBranchBUOSessionInterface
    {

        public DABAppBUO()
        {
        }

        #region IBranchBUOSessionInterface implementation

        public void InitSessionComplete(BranchUniversalObject buo, BranchLinkProperties blp)
        {
        }

        public void SessionRequestError(BranchError error)
        {
        }

        #endregion
    }
}