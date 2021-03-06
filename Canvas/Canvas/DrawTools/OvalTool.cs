﻿using System.Drawing;

namespace Canvas.DrawTools
{
    class OvalShape : RectBase
    {
        public OvalShape()
            : base()
        {
        }
        public static new string ObjectType
        {
            get { return "oval"; }
        }
        public override IDrawObject Clone()
        {
            OvalShape a = new OvalShape();
            a.Copy(this);
            return a;
        }
        public override void Draw(ICanvas canvas, RectangleF unitrect)
        {
            float halfWidth, halfHeight;
            GetHalfWidthAndHeight(out halfWidth, out halfHeight);
            UnitPoint ptLeftTop = new UnitPoint(Center.X - halfWidth, Center.Y + halfHeight);
            double rwidth = halfWidth * 2;
            double rheight = halfHeight * 2;
            Pen pen = canvas.CreatePen(Color, Width);
            canvas.FillEllipse(canvas, FillShapeBrush, ptLeftTop, (float)rwidth, (float)rheight);
            canvas.DrawEllipse(canvas, pen, ptLeftTop, (float)rwidth, (float)rheight);
            if (Selected)
            {
                canvas.DrawRectangle(canvas, DrawUtils.SelectedPen, ptLeftTop, (float)rwidth, (float)rheight);
                DrawUtils.DrawNode(canvas, m_center);
                DrawNodes(canvas);
            }
            if (Text != string.Empty)
                canvas.DrawString(canvas, Text, Font, StrBrush, GetBoundingRect(canvas), StrFormat);
        }

        public override bool PointInObject(ICanvas canvas, UnitPoint point)
        {
            //RectangleF boundingrect = GetBoundingRect(canvas);
            //if (boundingrect.Contains(point.Point) == false)
            //    return false;
           // float thWidth = Line.ThresholdWidth(canvas, Width, ThresholdPixel);
            //if (HitUtil.PointInPoint(m_center, point, thWidth))
            //    return true;
            float halfWidth, halfHeight;
            GetHalfWidthAndHeight(out halfWidth, out halfHeight);
            return HitUtil.IsPointInOval(m_center, halfWidth, halfHeight, point);
        }

    }
}
