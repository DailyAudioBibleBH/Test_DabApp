using System;
using Android.Content;
using Android.Views;
using Android.Views.InputMethods;
using DABApp;
using DABApp.Droid;
using Plugin.CurrentActivity;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(DabEditor), typeof(DabEditorRenderer))]
namespace DABApp.Droid
{
	public class DabEditorRenderer : EditorRenderer
	{
        public DabEditorRenderer(Context context) : base(context)
        {
            GetInputMethodManager();
            SubscribeEvents();
        }
        protected override void OnElementChanged(ElementChangedEventArgs<Editor> e)
		{
            //Code for this custom renderer found here: https://solidbrain.com/2017/07/10/placeholder-text-in-xamarin-forms-editor/
			base.OnElementChanged(e);

			if (Element == null)
				return;

			var element = (DabEditor)Element;

            if (Control != null)
            {
                Control.Hint = element.Placeholder;
                Control.SetHintTextColor(element.PlaceholderColor.ToAndroid());
            }

            // Subscribe event handler for focus change
            Control.FocusChange += Control_FocusChange;
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
            try
            {
                return inputMethodManager.IsAcceptingText;
            }
            catch (Exception)
            {
                //object is disposed
                return false;
            }
        }

        private void GetInputMethodManager()
        {
            if (inputMethodManager == null || inputMethodManager.Handle == IntPtr.Zero)
            {
                inputMethodManager = (InputMethodManager)CrossCurrentActivity.Current.AppContext.GetSystemService(Context.InputMethodService);
            }
        }

        private void SubscribeEvents()
        {
            (CrossCurrentActivity.Current.Activity).Window.DecorView.ViewTreeObserver.GlobalLayout += this.OnGlobalLayout;
        }

        void Control_FocusChange(object sender, FocusChangeEventArgs e)
        {
            //if entry has focus and using system keyboard
            if (e.HasFocus && IsCurrentlyShown())
            {
                (CrossCurrentActivity.Current.Activity).Window.SetSoftInputMode(SoftInput.AdjustResize);
            }
            else
                (CrossCurrentActivity.Current.Activity).Window.SetSoftInputMode(SoftInput.AdjustNothing);
        }
    }
}
