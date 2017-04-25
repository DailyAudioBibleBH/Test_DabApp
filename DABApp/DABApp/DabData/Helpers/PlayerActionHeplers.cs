using System;
using System.Collections.Generic;
using System.Linq;

namespace DABApp
{
	public class PlayerEpisodeAction 
	{
		public string entity_datetime { get; set;}
		public string entity_type { get; set;}
		public string entity_id { get; set;}
		public List<PlayerAction> entity_data { get; set; } = new List<PlayerAction>();

		public static List<PlayerEpisodeAction> ParsePlayerActions(List<dbPlayerActions> actions) {
			List<PlayerEpisodeAction> result = new List<PlayerEpisodeAction>();
			var grouped = actions.GroupBy(x => x.EpisodeId).ToList();
			foreach (var episode in grouped) {
				PlayerEpisodeAction action = new PlayerEpisodeAction();
				var first = episode.First();
				action.entity_id = first.EpisodeId.ToString();
				action.entity_datetime = first.ActionDateTime.ToString();
				action.entity_type = first.entity_type;
				foreach (var log in episode) {
					var subaction = new PlayerAction();
					subaction.action = log.ActionType;
					subaction.playertime = log.PlayerTime.ToString();
					action.entity_data.Add(subaction);
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
	}

	public class LoggedEvents { 
		public List<PlayerEpisodeAction> data { get; set;}
	}
}
