using System;
using System.Windows.Forms;
using System.Drawing;
using System.Xml;
using System.Collections.Generic;
using System.Diagnostics;
namespace Canvas.DrawTools
{
    class NodePointRectBase : INodePoint
    {
        public enum ePoint
        {
            P1, P3,/*P4,P2,*/Center,
        }
        protected RectBase m_owner;
        protected RectBase m_clone;
        protected UnitPoint m_originalPoint;
        protected UnitPoint m_endPoint;
        ePoint m_pointId;
        public NodePointRectBase(RectBase owner, ePoint id)
        {
            m_owner = owner;
            m_clone = m_owner.Clone() as RectBase;
            m_clone.CurrentPoint = m_owner.CurrentPoint;
            m_originalPoint = GetPoint(id);
            m_pointId = id;
        }
        protected UnitPoint GetPoint(ePoint pointid)
        {
            if (pointid == ePoint.P1)
                return m_clone.P1;
            else if (pointid == ePoint.P3)
                return m_clone.P3;
            else if (pointid == ePoint.Center)
                return m_clone.Center;
            else
                Debug.Assert(false);
            return m_owner.P1;
        }
        protected void SetPoint(ePoint pointid, UnitPoint point, RectBase oval)
        {
            if (pointid == ePoint.P1)
                oval.P1 = point;
            else if (pointid == ePoint.P3)
                oval.P3 = point;
            else if (pointid == ePoint.Center)
                oval.Center = point;
        }
        protected UnitPoint OtherPoint(ePoint currentpointid)
        {
            if (currentpointid == ePoint.P1)
                return GetPoint(ePoint.P3);
            return GetPoint(ePoint.P1);
        }
        #region INodePoint Members
        public IDrawObject GetClone()
        {
            return m_clone;
        }
        public IDrawObject GetOriginal()
        {
            return m_owner;
        }
        public virtual void SetPosition(UnitPoint pos)
        {
            if (m_pointId != ePoint.Center && Control.ModifierKeys == Keys.Control)
                pos = HitUtil.OrthoPointD(OtherPoint(m_pointId), pos, 45);
            SetPoint(m_pointId, pos, m_clone);
        }
        public virtual void Finish()
        {
            m_endPoint = GetPoint(m_pointId);
            m_owner.P1 = m_clone.P1;
            m_owner.P3 = m_clone.P3;
            m_clone = null;
        }
        public void Cancel()
        {
            m_owner.Selected = true;
        }
        public virtual void Undo()
        {
            SetPoint(m_pointId, m_originalPoint, m_owner);
        }
        public virtual void Redo()
        {
            SetPoint(m_pointId, m_endPoint, m_owner);
        }
        public void OnKeyDown(ICanvas canvas, KeyEventArgs e)
        {
        }

        public UnitPoint GetPosition()
        {
            if (m_pointId == ePoint.P1)
                return m_owner.P1;
            else if (m_pointId == ePoint.P3)
                return m_owner.P3;
            return m_owner.Center;
        }
        #endregion
    }

  public  class Symbol
    {
        string m_txt = string.Empty;
        RectangleF m_boundingBox = RectangleF.Empty;
        IDrawObject m_owner = null;
        Font m_font;
        StringFormat m_strFormat;
        Brush m_strBrush, m_fillBrush;
        float m_relativeOffSetToLeft, m_relativeOffSetToTop;
        public delegate void DelegateClick(Symbol sender);
        public event DelegateClick Click = null;

        public Symbol(IDrawObject owner, string txt, float relativeOffsetToLeft, float relativeOffSetToTop)
        {
            m_owner = owner;
            m_txt = txt;
            m_relativeOffSetToLeft = relativeOffsetToLeft;
            m_relativeOffSetToTop = relativeOffSetToTop;
            m_font = new Font("Calibri", 20, FontStyle.Regular, GraphicsUnit.Point);
            m_strFormat = new StringFormat();
            m_strFormat.Alignment = StringAlignment.Center;
            m_strFormat.LineAlignment = StringAlignment.Center;
            m_strBrush = Brushes.Black;
            m_fillBrush = Brushes.Red;
        }

        public string Text { get { return m_txt; } }

        public IDrawObject Owner { get { return m_owner; } }

        public void Draw(ICanvas canvas)
        {
            if (m_txt.Length < 1)
                return;
            UpdateSymbol(canvas);
            if (!m_boundingBox.IsEmpty)
            {
                float penWidth = 0.02f;
                Pen pen = canvas.CreatePen(Color.Black, penWidth);
                UnitPoint ptTopLeft = new UnitPoint(m_boundingBox.Left, m_boundingBox.Bottom);
                canvas.FillRectangle(canvas, m_fillBrush, ptTopLeft,
                    m_boundingBox.Width, m_boundingBox.Height);
                canvas.DrawString(canvas, m_txt, m_font, m_strBrush, m_boundingBox, m_strFormat);
                canvas.DrawRectangle(canvas, pen, ptTopLeft, m_boundingBox.Width, m_boundingBox.Height);
            }
        }

        public bool PointInObject(ICanvas canvas, UnitPoint point)
        {
            bool contain= m_boundingBox.Contains(point.Point);
            return contain;
        }

        void UpdateSymbol(ICanvas canvas)
        {
            RectangleF ownerRect = m_owner.GetBoundingRect(canvas);
            const float boxSizeScale = 0.05f;
            float radius = ownerRect.Height * boxSizeScale;
            m_boundingBox.X = ownerRect.Left + ownerRect.Width * m_relativeOffSetToLeft;
            m_boundingBox.Y = ownerRect.Bottom - ownerRect.Height * m_relativeOffSetToTop;
            m_boundingBox.Width = 0.0f;
            m_boundingBox.Height = 0.0f;
            m_boundingBox.Inflate(radius, radius);
        }

        public void OnClick()
        {
            if (Click != null)
                Click(this);
        }
    }

 public  class RectBase : DrawObjectBase, IDrawObject, ISerialize
    {
        //p1------------------------p2
        //|                         |
        //|                         |
        //|                         |
        //p4------------------------p3
        protected UnitPoint m_center;
        protected UnitPoint m_p1, m_p3/*,m_p2,m_p4*/;
        protected static int ThresholdPixel = 6;
        eCurrentPoint m_curPoint = eCurrentPoint.center;

        [XmlSerializable]
        public UnitPoint P1
        {
            get { return m_p1; }
            set
            {
                m_p1 = value;
                UpdateCenter();
            }
        }
        [XmlSerializable]
        public UnitPoint P3
        {
            get { return m_p3; }
            set
            {
                m_p3 = value;
                UpdateCenter();
            }
        }

        public UnitPoint Center
        {
            get { return m_center; }
            set
            {
                float halfWidth, halfHeight;
                GetHalfWidthAndHeight(out halfWidth, out halfHeight);
                m_center = value;
                m_p1.X = m_center.X - halfWidth;
                m_p1.Y = m_center.Y + halfHeight;
                m_p3.X = m_center.X + halfWidth;
                m_p3.Y = m_center.Y - halfHeight;
            }
        }

        public static string ObjectType
        {
            get { return "rectBase"; }
        }

        public enum eCurrentPoint
        {
            center,
            p1, p3, done,
        }

        public enum eVertexId
        {
            None = 0,
            LeftTopCorner = 1,
            RightTopCorner,
            LeftBottomCorner,
            RightBottomCorner,

            TopEdgeMidPoint,
            BottomEdgeMidPoint,
            LeftEdgeMidPoint,
            RigthEdgeMidPoint,
        }

        public RectBase()
        {
            CurrentPoint = eCurrentPoint.p1;
            Text = "Double click me to add text!";
            Font = new Font("Arial", 12, FontStyle.Regular, GraphicsUnit.Point);
            StrFormat = new StringFormat();
            StrFormat.Alignment = StringAlignment.Center;
            StrFormat.LineAlignment = StringAlignment.Center;
            StrBrush = Brushes.Black;
            FillShapeBrush = Brushes.LightBlue;
            m_guid = System.Guid.NewGuid().ToString();
        }
        public override void InitializeFromModel(UnitPoint point, DrawingLayer layer, ISnapPoint snap)
        {
            Color = layer.Color;
            m_center = point;
            const float defaultHalfWidth = 0.8f;
            const float defaultHalfHeight = 0.4f;
            m_p1.X = m_center.X - defaultHalfWidth;
            m_p1.Y = m_center.Y + defaultHalfHeight;
            m_p3.X = m_center.X + defaultHalfWidth;
            m_p3.Y = m_center.Y - defaultHalfHeight;
            //OnMouseDown(null, point, snap);
            Selected = true;
        }
        public UnitPoint[] TextInputPoint()
        {
            UnitPoint[] arr = new UnitPoint[1];
            arr[0] = m_center;
            return arr;
        }
        public void Copy(RectBase acopy)
        {
            base.Copy(acopy);
            P1 = acopy.P1;
            P3 = acopy.P3;
            Selected = acopy.Selected;
            CurrentPoint = acopy.CurrentPoint;
            m_guid = acopy.m_guid;

            UpdateCenter();
        }
        public string Id
        {
            get { return ObjectType; }
        }
        public virtual IDrawObject Clone()
        {
            throw new Exception();
        }

        void UpdateCenter()
        {
            m_center = HitUtil.LineMidpoint(m_p1, m_p3);
            foreach (var curItme in m_allConnectionCrvNodes)
            {
                curItme.connectionCrvNode.SetPosition(GetPointFromVertexId(curItme.rectNodeId));
                curItme.connectionCrvNode.Finish();
            }
        }

        public virtual void Draw(ICanvas canvas, RectangleF unitrect)
        {
            throw new System.Exception();
            //float halfWidth, halfHeight;
            //GetHalfWidthAndHeight(out halfWidth, out halfHeight);
            //UnitPoint ptemp = new UnitPoint(Center.X - halfWidth, Center.Y + halfHeight);
            //double rwidth = halfWidth * 2;
            //double rheight = halfHeight * 2;
            //Pen pen = canvas.CreatePen(Color, Width);
            //canvas.DrawEllipse(canvas, pen, ptemp, (float)rwidth, (float)rheight);
            //if (Selected)
            //{
            //    canvas.DrawEllipse(canvas, DrawUtils.SelectedPen, ptemp, (float)rwidth, (float)rheight);
            //    canvas.DrawRectangle(canvas, DrawUtils.SelectedPen, ptemp, (float)rwidth, (float)rheight);
            //    DrawUtils.DrawNode(canvas, m_center);
            //    DrawNodes(canvas);
            //}
        }
        public virtual void OnMouseMove(ICanvas canvas, UnitPoint point)
        {
            if (CurrentPoint == eCurrentPoint.p1)
            {
                m_p1 = point;
                m_p3 = point;
                UpdateCenter();
            }
            if (CurrentPoint == eCurrentPoint.p3)
            {
                m_p3 = point;
                UpdateCenter();
            }
            if (CurrentPoint == eCurrentPoint.center)
            {
                Center = point;
            }
            if (Control.ModifierKeys == Keys.Control)
            {
                m_p3 = HitUtil.OrthoPointD(m_p1, point, 45);
                UpdateCenter();
            }
        }

        public void OnMouseUp(ICanvas canvas, UnitPoint point, ISnapPoint snappoint)
        {
        }

        public void OnKeyDown(ICanvas canvas, KeyEventArgs e)
        {
        }

        public virtual bool PointInObject(ICanvas canvas, UnitPoint point)
        {
            throw new Exception();
        }
        public bool ObjectInRectangle(ICanvas canvas, RectangleF rect, bool anyPoint)
        {
            //float halfWidth, halfHeight;
            //GetHalfWidthAndHeight(out halfWidth, out halfHeight);
            //RectangleF shapeRect = new RectangleF((float)m_center.X, (float)m_center.Y, 0, 0);
            //shapeRect.Inflate(halfWidth, halfHeight);
            RectangleF boundingrect = GetBoundingRect(canvas);
            if (anyPoint)
            {
                return rect.IntersectsWith(boundingrect);
            }
            return rect.Contains(boundingrect);
        }
        protected void GetHalfWidthAndHeight(out float halfWidth, out float halfHeight)
        {
            halfWidth = (float)Math.Abs(m_p3.X - m_p1.X) * 0.5f;
            halfHeight = (float)Math.Abs(m_p3.Y - m_p1.Y) * 0.5f;
        }
        public UnitPoint RepeatStartingPoint
        {
            get { return UnitPoint.Empty; }
        }
        public RectangleF GetBoundingRect(ICanvas canvas)
        {
            float thWidth = Line.ThresholdWidth(canvas, Width, ThresholdPixel);
            return ScreenUtils.GetRect(m_p1, m_p3, thWidth);
        }
        public eCurrentPoint CurrentPoint
        {
            get { return m_curPoint; }
            set
            {
                m_curPoint = value;
            }
        }
        public eDrawObjectMouseDown OnMouseDown(ICanvas canvas, UnitPoint point, ISnapPoint snappoint)
        {
            OnMouseMove(canvas, point);
            if (CurrentPoint == eCurrentPoint.p1)
            {
                CurrentPoint = eCurrentPoint.p3;
                return eDrawObjectMouseDown.Continue;
            }
            if (CurrentPoint == eCurrentPoint.p3)
            {
                CurrentPoint = eCurrentPoint.done;
                OnMouseMove(canvas, point);
                Selected = false;
                return eDrawObjectMouseDown.Done;
            }
            return eDrawObjectMouseDown.Done;
        }
        protected void DrawNodes(ICanvas canvas)
        {
            DrawUtils.DrawNode(canvas, m_p1);
            DrawUtils.DrawNode(canvas, m_p3);
        }
        public INodePoint NodePoint(ICanvas canvas, UnitPoint point)
        {
            float thWidth = Line.ThresholdWidth(canvas, Width, ThresholdPixel);
            if (HitUtil.CircleHitPoint(m_p1, thWidth, point))
                return new NodePointRectBase(this, NodePointRectBase.ePoint.P1);
            if (HitUtil.CircleHitPoint(m_p3, thWidth, point))
                return new NodePointRectBase(this, NodePointRectBase.ePoint.P3);
            if (HitUtil.CircleHitPoint(m_center, thWidth, point))
                return new NodePointRectBase(this, NodePointRectBase.ePoint.Center);
            return null;
        }
        public ISnapPoint SnapPoint(ICanvas canvas, UnitPoint point, List<IDrawObject> otherobj, Type[] runningsnaptypes, Type usersnaptype)
        {
            float thWidth = Line.ThresholdWidth(canvas, Width, ThresholdPixel);
            if (runningsnaptypes != null)
            {
                foreach (Type snaptype in runningsnaptypes)
                {
                    UnitPoint ptemp = UnitPoint.Empty;
                    if (snaptype == typeof(VertextSnapPoint))
                    {
                        ptemp = GetPointFromVertexId(eVertexId.LeftTopCorner);
                        if (HitUtil.CircleHitPoint(ptemp, thWidth, point))
                            return new VertextSnapPoint(canvas, this, ptemp, (int)eVertexId.LeftTopCorner);//left top corner
                        ptemp = GetPointFromVertexId(eVertexId.RightBottomCorner);
                        if (HitUtil.CircleHitPoint(ptemp, thWidth, point))
                            return new VertextSnapPoint(canvas, this, ptemp, (int)eVertexId.RightBottomCorner);//right bottom corner
                    }
                    if (snaptype == typeof(MidpointSnapPoint))
                    {
                        ptemp = GetPointFromVertexId(eVertexId.BottomEdgeMidPoint);
                        if (HitUtil.CircleHitPoint(ptemp, thWidth, point))
                            return new MidpointSnapPoint(canvas, this, ptemp, (int)eVertexId.BottomEdgeMidPoint);//bottom edge center

                        ptemp = GetPointFromVertexId(eVertexId.TopEdgeMidPoint);
                        if (HitUtil.CircleHitPoint(ptemp, thWidth, point))
                            return new MidpointSnapPoint(canvas, this, ptemp, (int)eVertexId.TopEdgeMidPoint);//top edge center
                        ptemp = GetPointFromVertexId(eVertexId.LeftEdgeMidPoint);
                        if (HitUtil.CircleHitPoint(ptemp, thWidth, point))
                            return new MidpointSnapPoint(canvas, this, ptemp, (int)eVertexId.LeftEdgeMidPoint);//left edge center
                        ptemp = GetPointFromVertexId(eVertexId.RigthEdgeMidPoint);
                        if (HitUtil.CircleHitPoint(ptemp, thWidth, point))
                            return new MidpointSnapPoint(canvas, this, ptemp, (int)eVertexId.RigthEdgeMidPoint);//right edge center
                    }
                    if (snaptype == typeof(CenterSnapPoint))
                    {
                        if (HitUtil.PointInPoint(m_center, point, thWidth))
                            return new CenterSnapPoint(canvas, this, m_center);
                    }
                }
            }
            return null;
        }
        public void Move(UnitPoint offset)
        {
            m_center.X += offset.X;
            m_center.Y += offset.Y;
            m_p1.X += offset.X;
            m_p1.Y += offset.Y;
            m_p3.X += offset.X;
            m_p3.Y += offset.Y;

            UpdateCenter();
        }
        #region ISerialize
        public virtual void GetObjectData(XmlWriter wr)
        {
            wr.WriteStartElement(Id);
            XmlUtil.WriteProperties(this, wr);
            wr.WriteEndElement();
        }
        public void AfterSerializedIn()
        {
        }
        #endregion
        public string GetInfoAsString()
        {
            return "";
        }

        public RectangleF GetExactBoundingRect(ICanvas canvas)
        {
            return ScreenUtils.GetRect(m_p1, m_p3, 0);
        }

        public struct ConnectionCrvNodeToRectBaseNodePair
        {
            public INodePoint connectionCrvNode;
            public eVertexId rectNodeId;
            public ConnectionCrvNodeToRectBaseNodePair(INodePoint node, eVertexId rectNodeId)
            {
                this.connectionCrvNode = node;
                this.rectNodeId = rectNodeId;
            }
        };


        List<ConnectionCrvNodeToRectBaseNodePair> m_allConnectionCrvNodes = new List<ConnectionCrvNodeToRectBaseNodePair>();
        public void AttachConnectionCrvNode(INodePoint node)
        {
            ConnectionCrvNodeToRectBaseNodePair existNode = m_allConnectionCrvNodes.Find(
                curNode => curNode.connectionCrvNode.GetOriginal() == node.GetOriginal()
                );
            if (existNode.connectionCrvNode == null)
                m_allConnectionCrvNodes.Add(new ConnectionCrvNodeToRectBaseNodePair(node, GetVertexIdFromPoint(node.GetPosition())));
        }

        public void DeattachConnectionCrvNode(INodePoint node)
        {
            ConnectionCrvNodeToRectBaseNodePair existNode = m_allConnectionCrvNodes.Find(
                curNode => curNode.connectionCrvNode.GetOriginal() == node.GetOriginal()
                );
            if (existNode.connectionCrvNode != null)
                m_allConnectionCrvNodes.Remove(existNode);

        }

        public List<ConnectionCrvNodeToRectBaseNodePair> AllConnectionCrvNodes
        {
            get { return m_allConnectionCrvNodes; }
        }

        string m_guid = string.Empty;
        public string Guid
        {
            get
            {
                return m_guid;
            }
        }

        public eVertexId GetVertexIdFromPoint(UnitPoint pt)
        {
            float threshold = 1e-10f;
            float halfWidth, halfHeight;
            GetHalfWidthAndHeight(out halfWidth, out halfHeight);
            if (HitUtil.CircleHitPoint(m_p1, threshold, pt))
                return eVertexId.LeftTopCorner;
            else if (HitUtil.CircleHitPoint(m_p3, threshold, pt))
                return eVertexId.RightBottomCorner;

            UnitPoint ptemp = new UnitPoint(Center.X, Center.Y - halfHeight);
            if (HitUtil.CircleHitPoint(ptemp, threshold, pt))
                return eVertexId.BottomEdgeMidPoint;

            ptemp = new UnitPoint(Center.X, Center.Y + halfHeight);
            if (HitUtil.CircleHitPoint(ptemp, threshold, pt))
                return eVertexId.TopEdgeMidPoint;

            ptemp = new UnitPoint(Center.X - halfWidth, Center.Y);
            if (HitUtil.CircleHitPoint(ptemp, threshold, pt))
                return eVertexId.LeftEdgeMidPoint;

            ptemp = new UnitPoint(Center.X + halfWidth, Center.Y);
            if (HitUtil.CircleHitPoint(ptemp, threshold, pt))
                return eVertexId.RigthEdgeMidPoint;
            return eVertexId.None;
        }

        public UnitPoint GetPointFromVertexId(eVertexId vId)
        {
            UnitPoint pt = UnitPoint.Empty;
            float halfWidth, halfHeight;
            GetHalfWidthAndHeight(out halfWidth, out halfHeight);
            switch (vId)
            {
                case eVertexId.LeftTopCorner:
                    pt = m_p1;
                    break;
                case eVertexId.RightBottomCorner:
                    pt = m_p3;
                    break;
                case eVertexId.BottomEdgeMidPoint:
                    pt = new UnitPoint(Center.X, Center.Y - halfHeight);
                    break;
                case eVertexId.TopEdgeMidPoint:
                    pt = new UnitPoint(Center.X, Center.Y + halfHeight);
                    break;
                case eVertexId.LeftEdgeMidPoint:
                    pt = new UnitPoint(Center.X - halfWidth, Center.Y);
                    break;
                case eVertexId.RigthEdgeMidPoint:
                    pt = new UnitPoint(Center.X + halfWidth, Center.Y);
                    break;
                default:
                    throw new Exception("not match");
            }

            return pt;
        }

        public virtual List<Symbol> GetAllSymbol()
        {
            List<Symbol> allSymbols = new List<Symbol>();
            return allSymbols;
        }
    }
}
