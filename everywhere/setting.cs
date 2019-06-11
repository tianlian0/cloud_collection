using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace everywhere
{
    public partial class setting : Form
    {
        public setting()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            BinaryWriter bw = new BinaryWriter(new FileStream("server.conf", FileMode.Create));
            byte[] btemp = Encoding.Default.GetBytes(textBox1.Text + "\n" + textBox2.Text + "\n" + textBox3.Text);
            bw.Write(Convert.ToBase64String(btemp));
            bw.Close();
            Form1.webdav_server = textBox1.Text;
            Form1.webdav_username = textBox2.Text;
            Form1.webdav_password = textBox3.Text;
            Close();
        }

        private void setting_Load(object sender, EventArgs e)
        {
            BinaryReader br = null;
            string line = null;
            try
            {
                if (File.Exists("server.conf"))
                {
                    br = new BinaryReader(new FileStream("server.conf", FileMode.Open)); //读取配置文件中保存的webdav服务器信息。需要注意的是：信息没有被加密保存，有此类需求请自行添加。
                    line = br.ReadString();
                    byte[] btemp = Convert.FromBase64String(line);
                    line = Encoding.Default.GetString(btemp);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                if (br != null)
                {
                    br.Close();
                }
            }
            if (line != null && line != "" && line.Split('\n').Length == 3)
            {
                textBox1.Text = line.Split('\n')[0];
                textBox2.Text = line.Split('\n')[1];
                textBox3.Text = line.Split('\n')[2];
            }
        }
    }
}
