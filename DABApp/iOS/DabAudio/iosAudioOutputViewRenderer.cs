using System;
using DABApp;
using DABApp.iOS;
using MediaPlayer;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;


[assembly: ExportRenderer(typeof(AudioOutputView), typeof(iosAudioOutputViewRenderer))]
namespace DABApp.iOS
{
	public class iosAudioOutputViewRenderer: ViewRenderer<AudioOutputView, UIView>
	{
		MPVolumeView view;
		UILabel label;

		public iosAudioOutputViewRenderer()
		{
			
		}

		protected override void OnElementChanged(ElementChangedEventArgs<AudioOutputView> e)
		{
			base.OnElementChanged(e);

			if (Control == null)
			{
				view = new MPVolumeView()
				{
					ShowsRouteButton = true,
					ShowsVolumeSlider = false,
					
				};
				//label = new UILabel()
				//{
				//	Text = "Hello World."
				//};
				SetNativeControl(view);
			}

			if (e.OldElement != null)
			{
				// Unsubscribe

			}
			if (e.NewElement != null)
			{
				// Subscribe

			}
		}

		

	}
}
