using System.Collections.Generic;
using System.Drawing;

namespace Canvas.DrawTools
{
    class RectangleShape: RectBase
    {
        Symbol m_plusSymbol = null, m_minusSymbol = null;
        public RectangleShape()
            : base()
        {
            m_plusSymbol = new Symbol(this, "+",0.9f,0.4f);
            m_minusSymbol = new Symbol(this, "-",0.9f,0.6f);

           // m_plusSymbol.Click += MinusSymbol_Click;
        }

        public static new string ObjectType
        {
            get { return "rectangle"; }
        }
        public override IDrawObject Clone()
        {
            RectangleShape a = new RectangleShape();
            a.Copy(this);
            return a;
        }
        public  override  void Draw(ICanvas canvas, RectangleF unitrect) 
        {
            float halfWidth, halfHeight;
            GetHalfWidthAndHeight(out halfWidth, out halfHeight);
            UnitPoint ptLeftTop = new UnitPoint(Center.X - halfWidth, Center.Y + halfHeight);
            double rwidth = halfWidth * 2;
            double rheight = halfHeight * 2;
            Pen pen = canvas.CreatePen(Color, Width);
            canvas.FillRectangle(canvas, FillShapeBrush, ptLeftTop, (float)rwidth, (float)rheight);
            canvas.DrawRectangle(canvas, pen, ptLeftTop, (float)rwidth, (float)rheight);
            if (Selected)
            {
                canvas.DrawRectangle(canvas, DrawUtils.SelectedPen, ptLeftTop, (float)rwidth, (float)rheight);
                DrawUtils.DrawNode(canvas, m_center);
                DrawNodes(canvas);
            }
            if (Text != string.Empty)
                canvas.DrawString(canvas, Text, Font, StrBrush, GetBoundingRect(canvas), StrFormat);
            m_plusSymbol.Draw(canvas);
            m_minusSymbol.Draw(canvas);
        }

        public override bool PointInObject(ICanvas canvas, UnitPoint point)
        {
            RectangleF boundingrect = GetBoundingRect(canvas);
            return boundingrect.Contains(point.Point);
        }

        public override List<Symbol> GetAllSymbol()
        {
            List<Symbol> allSymbols = new List<Symbol>();
            allSymbols.Add(m_plusSymbol);
            allSymbols.Add(m_minusSymbol);
            return allSymbols;
        }

    }
}
