using LibVLCSharp.Shared;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Data;
using System.Collections.Generic;
using System.Drawing;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private DataTable _cameraDT = null;
        private SqlConnection _connection;
        private SqlCommand _command;
        private SqlDataAdapter _adapter;

        private LibVLCSharp.WinForms.VideoView[][] cameras;

        private LibVLC _libvlc;

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
        }

        private void populateStreetTreeView()
        {
            DataTable distinctZonesDT = execSQL("SELECT DISTINCT zone FROM camera;");
            int zoneInd = 0;
            foreach (DataRow zdr in distinctZonesDT.Rows)
            {
                string zone = zdr["zone"].ToString();
                treeView1.Nodes.Add(zone);
                DataTable distinctStreetsDT = execSQL("SELECT DISTINCT camera.street FROM camera WHERE zone LIKE '" + zone + "';");
                int stInd = 0;
                foreach(DataRow sdr in distinctStreetsDT.Rows)
                {
                    string street = sdr["street"].ToString();
                    treeView1.Nodes[zoneInd].Nodes.Add(street);
                    DataTable distinctAreasDT = execSQL("SELECT DISTINCT camera.area FROM camera WHERE zone LIKE '" + zone + "' AND street LIKE '" + street + "';");
                    int areaInd = 0;
                    foreach (DataRow adr in distinctAreasDT.Rows)
                    {
                        string area = adr["area"].ToString();
                        treeView1.Nodes[zoneInd].Nodes[stInd].Nodes.Add(area);
                        DataRow[] ipsDR = _cameraDT.Select("zone LIKE '" + zone + "' AND street LIKE '" + street + "' AND area LIKE '" + area + "'");//execSQL("SELECT camera.ip FROM camera WHERE zone LIKE '" + zone + "' AND street LIKE '" + street + "' AND area LIKE '" + area + "';");
                        foreach (DataRow cdr in ipsDR)
                        {
                            string ip = cdr["ip"].ToString();
                            treeView1.Nodes[zoneInd].Nodes[stInd].Nodes[areaInd].Nodes.Add(ip);
                        }
                        areaInd++;
                    }
                    stInd++;
                }
                zoneInd++;
            }
        }

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

        private void initializeCameras()
        {
            cameras = new LibVLCSharp.WinForms.VideoView[3][];
            for(int i = 0; i < 3; i++)
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
            for(int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    cameras[i][j].MediaPlayer = new MediaPlayer(_libvlc);
                }
            }
        }

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

        private void clearColSpan()
        {
            if (videosTableLayout.GetRowSpan(videosTableLayout.Controls[0]) > 1)
                videosTableLayout.SetRowSpan(videosTableLayout.Controls[0], 1);
        }

        private void treeView1_ItemDrag(object sender, ItemDragEventArgs e)
        {
            string s = e.Item.ToString().Split(' ')[1];
            if(s.Split('.').Length > 1)
                ((Control)sender).DoDragDrop(s, DragDropEffects.Copy);
        }

        private void videosTableLayout_DragDrop(object sender, DragEventArgs e)
        {
            string ip = e.Data.GetData(typeof(System.String)).ToString();
            Console.WriteLine("table drop: " + ip);
            TableLayoutPanelCellPosition position = GetCellPosotion();
            Console.WriteLine(position);
            DataRow[] camera = _cameraDT.Select("ip LIKE '" + ip + "'");
            ((LibVLCSharp.Shared.IVideoView) videosTableLayout.GetControlFromPosition(position.Column, position.Row)).MediaPlayer.Stop();
            ((LibVLCSharp.Shared.IVideoView) videosTableLayout.GetControlFromPosition(position.Column, position.Row)).MediaPlayer.Play(new Media(_libvlc, camera[0]["url"].ToString(), FromType.FromLocation));
        }

        private void videosTableLayout_DragOver(object sender, DragEventArgs e)
        {
            if(e.Data.GetDataPresent(typeof(System.String)))
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

    }
}

