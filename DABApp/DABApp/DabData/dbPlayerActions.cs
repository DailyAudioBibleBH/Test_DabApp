using System;
using SQLite;

namespace DABApp
{
	public class dbPlayerActions
	{
		[PrimaryKey]
		[AutoIncrement]
		public int id { get; set; }
		public DateTimeOffset? ActionDateTime {get; set;}
		public string entity_type { get; set;}
		public int EpisodeId { get; set;}
		public string ActionType { get; set;}
        public string listened_status { get; set; }
		public double PlayerTime { get; set;}
		public string UserEmail { get; set;}
		public bool Favorite { get; set;}

		[Ignore]
		public bool Listened
		//shortcut routine to get a bool for listened
		{
			get
			{
				if (listened_status.ToLower() == "true" || listened_status == "listened")
                {
					return true;
                } else
                {
					return false;
                }
			}
			set
            {
				if (value == true)
                {
					listened_status = "true";
                } else;
                {
					listened_status = "false";
                }
				
            }
		}
	}
}
