using Facebook;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FacebookApi
{
    public partial class LoginFrm : Form
    {

        public string AccessToken;



        public LoginFrm()
        {
            InitializeComponent();
        }

        private void LoginFrm_Load(object sender, EventArgs e)
        {
            //Clear the browser cache
            WebBrowserHelper.ClearCache();
            //Open the facebook url Login
            var FacebookUrl = "https://www.facebook.com/dialog/oauth?client_id=145634995501895&redirect_uri=https:%2F%2Fm.facebook.com%2Fconnect%2Flogin_success.html&response_type=token&display=popup&scope=user_groups%2Coffline_access%2Cpublic_profile%2Cuser_friends%2Cemail%2Cuser_about_me%2Cuser_actions.books%2Cuser_actions.fitness%2Cuser_actions.music%2Cuser_actions.news%2Cuser_actions.video%2Cuser_birthday%2Cuser_education_history%2Cuser_events%2Cuser_games_activity%2Cuser_hometown%2Cuser_likes%2Cuser_location%2Cuser_managed_groups%2Cuser_photos%2Cuser_posts%2Cuser_relationships%2Cuser_relationship_details%2Cuser_religion_politics%2Cuser_tagged_places%2Cuser_videos%2Cuser_website%2Cuser_work_history%2Cread_custom_friendlists%2Cread_insights%2Cread_audience_network_insights%2Cread_page_mailboxes%2Cmanage_pages%2Cpublish_pages%2Cpublish_actions%2Crsvp_event%2Cpages_show_list%2Cpages_manage_cta%2Cpages_manage_instant_articles%2Cads_read%2Cads_management%2Cpages_messaging%2Cpages_messaging_phone_number";

            webBrowser1.Navigate(FacebookUrl);
            
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (webBrowser1.Url.AbsoluteUri.Contains("access_token"))
            {
                //authentcation is successfull
                string url1 = webBrowser1.Url.AbsoluteUri;
                string url2 = url1.Substring(url1.IndexOf("access_token", StringComparison.Ordinal) + 13);
                string token = url2.Substring(0, url2.IndexOf("&", StringComparison.Ordinal));
                AccessToken = token;
                //save to database
                FacebookApiEntities db = new FacebookApiEntities();

                FacebookClient fbclient = new FacebookClient(token);
                dynamic account = fbclient.Get("/me?fields=email,name");

                AccountTbl acc = db.AccountTbls.Find(account.id);
                if (acc !=null)
                {
                    acc.AccEmail = account.email;
                    acc.AccName = account.name;
                    acc.AccToken = token;
                }
                else
                {
                    AccountTbl NewAcc = new AccountTbl();
                    NewAcc.AccEmail = account.email;
                    NewAcc.AccName = account.name;
                    NewAcc.AccToken = token;
                    NewAcc.AccID = account.id;
                    db.AccountTbls.Add(NewAcc);

                }

                db.SaveChanges();


            }
        }
    }
}
