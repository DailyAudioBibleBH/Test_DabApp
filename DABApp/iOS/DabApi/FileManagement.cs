using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;
using DABApp.iOS;

[assembly: Dependency(typeof(FileManagement))]
namespace DABApp.iOS
{
	public class FileManagement: IFileManagement
	{
		public async Task<bool> DownloadEpisodeAsync(string address, string episodeTitle)
		{
			try
			{
				var doc = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				var fileName = Path.Combine(doc, $"{episodeTitle}.mp4");
				//if (!File.Exists(fileName)) {
				//	File.Create(fileName);
				//}
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
