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
using System.Diagnostics;
using Plugin.MediaManager.ExoPlayer;
using System.Globalization;
using FFImageLoading;
using FFImageLoading.Forms;
using Plugin.Connectivity;
using Android.Media;

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
		double tt;

		public AudioService()
		{
		}

        public void SetAudioFile(string fileName)
        {
            CrossMediaManager.Current.StatusChanged += OnStatusChanged;
            CrossMediaManager.Current.Play(fileName, Plugin.MediaManager.Abstractions.Enums.MediaFileType.Audio);
            tt = 60;
            IsLoaded = true;
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
            //var ImageUri = ContentConfig.Instance.views.Single(x => x.title == "Channels").resources.Single(x => x.title == Episode.channel_title).images.thumbnail;
            //This is a different way to get pictures onto the notifications.  Not as reliable but should theoretically do it before the track starts playing
            //CrossMediaManager.Current.SetOnBeforePlay(async (Plugin.MediaManager.Abstractions.IMediaFile arg) =>
            //{
            //	if (arg.Metadata.AlbumArtUri != ImageUri)
            //	{
            //		await Task.Run(async () =>
            //		{
            //			var a = await fetchBitmap(ImageUri);

            //			Device.BeginInvokeOnMainThread(() =>
            //			{
            //				arg.Metadata.AlbumArt = a;
            //				arg.Metadata.Art = a;
            //				arg.Metadata.DisplayIcon = a;
            //				arg.Metadata.AlbumArtUri = ImageUri;
            //			});
            //		});
            //	}
            //});
            var doc = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            fileName = Path.Combine(doc, fileName);
            if (!File.Exists(fileName))
			{
				CrossMediaManager.Current.Play(episode.url, Plugin.MediaManager.Abstractions.Enums.MediaFileType.Audio);
				//player.SetDataSource(fileName);
			}
			else
			{
				CrossMediaManager.Current.Play(fileName, Plugin.MediaManager.Abstractions.Enums.MediaFileType.Audio);
				//player.SetDataSource(fileName);
			}
			if (Device.Idiom == TargetIdiom.Phone)
			{
				CrossMediaManager.Current.Pause();
			}
			var r = episode.remaining_time.Where(x => x == ':').Count() < 2 ? "00:" + episode.remaining_time : episode.remaining_time;
			Debug.WriteLine($"r = {r}");
			if (episode.remaining_time.Contains("-"))
			{
				tt = 60;
			}
			else
			{
				double conversion = episode.stop_time + TimeSpan.Parse(r).TotalSeconds;
				tt = conversion > 0 ? conversion : 60;
			}
            CrossMediaManager.Current.StatusChanged += Current_MediaFailed;
			Episode = episode;
			FileName = fileName;
			//player.Prepare();
		}

        private void Current_MediaFailed(object sender, StatusChangedEventArgs e)
        {
            if (e.Status == Plugin.MediaManager.Abstractions.Enums.MediaPlayerStatus.Loading && !CrossConnectivity.Current.IsConnected && !Episode.is_downloaded)
            {
                PlayerCanKeepUp = false;
            }
        }

        public void Play() {
			CrossMediaManager.Current.Play();
            if (!PlayerCanKeepUp && CrossConnectivity.Current.IsConnected)
            {
                PlayerCanKeepUp = true;
            }
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
			get {
				return CrossMediaManager.Current.Duration.TotalSeconds > 0 ? CrossMediaManager.Current.Duration.TotalSeconds : tt; 
			}
		}

        public bool PlayerCanKeepUp { get; set; } = true;

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

		void SetMetaData(object o, MediaFileChangedEventArgs e)
		{
			e.File.Metadata.Artist = Episode.channel_title;
			e.File.Metadata.Title = Episode.title;
			e.File.Metadata.Album = null;
			var ImageUri = ContentConfig.Instance.views.Single(x => x.title == "Channels").resources.Single(x => x.title == Episode.channel_title).images.thumbnail;
			if (e.File.Metadata.AlbumArtUri != ImageUri)
			{
				Task.Run(async () =>
					{
						Debug.WriteLine($"Before getting Bitmap {ImageUri}");
						//var input = new Java.Net.URL(ImageUri).OpenStream();
						//var a = await Android.Graphics.BitmapFactory.DecodeStreamAsync(input);
						var a = await fetchBitmap(ImageUri);
						Debug.WriteLine($"Bitmap: {a}");
						Device.BeginInvokeOnMainThread(() =>
						{
							e.File.Metadata.AlbumArt = a;
							e.File.Metadata.Art = a;
							e.File.Metadata.DisplayIcon = a;
							e.File.Metadata.AlbumArtUri = ImageUri;
						});
					});
			}
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

		async Task<Android.Graphics.Bitmap> fetchBitmap(string imageUri)
		{
			try
			{
				var draw = await ImageService.Instance.LoadUrl(imageUri).DownSample().AsBitmapDrawableAsync();
				return draw.Bitmap;
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Bitmap exception: {e.Message}");
				return null;
			}
		}

        public void DeCouple()
        {
            CrossMediaManager.Current.Pause();
            CrossMediaManager.Current.Stop();
            CrossMediaManager.Current.MediaQueue.Clear();
            IsLoaded = false;
        }
    }
}
