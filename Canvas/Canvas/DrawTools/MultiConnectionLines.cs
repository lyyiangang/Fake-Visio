﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace Canvas.DrawTools
{
    class MultiConnectionLines : DrawObjectBase, IDrawObject, ISerialize
    {
        protected List<UnitPoint> m_allPts;
		protected static int ThresholdPixel = 6;
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
            throw new NotImplementedException();
        }

        public RectangleF GetBoundingRect(ICanvas canvas)
        {
            float thWidth =Line.ThresholdWidth(canvas, Width,ThresholdPixel);
            return ScreenUtils.GetRect(m_allPts.First(), m_allPts.Last(), thWidth);
        }

        public string GetInfoAsString()
        {
			return string.Format("MultiConnectionLines@{0:f4},{1:f4}",m_allPts.First(),m_allPts.Last());
        }

        public void GetObjectData(XmlWriter wr)
        {
            throw new NotImplementedException();
        }

        public override void InitializeFromModel(UnitPoint point, DrawingLayer layer, ISnapPoint snap)
        {
            m_startPt = point;
            m_allPts = new List<UnitPoint>();
            m_allPts.Add(point);
            Width = layer.Width;
            Color = layer.Color;
            OnMouseDown(null, point, snap);
            Selected = true;
        }

        public void Move(UnitPoint offset)
        {
            throw new NotImplementedException();
        }

        public INodePoint NodePoint(ICanvas canvas, UnitPoint point)
        {
            throw new NotImplementedException();
        }

        public bool ObjectInRectangle(ICanvas canvas, RectangleF rect, bool anyPoint)
        {
            RectangleF boundingrect = GetBoundingRect(canvas);
            if (anyPoint)
                return HitUtil.LineIntersectWithRect(m_allPts.Last(), m_allPts.First(), rect);
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
            throw new NotImplementedException();
        }

        public eDrawObjectMouseDown OnMouseDown(ICanvas canvas, UnitPoint point, ISnapPoint snappoint)
        {
            Selected = false;
            m_endPt = point;
            m_allPts.Add(point);
            return eDrawObjectMouseDown.Done;
        }

        public void OnMouseMove(ICanvas canvas, UnitPoint point)
        {
            m_endPt = point;
            UpdatePath(canvas);
        }
        void UpdatePath(ICanvas canvas)
        {

        }
        public void OnMouseUp(ICanvas canvas, UnitPoint point, ISnapPoint snappoint)
        {
        }

        public bool PointInObject(ICanvas canvas, UnitPoint point)
        {
            System.Diagnostics.Debug.Assert(m_allPts.Count > 1);
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