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
        private DataTable _cameraTB = null;
        private SqlConnection _connection;
        private SqlCommand _command;
        private SqlDataAdapter _adapter;

        private LibVLCSharp.WinForms.VideoView[][] cameras = new LibVLCSharp.WinForms.VideoView[3][];

        private string stream, username, password;

        public Form1()
        {
            InitializeComponent();

            _cameraTB = new DataTable();
            _connection = new SqlConnection("Data Source=(LocalDB)/MSSQLLocalDB;AttachDbFilename=C:/Users/new/Documents/camdb.mdf;Integrated Security=True;Connect Timeout=30");
            _command = new SqlCommand();
            _command.Connection = _connection;
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

        private void Form1_Load(object sender, EventArgs e)
        {
            //commandSql("SELECT *  FROM [camera]");
            //populateStreetTreeView();
        }

        private void commandSql(string selectStatement)
        {
            String sql = selectStatement;
            try
            {
                _connection.Open();
                _adapter = new SqlDataAdapter(sql, _connection);

                _adapter.Fill(_cameraTB);
            }
            catch (SqlException ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _connection.Close();
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

