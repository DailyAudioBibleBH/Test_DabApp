using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using SQLite;
using Xamarin.Forms;

namespace DABApp
{
    public class dbEpisodes
    {
        [PrimaryKey, Indexed]
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
        //public bool is_listened_to { get; set; } //Replaced with UserData
        public double start_time { get; set; } = 0;
        //public double stop_time { get; set; } = 0; //Replaced with UserData
        public string remaining_time { get; set; } = "01:00";
        //public bool is_favorite { get; set; } //Replaced with UserData
        //public bool has_journal { get; set; } //Replaced with UserData
        public bool progressVisible { get; set; }

        public long? audio_size { get; set; } //Size of the file in bytes
        //TODO: Use this if it exists rather than downloading size when getting file progress

        public string audio_duration { get; set; } //Duration - "05:44"

        [Ignore]
        public double Duration  //Duration of the audio file in seconds
        {
            get
            {
                if (audio_duration != null)
                {
                    try
                    {
                        string d = audio_duration;
                        int segments = d.Split(':').Count(); //Count the colons so we can format the TS properly ( needs to be 00:00:00)
                        switch (segments)
                        {
                            case 1:
                                //seconds only
                                d = $"00:00:{d}";
                                break;
                            case 2:
                                //minutes/seconds
                                d = $"00:{d}";
                                break;
                            default:
                                //leave it alone and try as-is
                                break;
                        }
                        TimeSpan ts = TimeSpan.Parse(d);
                        return ts.TotalSeconds;
                    }
                    catch (Exception ex)
                    {
                        //Error converting duration into double
                        return 1;
                    }
                }
                else
                {
                    //No duration specified
                    return 1;
                }

            }
        }


        public string audio_type { get; set; } //Type of audio file "audio/mp3"

        [Ignore]
        public string File_extension
        //Extension of the file (always lower case)
        {
            get
            {
                return url.Split('.').Last().ToLower();
            }
        }

        [Ignore]
        public string File_name_local
        {
            get
            {
                if (is_downloaded)
                {
                    //Use the local file
                    var doc = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var fileName = System.IO.Path.Combine(doc, $"{id}.{File_extension}");
                    FileManager fm = new FileManager();
                    if (fm.FileExists(fileName))
                    {
                        return fileName;
                    }
                    else
                    {
                        //File is marked as downloaded but doesn't really exist
                        return null;
                    }
                }
                else
                {
                    //File isn't downloaded 
                    return null;
                }
            }
        }

        [Ignore]
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

        [Ignore]
        public dbEpisodeUserData UserData
        //User-specific episode data for this episode
        {
            get
            {
                int episodeId = 0;
                string userName = "";
                SQLiteConnection db;

                try
                {

                    episodeId = id.Value;
                    userName = GlobalResources.GetUserName();
                    db = DabData.database; //TODO - Verify this doesn't get overused

                    var data = db.Table<dbEpisodeUserData>()
                        .SingleOrDefault(x => x.EpisodeId == id && x.UserName == userName);

                    //Throw an exception if no data retrievied
                    //This is not an error, but we'll let the exception handler handle it
                    //and return an empty object
                    if (data == null)
                    {
                        throw new Exception($"User-Specific Episode data does not exist for user '{userName}' and episode {episodeId}.");
                    }

                    //Return the matching data
                    return data;
                }
                catch (Exception ex)
                {
                    //User-specific episode data could not be found or failed for some reason. Return an empty record

                    Debug.WriteLine($"User-Specific episode data could not be found: {ex.Message}");

                    return new dbEpisodeUserData()
                    {
                        EpisodeId = episodeId,
                        UserName = userName,
                        IsFavorite = false,
                        IsListenedTo = false,
                        HasJournal = false,
                        CurrentPosition = 0
                    };
                }


            }
        }

    }
}
