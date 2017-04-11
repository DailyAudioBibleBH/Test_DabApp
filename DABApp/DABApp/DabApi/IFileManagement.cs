using System;
using System.Threading.Tasks;

namespace DABApp
{
	public interface IFileManagement
	{
		Task<bool> DownloadEpisodeAsync(string address, string episodeTitle);
	}
}
