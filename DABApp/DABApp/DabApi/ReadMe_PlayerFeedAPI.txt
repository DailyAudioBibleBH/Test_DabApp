There are three different APIs that the DAB App interfaces with.
The Content API which drives pictures and page data for the app.
The Authentication API which controls user data and storage for the app.
And the PlayerFeed API which gets episode data from the server, streams episodes and downloads and deletes episodes from the device.

Each one of these different APIs are served by a class, each of which have static methods and properties which are accessed throughout the app.

The PlayerFeedAPI class takes 