The iOS AVPlayer does not read files directly from the disk memory of the device instead it caches the file into memory and plays that instead.
This causes the player to play a partially downloaded file even if that file is entirely downloaded on the disk.
Logic is required to make the AVPlayer switch from this cached partailly downloaded file to the fully downloaded file on disk.

The Logic proceeds as follows:

If a partially downloaded file is chosen while the device has an internet connection the AVPlayer will stream the file from the internet.
Should the file be downloaded completely while the AVPlayer is streaming from the internet then, 
when the AVPlayer is paused and then selected to play again it will switch to playing the downloaded file. 
The code for this is within the iOS AudioService Play method.  It is determined to run by a static boolean called UpdateOnPlay which is false by default.

When the internet connection is cut off before the streamed file is done downloading then, when the end of the buffered file is reached,
the AVPlayer will quickly pause and play again switching the buffered file for the fully downloaded file in the process.
If there is no downloaded file then an alert will be displayed to the user.

The UpdateOnPlay property is switched to true by a static event handler from the iOS FileManagement class called DoneDownloading.
DoneDownloading is reassigned to a method called DoneDownloading whenever a new file is set to the AVPlayer and the episode is both not downloaded 
and the partial file exists on the disk.

When the DoneDownloading event is triggered UpdateOnPlay is set to true only if the current episode the AVPlayer is playing is the same as that 
which is done downloading.
If a partially downloaded file is played while the device is disconnected from the internet the partial file will play
and switch to the completely downloaded file when the AVPlayer gets to the end of the partially downloaded file should that file exist.