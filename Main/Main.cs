using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using FruitWave.Comps;
using U6.Comp;
using ContentAlignment = System.Drawing.ContentAlignment;

namespace FruitWave
{
    public partial class Main : Form
    {
        private readonly Configurator.Configurator _reference;
        private bool _bother = true;
        private PrintPreviewDialog _preview;
        private readonly PrintDocument _doc = new PrintDocument();

        public Main()
        {
            InitializeComponent();

            WindowState = FormWindowState.Minimized;
            _reference = new Configurator.Configurator();
            _reference.SaveAndApply += BuildButtons;
            _reference.FormClosed += ReferenceOnFormClosed;
            _reference.Show();

            Visible = false;

            MinimumSize = new Size(800, 400);
            
            BuildScale();
        }

        public sealed override Size MinimumSize
        {
            get { return base.MinimumSize; }
            set { base.MinimumSize = value; }
        }

#region EVENT

        private void ReferenceOnFormClosed(object sender, FormClosedEventArgs e)
        {
            WindowState = FormWindowState.Normal;
        }

        private void LabelOnClick(object sender, EventArgs e)
        {
            btn_click(sender, e);
        }

        private void btn_click(object sender, EventArgs e)
        {
            Control[] scalePanel = Controls.Find("pnlScale", true);
            var num = scalePanel[0].Controls.Find("numBox", true);
            var grams = num[0].Text;

            if (grams == "" && _bother == true)
            {
                _bother = false;
                MessageBox.Show(@"Please insert a weight in grams first.");
                return;
            }

            var id = -1;
            var pricekg = new Decimal();

            if (sender.GetType() == typeof(Label))
            {
                var c = (Label)sender;
                var button = (cButton)c.Parent;
                id = button.ProductId;
                pricekg = button.ProductPpkg;
            }
            else if (sender.GetType() == typeof(cButton))
            {
                var c = (cButton)sender;
                id = c.ProductId;
                pricekg = c.ProductPpkg;
            }

            Decimal pt = Convert.ToDecimal(grams) * pricekg / 1000;
            pt = Decimal.Round(pt, 2);
            StoreSoldProduct(id, Convert.ToDecimal(grams), Convert.ToString(DateTime.Now), pt);

            Print();
            _preview.Document = _doc;
            _preview.ShowDialog();
            _bother = true;
            num[0].Text = "";
        }

#endregion

#region ONLOAD

        private void BuildButtons(DataSet inData)
        {
            /*
             * Panel to hold all the buttons
             */
            var panel = new Panel();
            panel.AutoScroll = true;
            panel.Width = 420;
            panel.Name = @"pnlMain";
            panel.AutoSize = true;
            panel.RightToLeft = RightToLeft.Yes;
            if (ActiveForm != null) panel.Location = new Point(ActiveForm.Size.Width - panel.Width, 0);

            Controls.Add(panel);

            var i = 0;
            var r = 0;
            foreach (DataRow data in inData.Tables[0].Rows)
            {
                if (i == 4)
                {
                    i = 0;
                    r++;
                }

                int id = Convert.ToInt16(data.ItemArray[0]);
                Decimal d = Convert.ToDecimal(data.ItemArray[2].ToString());
                var lBtn = new cButton(id, data.ItemArray[1].ToString(), d);
                lBtn.Name = Convert.ToString($@"btnProduct_{i}");
                lBtn.Height = 100;
                lBtn.Width = 100;
                lBtn.Location = new Point(lBtn.Width * i, lBtn.Height * r);
                lBtn.Click += btn_click;

                // fix controls
                foreach (Control o in lBtn.Controls)
                {
                    if (o.Name == "lblId" || o.Name == "lblName" || o.Name == "lblPrice")
                    {
                        o.Click += LabelOnClick;
                    }
                }

                panel.Controls.Add(lBtn);
                i++;
            }

            _reference.Close();
            WindowState = FormWindowState.Normal;
        }

        private void BuildScale()
        {
            var panel = new Panel();
            panel.AutoScroll = true;
            panel.Width = 200;
            panel.Height = 200;
            panel.Name = @"pnlScale";
            panel.AutoSize = true;
            panel.RightToLeft = RightToLeft.No;
            if (ActiveForm != null) panel.Location = new Point(0, (ActiveForm.Size.Height - panel.Height)/2);

            Controls.Add(panel);

            var numBox = new NumText();
            numBox.Name = "numBox";
            numBox.Location = new Point(panel.Width / 2 - numBox.Width / 2, panel.Height / 2 - numBox.Height / 2);
            numBox.Text = "";
            numBox.TextAlign = HorizontalAlignment.Center;
            numBox.Maximum = new Decimal(9999.99);
            numBox.Minimum = new Decimal(0.00);
            numBox.DecimalPlaces = 2;

            panel.Controls.Add(numBox);

            var labelIns = new Label();
            labelIns.AutoSize = true;
            labelIns.Text = @"Enter weight (grams)";
            labelIns.TextAlign = ContentAlignment.MiddleCenter;
            labelIns.Location = new Point(numBox.Location.X + numBox.Width / 2 - labelIns.Width / 2,
                numBox.Location.Y - labelIns.Height);

            panel.Controls.Add(labelIns);

            var labelIns2 = new Label();
            labelIns2.AutoSize = true;
            labelIns2.TextAlign = ContentAlignment.MiddleCenter;
            labelIns2.Text = @"Then click on desired product";
            labelIns2.Location = new Point(numBox.Location.X - numBox.Width / 2 + labelIns2.Width / 2,
                numBox.Location.Y + labelIns2.Height + 5);

            panel.Controls.Add(labelIns2);

            var btnOpenCfg = new Button();
            btnOpenCfg.Size = new Size(150, 25);
            btnOpenCfg.Text = @"Open Configurator";
            btnOpenCfg.Location = new Point(labelIns2.Location.X, labelIns2.Location.Y + 40);
            btnOpenCfg.Click += BtnOpenCfgOnClick;
            
            panel.Controls.Add(btnOpenCfg);
        }

        private void BtnOpenCfgOnClick(object sender, EventArgs e)
        {
            Application.Restart();
            Environment.Exit(0);
        }

#endregion

#region PRINT

        private void StoreSoldProduct(int inId, Decimal inGrams, string inTime, Decimal inCost)
        {
            using (var conn = new SqlConnection(@"Data Source=.;Initial Catalog=db_items;Integrated Security=True"))
            {
                conn.Open();
                var cmd =
                    "INSERT INTO sold (ProductID,ProductGrams,ProductTime,ProductCost) VALUES (@param1,@param2,@param3,@param4)";

                using (SqlCommand eco = new SqlCommand(cmd, conn))
                {
                    eco.Parameters.AddWithValue("@param1", inId);
                    eco.Parameters.AddWithValue("@param2", inGrams);
                    eco.Parameters.AddWithValue("@param3", inTime);
                    eco.Parameters.AddWithValue("@param4", inCost);
                    eco.ExecuteNonQuery();
                }
            }
        }

        private void Print()
        {
            _preview = new PrintPreviewDialog();
            _preview.ClientSize = new Size(800, 800);
            _preview.Location = new Point(300, 300);
            _preview.Name = "preview";
            _preview.MinimumSize = new Size(100, 100);
            _preview.UseAntiAlias = true;
            _doc.PrintPage+= DocOnPrintPage;
            // size is in inches because why the fuck not
            // 44mm print bond stock for receipts is roughly 1 3/4" inches
            _doc.DefaultPageSettings.PaperSize = new PaperSize("v", 173, 65);
        }

        private void DocOnPrintPage(object sender, PrintPageEventArgs e)
        {
            Header(e);
        }
        
#endregion
        
#region RECEIPT GFX

       private void Header(PrintPageEventArgs e)
    {
        // connect
        var conn = new SqlConnection(@"Data Source=.;Initial Catalog=db_items;Integrated Security=True");
        SqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = "Select TOP 1 * From sold ORDER BY ProductTime DESC";
        SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(cmd);
        DataSet sold = new DataSet();
        sqlDataAdapter.Fill(sold);
        conn.Open();
        
        // Find product name by Id
        sold.Tables[0].Columns.Add("ProductName", typeof(string));
        DataSet prodName = new DataSet();
        SqlCommand cmd2 = conn.CreateCommand();
        cmd2.CommandText = $"Select ProductName From products where ProductId = '{sold.Tables[0].Rows[0][0]}'";
                
        sqlDataAdapter = new SqlDataAdapter(cmd2);
        sqlDataAdapter.Fill(prodName);
        sold.Tables[0].Rows[0][4] = prodName.Tables[0].Rows[0][0];
        
        // Add priceKG to the table as well
        sold.Tables[0].Columns.Add("PriceKG",typeof(string));
        DataSet prodKG = new DataSet();
        SqlCommand cmd3 = conn.CreateCommand();
        cmd3.CommandText = $"Select ProductPriceKG from products where ProductId = '{sold.Tables[0].Rows[0][0]}'";
        sqlDataAdapter = new SqlDataAdapter(cmd3);
        sqlDataAdapter.Fill(prodKG);
        sold.Tables[0].Rows[0][5] = prodKG.Tables[0].Rows[0][0];

        // header
        List<string> heads = new List<string>();
        heads.AddRange(new List<string>
        { 
            "FruitWave","Fake Address 69, 420 Imagination",DateTime.Now.ToString(),"-------------------------------------------------"
        });
        StringFormat form = new StringFormat()
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        Font font = new Font("MS Gothic", 15, FontStyle.Underline);
        Font sub = new Font("MS Gothic", 4, FontStyle.Regular);

        e.Graphics.DrawString(heads[0],font,Brushes.Black,(e.PageBounds.Width / 2f),e.PageBounds.Height / 5.9f,form);
        e.Graphics.DrawString(heads[1],sub,Brushes.Black,(e.PageBounds.Width / 2f),e.PageBounds.Height / 2.65f,form);
        e.Graphics.DrawString(sold.Tables[0].Rows[0][2].ToString(),sub,Brushes.Black,(e.PageBounds.Width / 2f),e.PageBounds.Height / 2.1f,form);
        e.Graphics.DrawString(heads[3],sub,Brushes.Black,(e.PageBounds.Width / 2f),e.PageBounds.Height / 1.9f,form);
        
        Product(e,sold.Tables[0].Rows[0]);
    }

       private void Product(PrintPageEventArgs e, DataRow inRow)
    {
        StringFormat form = new StringFormat()
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center
        };
        StringFormat form2 = new StringFormat()
        {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Center
        };
        StringFormat center = new StringFormat()
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        Font product1 = new Font("Monaco", 4, FontStyle.Regular);
        Font style = new Font("MS Gothic", 4, FontStyle.Regular);
        
        e.Graphics.DrawString(inRow.ItemArray[4].ToString().ToUpper(),product1,Brushes.Black,(e.PageBounds.Width / 11f),e.PageBounds.Height / 1.65f,form);
        // go to the right and print price
        e.Graphics.DrawString(inRow.ItemArray[3]+" €",product1,Brushes.Black,(e.PageBounds.Width / 1.1f),e.PageBounds.Height / 1.65f,form2);
        // Add grams etc
        var gToKg = Convert.ToDecimal(inRow.ItemArray[1]) / 1000;
        e.Graphics.DrawString(gToKg+"kg @ "+inRow.ItemArray[5]+" kg",product1,Brushes.Black,(e.PageBounds.Width / 7f),e.PageBounds.Height / 1.45f,form);
        // Draw a cutoff
        e.Graphics.DrawString("-------------------------------------------------",style,Brushes.Black, (e.PageBounds.Width / 2f),e.PageBounds.Height / 1.3f,center);
        
        e.Graphics.DrawString("<insert bar code>",style,Brushes.Black, (e.PageBounds.Width / 2f),e.PageBounds.Height / 1.1f,center);
    }
    
#endregion
    }
}