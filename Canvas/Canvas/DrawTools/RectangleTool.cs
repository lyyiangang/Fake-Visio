using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Canvas.DrawTools
{
    class RectangleShape: RectBase
    {
        public RectangleShape()
            : base()
        {
            Text = "i am a rectangle please show me";
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
            UnitPoint ptemp = new UnitPoint(Center.X - halfWidth, Center.Y + halfHeight);
            double rwidth = halfWidth * 2;
            double rheight = halfHeight * 2;
            Pen pen = canvas.CreatePen(Color, Width);
            canvas.FillRectangle(canvas, FillShapeBrush, ptemp, (float)rwidth, (float)rheight);
            canvas.DrawRectangle(canvas, pen, ptemp, (float)rwidth, (float)rheight);
            if (Selected)
            {
                canvas.DrawRectangle(canvas, DrawUtils.SelectedPen, ptemp, (float)rwidth, (float)rheight);
                DrawUtils.DrawNode(canvas, m_center);
                DrawNodes(canvas);
            }
            if (Text != string.Empty)
                canvas.DrawString(canvas, Text, Font, StrBrush, GetBoundingRect(canvas), StrFormat);
        }

        public override bool PointInObject(ICanvas canvas, UnitPoint point)
        {
            RectangleF boundingrect = GetBoundingRect(canvas);
            return boundingrect.Contains(point.Point);
            //if (boundingrect.Contains(point.Point) == false)
            //    return false;
           //float thWidth = Line.ThresholdWidth(canvas, Width, ThresholdPixel);
            //if (HitUtil.PointInPoint(m_center, point, thWidth))
            //    return true;
            //float halfWidth, halfHeight;
            //GetHalfWidthAndHeight(out halfWidth, out halfHeight);
            //return HitUtil.IsPointInRect(m_center, halfWidth, halfHeight, point, 3 * thWidth);
        }

    }
}
