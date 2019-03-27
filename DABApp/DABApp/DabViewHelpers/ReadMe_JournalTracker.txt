The Journal Tracker ViewModel connects the Xamarin Forms View with the SocketService dependency services.   The Journal Tracker has a static instance called Current which is used whenever
the tracker is used.  It controls the height of the journal text box and it controls the content displayed by the journal text box should the content get updated from another device.

The SocketService is powered by web sockets.  Specifically SocketIOClientDotNet.  Important Note: As of 20190326 the DAB solution uses the defunct Xamarin Components
for both the MarkDownDeep and SocketIOClientDotNet libraries.  These files lie within the Component folder which is not tracked by Git and must be transfered to any new instances of the project.
Installing the equivalent NuGet packages for these libraries should fix this issue.

The Journal Tracker has two constructors, one which is static and one which is not.  The static constructor calculates the height of the Journal textbox based on the type of device and the 
device screenheight.  The second non static constructor calls the SocketService and sets up a timer which detects if the SocketService is disconnected either from the server or the room
and rejoins the server or room if needed.