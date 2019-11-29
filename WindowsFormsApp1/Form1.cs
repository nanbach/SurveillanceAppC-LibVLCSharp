using LibVLCSharp.Shared;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Data;
using System.Collections.Generic;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private DataTable _cameraDT = null;
        private SqlConnection _connection;
        private SqlCommand _command;
        private SqlDataAdapter _adapter;

        private LibVLCSharp.WinForms.VideoView[][] cameras;

        public Form1()
        {
            InitializeComponent();

            _cameraDT = new DataTable();
            _connection = new SqlConnection("Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\Users\\new\\Documents\\camdb.mdf;Integrated Security=True;Connect Timeout=30");
            _command = new SqlCommand();
            _command.Connection = _connection;

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
                        DataTable ipsDT = execSQL("SELECT camera.ip FROM camera WHERE zone LIKE '" + zone + "' AND street LIKE '" + street + "' AND area LIKE '" + area + "';");
                        foreach (DataRow cdr in ipsDT.Rows)
                        {
                            string ip = cdr["ip"].ToString();
                            treeView1.Nodes[zoneInd].Nodes[stInd].Nodes[areaInd].Nodes.Add(ip);
                            Console.WriteLine("zone: " + zone + " street: " + street + " area: " + area);
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
        }

        private void grid3_Click(object sender, EventArgs e)
        {
            videosTableLayout.Controls.Clear();
            videosTableLayout.ColumnCount = 2;
            videosTableLayout.RowCount = 1;
            TableLayoutPanel dynamicTableLayoutPanel = new TableLayoutPanel();
            Controls.Add(dynamicTableLayoutPanel);
            dynamicTableLayoutPanel.ColumnCount = 1;
            dynamicTableLayoutPanel.RowCount = 2;
            dynamicTableLayoutPanel.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
            dynamicTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 380));
            videosTableLayout.Controls.Add(dynamicTableLayoutPanel, 1, 0);
            dynamicTableLayoutPanel.Dock = DockStyle.Fill;
            videosTableLayout.Controls.Add(cameras[0][0]);
            dynamicTableLayoutPanel.Controls.Add(cameras[0][1]);
            dynamicTableLayoutPanel.Controls.Add(cameras[0][2]);
        }

        private void grid4_Click(object sender, EventArgs e)
        {
            videosTableLayout.Controls.Clear();
            videosTableLayout.ColumnCount = 2;
            videosTableLayout.RowCount = 2;
            videosTableLayout.Controls.AddRange(cameras[0]);
            videosTableLayout.Controls.Add(cameras[1][0], 0, 1);
        }

        private void grid6_Click(object sender, EventArgs e)
        {
            videosTableLayout.Controls.Clear();
            videosTableLayout.ColumnCount = 3;
            videosTableLayout.RowCount = 2;
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 3; j++)
                    videosTableLayout.Controls.Add(cameras[i][j], j, i);
        }

        private void grid9_Click(object sender, EventArgs e)
        {
            videosTableLayout.Controls.Clear();
            videosTableLayout.ColumnCount = 3;
            videosTableLayout.RowCount = 3;
            for (int i = 0; i < 3; i++)
                videosTableLayout.Controls.AddRange(cameras[i]);
        }

    }
}

