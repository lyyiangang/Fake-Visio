using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using Canvas.AStartPathFindAlgorithms;

namespace Canvas.DrawTools
{
    class MultiConnectionLines : DrawObjectBase, IDrawObject, ISerialize
    {
        protected AStartPathFinderWrapper m_pathFinder = null;
        protected List<UnitPoint> m_allPts;
		protected static int ThresholdPixel = 1;
        UnitPoint m_startPt, m_endPt;
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

        [XmlSerializable]
        public UnitPoint StartPt
        {
            get
            {
                return m_startPt;
            }

            set
            {
                m_startPt = value;
            }
        }

        [XmlSerializable]
        public UnitPoint EndPt
        {
            get
            {
                return m_endPt;
            }

            set
            {
                m_endPt = value;
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
            m_startPt = acopy.m_startPt;
            m_endPt = acopy.m_endPt;
            m_allPts = new List<UnitPoint>();
            foreach (var pt in acopy.m_allPts)
                m_allPts.Add(pt);
            Selected = acopy.Selected;
        }

        public void Draw(ICanvas canvas, RectangleF unitrect)
        {
            if (m_allPts==null || m_allPts.Count < 2)
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
                if (!m_startPt.IsEmpty)
                    DrawUtils.DrawNode(canvas, m_startPt);
                if (!m_endPt.IsEmpty)
                    DrawUtils.DrawNode(canvas, m_endPt);
            }
        }

        public RectangleF GetBoundingRect(ICanvas canvas)
        {
            if (m_pathFinder == null)
                return RectangleF.Empty;
            float thWidth =Line.ThresholdWidth(canvas, Width,ThresholdPixel);
            RectangleF rect = m_pathFinder.BoundingBox;
            rect.Inflate(thWidth,thWidth);
            return m_pathFinder.BoundingBox;
        }

        public string GetInfoAsString()
        {
			return string.Format("MultiConnectionLines@{0:f4},{1:f4}",m_startPt,m_endPt);
        }

        public void GetObjectData(XmlWriter wr)
        {
        }

        public override void InitializeFromModel(UnitPoint point, DrawingLayer layer, ISnapPoint snap)
        {
            m_startPt = point;
            Width = layer.Width;
            Color = layer.Color;
            Selected = true;
        }

        public void Move(UnitPoint offset)
        {
        }

        public INodePoint NodePoint(ICanvas canvas, UnitPoint point)
        {
            //float thWidth = Line.ThresholdWidth(canvas, Width, ThresholdPixel);
            //if (HitUtil.CircleHitPoint(m_startPt, thWidth, point))
            //    return new NodePointLine(this, NodePointLine.ePoint.P1);
            //if (HitUtil.CircleHitPoint(m_endPt, thWidth, point))
            //    return new NodePointLine(this, NodePointLine.ePoint.P2);
            return null;
        }

        public bool ObjectInRectangle(ICanvas canvas, RectangleF rect, bool anyPoint)
        {
            if (m_allPts.Count < 1)
                return false;
            RectangleF boundingrect = GetBoundingRect(canvas);
            if (anyPoint)
                return HitUtil.LineIntersectWithRect(m_startPt, m_endPt, rect);
            System.Diagnostics.Debug.Assert(m_allPts.Count > 1);
            for (int ii = 0; ii < m_allPts.Count-1; ++ii)
            {
               bool intersect= HitUtil.LineIntersectWithRect(m_allPts[ii], m_allPts[ii + 1], rect);
                if (intersect)
                    return true;
            }
            return false;
        }

        public void OnKeyDown(ICanvas canvas, KeyEventArgs e)
        {
        }

        public eDrawObjectMouseDown OnMouseDown(ICanvas canvas, UnitPoint point, ISnapPoint snappoint)
        {
            m_endPt = point;
            Selected = false;
            if (m_allPts == null || m_allPts.Count < 2)
                return eDrawObjectMouseDown.Cancel;
            return eDrawObjectMouseDown.Done;
        }

        public void OnMouseMove(ICanvas canvas, UnitPoint point)
        {
            m_endPt = point;
            UpdatePath(canvas);
        }
        void UpdatePath(ICanvas canvas)
        {
            if (m_pathFinder == null)
            {
                m_pathFinder = new AStartPathFinderWrapper(canvas);
            }
            m_pathFinder.StopFind();
            m_allPts= m_pathFinder.FindPath(m_startPt,m_endPt);
        }
        public void OnMouseUp(ICanvas canvas, UnitPoint point, ISnapPoint snappoint)
        {
        }

        public bool PointInObject(ICanvas canvas, UnitPoint point)
        {
            if (m_allPts.Count < 1)
                return false;
            for(int ii=0;ii<m_allPts.Count-1;++ii)
            {
                if(HitUtil.IsPointInLine(m_allPts[ii], m_allPts[ii + 1],point, ThresholdPixel))
                    return true;
            }
            return false;
        }

        public ISnapPoint SnapPoint(ICanvas canvas, UnitPoint point, List<IDrawObject> otherobj, Type[] runningsnaptypes, Type usersnaptype)
        {
            return null;
        }
    }
}
