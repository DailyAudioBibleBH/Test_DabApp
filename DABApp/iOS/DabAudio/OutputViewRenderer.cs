using System;
using System.IO;
using DABApp;
using DABApp.iOS;
using Foundation;
using MediaPlayer;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(OutputView), typeof(OutputViewRenderer))]
namespace DABApp.iOS
{
	public class OutputViewRenderer: ViewRenderer<OutputView, MPVolumeView>
	{
		MPVolumeView VolumeView;

		public OutputViewRenderer()
		{
			VolumeView.ShowsRouteButton = true;
			VolumeView.ShowsVolumeSlider = false;
			string fileName = "ic_menu_white.png";
			string sFilePath = NSBundle.MainBundle.PathForResource(Path.GetFileNameWithoutExtension(fileName), Path.GetExtension(fileName));
			VolumeView.SetRouteButtonImage(new UIImage(sFilePath), UIControlState.Normal);
		}

		protected override void OnElementChanged(ElementChangedEventArgs<OutputView> e)
		{
			base.OnElementChanged(e);
			if (Control == null) {
				VolumeView = new MPVolumeView();
				SetNativeControl(VolumeView);
			}
		}
	}
}
