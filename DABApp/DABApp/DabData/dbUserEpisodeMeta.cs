//using System;
//using SQLite;

//namespace DABApp
//{
//    public class dbUserEpisodeMeta
//    {
//        //This class contains user-episode meta data used to preload user information
//        //about episodes. It is preloaded with LastActions data and then pulled into the main
//        //episode table when the actual episode is loaded

//        [PrimaryKey, Indexed]
//        public int EpisodeId { get; set; }
//        public bool? IsListenedTo { get; set; }
//        public double? CurrentPosition { get; set; } = 0;
//        public bool? IsFavorite { get; set; }
//        public bool? HasJournal { get; set; }

//        public dbUserEpisodeMeta()
//        {
//        }
//    }
//}
