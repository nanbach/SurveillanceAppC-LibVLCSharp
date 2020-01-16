using LibVLCSharp.Shared;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using System.Drawing;
using System.Timers;
using LibVLCSharp.WinForms;
using System.Text;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private DataTable _cameraDT = null;
        private SqlConnection _connection;
        private SqlCommand _command;
        private SqlDataAdapter _adapter;

        private static string currentDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        private LibVLC _libvlc;

        private LibVLCSharp.WinForms.VideoView[][] cameras;
        private static Dictionary<DataRow, MediaPlayer> recordMP = new Dictionary<DataRow, MediaPlayer>();
        private static Dictionary<string, Media> recordM = new Dictionary<string, Media>();

        private static System.Timers.Timer aTimer, bTimer;

        public Form1()
        {
            InitializeComponent();

            _cameraDT = new DataTable();
            _connection = new SqlConnection("Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\Users\\new\\Documents\\camdb.mdf;Integrated Security=True;Connect Timeout=30");
            _command = new SqlCommand();
            _command.Connection = _connection;

            // this will load the native libvlc library (if needed, depending on the platform). 
            Core.Initialize();

            // instanciate the main libvlc object
            _libvlc = new LibVLC();

            initializeCameras();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _cameraDT = execSQL("SELECT * FROM camera;");
            populateStreetTreeView();
            //camRecord();
        }

        // initialization
        private void populateStreetTreeView()
        {
            DataTable distinctZonesDT = execSQL("SELECT DISTINCT zone FROM camera;");
            int zoneInd = 0;
            foreach (DataRow zdr in distinctZonesDT.Rows)
            {
                string zone = zdr["zone"].ToString();
                treeView1.Nodes.Add(zone, zone);
                DataTable distinctStreetsDT = execSQL("SELECT DISTINCT camera.street FROM camera WHERE zone LIKE '" + zone + "';");
                int stInd = 0;
                foreach (DataRow sdr in distinctStreetsDT.Rows)
                {
                    string street = sdr["street"].ToString();
                    treeView1.Nodes[zoneInd].Nodes.Add(street, street);
                    DataTable distinctAreasDT = execSQL("SELECT DISTINCT camera.area FROM camera WHERE zone LIKE '" + zone + "' AND street LIKE '" + street + "';");
                    int areaInd = 0;
                    foreach (DataRow adr in distinctAreasDT.Rows)
                    {
                        string area = adr["area"].ToString();
                        treeView1.Nodes[zoneInd].Nodes[stInd].Nodes.Add(area, area);
                        DataRow[] cameras = _cameraDT.Select("zone LIKE '" + zone + "' AND street LIKE '" + street + "' AND area LIKE '" + area + "'");//execSQL("SELECT camera.ip FROM camera WHERE zone LIKE '" + zone + "' AND street LIKE '" + street + "' AND area LIKE '" + area + "';");
                        foreach (DataRow cdr in cameras)
                        {
                            string ip = cdr["ip"].ToString();
                            treeView1.Nodes[zoneInd].Nodes[stInd].Nodes[areaInd].Nodes.Add(ip, ip);
                        }
                        areaInd++;
                    }
                    stInd++;
                }
                zoneInd++;
            }
        }
        private void initializeCameras()
        {
            cameras = new LibVLCSharp.WinForms.VideoView[3][];
            for (int i = 0; i < 3; i++)
                cameras[i] = new LibVLCSharp.WinForms.VideoView[3];
            cameras[0][0] = videoView1;
            cameras[0][1] = videoView2;
            cameras[0][2] = videoView3;
            cameras[1][0] = videoView4;
            cameras[1][1] = videoView5;
            cameras[1][2] = videoView6;
            cameras[2][0] = videoView7;
            cameras[2][1] = videoView8;
            cameras[2][2] = videoView9;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    cameras[i][j].MediaPlayer = new MediaPlayer(_libvlc);
                }
            }
        }

        // cam recording
        private void camRecord()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("........................................");
            int i = 0;
            foreach (DataRow camera in _cameraDT.Rows)
            {
                string ip = camera["ip"].ToString(), url = camera["url"].ToString();
                sb.AppendLine("starting: " + ip);
                // Create new media with HLS link
                Media media = new Media(_libvlc, url, FromType.FromLocation);
                // Define stream output options. 
                // In this case stream to a file with the given path and play locally the stream while streaming it.
                media.AddOption(":sout=#transcode{scodec=none}:duplicate{dst=std{access=file,mux=mp4,dst='" + GetRecordFilePath(ip) + "'}");
                media.AddOption(":no-sout-all:sout-keep");
                // create video view for the corresponding url
                VideoView videoView = new VideoView();
                // Start recording
                videoView.MediaPlayer = new MediaPlayer(_libvlc);
                videoView.MediaPlayer.Play(media);
                // insert mediaplayer into the list for controlling it
                recordMP.Add(camera, videoView.MediaPlayer);
                recordM.Add(ip, media);
                // ensure only first 3 ips
                if (++i == 3) break;
            }
            // Create a timer with end of half hour interval.
            var now_timespan = new TimeSpan(0, 0, DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Millisecond);//15.20.13
            var to_timespan = new TimeSpan(0, 0, ((DateTime.Now.Minute >= 30) ? 59 : 29), 59, 980);//15.29.29
            var dif_timespan = to_timespan - now_timespan;
            sb.AppendLine("... " + DateTime.Now.ToString("HH:mm:ss") + " " + DateTime.Now.Millisecond);
            sb.AppendLine("expected return " + to_timespan);
            aTimer = new System.Timers.Timer(dif_timespan.TotalMilliseconds);
            // Hook up the Elapsed event for the timer.
            aTimer.Elapsed += (sender, e) => OnTimedEvent(sender, e);
            aTimer.AutoReset = false;
            aTimer.Enabled = true;
            File.AppendAllText(currentDirectory + "log.txt", sb.ToString());
            // timer for checking that the cameras are constantly playing (restart them if not) every 1 min
            to_timespan = new TimeSpan(0, 0, DateTime.Now.Minute, 59, 980);
            dif_timespan = to_timespan - now_timespan;
            sb.AppendLine("expected revival " + to_timespan);
            bTimer = new System.Timers.Timer(dif_timespan.TotalMilliseconds);
            bTimer.Elapsed += (sender, e) => reviveCams(sender, e);
            bTimer.AutoReset = false;
            bTimer.Enabled = true;
            sb.Clear();
        }
        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("... " + DateTime.Now.ToString("HH:mm:ss") + " " + DateTime.Now.Millisecond);
            var now_timespan = new TimeSpan(0, 0, DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Millisecond);
            var to_timespan = new TimeSpan(0, 0, ((DateTime.Now.Minute >= 29  && DateTime.Now.Minute < 59) ? 59 : 29), 59, 999);
            sb.AppendLine("expected return " + to_timespan);
            var dif_timespan = to_timespan - now_timespan;
            aTimer.Interval = Math.Abs(dif_timespan.TotalMilliseconds);
            sb.AppendLine("closing all... " + DateTime.Now.ToString("HH:mm:ss") + " " + DateTime.Now.Millisecond);
            foreach(DataRow camera in recordMP.Keys)
            {
                sb.AppendLine($"closing {camera["ip"]} {DateTime.Now.ToString("HH:mm:ss")} {DateTime.Now.Millisecond}");
                MediaPlayer mp = recordMP[camera];
                mp.Stop();
            }
            foreach(DataRow camera in recordMP.Keys)
            {
                Media m = recordM[camera["ip"].ToString()];
                m.AddOption(":sout=#transcode{scodec=none}:duplicate{dst=std{access=file,mux=mp4,dst='" + GetRecordFilePath(camera["ip"].ToString()) + "'}}");
                m.AddOption(":no-sout-all:sout-keep");
                recordM[camera["ip"].ToString()] = m;
            }
            sb.AppendLine("opening all " + DateTime.Now.ToString("HH:mm:ss") + " " + DateTime.Now.Millisecond); int i = 0;
            foreach(DataRow camera in recordMP.Keys)
            {
                MediaPlayer mp = recordMP[camera];
                mp.Media = recordM[camera["ip"].ToString()];
                sb.AppendLine("Opening.. cam" + (i++) + " " + DateTime.Now.ToString("HH:mm:ss") + " " + DateTime.Now.Millisecond);
                mp.Play();
            }
            sb.AppendLine("all are playing..." + DateTime.Now.ToString("HH:mm:ss") + " " + DateTime.Now.Millisecond);
            File.AppendAllText(currentDirectory + "log.txt", sb.ToString());
            sb.Clear();
        }
        private static string GetRecordFilePath(string cam_ip)
        {
            var dir = Path.Combine(currentDirectory, "data/" + DateTime.Now.ToString("yyyy-MM-dd")) + "/" + cam_ip;
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, DateTime.Now.ToString("HH.mm.ss") + ".mp4");
        }
        private static void reviveCams(Object source, ElapsedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("reviving... " + DateTime.Now.ToString("HH:mm:ss") + " " + DateTime.Now.Millisecond);
            var now_timespan = new TimeSpan(0, 0, DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Millisecond);
            var to_timespan = new TimeSpan(0, 0, DateTime.Now.Minute, 59, 998);
            sb.AppendLine("expected revival " + to_timespan);
            var dif_timespan = to_timespan - now_timespan;
            bTimer.Interval = Math.Abs(dif_timespan.TotalMilliseconds+1.5);
            switch (DateTime.Now.Minute)
            {
                case 30 | 00:
                    return;
            };
            foreach (DataRow camera in recordMP.Keys)
            {
                MediaPlayer mp = recordMP[camera];
                if (!mp.IsPlaying)
                {
                    mp.Stop();
                    mp.Media = recordM[camera["ip"].ToString()];
                    mp.Media.AddOption(":sout=#transcode{scodec=none}:duplicate{dst=std{access=file,mux=ts,dst='" + GetRecordFilePath(camera["ip"].ToString()) + "'}");
                    mp.Media.AddOption(":no-sout-all:sout-keep");
                    mp.Play();
                    sb.AppendLine("restarting cam " + camera["ip"] + " " + DateTime.Now.ToString("HH:mm:ss"));
                }
            }
            File.AppendAllText(currentDirectory + "log.txt", sb.ToString());
            sb.Clear();
        }
        // grid
        private void grid1_Click(object sender, EventArgs e)
        {
            videosTableLayout.Controls.Clear();
            videosTableLayout.ColumnCount = 1;
            videosTableLayout.RowCount = 1;
            videosTableLayout.Controls.Add(cameras[0][0]);
        }
        private void grid2_Click(object sender, EventArgs e)
        {
            videosTableLayout.Controls.Clear();
            videosTableLayout.ColumnCount = 2;
            videosTableLayout.RowCount = 1;
            for (int i = 0; i < 2; i++)
                videosTableLayout.Controls.Add(cameras[0][i], i, 0);
            clearColSpan();
        }
        private void grid3_Click(object sender, EventArgs e)
        {
            videosTableLayout.Controls.Clear();
            videosTableLayout.ColumnCount = 2;
            videosTableLayout.RowCount = 2;
            videosTableLayout.Controls.AddRange(cameras[0]);
            videosTableLayout.SetRowSpan(videosTableLayout.Controls[0], 2);
        }
        private void grid4_Click(object sender, EventArgs e)
        {
            videosTableLayout.Controls.Clear();
            videosTableLayout.ColumnCount = 2;
            videosTableLayout.RowCount = 2;
            videosTableLayout.Controls.AddRange(cameras[0]);
            videosTableLayout.Controls.Add(cameras[1][0], 0, 1);
            clearColSpan();
        }
        private void grid6_Click(object sender, EventArgs e)
        {
            videosTableLayout.Controls.Clear();
            videosTableLayout.ColumnCount = 3;
            videosTableLayout.RowCount = 2;
            videosTableLayout.Controls.AddRange(cameras[0]);
            videosTableLayout.Controls.AddRange(cameras[1]);
            clearColSpan();
        }
        private void grid9_Click(object sender, EventArgs e)
        {
            videosTableLayout.Controls.Clear();
            videosTableLayout.ColumnCount = 3;
            videosTableLayout.RowCount = 3;
            for (int i = 0; i < 3; i++)
                videosTableLayout.Controls.AddRange(cameras[i]);
            clearColSpan();
        }

        private void treeView1_DoubleClick(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode.Level == 2)
            {
                TreeNode node = treeView1.SelectedNode;
                string zone = node.Parent.Parent.Text, street = node.Parent.Text, area = node.Text;
                DataRow[] cameras = _cameraDT.Select("zone LIKE '" + zone + "' AND street LIKE '" + street + "' AND area LIKE '" + area + "'");
                if (cameras.Length > 9) return;
                switch (cameras.Length)
                {
                    case 1:
                        grid1_Click(null, EventArgs.Empty);
                        break;
                    case 2:
                        grid2_Click(null, EventArgs.Empty);
                        break;
                    case 3:
                        grid3_Click(null, EventArgs.Empty);
                        break;
                    case 4:
                        grid4_Click(null, EventArgs.Empty);
                        break;
                    case 5 | 6:
                        grid6_Click(null, EventArgs.Empty);
                        break;
                    case 7 | 8 | 9:
                        grid9_Click(null, EventArgs.Empty);
                        break;
                }
                int columns = videosTableLayout.ColumnCount, rows = videosTableLayout.RowCount, i = 0, j = 0;
                foreach (DataRow camera in cameras)
                {
                    if (i == rows) i = 0;
                    if(j==columns)
                    ((LibVLCSharp.Shared.IVideoView)videosTableLayout.GetControlFromPosition(j, i)).MediaPlayer.Stop();
                    ((LibVLCSharp.Shared.IVideoView)videosTableLayout.GetControlFromPosition(j, i)).MediaPlayer.Play(new Media(_libvlc, camera["url"].ToString(), FromType.FromLocation));
                    i++; j++;
                }
            }
        }

        // drag and drop functionality
        private void videosTableLayout_DragDrop(object sender, DragEventArgs e)
        {
            string ip = e.Data.GetData(typeof(System.String)).ToString();
            Console.WriteLine("table drop: " + ip);
            TableLayoutPanelCellPosition position = GetCellPosotion();
            Console.WriteLine(position);
            DataRow[] camera = _cameraDT.Select("ip LIKE '" + ip + "'");
            ((LibVLCSharp.Shared.IVideoView)videosTableLayout.GetControlFromPosition(position.Column, position.Row)).MediaPlayer.Stop();
            ((LibVLCSharp.Shared.IVideoView)videosTableLayout.GetControlFromPosition(position.Column, position.Row)).MediaPlayer.Play(new Media(_libvlc, camera[0]["url"].ToString(), FromType.FromLocation));
        }
        private void videosTableLayout_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(System.String)))
                e.Effect = DragDropEffects.Copy;
        }
        private TableLayoutPanelCellPosition GetCellPosotion()
        {
            //mouse position
            Point p = videosTableLayout.PointToClient(Control.MousePosition);
            //Cell position
            TableLayoutPanelCellPosition pos = new TableLayoutPanelCellPosition(0, 0);
            //Panel size.
            Size size = videosTableLayout.Size;
            //average cell size.
            SizeF cellAutoSize = new SizeF(size.Width / videosTableLayout.ColumnCount, size.Height / videosTableLayout.RowCount);
            //Get the cell row.
            //y coordinate
            float y = 0;
            for (int i = 0; i < videosTableLayout.RowCount; i++)
            {
                //Calculate the summary of the row heights.
                SizeType type = videosTableLayout.RowStyles[i].SizeType;
                float height = videosTableLayout.RowStyles[i].Height;
                switch (type)
                {
                    case SizeType.Absolute:
                        y += height;
                        break;
                    case SizeType.Percent:
                        y += cellAutoSize.Height;
                        break;
                    case SizeType.AutoSize:
                        y += cellAutoSize.Height;
                        break;
                }
                //Check the mouse position to decide if the cell is in current row.
                if ((int)y > p.Y)
                {
                    pos.Row = i;
                    break;
                }
            }

            //Get the cell column.
            //x coordinate
            float x = 0;
            for (int i = 0; i < videosTableLayout.ColumnCount; i++)
            {
                //Calculate the summary of the row widths.
                SizeType type = videosTableLayout.ColumnStyles[i].SizeType;
                float width = videosTableLayout.ColumnStyles[i].Width;
                switch (type)
                {
                    case SizeType.Absolute:
                        x += width;
                        break;
                    case SizeType.Percent:
                        x += cellAutoSize.Width;
                        break;
                    case SizeType.AutoSize:
                        x += cellAutoSize.Width;
                        break;
                }
                //Check the mouse position to decide if the cell is in current column.
                if ((int)x > p.X)
                {
                    pos.Column = i;
                    break;
                }
            }

            //return the mouse position.
            return pos;
        }
        private void clearColSpan()
        {
            if (videosTableLayout.GetRowSpan(videosTableLayout.Controls[0]) > 1)
                videosTableLayout.SetRowSpan(videosTableLayout.Controls[0], 1);
        }
        private void treeView1_ItemDrag(object sender, ItemDragEventArgs e)
        {
            string s = e.Item.ToString().Split(' ')[1];
            if (s.Split('.').Length > 1)
                ((Control)sender).DoDragDrop(s, DragDropEffects.Copy);
        }

        // querying database
        private DataTable execSQL(string sql)
        {
            DataTable result = new DataTable();
            try
            {
                _connection.Open();
                _adapter = new SqlDataAdapter(sql, _connection);

                _adapter.Fill(result);
            }
            catch (SqlException ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _connection.Close();
            }
            return result;
        }

    }
}

