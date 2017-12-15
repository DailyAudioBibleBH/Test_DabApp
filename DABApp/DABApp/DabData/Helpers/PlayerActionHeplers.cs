using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DABApp
{
	public class PlayerEpisodeAction 
	{
		public string entity_datetime { get; set;}
		public string entity_type { get; set;}
		public string entity_id { get; set;}
		public object entity_data { get; set; }

		public static List<PlayerEpisodeAction> ParsePlayerActions(List<dbPlayerActions> actions) {
			List<PlayerEpisodeAction> result = new List<PlayerEpisodeAction>();
			foreach (var log in actions) {
				PlayerEpisodeAction action = new PlayerEpisodeAction();
				action.entity_id = log.EpisodeId.ToString();
				var month = log.ActionDateTime.ToLocalTime().ToString("MMM", CultureInfo.InvariantCulture);
				var time = log.ActionDateTime.ToLocalTime().ToString("HH:mm:ss");
                var offset = log.ActionDateTime.ToLocalTime().Offset.ToString().Replace(":", "");
                offset = offset.Substring(0, offset.Length-2);
                action.entity_datetime = $"{log.ActionDateTime.ToLocalTime().DayOfWeek.ToString().Substring(0, 3)} {month} {log.ActionDateTime.ToLocalTime().Day} {log.ActionDateTime.ToLocalTime().Year} {time} GMT{offset}";
				action.entity_type = log.entity_type;
				if (log.ActionType != "favorite")
				{
					action.entity_data = new PlayerAction(log.ActionType, log.PlayerTime.ToString());
				}
				else
				{
					action.entity_data = new FavoriteAction(log.Favorite);
				}
				result.Add(action);
			}
			return result;
		}
	}

	public class PlayerAction 
	{ 
		public string action { get; set;}
		public string playertime { get; set;}
		public PlayerAction(string Action, string PlayerTime)
		{
			action = Action;
			playertime = PlayerTime;
		}
	}

	public class FavoriteAction 
	{ 
		public bool favorite { get; set;}
		public FavoriteAction(bool Favorite) {
			favorite = Favorite;
		}
	}

	public class LoggedEvents { 
		public List<PlayerEpisodeAction> data { get; set;}
	}

	public class MemberData { 
		public string code { get; set; }
		public string message { get; set; }
		public List<dbEpisodes> episodes { get; set;}
	}
}
