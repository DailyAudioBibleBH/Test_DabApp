using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DABApp.DabViewHelpers.Controls;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using DABApp.Droid.DependencyServices;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Android.Views.InputMethods;
using static Android.Views.ViewTreeObserver;
using DABApp.DabViewHelpers;

[assembly: ExportRenderer(typeof(CustomEntry), typeof(CustomEntryRenderer))]
namespace DABApp.Droid.DependencyServices
{
    class CustomEntryRenderer : EntryRenderer, IKeyboardService
    {
        public CustomEntryRenderer(Context context) : base(context)
        {
            GetInputMethodManager();
            SubscribeEvents();
        }

        public event EventHandler KeyboardIsShown;
        public event EventHandler KeyboardIsHidden;

        private InputMethodManager inputMethodManager;

        private bool wasShown = false;

        public void OnGlobalLayout(object sender, EventArgs args)
        {
            GetInputMethodManager();
            if (!wasShown && IsCurrentlyShown())
            {
                KeyboardIsShown?.Invoke(this, EventArgs.Empty);
                wasShown = true;
            }
            else if (wasShown && !IsCurrentlyShown())
            {
                KeyboardIsHidden?.Invoke(this, EventArgs.Empty);
                wasShown = false;
            }
        }

        private bool IsCurrentlyShown()
        {
            //Checking if keyboard is showing or not
            return inputMethodManager.IsAcceptingText;
        }

        private void GetInputMethodManager()
        {
            if (inputMethodManager == null || inputMethodManager.Handle == IntPtr.Zero)
            {
                inputMethodManager = (InputMethodManager)Xamarin.Forms.Forms.Context.GetSystemService(Context.InputMethodService);
            }
        }

        private void SubscribeEvents()
        {
            ((Activity)Xamarin.Forms.Forms.Context).Window.DecorView.ViewTreeObserver.GlobalLayout += this.OnGlobalLayout;
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement == null) return;

            // Subscribe event handler for focus change
            Control.FocusChange += Control_FocusChange;
        }

        void Control_FocusChange(object sender, FocusChangeEventArgs e)
        {
            //if entry has focus and using system keyboard
            if (e.HasFocus && IsCurrentlyShown())
                (Forms.Context as Activity).Window.SetSoftInputMode(SoftInput.AdjustResize);
            else
                (Forms.Context as Activity).Window.SetSoftInputMode(SoftInput.AdjustNothing);
        }
    }
}