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

        public long audio_size { get; set; } //Size of the file in bytes
        //TODO: Use this if it exists rather than downloading size when getting file progress

        public TimeSpan audio_duration { get; set; } //Duration - "05:44"
                                                     //TODO: Validate this gets parsed correctly from JSON

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

    }
}
