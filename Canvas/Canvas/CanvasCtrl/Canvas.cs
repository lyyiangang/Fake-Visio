using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Canvas
{
    public struct CanvasWrapper : ICanvas
	{
		CanvasCtrl m_canvas; 
		Graphics m_graphics;
		Rectangle m_rect;
		public CanvasWrapper(CanvasCtrl canvas)
		{
			m_canvas = canvas;
			m_graphics = null;
			m_rect = new Rectangle();
		}
		public CanvasWrapper(CanvasCtrl canvas, Graphics graphics, Rectangle clientrect)
		{
			m_canvas = canvas;
			m_graphics = graphics;
			m_rect = clientrect;
		}
		public IModel Model
		{
			get { return m_canvas.Model; }
		}
		public CanvasCtrl CanvasCtrl
		{
			get { return m_canvas; }
		}
		public void Dispose()
		{
			m_graphics = null;
		}
		#region ICanvas Members
		public IModel DataModel
		{
			get { return m_canvas.Model; }
		}
		public UnitPoint ScreenTopLeftToUnitPoint()
		{
			return m_canvas.ScreenTopLeftToUnitPoint();
		}
		public UnitPoint ScreenBottomRightToUnitPoint()
		{
			return m_canvas.ScreenBottomRightToUnitPoint();
		}
		public PointF ToScreen(UnitPoint unitpoint)
		{
			return m_canvas.ToScreen(unitpoint);
		}
		public float ToScreen(double unitvalue)
		{
			return m_canvas.ToScreen(unitvalue);
		}
		public double ToUnit(float screenvalue)
		{
			return m_canvas.ToUnit(screenvalue);
		}
		public UnitPoint ToUnit(PointF screenpoint)
		{
			return m_canvas.ToUnit(screenpoint);
		}
		public Graphics Graphics
		{
			get { return m_graphics; }
		}
		public Rectangle ClientRectangle
		{
			get { return m_rect; }
			set { m_rect = value; }
		}
		public Pen CreatePen(Color color, float unitWidth)
		{
			return m_canvas.CreatePen(color, unitWidth);
		}
        public void DrawLine(ICanvas canvas, Pen pen, UnitPoint p1, UnitPoint p2)
        {
            m_canvas.DrawLine(canvas, pen, p1, p2);
        }
        public void DrawArc(ICanvas canvas, Pen pen, UnitPoint center, float radius, float beginangle, float angle)
		{
			m_canvas.DrawArc(canvas, pen, center, radius, beginangle, angle);
		}
        public void DrawBezier(ICanvas canvas, Pen pen, UnitPoint p1, UnitPoint p2, UnitPoint p3, UnitPoint p4)
        {
            m_canvas.DrawBezier(canvas, pen, p1, p2, p3, p4);
        }
        public void DrawRectangle(ICanvas canvas, Pen pen, UnitPoint ptTopLeft, float width, float height)
        {
            m_canvas.DrawRectangle(canvas, pen, ptTopLeft, width, height);
        }
        public void DrawEllipse(ICanvas canvas, Pen pen, UnitPoint ptTopLeft, float width, float height)
        {
            m_canvas.DrawEllipse(canvas, pen, ptTopLeft, width, height);
        }
        public void DrawString(ICanvas canvas, string str, Font font, Brush brush, RectangleF rect, StringFormat format)
        {
            m_canvas.DrawString( canvas, str, font, brush, rect,  format);
        }
        public void FillRectangle(ICanvas canvas, Brush brush, UnitPoint ptTopLeft, float width, float height)
        {
            m_canvas.FillRectangle(canvas, brush, ptTopLeft, width, height);
        }

        public void FillEllipse(ICanvas canvas, Brush brush, UnitPoint ptTopLeft, float width, float height)
        {
            m_canvas.FillEllipse(canvas, brush, ptTopLeft, width, height);
        }
        public void Invalidate()
		{
			m_canvas.DoInvalidate(false);
		}

        public IDrawObject CurrentObject
		{
			get { return m_canvas.NewObject; }
		}
        #endregion
    }
	public partial class CanvasCtrl : UserControl
	{
		enum eCommandType
		{
			select,
			pan,
			move,
			draw,
			edit,
			editNode,
		}

		ICanvasOwner		m_owner;
		CursorCollection	m_cursors = new CursorCollection();
		IModel				m_model;
		MoveHelper			m_moveHelper = null;
		NodeMoveHelper		m_nodeMoveHelper = null;
		CanvasWrapper		m_canvaswrapper;
		eCommandType		m_commandType = eCommandType.select;
		bool				m_runningSnaps = true;
		Type[]				m_runningSnapTypes = null;
		PointF				m_mousedownPoint;
		IDrawObject			m_newObject = null;
		IEditTool			m_editTool = null;
		SelectionRectangle	m_selection = null;
		string				m_drawObjectId = string.Empty;
		string				m_editToolId = string.Empty;
		Bitmap				m_staticImage = null;
		bool				m_staticDirty = true;
		ISnapPoint			m_snappoint = null;
		
		public Type[] RunningSnaps
		{
			get { return m_runningSnapTypes; }
			set { m_runningSnapTypes = value; }
		}
		public bool RunningSnapsEnabled
		{
			get { return m_runningSnaps; }
			set { m_runningSnaps = value; }
		}

		System.Drawing.Drawing2D.SmoothingMode	m_smoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
		public System.Drawing.Drawing2D.SmoothingMode SmoothingMode
		{
			get { return m_smoothingMode; }
			set { m_smoothingMode = value;}
		}

		public IModel Model
		{
			get { return m_model; }
			set { m_model = value; }
		}
        public CanvasCtrl(ICanvasOwner owner, IModel datamodel)
		{
			m_canvaswrapper = new CanvasWrapper(this);
			m_owner = owner;
			m_model = datamodel;

			InitializeComponent();
			this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
			this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

			m_commandType = eCommandType.select;
			m_cursors.AddCursor(eCommandType.select, Cursors.Arrow);
			m_cursors.AddCursor(eCommandType.draw, Cursors.Cross);
			m_cursors.AddCursor(eCommandType.pan, "hmove.cur");
			m_cursors.AddCursor(eCommandType.move, Cursors.SizeAll);
			m_cursors.AddCursor(eCommandType.edit, Cursors.Cross);
			UpdateCursor();

			m_moveHelper = new MoveHelper(this);
			m_nodeMoveHelper = new NodeMoveHelper(m_canvaswrapper);
		}
		public UnitPoint GetMousePoint()
		{
			Point point = this.PointToClient(Control.MousePosition);
			return ToUnit(point);
		}
		public void SetCenter(UnitPoint unitPoint)
		{
			PointF point = ToScreen(unitPoint);
			m_lastCenterPoint = unitPoint;
			SetCenterScreen(point, false);
		}
		public void SetCenter()
		{
			Point point = this.PointToClient(Control.MousePosition);
			SetCenterScreen(point, true);
		}
		public UnitPoint GetCenter()
		{
			return ToUnit(new PointF(this.ClientRectangle.Width/2, this.ClientRectangle.Height/2));
		}
		protected  void SetCenterScreen(PointF screenPoint, bool setCursor)
		{
			float centerX = ClientRectangle.Width / 2;
			m_panOffset.X += centerX - screenPoint.X;
			
			float centerY = ClientRectangle.Height / 2;
			m_panOffset.Y += centerY - screenPoint.Y;

			if (setCursor)
				Cursor.Position = this.PointToScreen(new Point((int)centerX, (int)centerY));
			DoInvalidate(true);
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			CommonTools.Tracing.StartTrack(Program.TracePaint);
			ClearPens();
			e.Graphics.SmoothingMode = m_smoothingMode;
			CanvasWrapper dc = new CanvasWrapper(this, e.Graphics, ClientRectangle);
			Rectangle cliprectangle = e.ClipRectangle;
			if (m_staticImage == null)
			{
				cliprectangle = ClientRectangle;
				m_staticImage = new Bitmap(ClientRectangle.Width, ClientRectangle.Height);
				m_staticDirty = true;
			}
			RectangleF r = ScreenUtils.ToUnitNormalized(dc, cliprectangle);
			if (float.IsNaN(r.Width) || float.IsInfinity(r.Width))
			{
				r = ScreenUtils.ToUnitNormalized(dc, cliprectangle);
			}
			if (m_staticDirty)
            {
				m_staticDirty = false;
				CanvasWrapper dcStatic = new CanvasWrapper(this, Graphics.FromImage(m_staticImage), ClientRectangle);
				dcStatic.Graphics.SmoothingMode = m_smoothingMode;
				m_model.BackgroundLayer.Draw(dcStatic, r);
				if (m_model.GridLayer.Enabled)
					m_model.GridLayer.Draw(dcStatic, r);
              //  TestBezier(dcStatic.Graphics);
                PointF nullPoint = ToScreen(new UnitPoint(0, 0));
				dcStatic.Graphics.DrawLine(Pens.Blue, nullPoint.X - 10, nullPoint.Y, nullPoint.X + 10, nullPoint.Y);
				dcStatic.Graphics.DrawLine(Pens.Blue, nullPoint.X, nullPoint.Y - 10, nullPoint.X, nullPoint.Y + 10);

				ICanvasLayer[] layers = m_model.Layers;
				for (int layerindex = layers.Length - 1; layerindex >= 0; layerindex--)
				{
					if (layers[layerindex] != m_model.ActiveLayer && layers[layerindex].Visible)
						layers[layerindex].Draw(dcStatic, r);
				}
				if (m_model.ActiveLayer != null)
					m_model.ActiveLayer.Draw(dcStatic, r);

				dcStatic.Dispose();
			}
			e.Graphics.DrawImage(m_staticImage, cliprectangle, cliprectangle, GraphicsUnit.Pixel);
			
			foreach (IDrawObject drawobject in m_model.SelectedObjects)
				drawobject.Draw(dc, r);

			if (m_newObject != null)
				m_newObject.Draw(dc, r);
			
			if (m_snappoint != null)
				m_snappoint.Draw(dc);
			
			if (m_selection != null)
			{
				m_selection.Reset();
				m_selection.SetMousePoint(e.Graphics, this.PointToClient(Control.MousePosition));
			}
			if (m_moveHelper.IsEmpty == false)
				m_moveHelper.DrawObjects(dc, r);

			if (m_nodeMoveHelper.IsEmpty == false)
				m_nodeMoveHelper.DrawObjects(dc, r);
			dc.Dispose();
			ClearPens();
			CommonTools.Tracing.EndTrack(Program.TracePaint, "OnPaint complete");
		}
        void PlotPoint(Graphics g, Point pt)
        {
            Rectangle rect = new Rectangle(pt.X,pt.Y,0,0);
            rect.Inflate(2, 2);
            g.DrawRectangle(Pens.Green, rect);
        }
        void TestBezier(Graphics g)
        {
            //-----------------------------------------------------------------------
            int scale = 30;
            Point start = new Point(100, 100);
            Point control1 = new Point(100+scale, 100);
            Point control2 = new Point(150-scale, 150);
            Point end1 = new Point(150,150);
            Point[] bezierPoints =
                     {
                 start, control1, control2, end1
             };

            g.DrawBeziers(Pens.Red, bezierPoints);
            //foreach(var pt in bezierPoints)
            //    PlotPoint(g,pt);
           // PlotBezierCrv(g, bezierPoints);
            //-----------------------------------------------------------------------

        }

        void PlotBezierCrv(Graphics g, Point[] ctrlPts)
        {
            CubicBezierCurveCurve bezierCrv = new CubicBezierCurveCurve(new float[2] { ctrlPts[0].X, ctrlPts[0].Y },
                                        new float[2] { ctrlPts[1].X, ctrlPts[1].Y },
                                        new float[2] { ctrlPts[2].X, ctrlPts[2].Y },
                                        new float[2] { ctrlPts[3].X, ctrlPts[3].Y });
            //{
            //    //check point on curve
            //    float[] pos = { 0, 0 };
            //    Point ptCur = new Point();
            //    int nSegments = 20;
            //    float stepSize = 1.0f / nSegments;
            //    for (int ii = 1; ii < nSegments + 1; ++ii)
            //    {
            //        bezierCrv.Eval(stepSize * ii, ref pos[0], ref pos[1]);
            //        ptCur.X = (int)pos[0];
            //        ptCur.Y = (int)pos[1];
            //        PlotPoint(g, ptCur);
            //    }
            //}
            //check the project function
            float t = 0.0f, squareDistance = 0.0f ;
            float[] posOnCrv = { 0.0f, 0.0f };
            Point cursorClientPt = this.PointToClient(Control.MousePosition);
            bezierCrv.ProjectPointToCurve(new float[2] { cursorClientPt.X, cursorClientPt.Y }, ref t, ref posOnCrv, ref squareDistance);
            Point Q = new Point((int)posOnCrv[0], (int)posOnCrv[1]);
            g.DrawLine(Pens.Red, Q, cursorClientPt);

        }
        void RepaintStatic(Rectangle r)
		{
			if (m_staticImage == null)
				return;
			Graphics dc = Graphics.FromHwnd(Handle);
			if (r.X < 0) r.X = 0;
			if (r.X > m_staticImage.Width) r.X = 0;
			if (r.Y < 0) r.Y = 0;
			if (r.Y > m_staticImage.Height) r.Y = 0;
			
			if (r.Width > m_staticImage.Width || r.Width < 0)
				r.Width = m_staticImage.Width;
			if (r.Height > m_staticImage.Height || r.Height < 0)
				r.Height = m_staticImage.Height;
			dc.DrawImage(m_staticImage, r, r, GraphicsUnit.Pixel);
			dc.Dispose();
		}
		void RepaintSnappoint(ISnapPoint snappoint)
		{
			if (snappoint == null)
				return;
			CanvasWrapper dc = new CanvasWrapper(this, Graphics.FromHwnd(Handle), ClientRectangle);
			snappoint.Draw(dc);
			dc.Graphics.Dispose();
			dc.Dispose();
		}
		void RepaintObject(IDrawObject obj)
		{
			if (obj == null)
				return;
			CanvasWrapper dc = new CanvasWrapper(this, Graphics.FromHwnd(Handle), ClientRectangle);
			RectangleF invalidaterect = ScreenUtils.ConvertRect(ScreenUtils.ToScreenNormalized(dc, obj.GetBoundingRect(dc)));
			obj.Draw(dc, invalidaterect);
			dc.Graphics.Dispose();
			dc.Dispose();
		}
		public void DoInvalidate(bool dostatic, RectangleF rect)
		{
			if (dostatic)
				m_staticDirty = true;
			Invalidate(ScreenUtils.ConvertRect(rect));
		}
		public void DoInvalidate(bool dostatic)
		{
			if (dostatic)
				m_staticDirty = true;
			Invalidate();
		}
		public IDrawObject NewObject
		{
			get { return m_newObject; }
		}


        protected void HandleSelection(List<IDrawObject> selected)
		{
			bool add = Control.ModifierKeys == Keys.Shift;
			bool toggle = Control.ModifierKeys == Keys.Control;
			bool invalidate = false;
			bool anyoldsel = false;
			int selcount = 0;
			if (selected != null)
				selcount = selected.Count;
			foreach(IDrawObject obj in m_model.SelectedObjects)
			{
				anyoldsel = true;
				break;
			}
			if (toggle && selcount > 0)
			{
				invalidate = true;
				foreach (IDrawObject obj in selected)
				{
					if (m_model.IsSelected(obj))
						m_model.RemoveSelectedObject(obj);
					else
						m_model.AddSelectedObject(obj);
				}
			}
			if (add && selcount > 0)
			{
				invalidate = true;
				foreach (IDrawObject obj in selected)
					m_model.AddSelectedObject(obj);
			}
			if (add == false && toggle == false && selcount > 0)
			{
				invalidate = true;
				m_model.ClearSelectedObjects();
				foreach (IDrawObject obj in selected)
					m_model.AddSelectedObject(obj);
			}
			if (add == false && toggle == false && selcount == 0 && anyoldsel)
			{
				invalidate = true;
				m_model.ClearSelectedObjects();
			}

			if (invalidate)
				DoInvalidate(false);
		}
		void FinishNodeEdit()
		{
			m_commandType = eCommandType.select;
			m_snappoint = null;
		}
		protected virtual void HandleMouseDownWhenDrawing(UnitPoint mouseunitpoint, ISnapPoint snappoint)
		{
			if (m_commandType == eCommandType.draw)
			{
				if (m_newObject == null)
				{
					m_newObject = m_model.CreateObject(m_drawObjectId, mouseunitpoint, snappoint);
					DoInvalidate(false, m_newObject.GetBoundingRect(m_canvaswrapper));
				}
				else
				{
					if (m_newObject != null)
					{
						eDrawObjectMouseDown result = m_newObject.OnMouseDown(m_canvaswrapper, mouseunitpoint, snappoint);
						switch (result)
						{
							case eDrawObjectMouseDown.Done:
								m_model.AddObject(m_model.ActiveLayer, m_newObject);
								m_newObject = null;
								DoInvalidate(true);
								break;
							case eDrawObjectMouseDown.DoneRepeat:
								m_model.AddObject(m_model.ActiveLayer, m_newObject);
								m_newObject = m_model.CreateObject(m_newObject.Id, m_newObject.RepeatStartingPoint, null);
								DoInvalidate(true);
								break;
							case eDrawObjectMouseDown.Continue:
								break;
                            case eDrawObjectMouseDown.Cancel:
                                m_newObject = null;
                                DoInvalidate(true);
                                break;
						}
					}
				}
			}
		}
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (m_commandType != eCommandType.select || m_model.SelectedCount < 1)
                return;
            UnitPoint mousePt = ToUnit(new PointF(e.X, e.Y));
            foreach (var curObj in m_model.SelectedObjects)
            {
                DrawTools.RectBase rect = curObj as DrawTools.RectBase;
                if (rect == null || !rect.PointInObject(m_canvaswrapper, mousePt))
                    continue;
               // rect.OnMouseDoubleClick(m_canvaswrapper, e);
            }
            base.OnMouseDoubleClick(e);
        }
        protected override void OnMouseDown(MouseEventArgs e)
		{
            if (e.Button == MouseButtons.Middle)
            {
                CommandEscape();
                CommandPan();
            }
            if (e.Button == MouseButtons.Right)
                CommandEscape();
            m_mousedownPoint = new PointF(e.X, e.Y); // used when panning
			m_dragOffset = new PointF(0,0);

			UnitPoint mousepoint = ToUnit(m_mousedownPoint);
			if (m_snappoint != null)
				mousepoint = m_snappoint.SnapPoint;//snap point: rect point
			
			if (m_commandType == eCommandType.editNode)
			{
                //modify a obj's node, e.g: move node
				bool handled = false;
				if (m_nodeMoveHelper.HandleMouseDown(mousepoint, ref handled))
				{
					FinishNodeEdit();
					base.OnMouseDown(e);
					return;
				}
			}
			if (m_commandType == eCommandType.select)
			{
                //select an exsiting obj
				bool handled = false;
				if (m_nodeMoveHelper.HandleMouseDown(mousepoint, ref handled))
				{
					m_commandType = eCommandType.editNode;
					m_snappoint = null;
					base.OnMouseDown(e);
					return;
				}
				m_selection = new SelectionRectangle(m_mousedownPoint);
			}
			if (m_commandType == eCommandType.move)
			{
				m_moveHelper.HandleMouseDownForMove(mousepoint, m_snappoint);
			}
			if (m_commandType == eCommandType.draw)
			{
                //make a new obj and add it to model
				HandleMouseDownWhenDrawing(mousepoint, m_snappoint);
				DoInvalidate(true);
			}
			if (m_commandType == eCommandType.edit)
			{
				if (m_editTool == null)
					m_editTool = m_model.GetEditTool(m_editToolId);
				if (m_editTool != null)
				{
					if (m_editTool.SupportSelection)
						m_selection = new SelectionRectangle(m_mousedownPoint);

					eDrawObjectMouseDown mouseresult = m_editTool.OnMouseDown(m_canvaswrapper, mousepoint, m_snappoint);
					/*
					if (mouseresult == eDrawObjectMouseDown.Continue)
					{
						if (m_editTool.SupportSelection)
							m_selection = new SelectionRectangle(m_mousedownPoint);
					}
					 * */
					if (mouseresult == eDrawObjectMouseDown.Done)
					{
						m_editTool.Finished();
						m_editTool = m_model.GetEditTool(m_editToolId); // continue with new tool
						//m_editTool = null;
						
						if (m_editTool.SupportSelection)
							m_selection = new SelectionRectangle(m_mousedownPoint);
					}
				}
				DoInvalidate(true);
				UpdateCursor();
			}
			base.OnMouseDown(e);
		}
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
            if(e.Button==MouseButtons.Right)
                CommandEscape();

            if (m_commandType == eCommandType.pan)
			{
				m_panOffset.X += m_dragOffset.X;
				m_panOffset.Y += m_dragOffset.Y;
				m_dragOffset = new PointF(0, 0);
			}

			List<IDrawObject> hitlist = null;
			Rectangle screenSelRect = Rectangle.Empty;
			if (m_selection != null)
			{
				screenSelRect = m_selection.ScreenRect();
				RectangleF selectionRect = m_selection.Selection(m_canvaswrapper);
				if (selectionRect != RectangleF.Empty)
				{
					// is any selection rectangle. use it for selection
					hitlist = m_model.GetHitObjects(m_canvaswrapper, selectionRect, m_selection.AnyPoint());
					DoInvalidate(true);
				}
				else
				{
					// else use mouse point
					UnitPoint mousepoint = ToUnit(new PointF(e.X, e.Y));
					hitlist = m_model.GetHitObjects(m_canvaswrapper, mousepoint);
				}
				m_selection = null;
			}
			if (m_commandType == eCommandType.select)
			{
				if (hitlist != null)
					HandleSelection(hitlist);
			}
			if (m_commandType == eCommandType.edit && m_editTool != null)
			{
				UnitPoint mousepoint = ToUnit(m_mousedownPoint);
				if (m_snappoint != null)
					mousepoint = m_snappoint.SnapPoint;
				if (screenSelRect != Rectangle.Empty)
					m_editTool.SetHitObjects(mousepoint, hitlist);
				m_editTool.OnMouseUp(m_canvaswrapper, mousepoint, m_snappoint);
			}
			if (m_commandType == eCommandType.draw && m_newObject != null)
			{
				UnitPoint mousepoint = ToUnit(m_mousedownPoint);
				if (m_snappoint != null)
					mousepoint = m_snappoint.SnapPoint;
				m_newObject.OnMouseUp(m_canvaswrapper, mousepoint, m_snappoint);
			}
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (m_selection != null)
			{
				Graphics dc = Graphics.FromHwnd(Handle);
				m_selection.SetMousePoint(dc, new PointF(e.X, e.Y));
				dc.Dispose();
				return;
			}

			if (m_commandType == eCommandType.pan &&
                (e.Button == MouseButtons.Left || e.Button==MouseButtons.Middle))
			{
				m_dragOffset.X = -(m_mousedownPoint.X - e.X);
				m_dragOffset.Y = -(m_mousedownPoint.Y - e.Y);
				m_lastCenterPoint = CenterPointUnit();
				DoInvalidate(true);
			}
			UnitPoint mousepoint;
			UnitPoint unitpoint = ToUnit(new PointF(e.X, e.Y));
			if (m_commandType == eCommandType.draw || m_commandType == eCommandType.move || m_nodeMoveHelper.IsEmpty == false)
			{
				Rectangle invalidaterect = Rectangle.Empty;
				ISnapPoint newsnap = null;
				mousepoint = GetMousePoint();
				if (RunningSnapsEnabled)
					newsnap = m_model.SnapPoint(m_canvaswrapper, mousepoint, m_runningSnapTypes, null);
				if (newsnap == null)
					newsnap = m_model.GridLayer.SnapPoint(m_canvaswrapper, mousepoint, null);
				if ((m_snappoint != null) && ((newsnap == null) || (newsnap.SnapPoint != m_snappoint.SnapPoint) || m_snappoint.GetType() != newsnap.GetType()))
				{
					invalidaterect = ScreenUtils.ConvertRect(ScreenUtils.ToScreenNormalized(m_canvaswrapper, m_snappoint.BoundingRect));
					invalidaterect.Inflate(2, 2);
					RepaintStatic(invalidaterect); // remove old snappoint
					m_snappoint = newsnap;
				}
				if (m_commandType == eCommandType.move)
					Invalidate(invalidaterect);

				if (m_snappoint == null)
					m_snappoint = newsnap;
			}
			m_owner.SetPositionInfo(unitpoint);
			m_owner.SetSnapInfo(m_snappoint);

			//UnitPoint mousepoint;
			if (m_snappoint != null)
				mousepoint = m_snappoint.SnapPoint;
			else
				mousepoint = GetMousePoint();

			if (m_newObject != null)
			{
				Rectangle invalidaterect = ScreenUtils.ConvertRect(ScreenUtils.ToScreenNormalized(m_canvaswrapper, m_newObject.GetBoundingRect(m_canvaswrapper)));
				invalidaterect.Inflate(2, 2);
				RepaintStatic(invalidaterect);

				m_newObject.OnMouseMove(m_canvaswrapper, mousepoint);
				RepaintObject(m_newObject);
			}
			if (m_snappoint != null)
				RepaintSnappoint(m_snappoint);

			if (m_moveHelper.HandleMouseMoveForMove(mousepoint))
				Refresh(); //Invalidate();

			RectangleF rNoderect = m_nodeMoveHelper.HandleMouseMoveForNode(mousepoint);
			if (rNoderect != RectangleF.Empty)
			{
				Rectangle invalidaterect = ScreenUtils.ConvertRect(ScreenUtils.ToScreenNormalized(m_canvaswrapper, rNoderect));
				RepaintStatic(invalidaterect);

				CanvasWrapper dc = new CanvasWrapper(this, Graphics.FromHwnd(Handle), ClientRectangle);
				dc.Graphics.Clip = new Region(ClientRectangle);
				//m_nodeMoveHelper.DrawOriginalObjects(dc, rNoderect);
				m_nodeMoveHelper.DrawObjects(dc, rNoderect);
				if (m_snappoint != null)
					RepaintSnappoint(m_snappoint);

				dc.Graphics.Dispose();
				dc.Dispose();
			}
		}
		protected override void OnMouseWheel(MouseEventArgs e)
		{
			UnitPoint p = GetMousePoint();
			float wheeldeltatick = 120;
			float zoomdelta = (1.25f * (Math.Abs(e.Delta) / wheeldeltatick));
			if (e.Delta < 0)
				m_model.Zoom = m_model.Zoom / zoomdelta;
			else
				m_model.Zoom = m_model.Zoom * zoomdelta;
			SetCenterScreen(ToScreen(p), true);
			DoInvalidate(true);
			base.OnMouseWheel(e);
		}
  
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			if (m_lastCenterPoint != UnitPoint.Empty && Width != 0)
				SetCenterScreen(ToScreen(m_lastCenterPoint), false);
			m_lastCenterPoint = CenterPointUnit();
			m_staticImage = null;
			DoInvalidate(true);
		}

		UnitPoint m_lastCenterPoint;
		PointF m_panOffset = new PointF(25, -25);
		PointF m_dragOffset = new PointF(0, 0);
		float m_screenResolution = 96;

		PointF Translate(UnitPoint point)
		{
			return point.Point;
		}
		float ScreenHeight()
		{
			return (float)(ToUnit(ClientRectangle.Height) / m_model.Zoom);
		}

		#region ICanvas
		public UnitPoint CenterPointUnit()
		{
			UnitPoint p1 = ScreenTopLeftToUnitPoint();
			UnitPoint p2 = ScreenBottomRightToUnitPoint();
			UnitPoint center = new UnitPoint();
			center.X = (p1.X + p2.X) / 2;
			center.Y = (p1.Y + p2.Y) / 2;
			return center;
		}
		public UnitPoint ScreenTopLeftToUnitPoint()
		{
			return ToUnit(new PointF(0, 0));
		}
		public UnitPoint ScreenBottomRightToUnitPoint()
		{
			return ToUnit(new PointF(ClientRectangle.Width, ClientRectangle.Height));
		}
        public PointF ToScreen(UnitPoint point)
		{
			PointF transformedPoint = Translate(point);
			transformedPoint.Y = ScreenHeight() - transformedPoint.Y;
			transformedPoint.Y *= m_screenResolution * m_model.Zoom;
			transformedPoint.X *= m_screenResolution * m_model.Zoom;

			transformedPoint.X += m_panOffset.X + m_dragOffset.X;
			transformedPoint.Y += m_panOffset.Y + m_dragOffset.Y;
			return transformedPoint;
		}
		public float ToScreen(double value)
		{
			return (float)(value * m_screenResolution * m_model.Zoom);
		}
		public double ToUnit(float screenvalue)
		{
			return screenvalue / (double)(m_screenResolution * m_model.Zoom);
		}
		public UnitPoint ToUnit(PointF screenpoint)
		{
			float panoffsetX = m_panOffset.X + m_dragOffset.X;
			float panoffsetY = m_panOffset.Y + m_dragOffset.Y;
			float xpos = (screenpoint.X - panoffsetX) / (m_screenResolution * m_model.Zoom);
			float ypos = ScreenHeight() - ((screenpoint.Y - panoffsetY)) / (m_screenResolution * m_model.Zoom);
			return new UnitPoint(xpos, ypos);
		}
		public Pen CreatePen(Color color, float unitWidth)
		{
			return GetPen(color, ToScreen(unitWidth));
		}
        public void DrawLine(ICanvas canvas, Pen pen, UnitPoint p1, UnitPoint p2)
		{
			PointF tmpp1 = ToScreen(p1);
			PointF tmpp2 = ToScreen(p2);
			canvas.Graphics.DrawLine(pen, tmpp1, tmpp2);
		}
		public void DrawArc(ICanvas canvas, Pen pen, UnitPoint center, float radius, float startAngle, float sweepAngle)
		{
			PointF p1 = ToScreen(center);
			radius = (float)Math.Round(ToScreen(radius));
			RectangleF r = new RectangleF(p1, new SizeF());
			r.Inflate(radius, radius);
			if (radius > 0 && radius < 1e8f )
				canvas.Graphics.DrawArc(pen, r, -startAngle, -sweepAngle);
		}
        public void DrawBezier(ICanvas canvas, Pen pen, UnitPoint p1,UnitPoint p2,UnitPoint p3,UnitPoint p4)
        {
            canvas.Graphics.DrawBezier(pen, ToScreen(p1), ToScreen(p2), ToScreen(p3), ToScreen(p4));
        }
        public void DrawRectangle(ICanvas canvas, Pen pen, UnitPoint ptTopLeft,float width,float height)
        {
            PointF ptScreen = ToScreen(ptTopLeft);
            canvas.Graphics.DrawRectangle(pen,ptScreen.X,ptScreen.Y, ToScreen(width), ToScreen(height));
        }
        public void FillRectangle(ICanvas canvas,Brush brush,UnitPoint ptUpperLeft,float width,float height)
        {
            PointF ptScreen = ToScreen(ptUpperLeft);
            canvas.Graphics.FillRectangle(brush, ptScreen.X, ptScreen.Y, ToScreen(width), ToScreen(height));
        }
        public void DrawEllipse(ICanvas canvas, Pen pen, UnitPoint ptTopLeft, float width, float height)
        {
            PointF p1 = ToScreen(ptTopLeft);
            width = (float)Math.Round(ToScreen(width));
            height = (float)Math.Round(ToScreen(height));
            if (width > 0 && height > 0 && width < 1e5f && height < 1e5f)
                canvas.Graphics.DrawEllipse(pen, p1.X, p1.Y, width, height);
        }
        public void FillEllipse(ICanvas canvas, Brush brush, UnitPoint ptTopLeft, float width, float height)
        {
            PointF p1 = ToScreen(ptTopLeft);
            width = (float)Math.Round(ToScreen(width));
            height = (float)Math.Round(ToScreen(height));
            if (width > 0 && height > 0 && width < 1e5f && height < 1e5f)
                canvas.Graphics.FillEllipse(brush, p1.X, p1.Y, width, height);
        }

        public void DrawString(ICanvas canvas, string str, Font font, Brush brush, RectangleF rect, StringFormat format)
        {
            if (str == string.Empty)
                return;
            RectangleF screenRect = ScreenUtils.ToScreenNormalized(canvas, rect);
            canvas.Graphics.DrawString(str, font, brush, screenRect,format);
        }
        #endregion

        Dictionary<float, Dictionary<Color, Pen>> m_penCache = new Dictionary<float,Dictionary<Color,Pen>>();
		Pen GetPen(Color color, float width)
		{
			if (m_penCache.ContainsKey(width) == false)
				m_penCache[width] = new Dictionary<Color,Pen>();
			if (m_penCache[width].ContainsKey(color) == false)
				m_penCache[width][color] = new Pen(color, width);
			return m_penCache[width][color];
		}
		void ClearPens()
		{
			m_penCache.Clear();
		}
		
		void UpdateCursor()
		{
			Cursor = m_cursors.GetCursor(m_commandType);
		}

		Dictionary<Keys, Type> m_QuickSnap = new Dictionary<Keys,Type>();
		public void AddQuickSnapType(Keys key, Type snaptype)
		{
			m_QuickSnap.Add(key, snaptype);
		}

		public void CommandSelectDrawTool(string drawobjectid)
		{
			CommandEscape();
			m_model.ClearSelectedObjects();
			m_commandType = eCommandType.draw;
			m_drawObjectId = drawobjectid;
			UpdateCursor();
		}
		public void CommandEscape()
		{
			bool dirty = (m_newObject != null) || (m_snappoint != null);
			m_newObject = null;
			m_snappoint = null;
			if (m_editTool != null)
				m_editTool.Finished();
			m_editTool	= null;
			m_commandType = eCommandType.select;
			m_moveHelper.HandleCancelMove();
			m_nodeMoveHelper.HandleCancelMove();
			DoInvalidate(dirty);
			UpdateCursor();
		}
		public void CommandPan()
		{
			if (m_commandType == eCommandType.select || m_commandType == eCommandType.move)
				m_commandType = eCommandType.pan;
			UpdateCursor();
		}
		public void CommandMove(bool handleImmediately)
		{
			if (m_model.SelectedCount > 0)
			{
				if (handleImmediately && m_commandType == eCommandType.move)
					m_moveHelper.HandleMouseDownForMove(GetMousePoint(), m_snappoint);
				m_commandType = eCommandType.move;
				UpdateCursor();
			}
		}
		public void CommandDeleteSelected()
		{
			m_model.DeleteObjects(m_model.SelectedObjects);
			m_model.ClearSelectedObjects();
			DoInvalidate(true);
			UpdateCursor();
		}
		public void CommandEdit(string editid)
		{
			CommandEscape();
			m_model.ClearSelectedObjects();
			m_commandType = eCommandType.edit;
			m_editToolId = editid;
			m_editTool = m_model.GetEditTool(m_editToolId);
			UpdateCursor();
		}
		void HandleQuickSnap(KeyEventArgs e)
		{
			if (m_commandType == eCommandType.select || m_commandType == eCommandType.pan)
				return;
			ISnapPoint p = null;
			UnitPoint mousepoint = GetMousePoint();
			if (m_QuickSnap.ContainsKey(e.KeyCode))
				p = m_model.SnapPoint(m_canvaswrapper, mousepoint, null, m_QuickSnap[e.KeyCode]);
			if (p != null)
			{
				if (m_commandType == eCommandType.draw)
				{
					HandleMouseDownWhenDrawing(p.SnapPoint, p);
					if (m_newObject != null)
						m_newObject.OnMouseMove(m_canvaswrapper, GetMousePoint());
					DoInvalidate(true);
					e.Handled = true;
				}
				if (m_commandType == eCommandType.move)
				{
					m_moveHelper.HandleMouseDownForMove(p.SnapPoint, p);
					e.Handled = true;
				}
				if (m_nodeMoveHelper.IsEmpty == false)
				{
					bool handled = false;
					m_nodeMoveHelper.HandleMouseDown(p.SnapPoint, ref handled);
					FinishNodeEdit();
					e.Handled = true;
				}
				if (m_commandType == eCommandType.edit)
				{
				}
			}
		}
		protected override void OnKeyDown(KeyEventArgs e)
		{
			HandleQuickSnap(e);

			if (m_nodeMoveHelper.IsEmpty == false)
			{
				m_nodeMoveHelper.OnKeyDown(m_canvaswrapper, e);
				if (e.Handled)
					return;
			}
			base.OnKeyDown(e);
			if (e.Handled)
			{
				UpdateCursor();
				return;
			}
			if (m_editTool != null)
			{
				m_editTool.OnKeyDown(m_canvaswrapper, e);
				if (e.Handled)
					return;
			}
			if (m_newObject != null)
			{
				m_newObject.OnKeyDown(m_canvaswrapper, e);
				if (e.Handled)
					return;
			}
			foreach (IDrawObject obj in m_model.SelectedObjects)
			{
				obj.OnKeyDown(m_canvaswrapper, e);
				if (e.Handled)
					return;
			}

			if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
			{
				if (e.KeyCode == Keys.G)
				{
					m_model.GridLayer.Enabled = !m_model.GridLayer.Enabled;
					DoInvalidate(true);
				}
				if (e.KeyCode == Keys.S)
				{
					RunningSnapsEnabled = !RunningSnapsEnabled;
					if (!RunningSnapsEnabled)
						m_snappoint = null;
					DoInvalidate(false);
				}
				return;
			}

			if (e.KeyCode == Keys.Escape)
			{
				CommandEscape();
			}
			if (e.KeyCode == Keys.P)
			{
				CommandPan();
			}
			if (e.KeyCode == Keys.S)
			{
				RunningSnapsEnabled = !RunningSnapsEnabled;
				if (!RunningSnapsEnabled)
					m_snappoint = null;
				DoInvalidate(false);
			}
			if (e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9)
			{
				int layerindex = (int)e.KeyCode - (int)Keys.D1;
				if (layerindex >=0 && layerindex < m_model.Layers.Length)
				{
					m_model.ActiveLayer = m_model.Layers[layerindex];
					DoInvalidate(true);
				}
			}
			if (e.KeyCode == Keys.Delete)
			{
				CommandDeleteSelected();
			}
			if (e.KeyCode == Keys.O)
			{
				CommandEdit("linesmeet");
			}
			UpdateCursor();
		}
	}
}
