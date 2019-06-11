using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace everywhere
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private Point mouseOffset; //记录鼠标指针的坐标 
        private bool isMouseDown = false; //记录鼠标按键是否按下 
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            int xOffset;
            int yOffset;
            if (e.Button == MouseButtons.Left)
            {
                xOffset = -e.X - SystemInformation.FrameBorderSize.Width;
                yOffset = -e.Y - SystemInformation.CaptionHeight -  SystemInformation.FrameBorderSize.Height;
                mouseOffset = new Point(xOffset, yOffset);
                isMouseDown = true;
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMouseDown)
            {
                Point mousePos = MousePosition;
                mousePos.Offset(mouseOffset.X + 4, mouseOffset.Y + 27);
                Location = mousePos;
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isMouseDown = false;
            }
        }


        Guid guid = Guid.NewGuid(); //用于上传文件时生成文件名
        public static string webdav_server = null;
        public static string webdav_username = null;
        public static string webdav_password = null;
        private void Form1_Load(object sender, EventArgs e)
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
                webdav_server = line.Split('\n')[0];
                webdav_username = line.Split('\n')[1];
                webdav_password = line.Split('\n')[2];
            }
            else
            {
                new setting().ShowDialog();
            }
            if (webdav_server == null)
            {
                MessageBox.Show("未配置webdav服务器将无法使用本软件！");
                退出ToolStripMenuItem.PerformClick();
            }

            int SH = Screen.PrimaryScreen.Bounds.Height;
            int SW = Screen.PrimaryScreen.Bounds.Width;
            Location = (Point)new Size(SW - 120, 60);
            Width = 79;
        }

        private void upload_context(object param)
        {
            IDataObject data = (IDataObject)param;
            if (data.GetFormats().Length <= 0)
            {
                return;
            }
            WebClient _webClient = new WebClient();
            _webClient.Credentials = new NetworkCredential(webdav_username, webdav_password);
            if (data.GetDataPresent(typeof(string))) //如果是文本
            {
                string context = (string)data.GetData(typeof(string));
                if (context.StartsWith("http://") || context.StartsWith("https://"))
                {
                    WebClient mywebclient = new WebClient();
                    byte[] temp = mywebclient.DownloadData(context);
                    if (bytes_is_bitmap(temp)) //如果上传的时图片连接则自动下载图片
                    {
                        Uri _dist_path_1 = new Uri(webdav_server + "图片链接-" + guid.ToString("N") + ".txt");
                        _webClient.UploadString(_dist_path_1, "PUT", context);
                        Uri _dist_path_2 = new Uri(webdav_server + "图片-" + guid.ToString("N") + ".bmp");
                        _webClient.UploadDataAsync(_dist_path_2, "PUT", temp);
                        return;
                    }
                }
                Uri _dist_path = new Uri(webdav_server + "文字-" + guid.ToString("N") + ".txt");
                _webClient.UploadString(_dist_path, "PUT", context);
            }
            else if (data.GetDataPresent(typeof(Bitmap))) //如果是图片
            {
                Bitmap context = (Bitmap)data.GetData(typeof(Bitmap));
                Uri _dist_path = new Uri(webdav_server + "图片-" + guid.ToString("N") + ".bmp");
                MemoryStream ms = new MemoryStream();
                context.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                byte[] bytes = ms.GetBuffer();
                ms.Close();
                _webClient.UploadDataAsync(_dist_path, "PUT", bytes);
            }
            else if (data.GetDataPresent(DataFormats.FileDrop)) //如果是文件
            {
                string[] files = (string [])data.GetData(DataFormats.FileDrop);
                Clipboard.GetFileDropList().CopyTo(files, 0);
                foreach (string file in files) //需要注意的是，如果上传的文件中有文件夹，文件夹结构将不会被保留
                {
                    string filename = Path.GetFileNameWithoutExtension(file);
                    string ext = Path.GetExtension(file);
                    Uri _dist_path = new Uri(webdav_server + filename + "-文件-" + guid.ToString("N") + ext);
                    _webClient.UploadFileAsync(_dist_path, "PUT", file);
                }
            }
            _webClient.Dispose();
        }

        private void 粘贴ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Thread t = new Thread(new ParameterizedThreadStart(upload_context));
            t.Start(Clipboard.GetDataObject());
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            Thread t = new Thread(new ParameterizedThreadStart(upload_context));
            t.Start(e.Data);
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void 隐藏悬浮窗ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private bool bytes_is_bitmap(byte[] Bytes)
        {
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Bytes);
                Bitmap temp =  new Bitmap(stream);
                stream.Close();
                return true;
            }
            catch (Exception ex)
            {
                stream.Close();
            }
            return false;
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            notifyIcon1.Visible = false;
            Environment.Exit(0);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.C && e.Control) //支持ctrl+c快捷键
            {
                粘贴ToolStripMenuItem.PerformClick();
            }
        }

        private void 设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new setting().ShowDialog();
        }

        private void 关于ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("任何问题可以去https://github.com/tianlian0/cloud_collection反馈哈");
        }

    }
}
