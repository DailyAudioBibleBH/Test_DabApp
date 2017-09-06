using System;
using DABApp;
using DABApp.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(AudioOutputView), typeof(DabAudioOutputRenderer))]
namespace DABApp.Droid
{
	public class DabAudioOutputRenderer: ViewRenderer
	{
		public DabAudioOutputRenderer()
		{
		}
	}
}
