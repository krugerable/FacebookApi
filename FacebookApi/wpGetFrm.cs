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
    public partial class wpGetFrm : Form
    {
        public wpGetFrm()
        {
            InitializeComponent();
        }
        public List<string> Links = new List<string>();

        private void button1_Click(object sender, EventArgs e)
        {
            Links.Clear();
            // Get the Wp Links
            var client = new WordPressPCL.WordPressClient(textBox1.Text + "/wp-json/" );

            var posts = client.Posts.GetAll().Result;

            foreach(WordPressPCL.Models.Post post in posts)
            {
                Links.Add(post.Link);

            }

            this.Close();

        }
    }
}
