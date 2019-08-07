using System;
using DABApp;
using DABApp.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using SlideOverKit.Droid;
using Android.App;
using System.Linq;
using Android.Support.V7.Widget;
using Android.Widget;
using System.Collections.Generic;
using Android.Content;

[assembly: ExportRenderer(typeof(DabBaseContentPage), typeof(DabBaseContentPageRenderer))]
namespace DABApp.Droid
{
    public class DabBaseContentPageRenderer : PageRenderer, ISlideOverKitPageRendererDroid
    {
        public Action<ElementChangedEventArgs<Page>> OnElementChangedEvent { get; set; }

        public Action<bool, int, int, int, int> OnLayoutEvent { get; set; }

        public Action<int, int, int, int> OnSizeChangedEvent { get; set; }

        public DabBaseContentPageRenderer(Context context) : base(context)
        {
            new SlideOverKitDroidHandler().Init(this);
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Page> e)
        {
            base.OnElementChanged(e);
            OnElementChangedEvent?.Invoke(e);

            var context = (Activity)Context;
            var toolbar = context.FindViewById<Android.Support.V7.Widget.Toolbar>(Droid.Resource.Id.toolbar);
            if (toolbar != null)
            {
                if (toolbar.NavigationIcon != null)
                {
                    toolbar.NavigationIcon.Bounds.Right = 0;
                    toolbar.NavigationIcon.Bounds.Left = 0;
                }
            }
        }

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            base.OnLayout(changed, left, top, right, bottom);
            OnLayoutEvent?.Invoke(changed, left, top, right, bottom);
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);
            OnSizeChangedEvent?.Invoke(w, h, oldw, oldh);
        }
    }
}
