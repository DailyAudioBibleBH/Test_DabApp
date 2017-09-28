using System;
using DABApp.Droid;
using Xamarin.Forms;
using System.IO;
using Plugin.MediaManager;
using Plugin.MediaManager.Abstractions.EventArguments;
using System.Linq;
using System.Threading.Tasks;
using Android.Content;
using Android.App;
using Android.Support.V4.Media.Session;

[assembly: Dependency(typeof(AudioService))]
namespace DABApp.Droid
{
	public class AudioService: IAudio
	{
		//public static MediaPlayer player;
		public static bool IsLoaded;
		public static dbEpisodes Episode;
		public static string FileName;
		private MediaSessionCompat mediaSessionCompat;

		public AudioService()
		{
		}

		public void SetAudioFile(string fileName, dbEpisodes episode)
		{
			//player = new MediaPlayer();
			//player.Prepared += (s, e) =>
			//{
			//	IsLoaded = true;
			//};
			//player.Completion += (s, e) =>
			//{
			//	IsLoaded = false;
			//	Completed.Invoke(s, e);
			//};
			//player.Error += OnError;
			Episode = episode;
			CrossMediaManager.Current.MediaFileChanged += SetMetaData;
			CrossMediaManager.Current.StatusChanged += OnStatusChanged;
			//CrossMediaManager.Current.SetOnBeforePlay(async (Plugin.MediaManager.Abstractions.IMediaFile arg) =>
			//{
			//	await Task.Run(async () =>
			//	{
			//		var ImageUri = ContentConfig.Instance.views.Single(x => x.title == "Channels").resources.Single(x => x.title == Episode.channel_title).images.thumbnail;
			//		var input = new Java.Net.URL(ImageUri).OpenStream();
			//		var a = await Android.Graphics.BitmapFactory.DecodeStreamAsync(input);

			//		arg.Metadata.AlbumArt = a;
			//		arg.Metadata.Art = a;
			//		arg.Metadata.DisplayIcon = a;
			//	});
			//});
			if (fileName.Contains("http://") || fileName.Contains("https://"))
			{
				CrossMediaManager.Current.Play(fileName, Plugin.MediaManager.Abstractions.Enums.MediaFileType.Audio);
				//player.SetDataSource(fileName);
			}
			else
			{
				var doc = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				fileName = Path.Combine(doc, fileName);
				CrossMediaManager.Current.Play(fileName, Plugin.MediaManager.Abstractions.Enums.MediaFileType.Audio);
				//player.SetDataSource(fileName);
			}
			Episode = episode;
			FileName = fileName;
			//player.Prepare();
		}

		public void Play() {
			//player.Start();
			CrossMediaManager.Current.Play();
		}

		public void Pause() {
			//player.Pause();
			CrossMediaManager.Current.Pause();
		}

		public void SeekTo(int seconds) {
			//player.SeekTo(seconds * 1000);
			CrossMediaManager.Current.Seek(TimeSpan.FromSeconds(seconds));
		}

		public void Skip(int seconds)
		{
			//player.SeekTo((Convert.ToInt32(CurrentTime) + seconds) * 1000);
			CrossMediaManager.Current.Seek(CrossMediaManager.Current.Position + TimeSpan.FromSeconds(seconds));
		}

		public void Unload()
		{
			IsLoaded = false;
		}

		public void SwitchOutputs()
		{
			throw new NotImplementedException();
		}

		public bool IsInitialized {
			get { return IsLoaded;}
		}

		public bool IsPlaying {
			//get { return player != null ? player.IsPlaying : false;}
			get { return CrossMediaManager.Current.AudioPlayer.Status == Plugin.MediaManager.Abstractions.Enums.MediaPlayerStatus.Playing ? true : false;}
		}

		public double CurrentTime {
			//get { return player.CurrentPosition > 0 ? player.CurrentPosition/1000: 0;}
			get { return CrossMediaManager.Current.Position.TotalSeconds;}
		}

		public double RemainingTime {
			//get { return (player.CurrentPosition - player.Duration) > 0 ? (player.CurrentPosition - player.Duration) / 1000: 0;}
			get { return (CrossMediaManager.Current.Duration.TotalSeconds - CrossMediaManager.Current.Position.TotalSeconds);}
		}

		public double TotalTime {
			//get { return player.Duration >0 ? player.Duration / 1000 : 60; }
			get { return CrossMediaManager.Current.Duration.TotalSeconds > 0 ? CrossMediaManager.Current.Duration.TotalSeconds : 60; }
		}

		public bool PlayerCanKeepUp
		{
			get
			{
				//if (player != null)
				//{
					return true;
				//}
				//else return false;
			}
		}

		public event EventHandler Completed;

		void OnStatusChanged(object o, StatusChangedEventArgs e)
		{
			switch (e.Status)
			{ 
				case Plugin.MediaManager.Abstractions.Enums.MediaPlayerStatus.Playing:
					IsLoaded = true; break;
				case Plugin.MediaManager.Abstractions.Enums.MediaPlayerStatus.Stopped:
					IsLoaded = false; break;
			}
		}

		async void SetMetaData(object o, MediaFileChangedEventArgs e)
		{
			e.File.Metadata.Artist = Episode.channel_title;
			e.File.Metadata.Title = Episode.title;
			e.File.Metadata.Album = null;
			var ImageUri = ContentConfig.Instance.views.Single(x => x.title == "Channels").resources.Single(x => x.title == Episode.channel_title).images.thumbnail;
			await Task.Run(async () =>
			{
				var input = new Java.Net.URL(ImageUri).OpenStream();
				var a = await Android.Graphics.BitmapFactory.DecodeStreamAsync(input);
				Device.BeginInvokeOnMainThread(() =>
				{
					e.File.Metadata.AlbumArt = a;
					e.File.Metadata.Art = a;
					e.File.Metadata.DisplayIcon = a;
				});
			});
            //SetNotificationManager();
		}

		void SetNotificationManager()
		{
			if (mediaSessionCompat == null)
			{
				Intent intent = new Intent(Forms.Context, typeof(MainActivity));
				PendingIntent pIntent = PendingIntent.GetActivity(Forms.Context, 0, intent, 0);
				ComponentName name = new ComponentName("dailyaudiobible.dabapp", new RemoteControlBroadcastReceiver().ComponentName);
				mediaSessionCompat = new MediaSessionCompat(Forms.Context, "DAB", name, pIntent);
			}
			CrossMediaManager.Current.MediaNotificationManager = new DabMediaNotificationManager(Forms.Context, mediaSessionCompat.SessionToken, typeof(MediaPlayerService));
		}
	}
}
