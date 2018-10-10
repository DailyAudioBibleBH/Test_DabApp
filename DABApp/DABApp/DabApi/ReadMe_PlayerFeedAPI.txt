There are three different APIs that the DAB App interfaces with.
The Content API which drives pictures and page data for the app.
The Authentication API which controls user data and storage for the app.
And the PlayerFeed API which gets episode data from the server, streams episodes and downloads and deletes episodes from the device.

Each one of these different APIs are served by a class, each of which have static methods and properties which are accessed throughout the app.

The PlayerFeedAPI class manages episodes and information regarding episodes.  This includes getting episodes from the playerfeedAPI and getting episodes from the local database.
It also deals with donations and episode readings.  There are a few static private fields to this class, two SQLiteDataBaseConnections, one asynchronous and one not,
three boolean values meant to stop asynchronous methods from running in multiple threads at the same time and an event handler called ProgressVisible 
which indicates which episodes will be getting downloaded when the DownloadEpisodes method is called.
The methods associated with this class are as follows:

IEnumberable<dbEpisodes> GetEpisodesList(Resource resource) makes a database call to get all episodes for a specific resource.
Used in the EpisodesPage and TabletPage to update the Episode list with info about episodes such as listened to status, episode location and favorited status.
Also used in ChannelsPage and TabletPage to verify if a resource has episodes saved in the database before navigating to that resource

async Task<string> GetEpisodes(Resource resource) makes a get call to the DAB server via the feedurl provided by the resource to get all episodes of that resource for the entire year
saves all episodes found then deletes any old episodes.  Returns a string indicating failure type should the method throw an exception or the server encountering an error.  Returns "OK" if successful
Called by the EpisodePage, ChannelPage and the TabletPage.  For the EpisodePage and TabletPage called OnRefresh when the Episode ListView is pulled down.  On the ChannelsPage and Tablet Page
called whenever a channel is selected so that the page has all updated episodes.

async Task<dbEpisodes> GetMostRecentEpisode(Resourc resource) gets most recent episode for the given resource from the database.  Old code not referenced any where in the project.
Uses the async SQLite Connection.

async GetEpisode(int id) gets specific episode from the database.  Used in the TabletPage to update the currently playing episode when the Episode list is refreshed.
Used in the PlayerBar in OnShare and OnShowPlayer in order to get all the information of the currently playing episode.

void CheckOfflineEpisodesSetting() Checks the offline episode settings by setting the static Instance of the OfflineEpsiodesSettings class to the OfflineEpisode setting stored in memory.
This method is called in the ContentAPI in the CheckContent method.

void UpdateOfflineEpisodeSettings() Updates the SQLite database version of the OfflineEpisodes setting class with that of the static OfflineEpisodesSetting stored in memory. Called in the 
DeleteOnAfterListening and DurationPicked methods of the OfflineEpisodeManagementPage.

async Task<bool>DownloadEpisodes() Downloads all episodes that have resource ids which belong to resources that have been marked for offline download.  
Returns a bool indicating wether or not the operation was successful.  The field DownloadIsRunning is used in this method in order to prevent the method from running on more than one thread simultaneously.
This is to reduce and prevent the SQLite database from locking.  The method does run some of it's code in more than one thread so as to invoke the MakeProgressVisible event.
This event reveals which episodes will be downloaded on both the EpisodesPage and TabletPage.  The DownloadEpisodes method uses the IFileManagement dependency service in order to download each episode
one by one in the order that they are stored in the database.  The DownloadEpisodes method is also recursive.  Should a channel be selected to download while another channel's episodes are downloading 
the method will call itself once it is done downloading the episodes for the first channel.  This method is called in GetEpisodes should the resource be marked as offline.
It is also called whenever the offline episodes switch on the popup menu is marked to download.  Finally it is called in the timed actions of the ChannelsPage and is therefore ran every 5 minutes 
when the app is open.

async void ResumeDownload(object o, Plugin.Connectivity.Abstractions.ConnectivityChangedEventArgs e) method which gets run whenever the device regains connectivity.  Runs the DownloadsEpisodes method.
This is to ensure that if the device loses internet connection it will at least be able to resume downloading as soon as it regains internet connectivity.  
This method is assigned to the ConnectivityChanged event on the static Current instance of the CrossConnectivity nuget package class.  It is assigned in the catch block of the DownloadEpisodes method.
It is unassigned in the ResumeDownload method right after DownloadEpisodes is run.

async Task DeleteEpisodes(Resource resource)  Method which deletes downloaded episodes from local storage and the disk.  Calls the StopDownloading method on the IFileManagement dependency service
in order to ensure that currently downloading episodes stop downloading.  Gets all episodes from the database which either have been downloaded or are set to be downloaded then calls the DeleteEpisode
method of the IFileManagement dependency service.  If the DeleteEpisode method call is successful then the database is updated so that the specific episode is not downloaded and is not queued for
downloading.  The DeleteEpisodes method is only called when the offline episodes switch is switched off.

async Task UpdateEpisodeProperty(int episodeId, string PropertyName = null)  Method which updates specific properties of an episode based on the episode id in the database.  This method has a switch
which targets the PropertyName parameter.  If PropertyName is null, which it is by default, the is_listened_to property of the episode is marked as "listened".  If the PropertyName is "is_favorite"
the episode is_favorite property is marked as the opposite of what it currently is.  This also happens to the has_journal property of the episode if the PropertyName is "has_journal".  If the PropertyName
is an empty string then the is_listened_to property of the episode is replaced with an empty string.  This method is called in the TabletPage, EpisodePage and the PlayerPage whenever an episode is marked
as favorited or listened to whether through the player or the episode list.  It is also ran whenever an episode is unfavorited or marked as not listened to.

