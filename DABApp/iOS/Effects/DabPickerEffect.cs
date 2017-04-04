using System;
using DABApp.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ResolutionGroupName("DabEffects")]
[assembly: ExportEffect(typeof(DabPickerEffect), "DabPickerEffect")]
namespace DABApp.iOS
{
	public class DabPickerEffect: PlatformEffect
	{
		protected override void OnAttached()
		{
			var picker = (UITextField)Control;
			picker.MinimumFontSize = 25f;
		}

		protected override void OnDetached()
		{
			throw new NotImplementedException();
		}
	}
}
