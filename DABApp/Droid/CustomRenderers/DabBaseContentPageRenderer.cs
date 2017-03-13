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

[assembly: ExportRenderer(typeof(DabBaseContentPage), typeof(DabBaseContentPageRenderer))]
namespace DABApp.Droid
{
	public class DabBaseContentPageRenderer: PageRenderer, ISlideOverKitPageRendererDroid
	{
		public Action<ElementChangedEventArgs<Page>> OnElementChangedEvent { get; set;}

		public Action<bool, int, int, int, int> OnLayoutEvent { get; set;}

		public Action<int, int, int, int> OnSizeChangedEvent { get; set;}

		public DabBaseContentPageRenderer()
		{
			new SlideOverKitDroidHandler().Init(this);
		}

		protected override void OnElementChanged(ElementChangedEventArgs<Page> e)
		{
			base.OnElementChanged(e);
			if (OnElementChangedEvent != null)
				OnElementChangedEvent(e);
		}

		protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
		{
			base.OnLayout(changed, left, top, right, bottom);
			if (OnLayoutEvent != null)
				OnLayoutEvent(changed, left, top, right, bottom);
		}

		protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
		{
			base.OnSizeChanged(w, h, oldw, oldh);
			if (OnSizeChangedEvent != null)
				OnSizeChangedEvent(w, h, oldw, oldh);
		}

		public override void OnWindowFocusChanged(bool hasWindowFocus)
		{
			base.OnWindowFocusChanged(hasWindowFocus);
			var activity = this.Context as MainActivity;

			var actionBar = activity.SupportActionBar;
			var toolbar = new Android.Support.V7.Widget.Toolbar(this.Context);

			var contentPage = this.Element as ContentPage;
			if (contentPage == null) {
				return;
			}

			var itemsInfo = contentPage.ToolbarItems;
			//var MenuItem = itemsInfo.Single(x => x.Priority == 1);
			var MenuButton = new Android.Widget.ImageButton(this.Context);
			MenuButton.SetImageResource(Resource.Drawable.ic_menu_white);
			MenuButton.Id = Resource.Id.MenuButton;
			var views = new List<Android.Views.View>();
			views.Add(MenuButton);
			toolbar.AddTouchables(views);
			//toolbar.SetForegroundGravity(Android.Views.GravityFlags.Left);
			var layoutParams = new Android.Support.V7.App.ActionBar.LayoutParams(8388611);//defining gravity as Start enum: 8388611
			actionBar.SetCustomView(MenuButton, layoutParams);
			actionBar.SetDisplayShowCustomEnabled(true);
			activity.SetSupportActionBar(toolbar);
		}
	}
}
