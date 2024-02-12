using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace FruitWave.Configurator
{
    //   public delegate void Apply(Config sendConfig);
    public delegate void Apply(DataSet sendData);
    public partial class Configurator : Form
    {
        private readonly Font _lblFont = new Font("MS Gothic", 11f);
        private readonly Font _btnFont = new Font("MS Gothic", 8.5f);
        public event Apply SaveAndApply;
        
        private SqlConnection _conn = new SqlConnection();
        private DataSet _ds = new DataSet();
        private DataGridView _dgv = new DataGridView();
        private DataGridView _dgvS = new DataGridView();
        
        public Configurator()
        {
            InitializeComponent();
            FormBorderStyle = FormBorderStyle.None;
            MinimumSize = new Size(400, 400);
        }

        public sealed override Size MinimumSize
        {
            get { return base.MinimumSize; }
            set { base.MinimumSize = value; }
        }

#region EVENT
        
        private void btnApply_Click(object sender, EventArgs e)
        {
            SaveAndApply?.Invoke(_ds);
            Visible = false;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!Connect())
            {
                MessageBox.Show("Failed to connect to db");
            }
            else
            {
                BuildGridViewProducts();
                BuildGridViewSold();
            }
        }
        /*
         * Ensure we can't insert wrong data into cells
         */
        private void DgvOnCellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            switch (e.ColumnIndex)
            {
                case 0:
                {
                    int i;
                    if (Convert.ToString(e.FormattedValue) == "")
                    {
                        return;
                    }
                    if (!int.TryParse(Convert.ToString(e.FormattedValue), out i))
                    {
                        e.Cancel = true;
                        MessageBox.Show(@"Ensure value is an integer!");
                    }
                    break;
                }
                case 1: break;
                case 2:
                {
                    Decimal @decimal;
                    if (Convert.ToString(e.FormattedValue) == "")
                    {
                        return;
                    }
                    if (!Decimal.TryParse(Convert.ToString(e.FormattedValue), out @decimal))
                    {
                        e.Cancel = true;
                        MessageBox.Show(@"Ensure value is a decimal value!");
                    }
                    break;
                }
                default: { return; }
            }
        }
        
        private void UpdateDb_Click(object sender, EventArgs e)
        {
            //bulk copy
            using (var bulk = new SqlBulkCopy(_conn))
            {
                bulk.BatchSize = 500;
                bulk.NotifyAfter = 1000;
                bulk.DestinationTableName = "products";
                _conn.Open();
                SqlCommand cmd = new SqlCommand(@"DELETE FROM products", _conn);
                cmd.CommandType = CommandType.Text;
                cmd.ExecuteNonQuery();
                bulk.WriteToServer(_ds.Tables[0]);
                _conn.Close();
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
            }
        }
        
#endregion
#region ONLOAD
        private void BuildGridViewProducts()
        { 
            // build
            _dgv = new DataGridView();
            _dgv.Location = new Point(10, 45);
            _dgv.CellValidating += DgvOnCellValidating;
            
            // get products from database
            SqlCommand comm = _conn.CreateCommand();
            comm.CommandText = "Select * From products";
            SqlDataAdapter dataAdapter = new SqlDataAdapter(comm);
            _ds = new DataSet();
            dataAdapter.Fill(_ds);
            _dgv.DataSource = _ds;
            _dgv.DataMember = _ds.Tables[0].TableName;
            _dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            _dgv.Font = new Font("MS Gothic", 9f);
            var style = _dgv.DefaultCellStyle;
            style.Alignment = DataGridViewContentAlignment.MiddleRight;
            _dgv.DefaultCellStyle = style;

            // add
            Controls.Add(_dgv);
            
            // fix width
            var sum = _dgv.Columns.GetColumnsWidth(DataGridViewElementStates.Visible);
            _dgv.Width = sum+100;
            _conn.Close();
            
            // build supporting labels etc..
            BuildGridViewAux();
        }

        private void BuildGridViewSold()
        {
            _dgvS = new DataGridView();

            var local = Controls.Find("lblSold",true);
            _dgvS.Location = local[0].Location + (Size)new Point(0,30);
            _dgvS.Width = 600;
            
            // db
            // sold
            SqlCommand cmd = _conn.CreateCommand();
            cmd.CommandText = "Select * From sold";
            SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(cmd);
            DataSet sold = new DataSet();
            sqlDataAdapter.Fill(sold);

            _dgvS.DataSource = sold;
            _dgvS.DataMember = sold.Tables[0].TableName;
            _dgvS.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            _dgvS.Font = new Font("MS Gothic", 9f);
            
            // insert product name field
            sold.Tables[0].Columns.Add("ProductName", typeof(string));
            
            foreach (DataRow r in sold.Tables[0].Rows)
            {
                DataSet f = new DataSet();
                SqlCommand cmd2 = _conn.CreateCommand();
                cmd2.CommandText = $"Select ProductName From products where ProductId = {r[0]}";
                
                sqlDataAdapter = new SqlDataAdapter(cmd2);
                sqlDataAdapter.Fill(f);

                r[4] = f.Tables[0].Rows[0][0];
            }

            _dgvS.ReadOnly = true;
            Controls.Add(_dgvS);
            _conn.Close();
        }
        
        private void BuildGridViewAux()
        {
            // label 1
            var lblProducts = new Label();
            lblProducts.Width = 150;
            lblProducts.Location = new Point(10, 15);
            lblProducts.Text = @"Products Database";
            lblProducts.Font = _lblFont;
            
            Controls.Add(lblProducts);
            
            // label 2
            var lblSold = new Label();
            lblSold.Name = "lblSold";
            lblSold.Width = lblProducts.Width;
            lblSold.Font = _lblFont;
            lblSold.Text = @"Sold Products";
            var offset = _dgv.Location.Y + _dgv.Height;
            lblSold.Location = new Point(10, offset+30);
            
            Controls.Add(lblSold);
            
            // UpdateDb button
            var btnUpdateDb = new Button();
            btnUpdateDb.Name = "btnUpdate";
            btnUpdateDb.Font = _btnFont;
            btnUpdateDb.Text = @"Store Changes";
            btnUpdateDb.Width = 120;
            btnUpdateDb.Location = new Point(_dgv.Width - btnUpdateDb.Width, _dgv.Height + btnUpdateDb.Height+30);
            btnUpdateDb.Click += UpdateDb_Click;

            Controls.Add(btnUpdateDb);

            var clearSoldButton = new Button();
            clearSoldButton.Location = new Point((_dgvS.Size.Width+_dgvS.Width) + clearSoldButton.Width,(_dgvS.Location.Y + _dgvS.Height) + clearSoldButton.Height+240);
            clearSoldButton.Text = @"Clear sold";
            clearSoldButton.Click += ClearSoldButtonOnClick;
            Controls.Add(clearSoldButton);
        }

        private void ClearSoldButtonOnClick(object sender, EventArgs e)
        {
            _conn.Open();
            SqlCommand cmd = new SqlCommand(@"DELETE FROM sold", _conn);
            cmd.CommandType = CommandType.Text;
            cmd.ExecuteNonQuery();

            cmd = new SqlCommand(@"Select * from sold", _conn);
            cmd.CommandType = CommandType.Text;
            cmd.ExecuteNonQuery();
            SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            sqlDataAdapter.Fill(ds);
            
            _conn.Close();
            
            // insert product name field
            ds.Tables[0].Columns.Add("ProductName", typeof(string));

            _dgvS.DataSource = ds;
        }

#endregion
#region SQL
        private bool Connect()
        {
            _conn = new SqlConnection(@"Data Source=.;Initial Catalog=db_items;Integrated Security=True");
            string query = "select * from products";
            SqlDataAdapter sda = new SqlDataAdapter(query, _conn);
            _ds = new DataSet();
            sda.Fill(_ds);
            _conn.Open();
            return _ds.Tables[0].Rows.Count > 1;
        }
#endregion
    }
}