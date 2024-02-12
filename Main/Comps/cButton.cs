using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace FruitWave.Comps
{
    public partial class cButton : Button
    {
        //- Vars
        public string Product { get; set; }
        public int ProductId { get; set; }
        public Decimal ProductPpkg { get; set; }
        
    #region BuildTime

        private int _borderSize = 0;
        private int _borderRadius = 20;
        private Color _borderColor = Color.DimGray;
        private Color _textColor = Color.GhostWhite;
        [Category ("C")] 
        public int BorderSize
        {
            get => _borderSize;
            set 
            { 
                _borderSize = value;
                Invalidate();
            }
        }
        [Category ("C")]
        public int BorderRadius
        {
            get => _borderRadius;
            set
            {
                _borderRadius = value;
                Invalidate();
            }
        }
        [Category ("C")]
        public Color BorderColor
        {
            get => _borderColor;
            set
            {
                _borderColor = value;
                Invalidate();
            }
        }
        [Category ("C")]
        public Color BackgroundColor
        {
            get => BackColor;
            set => BackColor = value;
        }
        [Category ("C")]
        public Color TextColor
        {
            get => ForeColor;
            set => ForeColor = value;
        }
        public cButton(int inProductId, string inProductName, Decimal inPricePKG)
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Size = new Size(150, 40);
            BackColor = ColorTranslator.FromHtml("#1b1b1b");
            ForeColor = Color.White;

            ProductId = inProductId;
            string edit = inProductName;
            edit = char.ToUpper(edit[0]) + edit.Substring(1);
            Product = edit;
            ProductPpkg = inPricePKG;
            
            BuildElements();
            InitializeComponent();
        }

        public cButton(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
        }

        public void BuildElements()
        {
            var labelId = new Label();
            var localSize = Size;
            var localLocation = Location;
            labelId.Name = "lblId";
            labelId.ForeColor = _textColor;
            labelId.Text = ProductId.ToString();
            labelId.BackColor = Color.Transparent;
            labelId.Font = new Font("Roboto", 9.5f,FontStyle.Bold);
            labelId.RightToLeft = RightToLeft.Yes;
            labelId.Location = new Point(localLocation.X-5,localLocation.Y+10);
            
            
            Controls.Add(labelId);

            Label labelName = new Label();
            labelName.Name = "lblName";
            labelName.BackColor = Color.Transparent;
            labelName.ForeColor = _textColor;
            labelName.Font = new Font("Roboto", 12f);
            labelName.TextAlign = ContentAlignment.MiddleCenter;
            labelName.Location = new Point(labelName.Location.X, localSize.Height/2+10);
            labelName.Text = Product;

            Controls.Add(labelName);

            var labelPrice = new Label();
            labelPrice.Name = "lblPrice";
            labelPrice.BackColor = Color.Transparent;
            labelPrice.ForeColor = _textColor;
            labelPrice.Font = new Font("Roboto", 10f);
            labelPrice.RightToLeft = RightToLeft.No;
            labelPrice.Location = new Point(localLocation.X+5, localLocation.Y+localSize.Height+25);
            labelPrice.Text = ProductPpkg.ToString(CultureInfo.InvariantCulture) + @" €/kg";
            
            Controls.Add(labelPrice);
        }


        //- Func
        private GraphicsPath GetFigurePath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.StartFigure();
            path.AddArc(rect.X,rect.Y,radius,radius,180,90);
            path.AddArc(rect.Width-radius,rect.Y,radius,radius,270,90);
            path.AddArc(rect.Width-radius,rect.Height-radius,radius,radius,0,90);
            path.AddArc(rect.X,rect.Height-radius,radius,radius,90,90);
            path.CloseFigure();

            return path; 
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF rectSurface = new RectangleF(0, 0, this.Width, this.Height);
            RectangleF rectBorder = new RectangleF(1, 1, this.Width - 0.8f, this.Height - 1);
            if (_borderRadius > 2)
            {
                using (GraphicsPath pathSurface = GetFigurePath(rectSurface, _borderRadius)) 
                using (GraphicsPath pathBorder = GetFigurePath(rectBorder, _borderRadius - 1f))
                using (Pen penSurface = new Pen(Parent.BackColor, 2))
                using (Pen penBorder = new Pen(_borderColor, _borderSize))
                {
                    penBorder.Alignment = PenAlignment.Inset;
                    // Surface
                    Region = new Region(pathSurface);
                    // draw border
                    pevent.Graphics.DrawPath(penSurface,pathSurface);
                    // draw control
                    if (_borderSize >= 1)
                    {
                        pevent.Graphics.DrawPath(penBorder,pathBorder);
                    }
                }
            }
            // normal button
            else
            {
                Region = new Region(rectSurface);
                if (_borderSize >= 1)
                {
                    using (Pen penBorder = new Pen(_borderColor, _borderSize))
                    {
                        penBorder.Alignment = PenAlignment.Inset;
                        pevent.Graphics.DrawRectangle(penBorder,0,0,this.Width-1,this.Height-1);
                    }
                }
            } 
        }
 
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Parent.BackColorChanged += Container_BackColorChanged;
        }

        private void Container_BackColorChanged(object sender, EventArgs e)
        {
            if (DesignMode) Invalidate(); 
        }
    #endregion
        
    }
}