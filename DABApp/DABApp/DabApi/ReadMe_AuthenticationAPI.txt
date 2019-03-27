The AuthenticationAPI class is a container for all methods communicating with the DAB Authentication API.  The Authentication API deals with user information and methods which need user info.
All major methods which make calls with the Authentication API are static.  These methods are: 

ValidateLogin:  Logs the user in regardless of if they are a guest or an actual user.

CheckToken: Checks the User token which determines if the user should stay logged in or should be forced to log in again.

ConnectJournal: Connects the user to the Journal server using the same token used by the Check and Exchange token methods.

CreateNewMember: Used in order for users to register themselves with the DAB website through the app. This method uses a authentication bearer token hard coded in Global Resources called APIKey.

ResetPassword:  Sends password reset request to Server which then takes care of emailing the user to reset their password.

LogOut: Which tells the API that the user has logged out.

ExchangeToken:  Which renews the users token whenever they arrive on the Channels page.  This allows active users of the app to stay logged in.

EditMember: Edits the user info via an HttpPut method.

GetAddresses: Gets the billing and shipping addresses associated with the user and their donations.

GetCountries: Gets the list of countries from the API so that they don't have to be hard coded in the project.  This country list is used on the edit and create address page.

UpdateBillingAddress:  Sends the user changes to billing Address to the API via an HttpPut method.

GetWallet: Gets the users credit cards which they use for donations.  It also returns and array of Cards.

DeleteCard: Deletes a users credit card

AddCard: Adds a user credit card using the Stripe API.

GetDonations: Returns an array of Donations which are all of the current recurring donations associated with the user.

UpdateDonation: Updates a preexisting Donation.

AddDonation: Adds a Donation.

DeleteDonation: Deletes a recurring Donation.

GetDonationHistory: Gets all historical donations for a user.

CreateSettings: Overflow method for creating a users settings if they are a new user.

CreateNewActionLog:  Creates a new ActionLog and saves it to the Database.  Action logs are user actions on a specific episode.  They allow the App and API to know the location of the user
in an episode.  They also inform the App if an episode has been listened to or has been favorited.

PostActionLogs:  Posts all of the ActionLogs in the database to the API and then deletes all of the ActionLogs.

GetMemberData:  Gets all of the user episode data from the API such as user episode location and favorite and listened to episodes.

SaveMemberData: Saves the user episode data to the database.

GuestLogin: Deletes all user episode data when a guest logs in.

