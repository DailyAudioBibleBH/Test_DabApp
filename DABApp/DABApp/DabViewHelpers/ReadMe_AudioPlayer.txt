This is documentation about the Audio Player class of the DAB app.

While the AudioPlayer class itself is not static it uses a static singletoninitialized by a static constructor.  
This constructor is private and gets called whenever the AudioPlayer is first called by the program.
This usually occurs when the program is selected to play an episode after it's shut down.

The second non-static constructor also gets run at the same time as the first static one.
The second constructor initializes a new instance of the IAudio interface to the Player property of AudioPlayer.
It also sets up a timer which compares the values of a variety of properties on the AudioPlayer to those of the native IAudio Player and updates them accordingly.
Each property on the AudioPlayer which gets updated in this way has an if statement preventing the corresponding property from getting updated 
if there is no difference between it and the native player.
There is also an if statement that prevents the comparison between properties on the IAudio Player and the AudioPlayer if the IAudio Player has not been initialized.

The properties that get updated by this method are as follows:

*CurrentTime
*TotalTime
*IsPlaying
*IsInitialized
*PlayerCanKeepUp

If IAudio player is not initialized then CurrentTime is set to 0, TotalTime is set to 1 and IsPlaying and IsInitialized are both set to false.

When RemainingTime is updated whenever either TotalTime or CurrentTime are updated.  
RemainingTime is a string and is parsed by converting both CurrentTime and TotalTime to TimeSpan objects, getting their difference then parsing that difference into a string.

As a inheriter of the INotifyProperty interface the AudioPlayer takes advantage of the event OnPropertyChanged.  This updates properties throughout particular class instances.
In the set method for the CurrentTime property there is a check to deal with initialization of the player as well as a check to prevent the player from skipping less than 5 seconds.
When CurrentTime is updated the RemainingTime and Progress properties also get updated via a call to the OnProgressChanged method.
OnPropertyChanged is also called for the PlayPauseButtonImage and PlayPauseButtonImageBig properties whenever the app switches from playing to paused or vice versa.

Each episode has a property called stop_time which is the time at which the episode was last paused by the user.  
Immiediately after an episode is set to be played, via the SetAudioFile method, the CurrentTime of the AudioPlayer is set to that episodes stop_time.
Whenever there is a discrepency between the _player isPlaying property and the AudioPlayer isPlaying property either UpdatePause or UpdatePlay methods are called.
The UpdatePause method sets up a Task to run asynchronously in the background, unawaited.  It runs two methods.  
First is a call to AuthenticationAPI.CreateNewActionLog which creates a new pause action log for the episode so that the server knows where the user is in listening to the episode.
The Second is a call to PlayerFeedAPI.UpdateStopTime which updates the stop_time property of the episode in the local database.
The UpdatePause and UpdatePlay methods are updated through the timer instead of directly through the Play, Pause methods so as to prevent SQLite Database Locked exceptions.  

The SetAudioFile also updates the stop_time property of an episode should that episode be playing when the SetAudioFile is called.  
This would happen if a user was listening to an episode and then switched to a different episode while still playing.

There are two public static instances of the AudioPlayer class, Instance and RecordingInstance.  This is done so that a user can save their spot in a podcast they are listening to while
they record a message for the Daily Audio Bible.  On Android the static nature of CrossMediaPlayer.Current, which plays podcast episodes requires the use of MediaPlayer when the AudioService
plays back user recorded audio.  In order to deal with both the MediaPlayer and the CrossMediaPlayer there is an OnRecord property on both the AudioPlayer and the AudioService.
Setting this property via the AudioPlayer will also set this property on the AudioService for that player.  When OnRecord is true the AudioService returns values from the MediaPlayer
when it is false it returns values from the CrossMediaPlayer.Current.  The OnRecord property for the static AudioPlayer Instance class is currently always set to false while on RecordingInstance
it is set to true only when the DABRecordingPage is being displayed.