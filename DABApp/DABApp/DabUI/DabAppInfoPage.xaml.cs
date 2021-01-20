using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using Acr.DeviceInfo;
using DABApp.DabUI.BaseUI;
using DABApp.Service;
using Newtonsoft.Json;
using Version.Plugin;
using Xamarin.Forms;

namespace DABApp
{
    public partial class DabAppInfoPage : DabBaseContentPage
    {
        public DabAppInfoPage()
        {
            InitializeComponent();
            if (GlobalResources.ShouldUseSplitScreen) { NavigationPage.SetHasNavigationBar(this, false); }
            BindingContext = ContentConfig.Instance.blocktext;
            VersionNumber.Text = $"Version Number {CrossVersion.Current.Version}";

            //build stats
            var adb = DabData.AsyncDatabase;

            StringBuilder stats = new StringBuilder();


            stats.AppendLine("System Stats");
            stats.AppendLine($"Content API: {DateTime.Parse(ContentConfig.Instance.data.updated)}");
            stats.AppendLine($"Channels: {adb.Table<dbChannels>().CountAsync().Result}");
            stats.AppendLine($"Episodes: {adb.Table<dbEpisodes>().CountAsync().Result}");
            stats.AppendLine($"User Episode Data: {adb.Table<dbEpisodeUserData>().CountAsync().Result}");
            stats.AppendLine($"Last Action Date GMT: {GlobalResources.LastActionDate}");
            stats.AppendLine($"Badges: {adb.Table<dbBadges>().CountAsync().Result}");
            stats.AppendLine($"User Progress Data: {adb.Table<dbUserBadgeProgress>().CountAsync().Result}");
            stats.AppendLine($"Data Transfer Logs: {adb.Table<dbDataTransfers>().CountAsync().Result}");
            stats.AppendLine();

            //settings (debug mode only)
#if DEBUG
            foreach (var s in adb.Table<dbSettings>().ToListAsync().Result)
            {
                switch (s.Key.ToLower())
                {
                    case "contentjson":
                    case "country":
                    case "token":
                    case "labels":
                    case "states":
                        //do nothing for some settings
                        break;
                    default:
                        //show the settings
                        Debug.WriteLine(s.Key);
                        stats.AppendLine($"{s.Key}: \"{s.Value}\"");
                        stats.AppendLine();
                        break;
                }
            }

#endif


            lblStats.Text = stats.ToString();

        }

        async void Button_Clicked(System.Object sender, System.EventArgs e)
        {
            var adb = DabData.AsyncDatabase;
            //delete action dates
            var dateSettings = adb.Table<dbSettings>().Where(x => x.Key.StartsWith("ActionDate-")).ToListAsync().Result;
            foreach (var item in dateSettings)
            {
                int j = await adb.DeleteAsync(item);
            }

            //delete user actions
            int i = adb.ExecuteAsync("delete from dbPlayerActions").Result;
            //delete user episode data
            i = adb.ExecuteAsync("delete from dbEpisodeUserData").Result;
            //delete user credit cards
            i = adb.ExecuteAsync("DELETE FROM dbCreditCards").Result;
            //delete user donation sources
            i = adb.ExecuteAsync("DELETE FROM dbCreditSource").Result;
            //delete user donation history
            i = adb.ExecuteAsync("DELETE FROM dbDonationHistory").Result;
            //delete user badge progress
            i = adb.ExecuteAsync("DELETE FROM dbUserBadgeProgress").Result;
            //delete user campaigns
            i = adb.ExecuteAsync("DELETE FROM dbUserCampaigns").Result;

            dbUserData user = GlobalResources.Instance.LoggedInUser;
            user.ActionDate = DateTime.MinValue;
            user.CreditCardUpdateDate = DateTime.MinValue;
            user.DonationHistoryUpdateDate = DateTime.MinValue;
            user.DonationStatusUpdateDate = DateTime.MinValue;
            user.ProgressDate = DateTime.MinValue;
            await adb.InsertOrReplaceAsync(user);

            await DabServiceRoutines.GetRecentActions();
            await DabServiceRoutines.GetUpdatedCreditCards();
            await DabServiceRoutines.GetUpdatedDonationHistory();
            await DabServiceRoutines.GetUpdatedDonationStatus();
            await DabServiceRoutines.GetUserBadgesProgress();

            await DisplayAlert("Local User Data Reset", "We have reset your local user data. It will be reloaded when you return to the episodes page.", "OK");
        }

        private Attachment CreateAttachment(string FileName, string Content)
        {
            if (File.Exists(FileName))
            {
                File.Delete(FileName);
            }
            File.WriteAllText(FileName, Content);

            return new Attachment(FileName);
        }

        async void btnSendLogs_Clicked(System.Object sender, System.EventArgs e)
        {
            try
            {

                DabUserInteractionEvents.WaitStarted(sender, new DabAppEventArgs("Gathering and Sending Diagnostics...", true));


                //Start a new mail message with proper destination emails
                var mailSender = new MailAddress("noreply@c2itconsulting.net", "DAB App");
                var mailMessage = new MailMessage();
                mailMessage.From = mailSender;

                //Build the message content
                var adb = DabData.AsyncDatabase;
                var user = GlobalResources.Instance.LoggedInUser;

                if (user.Id == 0)
                {
                    user = new dbUserData()
                    {
                        Email = "noreply@c2itconsulting.net",
                        FirstName = "Guest",
                        LastName = "Guest"
                    };
                }

                mailMessage.To.Add(new MailAddress("appalerts@c2itconsulting.net", "C2IT App Alerts"));
                mailMessage.Subject = $"App Diagnostics: {user.Email}";
                mailMessage.IsBodyHtml = true;

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"<h1>DAB Diagnostics</h1>");
                sb.AppendLine("<table>");
                sb.AppendLine($"<tr><td>timestamp</td></td><td>{DateTime.Now}</td></tr>");
                sb.AppendLine($"<tr><td>email</td></td><td>{user.Email}</td></tr>");
                sb.AppendLine($"<tr><td>name</td></td><td>{user.FirstName} {user.LastName}</td></tr>");
                sb.AppendLine($"<tr><td>platform</td></td><td>{Device.RuntimePlatform}</td></tr>");
                sb.AppendLine($"<tr><td>idiom</td></td><td>{Device.Idiom}</td></tr>");
                sb.AppendLine($"<tr><td>app version</td></td><td>{CrossVersion.Current.Version}</td></tr>");
                sb.AppendLine($"<tr><td>os version</td></td><td>{DeviceInfo.Hardware.OperatingSystem}</td></tr>");
                sb.AppendLine($"<tr><td>width</td></td><td>{DeviceInfo.Hardware.ScreenWidth}</td></tr>");
                sb.AppendLine($"<tr><td>height</td></td><td>{DeviceInfo.Hardware.ScreenHeight}</td></tr>");
                sb.AppendLine($"<tr><td>manufacturer</td></td><td>{DeviceInfo.Hardware.Manufacturer}</td></tr>");
                sb.AppendLine($"<tr><td>model</td></td><td>{DeviceInfo.Hardware.Model}</td></tr>");
                sb.AppendLine("</table>");
                mailMessage.Body = sb.ToString();

                //Attach data files
                mailMessage.Attachments.Add(CreateAttachment("user.txt", JsonConvert.SerializeObject(user, Formatting.Indented)));
                mailMessage.Attachments.Add(CreateAttachment("dbDataTransfers.txt", JsonConvert.SerializeObject(adb.Table<dbDataTransfers>().ToListAsync().Result,Formatting.Indented)));
                mailMessage.Attachments.Add(CreateAttachment("dbPlayerActions.txt", JsonConvert.SerializeObject(adb.Table<dbPlayerActions>().ToListAsync().Result, Formatting.Indented)));
                mailMessage.Attachments.Add(CreateAttachment("dbEpisodeUserData.txt", JsonConvert.SerializeObject(adb.Table<dbEpisodeUserData>().ToListAsync().Result, Formatting.Indented)));
                mailMessage.Attachments.Add(CreateAttachment("dbEpisodes.txt", JsonConvert.SerializeObject(adb.Table<dbEpisodes>().ToListAsync().Result, Formatting.Indented)));
                mailMessage.Attachments.Add(CreateAttachment("dbChannels.txt", JsonConvert.SerializeObject(adb.Table<dbChannels>().ToListAsync().Result, Formatting.Indented)));
                mailMessage.Attachments.Add(CreateAttachment("Channel.txt", JsonConvert.SerializeObject(adb.Table<DabSockets.Channel>().ToListAsync().Result, Formatting.Indented)));
                mailMessage.Attachments.Add(CreateAttachment("dbUserBadgeProgress.txt", JsonConvert.SerializeObject(adb.Table<dbUserBadgeProgress>().ToListAsync().Result, Formatting.Indented)));
                mailMessage.Attachments.Add(CreateAttachment("dbBadges.txt", JsonConvert.SerializeObject(adb.Table<dbBadges>().ToListAsync().Result, Formatting.Indented)));
                mailMessage.Attachments.Add(CreateAttachment("ContentConfig.txt", JsonConvert.SerializeObject(ContentConfig.Instance, Formatting.Indented)));
                mailMessage.Attachments.Add(CreateAttachment("DeviceHardware.txt", JsonConvert.SerializeObject(DeviceInfo.Hardware, Formatting.Indented)));


                //Set up the SMTP client using Mandril API credentials
                var smtp = new SmtpClient();
                smtp.Port = 587;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.UseDefaultCredentials = false;
                smtp.Host = "smtp.mandrillapp.com";
                smtp.Credentials = new NetworkCredential("chetcromer@c2itconsulting.net", "-M0yjVB_9EqZEzuKUDjw3A");
                smtp.EnableSsl = true;

                //Send the email
                await smtp.SendMailAsync(mailMessage);

                DabUserInteractionEvents.WaitStopped(sender, new EventArgs());
                await DisplayAlert("Logs sent", "Logs sent", "OK");

            }
            catch (Exception ex)
            {
                DabUserInteractionEvents.WaitStopped(sender, new EventArgs());
                await DisplayAlert("Error", $"Logs coult not be sent: {ex.Message}", "OK");

            }

        }
    }
}
