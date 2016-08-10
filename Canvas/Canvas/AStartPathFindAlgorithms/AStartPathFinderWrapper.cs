using AStartPathFindAlgorithms;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Canvas.AStartPathFindAlgorithms
{
    //public enum eAstartPathFinderType
    //{
    //    PathFinderFast,
    //    PathFinder,
    //};
    class AStartPathFinderWrapper
    {
        private IPathFinder _pathFinder = null;
        ICanvas _canvas = null;
        byte[,] m_pixelMatrix = null;
        RectangleF _boundingBox = RectangleF.Empty;
        public AStartPathFinderWrapper(ICanvas canvas )
        {
            _canvas = canvas;
            Init();
        }
        void Init()
        {
            if (!InitPixelMatrix())
                return;
            //if (type == eAstartPathFinderType.PathFinderFast)
            //{
                _pathFinder = new PathFinderFast(m_pixelMatrix);
            //}
            //else if (type == eAstartPathFinderType.PathFinder)
            //{
            //  _pathFinder = new PathFinder(m_pixelMatrix);
            //}
            _pathFinder.Formula = HeuristicFormula.Manhattan;
            _pathFinder.Diagonals = false;
            _pathFinder.HeavyDiagonals = false;
            _pathFinder.HeuristicEstimate = 2;
            _pathFinder.PunishChangeDirection = true;
            _pathFinder.TieBreaker = false;
            _pathFinder.SearchLimit = 50000;
            _pathFinder.DebugProgress = false;
            _pathFinder.ReopenCloseNodes = false;
            _pathFinder.DebugFoundPath = true;
        }
        public List<UnitPoint> FindPath(UnitPoint startPt, UnitPoint endPt, bool passEndPoint=false)
        {
            if (_pathFinder == null)
                return null;
            //if the canvas control'size has changed, we need to update the matrix
            if (NeedReconstructMatrix())
            {
                m_pixelMatrix = null;
                Init();
            }
            Point pStart = ScreenUtils.ConvertPoint(_canvas.ToScreen(startPt));
            Point pEnd = ScreenUtils.ConvertPoint(_canvas.ToScreen(endPt));
            if (pStart == pEnd)
                return null;
            //-----------------------------------------------------------------------
            #region debug
            bool export= false;
            if (export)
            {
                int nItems = m_pixelMatrix.GetUpperBound(0)+1;
                int maxX = -1, maxY = -1, minX = 100000, minY = 100000;
                for(int ii=0;ii<nItems;++ii)//row
                {
                    for(int jj=0;jj<nItems;++jj)//column
                    {
                        int val = m_pixelMatrix[ii, jj];
                        if (val != 0)
                            continue;
                        if (jj < minX)
                            minX = jj;
                        if (jj > maxX)
                            maxX = jj;
                        if (ii < minY)
                            minY = ii;
                        if (ii > maxY)
                            maxY = ii;
                    }
                }


                for (int ii = 0; ii < nItems; ++ii)
                {
                    for (int jj = 0; jj < nItems; ++jj)
                    {
                        int val = m_pixelMatrix[ii, jj];
                        Console.Write("{0},", val);
                    }
                    Console.Write("\n");
                }
            }
            #endregion
            //-----------------------------------------------------------------------

            _pathFinder.FindPathStop();
            //if (passEndPoint)
            //    m_pixelMatrix[pEnd.X, pEnd.Y] = 1;
            List<PathFinderNode> path = _pathFinder.FindPath(pStart, pEnd);
            if (path == null || path.Count < 1)
                return null;
            List<PathFinderNode> cornerNodesPath= ExtractConnerNodes(path);
            List<UnitPoint> allPts = new List<UnitPoint>();

            PointF ptLeftBottom = PointF.Empty;
            PointF ptRightTop = PointF.Empty;
            foreach (var tmpNode in cornerNodesPath)
            {
                PointF pt = new PointF(tmpNode.X, tmpNode.Y);
                UnitPoint convertedPt = _canvas.ToUnit(pt);
                allPts.Add(convertedPt);
                //get bounding box
                if (ptLeftBottom == PointF.Empty)
                {
                    ptLeftBottom.X = (float)convertedPt.X;
                    ptLeftBottom.Y = (float)convertedPt.Y;
                    ptRightTop.X = (float)convertedPt.X;
                    ptRightTop.Y = (float)convertedPt.Y;
                }
                else
                {
                    if (convertedPt.X < ptLeftBottom.X)
                        ptLeftBottom.X =(float) convertedPt.X;
                    if (convertedPt.Y < ptLeftBottom.Y)
                        ptLeftBottom.Y = (float)convertedPt.Y;
                    if (convertedPt.X > ptRightTop.X)
                        ptRightTop.X = (float)convertedPt.X;
                    if (convertedPt.Y > ptRightTop.Y)
                        ptRightTop.Y = (float)convertedPt.Y;
                }
            }
            _boundingBox = new RectangleF(ptLeftBottom,
                new SizeF(ptRightTop.X - ptLeftBottom.X, ptRightTop.Y - ptLeftBottom.Y));
            List<UnitPoint> snapedPts = SnapToStartEndNodes(allPts, startPt, endPt);
            return snapedPts;
        }

        public void StopFind()
        {
            if (_pathFinder == null)
                return;
            _pathFinder.FindPathStop();
        }

        public RectangleF BoundingBox
        {
            get { return _boundingBox; }
        }

        RectangleF ScreenPixelRectToUnitRect()
        {
            UnitPoint bottomRightPos = _canvas.ScreenBottomRightToUnitPoint();
            double width = _canvas.ToUnit(((CanvasWrapper)_canvas).CanvasCtrl.ClientRectangle.Width);
            double height = _canvas.ToUnit(((CanvasWrapper)_canvas).CanvasCtrl.ClientRectangle.Height);
            return new RectangleF(bottomRightPos.Point.X-(float)width, bottomRightPos.Point.Y, (float)width, (float)height);
        }

        bool NeedReconstructMatrix()
        {
            if (_canvas is CanvasWrapper)
            {
                int curSize= m_pixelMatrix.GetUpperBound(0)+1;
                int newSize = GetPixelMatrixSize();
                if (curSize == newSize)
                    return false;
                return true;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
            }
            return true;
        }

        int GetPixelMatrixSize()
        {
            int nx = ((CanvasWrapper)_canvas).CanvasCtrl.ClientRectangle.Width;
            int ny = ((CanvasWrapper)_canvas).CanvasCtrl.ClientRectangle.Height;
            if (nx < 1 || ny < 1)
                return 0;
            double logNx = Math.Log(nx, 2);
            if (logNx != (int)logNx)
            {
                nx = (int)Math.Pow(2, (int)logNx + 1);
            }
            double logNy = Math.Log(ny, 2);
            if (logNy != (int)logNy)
            {
                ny = (int)Math.Pow(2, (int)logNy + 1);
            }
            //for fast path finder, nx should be equal with ny
            int maxVal = nx > ny ? nx : ny;
            return maxVal;
        }

        bool InitPixelMatrix()
        {
            if (_canvas is CanvasWrapper)
            {
                int matrixSize = GetPixelMatrixSize();
                if (matrixSize < 1)
                    return false;
                if (m_pixelMatrix == null)
                {
                    m_pixelMatrix = new byte[matrixSize, matrixSize];
                }
                for (int ii = 0; ii < matrixSize; ++ii)
                {
                    for (int jj = 0; jj < matrixSize; ++jj)
                    {//init
                        m_pixelMatrix[ii, jj] = 1;//the path can pass through these point
                    }
                }
                List<IDrawObject> allObjs = _canvas.DataModel.GetHitObjects(_canvas, ScreenPixelRectToUnitRect(), false);
                foreach (var obj in allObjs)
                {
                    DrawTools.RectBase rectBase = obj as DrawTools.RectBase;
                    if (rectBase == null)
                        continue;
                    Rectangle pixelRect = ScreenUtils.ConvertRect(ScreenUtils.ToScreenNormalized(_canvas, rectBase.GetExactBoundingRect(_canvas)));
                    //pixelRect.Inflate(-1, -1);
                    for (int ii = pixelRect.Y; ii < pixelRect.Height + pixelRect.Y; ++ii)
                    {
                        for (int jj = pixelRect.X; jj < pixelRect.Width+pixelRect.X; ++jj)
                        {
                            m_pixelMatrix[jj, ii] = 0;//the path should pass throught these point
                        }
                    }
                }
                return true;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
                return false;
            }
        }

        List<PathFinderNode> ExtractConnerNodes(List<PathFinderNode> originalPathNodes)
        {
            if (originalPathNodes.Count < 3)
                return originalPathNodes;
            List<PathFinderNode> cornnerNodes = new List<PathFinderNode>();
            cornnerNodes.Add(originalPathNodes[0]);
            for(int ii=1;ii<originalPathNodes.Count-1;++ii)
            {
                if((originalPathNodes[ii].Y==originalPathNodes[ii-1].Y &&originalPathNodes[ii].X==originalPathNodes[ii+1].X)||
                    (originalPathNodes[ii].X==originalPathNodes[ii-1].X && originalPathNodes[ii].Y==originalPathNodes[ii+1].Y))
                {
                    cornnerNodes.Add(originalPathNodes[ii]);
                }
            }
            cornnerNodes.Add(originalPathNodes[originalPathNodes.Count - 1]);
            return cornnerNodes;
        }

        List<UnitPoint> SnapToStartEndNodes(List<UnitPoint> allNodes,UnitPoint startPt,UnitPoint endPt)
        {
            //the first and last node don't equal the real start and end points.
            //we need to snap them here.
            System.Diagnostics.Debug.Assert(allNodes.Count >= 2);
            List<UnitPoint> modifiedNodes = new List<UnitPoint>();
            modifiedNodes.InsertRange(0,allNodes);
            if(allNodes.Count==2)
            {
                modifiedNodes[0] = startPt;
                modifiedNodes[1] = endPt;
                return modifiedNodes;
            }

            bool plot = false;
            if(plot)
            {
                DebugUtls.DrawPoints(_canvas, allNodes);
            }
            modifiedNodes[0] = startPt;
            //snap to start point
            UnitPoint tmpPoint = new UnitPoint();
            if (modifiedNodes[1].X == modifiedNodes[2].X)//constrain x
            {
                tmpPoint.X = modifiedNodes[1].X;
                tmpPoint.Y = startPt.Y;
                modifiedNodes[1] = tmpPoint;
            }
            else if (modifiedNodes[1].Y==modifiedNodes[2].Y)//constrain y
            {
                tmpPoint.X = startPt.X;
                tmpPoint.Y = modifiedNodes[1].Y;
                modifiedNodes[1] = tmpPoint;
            }
            //snap to end point
            int lastItemIndex = allNodes.Count - 1;
            modifiedNodes[lastItemIndex] = endPt;
            if(modifiedNodes[lastItemIndex-1].X==modifiedNodes[lastItemIndex-2].X)
            {
                tmpPoint.X = modifiedNodes[lastItemIndex - 1].X;
                tmpPoint.Y = endPt.Y;
                modifiedNodes[lastItemIndex-1] = tmpPoint;
            }
            else if (modifiedNodes[lastItemIndex-1].Y==modifiedNodes[lastItemIndex-2].Y)
            {
                tmpPoint.X = endPt.X;
                tmpPoint.Y = modifiedNodes[lastItemIndex - 1].Y;
                modifiedNodes[lastItemIndex - 1] = tmpPoint;
            }
            return modifiedNodes;
        }
    }
}
