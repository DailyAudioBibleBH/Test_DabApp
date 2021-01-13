using System;
using System.Collections.Generic;
using System.Linq;
using DABApp.DabSockets;
using DABApp.DabUI.BaseUI;
using Newtonsoft.Json;
using Xamarin.Forms;

namespace DABApp
{
    public partial class DabUtilityPage : DabBaseContentPage
    {
        public DabUtilityPage()
        {
            InitializeComponent();

            //actions
            pickAction.Items.Add("Receive Profile Changed Event");
            pickAction.Items.Add("Receive new episode");

            //tests
            pickTest.Items.Add("Shuffle episodes to random channels");
            pickTest.Items.Add("Remove every other episode");

            //episodes
            pickTable.Items.Add("Channels");
            pickTable.Items.Add("dbChannels");
            pickTable.Items.Add("dbEpisodes");
            pickTable.Items.Add("dbDataTransfers");
        }


        async void btnClose_Clicked(System.Object sender, System.EventArgs e)
        {
            await Navigation.PopAsync();
        }

        async void btnGraphQlResponse_Clicked(System.Object sender, System.EventArgs e)
        {

            var adb = DabData.AsyncDatabase;
            //build the object
            var data = new DabGraphQlRootObject();

            switch (pickAction.SelectedItem)
            {
                case "ka":
                    //ka
                    data.type = "ka";
                    break;
                case "Receive Profile Changed Event":
                    //user profile changed

                    //update the logged in user's profile with a new name for tests
                    var user = adb.Table<dbUserData>().FirstOrDefaultAsync().Result;

                    //build the message
                    data.type = "data";
                    data.payload = new DabGraphQlPayload()
                    {
                        data = new DabGraphQlData()
                        {
                            updateUser = new DabGraphQlUpdateUser()
                            {
                                user = new GraphQlUser()
                                {
                                    id = user.Id,
                                    wpId = user.WpId,
                                    firstName = $"TEST-{DateTime.Now.Second}",
                                    lastName = user.LastName,
                                    nickname = user.NickName,
                                    email = user.Email,
                                    language = user.Language,
                                    channel = user.Channel,
                                    channels = user.Channels,
                                    userRegistered = user.UserRegistered,
                                    token = user.Token
                                }
                            }
                        }
                    };
                    break;
                case "Receive new episode":
                    data.type = "data";
                    //get the most recent episode
                    var ep = adb.Table<dbEpisodes>().OrderByDescending(x => x.PubDate).FirstAsync().Result;
                    var newId = ep.id.Value + 1;
                    //build an episode
                    data.payload = new DabGraphQlPayload()
                    {
                        data = new DabGraphQlData()
                        {
                            episodePublished = new DabGraphQlEpisodePublished()
                            {
                                episode = new DabGraphQlEpisode()
                                {
                                    episodeId = newId,
                                    id = newId.ToString(),
                                    audioDuration = int.Parse(ep.Duration.ToString()),
                                    audioSize = int.Parse(ep.audio_size.Value.ToString()),
                                    audioType = ep.audio_type,
                                    audioURL = ep.url,
                                    author = ep.author,
                                    channelId = 227, //dab
                                    createdAt = DateTime.Now.ToUniversalTime(),
                                    date = ep.PubDate,
                                    description = $"TEST: {ep.description}",
                                    notes = ep.notes,
                                    readTranslation = ep.read_version_name,
                                    readTranslationShort = ep.read_version_tag,
                                    readURL = ep.read_link,
                                    shareURL = ep.url,
                                    title = $"TEST: {ep.title}",
                                    type = "episode",
                                    unitId = 1,
                                    updatedAt = ep.PubDate,
                                    year = ep.PubYear
                                }
                            }
                        }
                    };
                    break;
                default:
                    await DisplayAlert("Nothing to do.", $"This action has not been defined: {pickAction.SelectedItem}.", "OK");
                    break;
            }



            //serialize the object
            var json = JsonConvert.SerializeObject(data);

            //imitate a reception of the data
            Service.DabService.Socket.ImitateReceive(json);
        }

        async void btnShowTableData_Clicked(System.Object sender, System.EventArgs e)
        {
            //show some table data
            var adb = DabData.AsyncDatabase;
            string result;

            switch (pickTable.SelectedItem)
            {
                case "Channels":
                    //show channels
                    result = "";
                    var channels = adb.Table<Channel>().OrderBy(x => x.channelId).ToListAsync().Result;
                    foreach (var channel in channels)
                    {
                        result += $"{channel.channelId}: {channel.title}:\n";
                    }
                    await DisplayAlert($"Data: {channels.Count}", result, "OK");

                    break;

                case "dbChannels":
                    //show channels
                    result = "";
                    var dbchannels = adb.Table<dbChannels>().OrderBy(x => x.channelId).ToListAsync().Result;
                    foreach (var dbchannel in dbchannels)
                    {
                        result += $"{dbchannel.channelId}: {dbchannel.title}:\n";
                    }
                    await DisplayAlert($"Data: {dbchannels.Count}", result, "OK");
                    break;

                case "dbEpisodes":
                    //show episodes
                    result = "";
                    var dbepisodes = adb.Table<dbEpisodes>().OrderByDescending(x => x.PubDate).ToListAsync().Result;
                    foreach (var dbepisode in dbepisodes)
                    {
                        result += $"{dbepisode.channel_code} | {dbepisode.title} | {dbepisode.PubDate}\n";
                    }
                    await DisplayAlert($"Data: {dbepisodes.Count}", result, "OK");
                    break;

                case "dbDataTransfers":
                    //show episodes
                    result = "";
                    var dbts = adb.Table<dbDataTransfers>().OrderByDescending(x => x.Id).ToListAsync().Result;
                    foreach (var dbt in dbts)
                    {
                        result += $"{dbt.Id} | {dbt.Direction} | {dbt.Data}\n";
                    }
                    await DisplayAlert($"Data: {dbts.Count}", result, "OK");
                    break;

                default:
                    await DisplayAlert("Nothing to do.", $"This table has not been defined: {pickTable.SelectedItem}.", "OK");
                    break;
            }








        }


        async void btnRunTest_Clicked(System.Object sender, System.EventArgs e)
        {
            //show some table data
            var adb = DabData.AsyncDatabase;

            switch (pickTest.SelectedItem)
            {
                case "Shuffle episodes to random channels":
                    //randomly assign episodes to different channels to mix them all up
                    var channels = await adb.Table<Channel>().ToListAsync();
                    var episodes = await adb.Table<dbEpisodes>().ToListAsync();
                    var r = new Random(DateTime.Now.Millisecond);
                    foreach (var ep in episodes)
                    {
                        var channel = channels[r.Next(channels.Count - 1)];
                        ep.channel_code = channel.key;
                        ep.channel_description = channel.title;
                        ep.channel_title = channel.title;
                    }
                    await adb.UpdateAllAsync(episodes);
                    await DisplayAlert("Episodes are shuffled", $"{episodes.Count} have been randomly shuffled among {channels.Count} chnanels.", "OK");
                    break;
                case "Remove every other episode":
                    //remove every other episode
                    var removeEps = await adb.Table<dbEpisodes>().ToListAsync();
                    removeEps = removeEps.OrderBy(x=> x.channel_title).ThenByDescending(x => x.PubDate).ToList();
                    bool del = false;
                    foreach (var ep in removeEps)
                    {
                        if (del)
                        {
                            await adb.DeleteAsync(ep);
                        }
                        del = !del;
                    }
                    await DisplayAlert("Episodes removed", $"Every other episode has been removed.", "OK");
                    break;

                default:
                    await DisplayAlert("Nothing to do.", $"This action has not been defined: {pickAction.SelectedItem}.", "OK");
                    break;
            }
        }
    }
}
