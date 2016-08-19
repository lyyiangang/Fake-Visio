using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;

namespace Canvas.DrawTools
{
    class NodePointCubicBezier : INodePoint
    {
        public enum ePoint
        {
            P1,P2,
        }
        CubicBezier m_owner;
        CubicBezier m_clone;
        UnitPoint m_originalPoint, m_endPoint;
        ePoint m_pointId;

        public NodePointCubicBezier(CubicBezier owner,ePoint id)
        {
            m_owner = owner;
            m_clone = owner.Clone() as CubicBezier;
            m_pointId = id;
            m_originalPoint = GetPoint(m_pointId);
        }

        public IDrawObject GetOriginal()
        {
            return m_owner;
        }

        public void SetPosition(UnitPoint pos)
        {
            SetPoint(m_pointId, pos, m_clone);
        }
        public void Cancel()
        {
        }

        public void Finish()
        {
            m_endPoint = GetPoint(m_pointId);
            m_owner.P1 = m_clone.P1;
            m_owner.P2 = m_clone.P2;
            m_clone = null;
        }

        public IDrawObject GetClone()
        {
			return m_clone;
        }

        public void OnKeyDown(ICanvas canvas, KeyEventArgs e)
        {
        }

        public void Redo()
        {
            SetPoint(m_pointId, m_endPoint, m_owner);
        }

        public void Undo()
        {
            SetPoint(m_pointId, m_endPoint, m_owner);
        }
        protected UnitPoint GetPoint(ePoint pointid)
        {
            if (pointid == ePoint.P1)
                return m_clone.P1;
            if (pointid == ePoint.P2)
                return m_clone.P2;
            return m_owner.P1;
        }
        protected UnitPoint OtherPoint(ePoint currentpointid)
        {
            if (currentpointid == ePoint.P1)
                return GetPoint(ePoint.P2);
            return GetPoint(ePoint.P1);
        }
        protected void SetPoint(ePoint pointid, UnitPoint point, CubicBezier crv)
        {
            if (pointid == ePoint.P1)
                crv.P1 = point;
            if (pointid == ePoint.P2)
                crv.P2 = point;
        }

        public UnitPoint GetPosition()
        {
            if (m_pointId == ePoint.P1)
                return m_owner.P1;
            else if (m_pointId == ePoint.P2)
                return m_owner.P2;
            return UnitPoint.Empty;
        }
    }
    class CubicBezier : DrawObjectBase, IDrawObject, ISerialize, IConnectionCurve
    {
        System.Drawing.Drawing2D.AdjustableArrowCap m_arrowCap = null;
        protected static int ThresholdPixel = 6;
        protected UnitPoint m_p1, m_p2, m_p1Ctrl, m_p2Ctrl, m_center;
        CubicBezierCurveCurve m_crv = null;

        [XmlSerializable]
        public UnitPoint P1
        {
            get { return m_p1; }
            set {
                m_p1 = value;
                UpdateCtrlPts();
            }
        }
        [XmlSerializable]
        public UnitPoint P2
        {
            get { return m_p2; }
            set { m_p2 = value;
                UpdateCtrlPts();
            }
        }

        public static string ObjectType
        {
            get { return "cubicBezier"; }
        }
        public CubicBezier()
        {
        }
        public CubicBezier(UnitPoint point, UnitPoint endpoint, float width, Color color)
        {
            m_p1 = point;
            m_p2 = endpoint;
            m_useStartArrow = false;
            m_useEndArrow = false;
            UpdateCtrlPts();
            Width = width;
            Color = color;
            Selected = false;
        }
        public override void InitializeFromModel(UnitPoint point, DrawingLayer layer, ISnapPoint snap)
        {
            m_p1 = m_p2= point;
            UpdateCtrlPts();
            Width = layer.Width;
            Color = layer.Color;
            Selected = true;
        }
        public virtual void Copy(CubicBezier acopy)
        {
            base.Copy(acopy);
            m_p1 = acopy.m_p1;
            m_p2 = acopy.m_p2;
            m_useEndArrow = acopy.m_useEndArrow;
            m_useStartArrow = acopy.m_useStartArrow;
            m_crv = acopy.m_crv;
            UpdateCtrlPts();
            Selected = acopy.Selected;
        }
        public string Id
        {
            get { return ObjectType; }
        }
        public virtual IDrawObject Clone()
        {
            CubicBezier obj = new CubicBezier();
            obj.Copy(this);
            return obj;
        }
        public UnitPoint RepeatStartingPoint
        {
            get { return UnitPoint.Empty; }
        }
        bool m_useStartArrow = false, m_useEndArrow = false;
        public bool UseStartArrow
        {
            get
            {
                return m_useStartArrow;
            }

            set
            {
                m_useStartArrow = value;
            }
        }

        public bool UseEndArrow
        {
            get
            {
                return m_useEndArrow;
            }

            set
            {
                m_useEndArrow = value;
            }
        }

        public void AfterSerializedIn()
        {
        }

        public void Draw(ICanvas canvas, RectangleF unitrect)
        {
            Color color = Color;
            Pen pen = canvas.CreatePen(color, Width);
            pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
            canvas.DrawBezier(canvas,pen, m_p1, m_p1Ctrl, m_p2Ctrl,m_p2);
            if (m_useStartArrow)
                pen.CustomStartCap = m_arrowCap;
            else
                pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;

            if (m_useEndArrow)
                pen.CustomEndCap = m_arrowCap;
            else
                pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

            if (Highlighted)
                 canvas.DrawBezier(canvas, DrawUtils.SelectedPen, m_p1, m_p1Ctrl, m_p2Ctrl,m_p2);
            if (Selected)
            {
                 canvas.DrawBezier(canvas, DrawUtils.SelectedPen, m_p1, m_p1Ctrl, m_p2Ctrl,m_p2);
                if (!m_p1.IsEmpty )
                    DrawUtils.DrawNode(canvas, m_p1);
                if (!m_p2.IsEmpty )
                    DrawUtils.DrawNode(canvas, m_p2);
           }
        }

        public RectangleF GetBoundingRect(ICanvas canvas)
        {
            float thWidth = Line.ThresholdWidth(canvas, Width, ThresholdPixel);
            RectangleF rect = m_crv.GetBoundingBox();
            rect.Inflate(thWidth, thWidth);
            return rect;
        }

        public string GetInfoAsString()
        {
            return string.Format("CubicBezier@{0},{1}",
                P1.PosAsString(),
                P2.PosAsString());
        }

        public void GetObjectData(XmlWriter wr)
        {
            wr.WriteStartElement("CubicBezier");
            XmlUtil.WriteProperties(this, wr);
            wr.WriteEndElement();
        }

        public void Move(UnitPoint offset)
        {
            m_p1.X += offset.X;
            m_p1.Y += offset.Y;
            m_p2.X += offset.X;
            m_p2.Y += offset.Y;
            UpdateCtrlPts();
        }

        public INodePoint NodePoint(ICanvas canvas, UnitPoint point)
        {
            float thWidth = 0.0f;
            if(canvas !=null)
                thWidth = Line.ThresholdWidth(canvas, Width, ThresholdPixel);
            if (HitUtil.CircleHitPoint(m_p1, thWidth, point))
                return new NodePointCubicBezier(this, NodePointCubicBezier.ePoint.P1);
            if (HitUtil.CircleHitPoint(m_p2, thWidth, point))
                return new NodePointCubicBezier(this, NodePointCubicBezier.ePoint.P2);

            return null;
        }

        public bool ObjectInRectangle(ICanvas canvas, RectangleF rect, bool anyPoint)
        {
            if (anyPoint)
            {
                const int nSamplePts = 8;
                List<PointF> samplePts= m_crv.SamplePoints(nSamplePts);
                Debug.Assert(samplePts.Count > 1);
                UnitPoint prePt = new UnitPoint();
                UnitPoint curPt = new UnitPoint();
                for(int ii=1;ii<samplePts.Count;++ii)
                {
                    prePt.SetPoint(samplePts[ii - 1]);
                    curPt.SetPoint(samplePts[ii]);
                    if (HitUtil.LineIntersectWithRect(prePt, curPt, rect))
                        return true;
                }
                return false;
            }
            RectangleF boundingrect = GetBoundingRect(canvas);
            return rect.Contains(boundingrect);
        }

        public void OnKeyDown(ICanvas canvas, KeyEventArgs e)
        {
        }

        public eDrawObjectMouseDown OnMouseDown(ICanvas canvas, UnitPoint point, ISnapPoint snappoint)
        {
            Selected = false;
			OnMouseMove(canvas, point);
            if (snappoint is PerpendicularSnapPoint && snappoint.Owner is Line)
            {
                Line src = snappoint.Owner as Line;
                m_p2 = HitUtil.NearestPointOnLine(src.P1, src.P2, m_p1, true);
                return eDrawObjectMouseDown.DoneRepeat;
            }
            if (snappoint is PerpendicularSnapPoint && snappoint.Owner is Arc)
            {
                Arc src = snappoint.Owner as Arc;
                m_p2 = HitUtil.NearestPointOnCircle(src.Center, src.Radius, m_p1, 0);
                return eDrawObjectMouseDown.DoneRepeat;
            }
            if (Control.ModifierKeys == Keys.Control)
                point = HitUtil.OrthoPointD(m_p1, point, 45);
            m_p2 = point;
            UpdateCtrlPts();
            return eDrawObjectMouseDown.Done;
        }

        public void OnMouseMove(ICanvas canvas, UnitPoint point)
        {
            if (Control.ModifierKeys == Keys.Control)
                point = HitUtil.OrthoPointD(m_p1, point, 45);
            m_p2 = point;
            UpdateCtrlPts();
        }

        public void OnMouseUp(ICanvas canvas, UnitPoint point, ISnapPoint snappoint)
        {
        }

        public bool PointInObject(ICanvas canvas, UnitPoint point)
        {
            float thWidth = Line.ThresholdWidth(canvas, Width, ThresholdPixel);
            return m_crv.PointOnCurve(new float[2] { (float)point.X, (float)point.Y }, thWidth);
        }

        public ISnapPoint SnapPoint(ICanvas canvas, UnitPoint testPt, List<IDrawObject> otherobj, Type[] runningsnaptypes, Type usersnaptype)
        {
            float thWidth = Line.ThresholdWidth(canvas, Width, ThresholdPixel);
            if(runningsnaptypes!=null)
            {
                foreach (Type snaptype in runningsnaptypes)
                {
                    if (snaptype == typeof(VertextSnapPoint))
                    {
                        if (HitUtil.CircleHitPoint(m_p1, thWidth, testPt))
                            return new VertextSnapPoint(canvas, this, m_p1);
                        if (HitUtil.CircleHitPoint(m_p2, thWidth, testPt))
                            return new VertextSnapPoint(canvas, this, m_p2);
                    }
                    if (snaptype == typeof(MidpointSnapPoint))
                    {
                        if (HitUtil.CircleHitPoint(m_center, thWidth, testPt))
                            return new MidpointSnapPoint(canvas, this, m_center);
                    }
                }
                return null;
            }
            if (usersnaptype == typeof(MidpointSnapPoint))
            {
                float[] midPt = { 0.0f, 0.0f };
                m_crv.Eval(0.5f, ref midPt[0], ref midPt[1]);
                return new MidpointSnapPoint(canvas, this, new UnitPoint(midPt[0], midPt[1]));
            }
            if (usersnaptype == typeof(VertextSnapPoint))
            {
                double d1 = HitUtil.Distance(testPt, m_p1);
                double d2 = HitUtil.Distance(testPt, m_p2);
                if (d1 <= d2)
                    return new VertextSnapPoint(canvas, this, m_p1);
                return new VertextSnapPoint(canvas, this, m_p2);
            }
            if (usersnaptype == typeof(CenterSnapPoint))
            {
                return new CenterSnapPoint(canvas, this, m_center);
            }
            return null;
        }

        UnitPoint MidPoint(ICanvas canvas, UnitPoint p1, UnitPoint p2, UnitPoint hitpoint)
        {
            UnitPoint mid = HitUtil.LineMidpoint(p1, p2);
            float thWidth = Line.ThresholdWidth(canvas, Width,ThresholdPixel);
            if (HitUtil.CircleHitPoint(mid, thWidth, hitpoint))
                return mid;
            return UnitPoint.Empty;
        }
        void UpdateCtrlPts()
        {
            double xDelta = m_p2.X - m_p1.X;
            double scale = 0.6;
            m_p1Ctrl.X = m_p1.X + xDelta * scale;
            m_p1Ctrl.Y = m_p1.Y;
            m_p2Ctrl.X = m_p2.X - xDelta * scale;
            m_p2Ctrl.Y = m_p2.Y;

            if (m_crv == null)
                m_crv = new CubicBezierCurveCurve(new float[2] { (float)m_p1.X, (float)m_p1.Y },
                    new float[2] { (float)m_p1Ctrl.X, (float)m_p1Ctrl.Y },
                    new float[2] { (float)m_p2Ctrl.X, (float)m_p2Ctrl.Y },
                    new float[2] { (float)m_p2.X, (float)m_p2.Y });
            else
            {
                m_crv.SetPos(0, (float)m_p1.X, (float)m_p1.Y);
                m_crv.SetPos(1, (float)m_p1Ctrl.X, (float)m_p1Ctrl.Y);
                m_crv.SetPos(2, (float)m_p2Ctrl.X, (float)m_p2Ctrl.Y);
                m_crv.SetPos(3, (float)m_p2.X, (float)m_p2.Y);
            }
            float[] midPt = { 0.0f, 0.0f };
            m_crv.Eval(0.5f, ref midPt[0], ref midPt[1]);
            m_center.X = midPt[0];
            m_center.Y = midPt[1];
        }

        public RectangleF GetExactBoundingRect(ICanvas canvas)
        {
            throw new NotImplementedException();
        }

        public INodePoint GetNodePointFromPos(UnitPoint pt)
        {
            throw new NotImplementedException();
        }
    }
}
