using Facebook;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FacebookApi
{
    public partial class Form1 : Form
    {



        public string SelectedAccID;
        public string SelectedToken;




        #region main
        public Form1()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            LoginFrm NewLogin = new LoginFrm();
            NewLogin.ShowDialog();

            if (NewLogin.AccessToken != null)
            {
                //sucecss
                FacebookApiEntities fb = new FacebookApiEntities();
                cBxAccounts.DataSource = fb.AccountTbls.ToList();

                cBxAccounts.DisplayMember = "AccEmail";
                cBxAccounts.ValueMember = "AccID";
                cBxAccounts.Invalidate();
            }




        }

        private void Form1_Load(object sender, EventArgs e)
        {

            //bind the combo box to accounts table
            FacebookApiEntities fb = new FacebookApiEntities();
            cBxAccounts.DataSource = fb.AccountTbls.ToList();

            cBxAccounts.DisplayMember = "AccEmail";
            cBxAccounts.ValueMember = "AccID";
            cBxAccounts.Invalidate();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FacebookApiEntities fb = new FacebookApiEntities();
            AccountTbl acc = fb.AccountTbls.Find(cBxAccounts.SelectedValue.ToString());
            SelectedAccID = acc.AccID;
            textToken.Text = acc.AccToken;
            SelectedToken = acc.AccToken;
            textAccountName.Text = acc.AccName;


            //validate
            try
            {
                FacebookClient FbCLient = new FacebookClient(SelectedToken);
                dynamic testacc = FbCLient.Get("/me");
                lblStat.Text = "Success";
                lblStat.ForeColor = Color.Green;
            }
            catch
            {
                lblStat.Text = "Invalid";
                lblStat.ForeColor = Color.Red;

            }




        }


        #endregion

        #region Getting Started

        private void button2_Click(object sender, EventArgs e)
        {
            FacebookClient fbclient = new FacebookClient(SelectedToken);
            string postText = "me/feed?message=" + txtbxPostToProfile.Text;
            fbclient.Post(postText, null);

        }


        private void BtnGetPageLikes_Click(object sender, EventArgs e)
        {
            FacebookClient fbclient = new FacebookClient(SelectedToken);
            dynamic pageLikes = fbclient.Get("me/likes?fields=name,fan_count&limit=100");
            int counter = Convert.ToInt32(pageLikes.data.Count);

            for (var i = 0; i < counter; i++)
            {
                ListViewItem ls = new ListViewItem();
                ls.Text = pageLikes.data[i].name;
                ls.SubItems.Add(pageLikes.data[i].fan_count.ToString());
                listViewpageLikes.Items.Add(ls);


            }

        }

        #endregion

        #region Pages

        private void BtnUpdateManagedPages_Click(object sender, EventArgs e)
        {
            FacebookApiEntities db = new FacebookApiEntities();
            FacebookClient fbclient = new FacebookClient(SelectedToken);
            dynamic Mypages = fbclient.Get("/me/accounts?fields=access_token,name,id,fan_count&limit=1000");
            int counter = Convert.ToInt32(Mypages.data.Count);

            //save to database
            for (var i = 0; i < counter; i++)
            {
                PagesTbl pg = db.PagesTbls.Find(Mypages.data[i].id);

                if (pg != null)
                {
                    //page already exists then update
                    pg.PageName = Mypages.data[i].name;
                    pg.PageToken = Mypages.data[i].access_token;
                    pg.fans = Mypages.data[i].fan_count;



                }
                else
                {
                    //create a record for the page
                    PagesTbl Newpage = new PagesTbl();
                    Newpage.PageName = Mypages.data[i].name;
                    Newpage.PageToken = Mypages.data[i].access_token;
                    Newpage.fans = Mypages.data[i].fan_count;
                    Newpage.PageID = Mypages.data[i].id;
                    Newpage.ParentAccountID = SelectedAccID;
                    db.PagesTbls.Add(Newpage);




                }

                db.SaveChanges();

                //load to our listview

                listViewManagedPages.Items.Clear();

                foreach (PagesTbl page in db.PagesTbls.ToList())
                {
                    ListViewItem lsitem = new ListViewItem();
                    lsitem.Text = page.PageName;
                    lsitem.SubItems.Add(page.fans.ToString());
                    lsitem.SubItems.Add(page.PageID);
                    lsitem.SubItems.Add(page.PageToken);
                    listViewManagedPages.Items.Add(lsitem);

                }




            }
        }

        private void BtnGetManagedPages_Click(object sender, EventArgs e)
        {
            //load to our listview
            FacebookApiEntities db = new FacebookApiEntities();
            listViewManagedPages.Items.Clear();

            foreach (PagesTbl page in db.PagesTbls.ToList())
            {
                ListViewItem lsitem = new ListViewItem();
                lsitem.Text = page.PageName;
                lsitem.SubItems.Add(page.fans.ToString());
                lsitem.SubItems.Add(page.PageID);
                lsitem.SubItems.Add(page.PageToken);
                listViewManagedPages.Items.Add(lsitem);

            }


        }

        #region Links
        private void buttonAddLink_Click(object sender, EventArgs e)
        {
            string inputLink = Microsoft.VisualBasic.Interaction.InputBox("Add Link", "Enter you link:", "", 0, 0);
            listBoxLinks.Items.Add(inputLink);

        }

        private void buttonAddLinksFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog filedialog = new OpenFileDialog();

            DialogResult dr = filedialog.ShowDialog();

            if (dr == DialogResult.OK)
            {
                var lines = File.ReadAllLines(filedialog.FileName);
                for (var i = 0; i < lines.Length; i += 1)
                {
                    listBoxLinks.Items.Add(lines[i]);
                }

            }
        }

        private void scheduleLink(string pageToken, string pageid, string link, DateTime schedule)
        {
            FacebookClient fbclient = new FacebookClient(pageToken);
            string dts = DateTimeConvertor.ToUnixTime(schedule).ToString();
            dynamic parms = new ExpandoObject();
            parms.link = link;
            parms.published = false;
            parms.scheduled_publish_time = dts;
            fbclient.Post(pageid + "/feed", parms);


        }

        List<string> Links = new List<string>();
        string SelectedPageToken;
        string selectedPageID;

        private void BtnLinksSchedule_Click(object sender, EventArgs e)
        {


            BtnLinksSchedule.Enabled = false;


            lblLoadingLinks.Visible = true;
            progressBarLinks.Value = 0;
            SelectedPageToken = listViewManagedPages.SelectedItems[0].SubItems[3].Text;
            selectedPageID = listViewManagedPages.SelectedItems[0].SubItems[2].Text;
            Links.Clear();
            foreach (string link in listBoxLinks.Items)
            {
                Links.Add(link);
            }
            progressBarLinks.Maximum = Links.Count;
            backgroundWorkerLinks.RunWorkerAsync();


        }

        private void backgroundWorkerLinks_DoWork(object sender, DoWorkEventArgs e)
        {

            DateTime dt = DateTime.Now.AddHours(Convert.ToDouble(numericUpDownhors.Value));
            foreach (string link in Links)
            {
                scheduleLink(SelectedPageToken, selectedPageID, link, dt);
                backgroundWorkerLinks.ReportProgress(1);
                dt = dt.AddHours(Convert.ToDouble(numericUpDownhors.Value));

            }

        }
        private void backgroundWorkerLinks_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBarLinks.Increment(1);

        }
        private void backgroundWorkerLinks_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            lblLoadingLinks.Visible = false;
            MessageBox.Show("The operation is finished");
            BtnLinksSchedule.Enabled = true;


        }


        private void BtnWp_Click(object sender, EventArgs e)
        {
            wpGetFrm wp = new wpGetFrm();
            wp.ShowDialog();

            foreach (string link in wp.Links)
            {
                listBoxLinks.Items.Add(link);
            }
        }







        #endregion

        #region images

        private void ScheduleImage(string pageToken, string pageid, string imgPath, DateTime schedDate, string description)
        {
            FacebookClient fbclient = new FacebookClient(pageToken);
            string dts = DateTimeConvertor.ToUnixTime(schedDate).ToString();
            dynamic parameters = new ExpandoObject();
            parameters.scheduled_publish_time = dts;
            parameters.published = false;

            if (description != null)
            {
                parameters.message = description;

            }


            parameters.source = new FacebookMediaObject
            {
                ContentType = "image/" + Path.GetExtension(imgPath),
                FileName = imgPath

            }.SetValue(File.ReadAllBytes(imgPath));


            fbclient.Post(pageid + "/photos", parameters);


        }

        private void BtnImageBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = true;
            DialogResult dr = fileDialog.ShowDialog();

            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                foreach (string filepath in fileDialog.FileNames)
                {
                    listBoxImages.Items.Add(filepath);
                }
            }



        }

        List<string> ImagesList = new List<string>();
        private void btnScheduleImages_Click(object sender, EventArgs e)
        {
            btnScheduleImages.Enabled = false;


            lblLoadingImages.Visible = true;
            progressBarImages.Value = 0;
            SelectedPageToken = listViewManagedPages.SelectedItems[0].SubItems[3].Text;
            selectedPageID = listViewManagedPages.SelectedItems[0].SubItems[2].Text;
            ImagesList.Clear();
            foreach (string img in listBoxImages.Items)
            {
                ImagesList.Add(img);
            }
            progressBarImages.Maximum = ImagesList.Count;
            backgroundWorkerimages.RunWorkerAsync();
        }

        private void backgroundWorkerimages_DoWork(object sender, DoWorkEventArgs e)
        {
            DateTime dt = DateTime.Now.AddHours(Convert.ToDouble(numericUpDownhors.Value));
            foreach (string img in ImagesList)
            {
                ScheduleImage(SelectedPageToken, selectedPageID, img, dt, TxtDescription.Text);
                backgroundWorkerimages.ReportProgress(1);
                dt = dt.AddHours(Convert.ToDouble(numericUpDownhors.Value));

            }
        }

        private void backgroundWorkerimages_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBarImages.Increment(1);
        }

        private void backgroundWorkerimages_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            lblLoadingImages.Visible = false;
            MessageBox.Show("The operation is finished");
            btnScheduleImages.Enabled = true;
        }




        #endregion

        #region videos


        private void ScheduleVideo(string pageToken, string pageid, string vidPath, DateTime schedDate, string description)
        {
            FacebookClient fbclient = new FacebookClient(pageToken);
            string dts = DateTimeConvertor.ToUnixTime(schedDate).ToString();
            dynamic parameters = new ExpandoObject();
            parameters.scheduled_publish_time = dts;
            parameters.published = false;

            if (description != null)
            {
                parameters.message = description;

            }
            
            parameters.source = new FacebookMediaObject
            {
                ContentType = "video/" + Path.GetExtension(vidPath),
                FileName = vidPath

            }.SetValue(File.ReadAllBytes(vidPath));
         

            fbclient.Post(pageid + "/videos", parameters);


        }






        List<string> VideosList = new List<string>();
        private void BtnScheduleVideo_Click(object sender, EventArgs e)
        {
            BtnScheduleVideo.Enabled = false;


            lblLoadingVideo.Visible = true;
            progressBarVideo.Value = 0;
            SelectedPageToken = listViewManagedPages.SelectedItems[0].SubItems[3].Text;
            selectedPageID = listViewManagedPages.SelectedItems[0].SubItems[2].Text;
            VideosList.Clear();
            foreach (string vid in listBoxVideos.Items)
            {
                VideosList.Add(vid);
            }
            progressBarVideo.Maximum = VideosList.Count;
            backgroundWorkerVideo.RunWorkerAsync();
        }


        private void backgroundWorkerVideo_DoWork(object sender, DoWorkEventArgs e)
        {
            DateTime dt = DateTime.Now.AddHours(Convert.ToDouble(numericUpDownhors.Value));
            foreach (string vid in VideosList)
            {
                ScheduleVideo(SelectedPageToken, selectedPageID, vid, dt, TxtDescription.Text);
                backgroundWorkerVideo.ReportProgress(1);
                dt = dt.AddHours(Convert.ToDouble(numericUpDownhors.Value));

            }
        }

        private void backgroundWorkerVideo_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBarVideo.Increment(1);

        }

        private void backgroundWorkerVideo_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            lblLoadingVideo.Visible = false;
            MessageBox.Show("The operation is finished");
            BtnScheduleVideo.Enabled = true;
        }

        private void BtnVideoBrowse_Click(object sender, EventArgs e)
        {

            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = true;

            DialogResult dr = fileDialog.ShowDialog();

            if (dr == System.Windows.Forms.DialogResult.OK)
            {


                foreach (string filepath in fileDialog.FileNames)
                {


                    listBoxVideos.Items.Add(filepath);



                }



            }
        }





        #endregion

        #endregion

        private string nextposts;
        private void BtngetPosts_Click(object sender, EventArgs e)
        {

            SelectedPageToken = listViewManagedPages.SelectedItems[0].SubItems[3].Text;
            selectedPageID = listViewManagedPages.SelectedItems[0].SubItems[2].Text;


            listViewPosts.Items.Clear();
            FacebookClient fbclient = new FacebookClient(SelectedPageToken);
            dynamic posts = fbclient.Get(selectedPageID +
                                         "/feed?fields=comments,likes,message,full_picture,created_time&limit=10");

            int postcount = Convert.ToInt32(posts.data.Count);

            for (var i = 0; i < postcount; i++)
            {
                ListViewItem ls = new ListViewItem();
                ls.Text = posts.data[i].id;

                if (posts.data[i].message != null)
                {
                    ls.SubItems.Add(posts.data[i].message.ToString());
                }
                else
                {
                    ls.SubItems.Add("");
                }
                if (posts.data[i].likes != null)
                {
                    ls.SubItems.Add(posts.data[i].likes.data.Count.ToString());
                }
                else
                {
                    ls.SubItems.Add("");
                }
                if (posts.data[i].comments != null)
                {
                    ls.SubItems.Add(posts.data[i].comments.data.Count.ToString());
                }
                else
                {
                    ls.SubItems.Add("");
                }
                ls.SubItems.Add(posts.data[i].created_time.ToString());


                if (posts.data[i].full_picture != null)
                {
                    ls.SubItems.Add(posts.data[i].full_picture.ToString());
                }
                else
                {
                    ls.SubItems.Add("");
                }

                listViewPosts.Items.Add(ls);

            }

            nextposts = posts.paging.next;

        }

      
        private void BtngetNextPosts_Click(object sender, EventArgs e)
        {
            SelectedPageToken = listViewManagedPages.SelectedItems[0].SubItems[3].Text;
            selectedPageID = listViewManagedPages.SelectedItems[0].SubItems[2].Text;


            FacebookClient fbclient = new FacebookClient(SelectedPageToken);
            dynamic posts = fbclient.Get(nextposts);


            int postcount = Convert.ToInt32(posts.data.Count);

            for (var i = 0; i < postcount; i++)
            {
                ListViewItem ls = new ListViewItem();
                ls.Text = posts.data[i].id;

                if (posts.data[i].message != null)
                {
                    ls.SubItems.Add(posts.data[i].message.ToString());
                }
                else
                {
                    ls.SubItems.Add("");
                }
                if (posts.data[i].likes != null)
                {
                    ls.SubItems.Add(posts.data[i].likes.data.Count.ToString());
                }
                else
                {
                    ls.SubItems.Add("");
                }
                if (posts.data[i].comments != null)
                {
                    ls.SubItems.Add(posts.data[i].comments.data.Count.ToString());
                }
                else
                {
                    ls.SubItems.Add("");
                }
                ls.SubItems.Add(posts.data[i].created_time.ToString());


                if (posts.data[i].full_picture != null)
                {
                    ls.SubItems.Add(posts.data[i].full_picture.ToString());
                }
                else
                {
                    ls.SubItems.Add("");
                }

                listViewPosts.Items.Add(ls);

            }

            nextposts = posts.paging.next;
        }


        private string NextComments;
        private void btnGetComments_Click(object sender, EventArgs e)
        {
            SelectedPageToken = listViewManagedPages.SelectedItems[0].SubItems[3].Text;
            listViewComments.Items.Clear();
            FacebookClient fbclient = new FacebookClient(SelectedPageToken);
            dynamic comments = fbclient.Get(listViewPosts.SelectedItems[0].Text + "/?fields=comments{comments,created_time,message,from,likes}");
            int commentsCounts = Convert.ToInt32(comments.comments.data.Count);

            for (var i = 0; i < commentsCounts; i++)
            {
                ListViewItem ls = new ListViewItem();
                ls.Text = comments.comments.data[i].id;
                ls.SubItems.Add(comments.comments.data[i].created_time);
                ls.SubItems.Add(comments.comments.data[i].message);
                ls.SubItems.Add(comments.comments.data[i].from.name);

                if (comments.comments.data[i].comments != null)
                {
                    int replies = comments.comments.data[i].comments.data.Count;
                    ls.SubItems.Add(replies.ToString());
                }
                else
                {
                    ls.SubItems.Add("0");
                }

                if (comments.comments.data[i].likes != null)
                {
                    int likes = comments.comments.data[i].likes.Count;
                    ls.SubItems.Add(likes.ToString());
                }
                else
                {
                    ls.SubItems.Add("0");

                }



                listViewComments.Items.Add(ls);




            }

            NextComments = comments.comments.paging.next;
            lblcommentCount.Text = listViewComments.Items.Count.ToString();


        }

        private void btngetNextComments_Click(object sender, EventArgs e)
        {
            SelectedPageToken = listViewManagedPages.SelectedItems[0].SubItems[3].Text;
            listViewComments.Items.Clear();
            FacebookClient fbclient = new FacebookClient(SelectedPageToken);
            dynamic comments = fbclient.Get(NextComments);
            int commentsCounts = Convert.ToInt32(comments.data.Count);

            for (var i = 0; i < commentsCounts; i++)
            {
                ListViewItem ls = new ListViewItem();
                ls.Text = comments.data[i].id;
                ls.SubItems.Add(comments.data[i].created_time);
                ls.SubItems.Add(comments.data[i].message);
                ls.SubItems.Add(comments.data[i].from.name);

                if (comments.data[i].comments != null)
                {
                    int replies = comments.data[i].Count;
                    ls.SubItems.Add(replies.ToString());
                }
                else
                {
                    ls.SubItems.Add("0");
                }

                if (comments.data[i].likes != null)
                {
                    int likes = comments.data[i].likes.Count;
                    ls.SubItems.Add(likes.ToString());
                }
                else
                {
                    ls.SubItems.Add("0");

                }



                listViewComments.Items.Add(ls);




            }

            NextComments = comments.paging.next;
            lblcommentCount.Text = listViewComments.Items.Count.ToString();

        }

        private void BtnCommentSelected_Click(object sender, EventArgs e)
        {
            SelectedPageToken = listViewManagedPages.SelectedItems[0].SubItems[3].Text;
            FacebookClient fbclient = new FacebookClient(SelectedPageToken);
            foreach (ListViewItem ls in listViewComments.Items)
            {
                if (ls.Selected == true)
                {
                    string commentToPost = ls.Text + "/comments?message=" + TxtBoxAutoComment.Text;
                    fbclient.Post(commentToPost, null);

                }
            }

        }

        private void BtnCommentZero_Click(object sender, EventArgs e)
        {
            SelectedPageToken = listViewManagedPages.SelectedItems[0].SubItems[3].Text;
            FacebookClient fbclient = new FacebookClient(SelectedPageToken);
            foreach (ListViewItem ls in listViewComments.Items)
            {
                if (ls.SubItems[4].Text == "0")
                {
                    string commentToPost = ls.Text + "/comments?message=" + TxtBoxAutoComment.Text;
                    fbclient.Post(commentToPost, null);

                }
            }
        }

        private void BtnAutoLike_Click(object sender, EventArgs e)
        {
            SelectedPageToken = listViewManagedPages.SelectedItems[0].SubItems[3].Text;
            FacebookClient fbclient = new FacebookClient(SelectedPageToken);
            foreach (ListViewItem ls in listViewComments.Items)
            {
                if (ls.Selected == true)
                {
                    string commentToPost = ls.Text + "/likes";
                    fbclient.Post(commentToPost, null);

                }
            }
        }

        private void BtnBrowsePath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();


            DialogResult dr = folderDialog.ShowDialog();

            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                textBoxImageDownloadPath.Text = folderDialog.SelectedPath;


            }
        }

        private void BtnDownloadImages_Click(object sender, EventArgs e)
        {
            BtnDownloadImages.Enabled = false;


            backgroundWorkerDownloadImages.RunWorkerAsync();

            lblDownloadImages.Visible = true;
        }

        private void backgroundWorkerDownloadImages_DoWork(object sender, DoWorkEventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;



            SelectedPageToken = listViewManagedPages.SelectedItems[0].SubItems[3].Text;
            selectedPageID = listViewManagedPages.SelectedItems[0].SubItems[2].Text;

            FacebookClient fbclient = new FacebookClient(SelectedPageToken);
            dynamic objAlbums = fbclient.Get(selectedPageID + "/?fields=albums");
            int albumscount = Convert.ToInt32(objAlbums.albums.data.Count);


            //loop through albums and get image objects
            for (var i = 0; i < albumscount; i++)

            {

                dynamic objImages = fbclient.Get(objAlbums.albums.data[i].id + "?fields=photos.limit(1000){images}");
                int imgcount = Convert.ToInt32(objImages.photos.data.Count);

                for (var j = 0; j < imgcount; j++)
                {

                    string imgID = objImages.photos.data[j].id;
                    dynamic imglist = fbclient.Get(imgID + "?fields=images");
                    string imgLink = imglist.images[0].source;

                    WebRequest request = WebRequest.Create(imgLink);
                    WebResponse response = request.GetResponse();

                    var img = Image.FromStream(response.GetResponseStream());
                    var ext = imgLink.ToLower().Contains(".jpg") ? ".jpg" : ".png";
                    img.Save(textBoxImageDownloadPath.Text + "\\" + imgID + ext);


                }





            }


        }

        private void backgroundWorkerDownloadImages_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("operation finished");
            lblDownloadImages.Visible = false;
            BtnDownloadImages.Enabled = true;

        }
    }
}
