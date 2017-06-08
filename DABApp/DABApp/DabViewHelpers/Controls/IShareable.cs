using System;
namespace DABApp
{
	public interface IShareable
	{
		void OpenShareIntent(string Channelcode, string episodeId);
	}
}
