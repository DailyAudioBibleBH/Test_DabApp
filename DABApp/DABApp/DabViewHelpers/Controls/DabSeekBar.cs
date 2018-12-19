using System;
using Xamarin.Forms;

namespace DABApp
{
	public class DabSeekBar: Slider
	{
		public event EventHandler<EventArgs> UserInteraction;

		public void Touched(object o, EventArgs e) {
			if (UserInteraction != null) {
				UserInteraction.Invoke(o, e);
			}
		}

        public static readonly BindableProperty OnRecordPageProperty = BindableProperty.Create("OnRecordPage", typeof(bool), typeof(bool), false);
    }
}
