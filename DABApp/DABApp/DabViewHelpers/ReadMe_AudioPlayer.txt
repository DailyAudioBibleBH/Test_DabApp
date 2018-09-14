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

