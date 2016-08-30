using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Canvas.DrawTools
{
    public class DrawUtils
	{
		static Pen m_selectedPen = null;
		static public Pen SelectedPen
		{
			get
			{
				if (m_selectedPen == null)
				{
					m_selectedPen = new Pen(Color.Magenta, 1);
					m_selectedPen.DashStyle = DashStyle.Dash;
				}
				return m_selectedPen;
			}
		}
		static public void DrawNode(ICanvas canvas, UnitPoint nodepoint)
		{
            try
            {
                RectangleF r = new RectangleF(canvas.ToScreen(nodepoint), new SizeF(0, 0));
                r.Inflate(3, 3);
                if (r.Right < 0 || r.Left > canvas.ClientRectangle.Width)
                    return;
                if (r.Top < 0 || r.Bottom > canvas.ClientRectangle.Height)
                    return;
                canvas.Graphics.FillRectangle(Brushes.White, r);
                r.Inflate(1, 1);
                canvas.Graphics.DrawRectangle(Pens.Black, ScreenUtils.ConvertRect(r));
            }
            catch (Exception e)
            {
            }
		}
        static public RectangleF ConvertLeftTopRect2LeftBottomRect(RectangleF rect)
        {
            return new RectangleF(rect.X, rect.Y - rect.Height, rect.Width, rect.Height);
        }
		static public void DrawTriangleNode(ICanvas canvas, UnitPoint nodepoint)
		{
			PointF screenpoint = canvas.ToScreen(nodepoint);
			float size = 4;
			PointF[] p = new PointF[] 
			{
				new PointF(screenpoint.X - size, screenpoint.Y),
				new PointF(screenpoint.X, screenpoint.Y + size),
				new PointF(screenpoint.X + size, screenpoint.Y),
				new PointF(screenpoint.X, screenpoint.Y - size),
			};
			canvas.Graphics.FillPolygon(Brushes.White, p);
		}
	}


	interface IObjectEditInstance
	{
		IDrawObject GetDrawObject();
	}
   public abstract class DrawObjectBase
	{
		float			m_width;
		Color			m_color;
		DrawingLayer	m_layer;
        string m_text;
        Font m_font;
        StringFormat m_strFormat;
        Brush m_strBrush,m_fillShapeBrush;
        enum eFlags
		{
			selected		= 0x00000001,
			highlighted		= 0x00000002,
			useLayerWidth	= 0x00000004,
			useLayerColor	= 0x00000008,
		}
		int m_flag = (int)(eFlags.useLayerWidth | eFlags.useLayerColor);
		bool GetFlag(eFlags flag)
		{
			return ((int)m_flag & (int)flag) > 0;
		}
		void SetFlag(eFlags flag, bool enable)
		{
			if (enable)
				m_flag |= (int)flag;
			else
				m_flag &= ~(int)flag;
		}

		[XmlSerializable]
		public bool UseLayerWidth
		{
			get { return GetFlag(eFlags.useLayerWidth); }
			set { SetFlag(eFlags.useLayerWidth, value); }
		}
		[XmlSerializable]
		public bool UseLayerColor
		{
			get { return GetFlag(eFlags.useLayerColor); }
			set { SetFlag(eFlags.useLayerColor, value); }
		}
		[XmlSerializable]
		public float Width
		{
			set { m_width = value; }
			get 
			{ 
				if (Layer != null && UseLayerWidth)
					return Layer.Width;
				return m_width; 
			}
		}
		[XmlSerializable]
		public Color Color
		{
			set { m_color = value; }
			get 
			{ 
				if (Layer != null && UseLayerColor)
					return Layer.Color;
				return m_color; 
			}
		}
        [XmlSerializable]
        public string Text
        {
            set { m_text = value; }
            get{return m_text;}
        }
        [XmlSerializable]
        public Font Font
        {
            set { m_font = value; }
            get { return m_font; }
        }
        [XmlSerializable]
        public StringFormat StrFormat
        {
            get{ return m_strFormat;}
            set{ m_strFormat = value;}
        }
        [XmlSerializable]
        public Brush StrBrush
        {
            get { return m_strBrush; }
            set { m_strBrush = value; }
        }
        [XmlSerializable]
        public Brush FillShapeBrush
        {
            get { return m_fillShapeBrush; }
            set { m_fillShapeBrush = value; }
        }
        public DrawingLayer Layer
		{
			get { return m_layer; }
			set { m_layer = value; }
		}

		abstract public void InitializeFromModel(UnitPoint point, DrawingLayer layer, ISnapPoint snap);
		public virtual bool Selected
		{
			get { return GetFlag(eFlags.selected); }
			set { SetFlag(eFlags.selected, value); }
		}
		public virtual bool Highlighted
		{
			get { return GetFlag(eFlags.highlighted); }
			set { SetFlag(eFlags.highlighted, value); }
		}

        public virtual void Copy(DrawObjectBase acopy)
		{
			UseLayerColor = acopy.UseLayerColor;
			UseLayerWidth = acopy.UseLayerWidth;
			Width = acopy.Width;
			Color = acopy.Color;
            Text = acopy.Text;
		}

	}
}
