using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DABApp.Droid;
using Xamarin.Forms;

[assembly: Dependency(typeof(FileManagement))]
namespace DABApp.Droid
{
	public class FileManagement: IFileManagement
	{
		public FileManagement()
		{
		}

		public bool DeleteEpisode(string episodeId, string extension)
		{
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
	}
}
