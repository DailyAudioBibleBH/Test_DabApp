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

    public class DabGraphQlSeeProgress
    {
        public int id { get; set; }
        public int badgeId { get; set; }
        public int percent { get; set; }
        public int year { get; set; }
        public bool seen { get; set; }

    }


    public class DabGraphQlAction
    {
        public int userId { get; set; }
        public int episodeId { get; set; }
        public bool? listen { get; set; }
        public int? position { get; set; }
        public bool? favorite { get; set; }
        public object entryDate { get; set; }

        public bool? hasJournal
        {
            get
            {
                if (entryDate == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
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
        public bool updatePassword { get; set; }
        public bool resetPassword { get; set; }
        public DabGraphQlSeeProgress seeProgress { get; set; }
        public DabGraphQlRegisterUser registerUser { get; set; }
        public DabGraphQlUpdateUser updateUser { get; set; } //User Information
        public DabGraphQlActionLogged actionLogged { get; set; } //Actions Logged
        public List<Channel> channels { get; set; } //Channels
        public DabGraphQlLastActions lastActions { get; set; } //Last Actions
        public DabGraphQlEpisodes episodes { get; set; } //Episodes
        public DabGraphQlEpisodes updatedEpisodes { get; set; } //Updated Episodes (only query used for episodes right now)
        public TriggerEpisodeSubscription triggerEpisodeSubscription { get; set; } //New Episodes
        public TokenRemoved tokenRemoved { get; set; } //Forceful logout
        public GraphQlLoginUser loginUser { get; set; }
        public GraphQlUser user { get; set; }
        public DabGraphQlUpdateToken updateToken { get; set; }
        public DabGraphQlUpdatedBadges updatedBadges { get; set; }
        public DabGraphQlUpdatedProgress updatedProgress { get; set; }
        public DabGraphQlProgressUpdated progressUpdated { get; set; }
        public DabGraphQlEpisodePublished episodePublished { get; set; }
        public DabGraphQlLogAction logAction { get; set; }
        public DabGraphQlUpdateUserFields updateUserFields { get; set; }
        public bool checkEmail { get; set; }
        public List<DabGraphQlAddress> addresses { get; set; }
        public DabGraphQlAddress updateUserAddress { get; set; }
        public List<DabGraphQlCreditCard> updatedCards { get; set; }
        public DabGraphQlCreditCard updatedCard { get; set; }
    }

    public class DabGraphQlCreditCard
    {
        public int wpId { get; set; }
        public int userId { get; set; }
        public int lastFour { get; set; }
        public int expMonth { get; set; }
        public int expYear { get; set; }
        public string type { get; set; }
        public string status { get; set; }
    }

    public class DabGraphQlAddress
    {
        public int wpId { get; set; }
        public string type { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string company { get; set; }
        public string addressOne { get; set; }
        public object addressTwo { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string postcode { get; set; }
        public string country { get; set; }
        public string phone { get; set; }
        public string email { get; set; }

        public DabGraphQlAddress()
        {

        }

        public DabGraphQlAddress(DabGraphQlAddress address)
        {
            this.wpId = address.wpId;
            this.type = address.type;
            this.firstName = address.firstName;
            this.lastName = address.lastName;
            this.company = address.company;
            this.addressOne = address.addressOne;
            this.addressTwo = address.addressTwo;
            this.city = address.city;
            this.state = address.state;
            this.postcode = address.postcode;
            this.country = address.country;
            this.phone = address.phone;
            this.email = address.email;
        }
    }

    public class DabGraphQlLogAction
    {
        public int episodeId { get; set; }
        public int userId { get; set; }
        public bool? listen { get; set; }
        public int? position { get; set; }
        public bool? favorite { get; set; }
        public object entryDate { get; set; }
        public DateTime updatedAt { get; set; } //server timestamp of update
        public DateTime favoriteUpdatedAt { get; set; } //device timestamp
        public DateTime listenUpdatedAt { get; set; } //device timestamp
        public DateTime positionUpdatedAt { get; set; } //device timestamp
        public DateTime entryDateUpdatedAt { get; set; } //device timestamp
    }


    public class DabGraphQlRegisterUser
    {
        public int id { get; set; }
        public int wpId { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string nickname { get; set; }
        public string email { get; set; }
        public string language { get; set; }
        public string channel { get; set; }
        public string channels { get; set; }
        public string userRegistered { get; set; }
        public string token { get; set; }
    }

    public class DabGraphQlUpdateUserFields
    {
        public int id { get; set; }

        public int wpId { get; set; }

        public string firstName { get; set; }

        public string lastName { get; set; }

        public string nickname { get; set; }

        public string email { get; set; }

        public string language { get; set; }

        public string channel { get; set; }

        public string channels { get; set; }

        public DateTimeOffset userRegistered { get; set; }

        public string token { get; set; }
    }

    public class DabGraphQlEpisodePublished
    {
        public DabGraphQlEpisode episode { get; set; }
    }

    public class DabGraphQlProgressUpdated
    {
        public DabGraphQlProgress progress { get; set; }
    }

    public class DabGraphQlProgress
    {
        private DabGraphQlProgress progress;

        public int id { get; set; }
        public int badgeId { get; set; }
        public string data { get; set; }
        public int percent { get; set; }
        public int year { get; set; }
        public bool? seen { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }

        public DabGraphQlProgress()
        {

        }
    
    }

    public class DabGraphQlUpdatedProgress
    {
        public List<dbUserBadgeProgress> edges { get; set; }
        public DabGraphQlPageInfo pageInfo { get; set; }
    }

    public class DabGraphQlUpdatedBadges
    {
        public List<Badge> edges { get; set; }
        public DabGraphQlPageInfo pageInfo { get; set; }
    }

    public class GraphQlUser
    {
        public GraphQlUser()
        {

        }
        public GraphQlUser(GraphQlUser userData)
        {
            this.id = userData.id;
            this.wpId = userData.wpId;
            this.firstName = userData.firstName;
            this.lastName = userData.lastName;
            this.nickname = userData.nickname;
            this.email = userData.email;
            this.language = userData.language;
            this.channel = userData.channel;
            this.channels = userData.channels;
            this.userRegistered = userData.userRegistered;
            this.token = userData.token;

        }

        public GraphQlUser(dbUserData userData)
        {
            this.id = userData.Id;
            this.wpId = userData.WpId;
            this.firstName = userData.FirstName;
            this.lastName = userData.LastName;
            this.nickname = userData.NickName;
            this.email = userData.Email;
            this.language = userData.Language;
            this.channel = userData.Channel;
            this.channels = userData.Channels;
            this.userRegistered = userData.UserRegistered;
            this.token = userData.Token;
        }

        public int id { get; set; }
        public int wpId { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string nickname { get; set; }
        public string email { get; set; }
        public string language { get; set; }
        public string channel { get; set; }
        public string channels { get; set; }
        public DateTime userRegistered { get; set; }
        public string token { get; set; }
    }

    public class DabGraphQlUpdateUser
    {
        public GraphQlUser user { get; set; }
    }

    public class GraphQlLoginUser
    {
        public string token { get; set; }
    }

    public class GraphQlError
    {
        public string message { get; set; }
        public List<GraphQlLocation> locations { get; set; }
        public List<string> path { get; set; }
        public GraphQlExtensions extensions { get; set; }
    }

    public class GraphQlLocation
    {
        public int line { get; set; }
        public int column { get; set; }
    }

    public class GraphQlExtensions
    {
        public string code { get; set; }
    }


    public class DabGraphQlPayload
    {
        public DabGraphQlData data { get; set; } //Actions Logged, Last Actions
        public string query { get; set; }
        public DabGraphQlVariables variables {get; set;}
        public List<GraphQlError> errors { get; set; }
        public string message { get; set; }

        public DabGraphQlPayload()
        {

        }

        public DabGraphQlPayload(string query, DabGraphQlVariables variables)
        {
            this.query = query;
            this.variables = variables;
        }
    }

    public class DabGraphQlUpdateToken
    {
        public string token { get; set; }
    }

    public class DabGraphQlRootObject
    {
        public string type { get; set; } //Actions Logged, Last Actions
        public int id { get; set; }
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

    //public class DabGraphQlNewEpisode
    //{
    //    public string id { get; set; }
    //    public int episodeId { get; set; }
    //    public string type { get; set; }
    //    public string title { get; set; }
    //    public string description { get; set; }
    //    public string notes { get; set; }
    //    public string author { get; set; }
    //    public DateTime date { get; set; }
    //    public string audioURL { get; set; }
    //    public int audioSize { get; set; }
    //    public int audioDuration { get; set; }
    //    public string audioType { get; set; }
    //    public object readURL { get; set; }
    //    public object readTranslationShort { get; set; }
    //    public object readTranslation { get; set; }
    //    public int channelId { get; set; }
    //    public int unitId { get; set; }
    //    public int year { get; set; }
    //    public string shareURL { get; set; }
    //    public DateTime createdAt { get; set; }
    //    public DateTime updatedAt { get; set; }
    //}

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

    public class Badge
    {
        public int badgeId { get; set; }
        public string name { get; set; }
        public int id { get; set; }
        public string description { get; set; }
        public string imageURL { get; set; }
        public string type { get; set; }
        public string method { get; set; }
        public string data { get; set; }
        public bool visible { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
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
