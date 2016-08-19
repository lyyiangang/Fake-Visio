using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Xml;
using Canvas.AStartPathFindAlgorithms;

namespace Canvas.DrawTools
{
    class NodePointMultiConnectionLine : INodePoint
    {
        public enum ePoint
        {
            P1, P2,
        }
        MultiConnectionLines m_owner;
        MultiConnectionLines m_clone;
        UnitPoint m_originalPoint, m_endPoint;
        ePoint m_pointId;

        public NodePointMultiConnectionLine(MultiConnectionLines owner, ePoint id)
        {
            m_owner = owner;
            m_clone = owner.Clone() as MultiConnectionLines;
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
        protected void SetPoint(ePoint pointid, UnitPoint point, MultiConnectionLines crv)
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

    class MultiConnectionLines : DrawObjectBase, IDrawObject, ISerialize
    {
		protected static int ThresholdPixel = 6;
        protected AStartPathFinderWrapper m_pathFinder = null;
        protected List<UnitPoint> m_allPts;
        UnitPoint m_p1, m_p2;
        public string Id
        {
            get
            {
                return "multiConnectionLines";
            }
        }
        public MultiConnectionLines()
        {
            m_allPts = new List<UnitPoint>();
        }
        public MultiConnectionLines(List<UnitPoint> allPts,float width,Color color)
        {
            m_allPts = new List<UnitPoint>();
            foreach (var pt in allPts)
                m_allPts.Add(pt);
            Width = width;
            Color = color;
            Selected = false;
        }
        public UnitPoint RepeatStartingPoint
        {
            get
            {
                return UnitPoint.Empty;
            }
        }
        bool m_forceFindPath = false;
        [XmlSerializable]
        public UnitPoint P1
        {
            get
            {
                return m_p1;
            }

            set
            {
                m_p1 = value;
                m_forceFindPath = true;
            }
        }

        [XmlSerializable]
        public UnitPoint P2
        {
            get
            {
                return m_p2;
            }

            set
            {
                m_p2 = value;
                m_forceFindPath = true;
            }
        }

        public void AfterSerializedIn()
        {
        }

        public IDrawObject Clone()
        {
            MultiConnectionLines a = new MultiConnectionLines();
            a.Copy(this);
            return a;
        }

        public void Copy(MultiConnectionLines acopy)
        {
            base.Copy(acopy);
            m_p1 = acopy.m_p1;
            m_p2 = acopy.m_p2;
            Selected = acopy.Selected;
        }

        public void Draw(ICanvas canvas, RectangleF unitrect)
        {
            //when move the nodepoint p1 or p2, we need to recaculate the whole path
            if (m_forceFindPath)
            {
                UpdatePath(canvas);
                m_forceFindPath = false;
            }
            if (m_allPts == null || m_allPts.Count < 2)
                return;
            Pen pen = canvas.CreatePen(Color, Width);
            pen.EndCap = LineCap.Round;
            pen.StartCap = LineCap.Round;
            for(int ii=0;ii< m_allPts.Count-1;++ii)
            {
                canvas.DrawLine(canvas, pen, m_allPts[ii], m_allPts[ii + 1]);
            }
            if(Selected)
            {
                for (int ii = 0; ii < m_allPts.Count - 1; ++ii)
                {
                    canvas.DrawLine(canvas, DrawUtils.SelectedPen, m_allPts[ii], m_allPts[ii + 1]);
                }
                if (!m_p1.IsEmpty)
                    DrawUtils.DrawNode(canvas, m_p1);
                if (!m_p2.IsEmpty)
                    DrawUtils.DrawNode(canvas, m_p2);
            }
        }

        public RectangleF GetBoundingRect(ICanvas canvas)
        {
            if (m_pathFinder == null)
                return RectangleF.Empty;
            float thWidth =Line.ThresholdWidth(canvas, Width, ThresholdPixel);
            RectangleF rect = m_pathFinder.BoundingBox;
            rect.Inflate(thWidth,thWidth);
            return rect;
        }

        public string GetInfoAsString()
        {
			return string.Format("MultiConnectionLines@{0:f4},{1:f4}",m_p1,m_p2);
        }

        public void GetObjectData(XmlWriter wr)
        {
        }

        public override void InitializeFromModel(UnitPoint point, DrawingLayer layer, ISnapPoint snap)
        {
            m_p1 = point;
            m_p2 = point;
            Width = layer.Width;
            Color = layer.Color;
            Selected = true;
        }

        public void Move(UnitPoint offset)
        {
        }

        public INodePoint NodePoint(ICanvas canvas, UnitPoint point)
        {
            float thWidth = 0.0f;
            if (canvas != null)
                thWidth = Line.ThresholdWidth(canvas, Width, ThresholdPixel);
            if (HitUtil.CircleHitPoint(m_p1, thWidth, point))
                return new NodePointMultiConnectionLine(this, NodePointMultiConnectionLine.ePoint.P1);
            if (HitUtil.CircleHitPoint(m_p2, thWidth, point))
                return new NodePointMultiConnectionLine(this, NodePointMultiConnectionLine.ePoint.P2);
            return null;
        }

        public bool ObjectInRectangle(ICanvas canvas, RectangleF rect, bool anyPoint)
        {
            if (m_allPts==null || m_allPts.Count < 1)
                return false;
            RectangleF boundingrect = GetBoundingRect(canvas);
            if (anyPoint)
            {
                for(int ii=0;ii<m_allPts.Count-1;++ii)
                {
                    if (HitUtil.LineIntersectWithRect(m_allPts[ii], m_allPts[ii + 1], rect))
                        return true;
                }
                return false;
            }
            //make sure all points are in the rectangle
            foreach(var curPt in m_allPts)
            {
                if (!rect.Contains(curPt.Point))
                    return false;
            }
            return true;
        }

        public void OnKeyDown(ICanvas canvas, KeyEventArgs e)
        {
        }

        public eDrawObjectMouseDown OnMouseDown(ICanvas canvas, UnitPoint point, ISnapPoint snappoint)
        {
            OnMouseMove(canvas,point);
            Selected = false;
            if (m_allPts == null || m_allPts.Count < 2)
                return eDrawObjectMouseDown.Cancel;
            return eDrawObjectMouseDown.Done;
        }

        public void OnMouseMove(ICanvas canvas, UnitPoint point)
        {
            m_p2 = point;
            UpdatePath(canvas);
        }
        void UpdatePath(ICanvas canvas)
        {
            if (m_pathFinder == null)
            {
                m_pathFinder = new AStartPathFinderWrapper(canvas);
            }
            m_pathFinder.StopFind();
            m_allPts= m_pathFinder.FindPath(m_p1,m_p2);
        }
        public void OnMouseUp(ICanvas canvas, UnitPoint point, ISnapPoint snappoint)
        {
        }

        public bool PointInObject(ICanvas canvas, UnitPoint point)
        {
            if (m_allPts.Count < 1)
                return false;
            float thWidth =Line.ThresholdWidth(canvas, Width, ThresholdPixel);
            for(int ii=0;ii<m_allPts.Count-1;++ii)
            {
                if(HitUtil.IsPointInLine(m_allPts[ii], m_allPts[ii + 1],point, thWidth))
                    return true;
            }
            return false;
        }

        public ISnapPoint SnapPoint(ICanvas canvas, UnitPoint point, List<IDrawObject> otherobj, Type[] runningsnaptypes, Type usersnaptype)
        {
            return null;
        }

        public RectangleF GetExactBoundingRect(ICanvas canvas)
        {
            throw new NotImplementedException();
        }
    }
}
