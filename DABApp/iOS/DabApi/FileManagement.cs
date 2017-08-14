using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;
using DABApp.iOS;
using System.Linq;

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
				var ext = address.Split('.').Last();
				var fileName = Path.Combine(doc, $"{episodeTitle}.{ext}");
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

		public bool DeleteEpisode(string episodeId, string extension) {
			try
			{
				var doc = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				var fileName = Path.Combine(doc, $"{episodeId}.{extension}");
				File.Delete(fileName);
				return true;
			}
			catch (Exception e)
			{
				return false;
			}
		}
	}
}
