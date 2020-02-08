using System;
using Xamarin.Forms;

namespace DABApp
{
	public class DabSeekBar: Slider
	{
		public event EventHandler<EventArgs> UserInteraction;
		public event EventHandler TouchDown;
		public event EventHandler TouchUp;

		public EventHandler TouchDownEvent;
		public EventHandler TouchUpEvent;
		public DabSeekBar()
		{
			TouchDownEvent = delegate
			{
				TouchDown?.Invoke(this, EventArgs.Empty);
			};
			TouchUpEvent = delegate
			{
				TouchUp?.Invoke(this, EventArgs.Empty);
			};
		}

		public void Touched(object o, EventArgs e) {
			if (UserInteraction != null) {
				UserInteraction.Invoke(o, e);
			}
		}



        public static readonly BindableProperty OnRecordPageProperty = BindableProperty.Create("OnRecordPage", typeof(bool), typeof(bool), false);
    }
}
