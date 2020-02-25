using System;
using System.Collections.Generic;

namespace DABApp.DabSockets
{
    //Event Handler used to send data from platform projects back to shared project
    public class DabGraphQlMessageEventHandler
    {
        public string Message;

        public DabGraphQlMessageEventHandler()
        { }

        public DabGraphQlMessageEventHandler(string message)
        {
            //Init with values
            this.Message = message;
        }
    }


    public class DabGraphQlAction
    {
        public int userId { get; set; }
        public int episodeId { get; set; }
        public bool? listen { get; set; }
        public int? position { get; set; }
        public bool? favorite { get; set; }
        public object entryDate { get; set; }
    }

    public class DabGraphQlActionLogged
    {
        public DabGraphQlAction action { get; set; }
    }

    public class TokenRemoved
    {
        public string token { get; set; }
    }

    public class DabGraphQlData
    {
        public DabGraphQlActionLogged actionLogged { get; set; } //Actions Logged
        public List<Channel> channels { get; set; } //Channels
        public DabGraphQlLastActions lastActions { get; set; } //Last Actions
        public DabGraphQlEpisodes episodes { get; set; } //Episodes
        public TriggerEpisodeSubscription triggerEpisodeSubscription { get; set; } //New Episodes
        public TokenRemoved tokenRemoved { get; set; } //Forceful logout
    }

    public class DabGraphQlPayload
    {
        public DabGraphQlData data { get; set; } //Actions Logged, Last Actions
        public string query { get; set; }
        public DabGraphQlVariables variables {get; set;}

        public DabGraphQlPayload()
        {

        }

        public DabGraphQlPayload(string query, DabGraphQlVariables variables)
        {
            this.query = query;
            this.variables = variables;
        }
    }

    public class DabGraphQlRootObject
    {
        public string type { get; set; } //Actions Logged, Last Actions
        public string id { get; set; } //Actions Logged
        public DabGraphQlPayload payload { get; set; } //Actions Logged, Last Actions
        public DabGraphQlData data { get; set; } //Channels

    }

    //Changed name of class so db could find it easily
    public class Channel
    {
        public string id { get; set; }
        public int channelId { get; set; }
        public string key { get; set; }
        public string title { get; set; }
        public string imageURL { get; set; }
        public int rolloverMonth { get; set; }
        public int rolloverDay { get; set; }
        public int bufferPeriod { get; set; }
        public int bufferLength { get; set; }
        public bool @public { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
    }

    public class DabGraphQlLastActions
    {
        public List<DabGraphQlEpisode> edges { get; set; }
        public DabGraphQlPageInfo pageInfo { get; set; }
    }

    public class DabGraphQlPageInfo
    {
        public bool hasNextPage { get; set; }
        public object endCursor { get; set; }
    }

    public class DabGraphQlEpisodes
    {
        public List<DabGraphQlEpisode> edges { get; set; }
        public DabGraphQlPageInfo pageInfo { get; set; }
    }

    public class DabGraphQlEpisode
    {
        //Last Actions Episodes
        public string id { get; set; }
        public int episodeId { get; set; }
        public int userId { get; set; }
        public bool? favorite { get; set; }
        public bool? listen { get; set; }
        public int? position { get; set; }
        public string entryDate { get; set; }
        public DateTime updatedAt { get; set; }
        public DateTime createdAt { get; set; }
        public bool? hasJournal
        {
            get
            {
                if (entryDate == null)
                {
                    return false;
                } else
                {
                    return true;
                }
            }
        }

        //Additional Episode Data
        public string type { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string notes { get; set; }
        public string author { get; set; }
        public DateTime date { get; set; }
        public string audioURL { get; set; }
        public int audioSize { get; set; }
        public int audioDuration { get; set; }
        public string audioType { get; set; }
        public string readURL { get; set; }
        public string readTranslationShort { get; set; }
        public string readTranslation { get; set; }
        public int channelId { get; set; }
        public int? unitId { get; set; }
        public int year { get; set; }
        public object shareURL { get; set; }

        //C2IT added properties
        public double stop_time { get; set; } = 0;
        public bool is_favorite { get; set; }
        public bool has_journal { get; set; }
        public bool is_listened_to { get; set; }
    }

    public class DabGraphQlVariables
    { }

    public class DabGraphQlCommunication
    {
        public string type { get; set; }
        public DabGraphQlPayload payload { get; set; }

        public DabGraphQlCommunication(string type, DabGraphQlPayload payload)
        {
            this.type = type;
            this.payload = payload;
        }
    }

    public class DabGraphQlSubscription
    {
        public string type { get; set; }
        public DabGraphQlPayload payload { get; set; }
        public int id { get; set; }

        public DabGraphQlSubscription(string type, DabGraphQlPayload payload, int id)
        {
            this.type = type;
            this.payload = payload;
            this.id = id;
        }
    }

    public class TriggerEpisodeSubscription
    {
        public string id { get; set; }
        public int episodeId { get; set; }
        public string type { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string notes { get; set; }
        public string author { get; set; }
        public DateTime date { get; set; }
        public string audioURL { get; set; }
        public int audioSize { get; set; }
        public int audioDuration { get; set; }
        public string audioType { get; set; }
        public string readURL { get; set; }
        public string readTranslationShort { get; set; }
        public string readTranslation { get; set; }
        public int channelId { get; set; }
        public int unitId { get; set; }
        public int year { get; set; }
        public object shareURL { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
    }

}
