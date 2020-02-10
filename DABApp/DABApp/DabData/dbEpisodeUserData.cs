using System;
using SQLite;

namespace DABApp
{
    public class dbEpisodeUserData

    /*
     * 
     * This class is used to store user-specific episode data such as listened to, favorite, has-journal, and position.
     * Adding this class with user-specific information allows for multiple users to use the app with their own personalized information.
     *
     * SQLite does not support a 2-field primary key, so care must be taken to avoid
     * duplicating data.
     * 
     */


    {
        [Indexed]
        public string UserName { get; set; } //the user id (PK tied with episode id)
        [Indexed]
        public int EpisodeId { get; set; } //the episode id (PK tied with user id)

        public bool IsListenedTo { get; set; } //whether or not the object has been marked listened to

        public double CurrentPosition { get; set; } //current position of the object for playback

        public bool IsFavorite { get; set; } //whether or not the object is favorited

        public bool HasJournal { get; set; } //whether or not the object has an associated journal tied to it



    }
}
