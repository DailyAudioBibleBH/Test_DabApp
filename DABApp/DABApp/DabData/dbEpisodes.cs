using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using SQLite;
using Xamarin.Forms;

namespace DABApp
{
    public class dbEpisodes
    {
        [PrimaryKey]
        public int? id { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string author { get; set; }
        [Indexed]
        public DateTime PubDate { get; set; }
        [Indexed]
        public int PubDay { get; set; }
        [Indexed]
        public string PubMonth { get; set; }
        [Indexed]
        public int PubYear { get; set; }
        public string url { get; set; }
        public string read_link { get; set; }
        public string read_version_tag { get; set; }
        public string read_version_name { get; set; }
        [Indexed]
        public string channel_code { get; set; }
        public string channel_title { get; set; }
        public string channel_description { get; set; }
        public bool is_downloaded { get; set; } = false;
        public string is_listened_to { get; set; }
        public double start_time { get; set; } = 0;
        public double stop_time { get; set; } = 0;
        public string remaining_time { get; set; } = "01:00";
        public bool is_favorite { get; set; }
        public bool has_journal { get; set; }
        public bool progressVisible { get; set; }


        public string File_extension
        //Extension of the file (always lower case)
        {
            get
            {
                return url.Split('.').Last().ToLower();
            }
        }

      public string File_name_local
        {
            get
            {
                if (is_downloaded)
                {
                    //Use the local file
                    var doc = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var fileName = System.IO.Path.Combine(doc, $"{id}.{File_extension}");
                    if (FileManager.Instance.FileExists(fileName))
                    {
                        return fileName;
                    } else
                    {
                        //File is marked as downloaded but doesn't really exist
                        return null;
                    }
                } else
                {
                    //File isn't downloaded 
                    return null;
                }
            }
        }

        public string File_name
            //File name used to access the file 
        {
            get
            {
                if (File_name_local != null)
                {
                    return File_name_local;
                }
                else
                {
                    //Use the remote file
                    return url;
                }
            }
        }

	//	[Ignore]
	//	public bool downloadVisible { 
	//		get {
	//			return is_downloaded;
	//		}
	//		set {
	//			is_downloaded = value;
	//			OnPropertyChanged("downloadVisible");
	//		}
	//	}

 //       [Ignore]
 //       public int downloadProgress
 //       {
 //           get {
 //               return progress;
 //           }
 //           set {
 //               progress = value;
 //               OnPropertyChanged("downloadProgress");
 //               OnPropertyChanged("progressVisible");
 //           }
 //       }

 //       [Ignore]
 //       public bool progressVisible
 //       {
 //           get {
 //               return noProgress = downloadProgress > -1 && !downloadVisible ? true : false;
 //           }
 //           set {
 //               noProgress = downloadProgress > -1 && !downloadVisible ? true : false;
 //               OnPropertyChanged("progressVisible");
 //           }
 //       }

	//	[Ignore]
	//	public bool listenedToVisible { 
	//		get {
	//			return unTouched = is_listened_to == "listened" ? true : false;
	//		}
	//		set{
	//			unTouched = is_listened_to == "listened" ? true : false;
	//			OnPropertyChanged("listenedToVisible");
	//		}
	//	}

	//	[Ignore]
	//	public bool favoriteVisible 
	//	{ 
	//		get 
	//		{
	//			return is_favorite;
	//		}
	//		set 
	//		{
	//			is_favorite = value;
	//			OnPropertyChanged("favoriteVisible");
	//			OnPropertyChanged("favoriteSource");
	//		}
	//	}

	//	[Ignore]
	//	public string favoriteSource
	//	{ 
	//		get 
	//		{
	//			if (Device.RuntimePlatform == "iOS")
	//			{
	//				if (is_favorite)
	//				{
	//					return "ic_star_white_3x.png";
	//				}
	//				else return "ic_star_border_white_3x.png";
	//			}
	//			else {
	//				return is_favorite ? "ic_star_white.png" : "ic_star_border_white.png";
	//			}
	//		}
	//	}

 //       [Ignore]
 //       public string listenedToSource
 //       {
 //           get
 //           {
 //               if (listenedToVisible)
 //               {
 //                   return "ic_check_box_white_3x.png";
 //               }
 //               else return "ic_check_box_outline_blank_white_3x.png";
 //           }
 //       }

	//	[Ignore]
	//	public bool hasJournalVisible
	//	{ 
	//		get 
	//		{
	//			return has_journal;
	//		}
	//		set {
	//			has_journal = value;
	//			OnPropertyChanged("hasJournalVisible");
	//		}
	//	}

	//	public event PropertyChangedEventHandler PropertyChanged;

	//	protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null) { 
	//		var handler = PropertyChanged;
	//		if (handler != null)
	//			handler(this, new PropertyChangedEventArgs(propertyName));
	//	}

	//	public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged = delegate { };

	//	private static void NotifyStaticPropertyChanged(string propertyName)
	//	{
	//		StaticPropertyChanged(null, new PropertyChangedEventArgs(propertyName));
	//	}
	}
}
