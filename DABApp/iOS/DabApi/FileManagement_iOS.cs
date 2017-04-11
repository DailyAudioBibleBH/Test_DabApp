using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;

namespace DABApp.iOS
{
	public class FileManagement_iOS: IFileManagement
	{
		public async Task<bool> DownloadEpisodeAsync(string address, string episodeTitle)
		{
			try
			{
				var doc = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
				var fileName = Path.Combine(doc, $"{episodeTitle}.mp4");
				WebClient client = new WebClient();
				await client.DownloadFileTaskAsync(address, fileName);
				return true;
			}
			catch (Exception e)
			{
				return false;
			}
		}
	}
}
