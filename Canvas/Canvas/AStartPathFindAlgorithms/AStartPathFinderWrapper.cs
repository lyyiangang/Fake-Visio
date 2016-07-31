using AStartPathFindAlgorithms;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Canvas.AStartPathFindAlgorithms
{
    public enum eAstartPathFinderType
    {
        PathFinderFast,
        PathFinder,
    };
    class AStartPathFinderWrapper
    {
        private IPathFinder _pathFinder = null;
        ICanvas _canvas = null;
        byte[,] m_pixelMatrix = null;
        public AStartPathFinderWrapper(/*eAstartPathFinderType type,*/ICanvas canvas )
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
        public List<UnitPoint> FindPath(UnitPoint startPt, UnitPoint endPt)
        {
            if (_pathFinder == null)
                return null;
            if (NeedReconstructMatrix())
            {
                m_pixelMatrix = null;
                Init();
            }
            Point pStart = ScreenUtils.ConvertPoint(_canvas.ToScreen(startPt));
            Point pEnd = ScreenUtils.ConvertPoint(_canvas.ToScreen(endPt));
            if (pStart == pEnd)
                return null;
            _pathFinder.FindPathStop();
            List<PathFinderNode> path = _pathFinder.FindPath(pStart, pEnd);
            if (path == null || path.Count < 1)
                return null;
            List<PathFinderNode> cornerNodesPath= ExtractConnerNodes(path);
            List<UnitPoint> allPts = new List<UnitPoint>();
            foreach(var tmpNode in cornerNodesPath)
            {
                PointF pt = new PointF(tmpNode.X, tmpNode.Y);
                allPts.Add(_canvas.ToUnit(pt));
            }
            return allPts;
        }

        public void StopFind()
        {
            if (_pathFinder == null)
                return;
            _pathFinder.FindPathStop();
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
                        m_pixelMatrix[ii, jj] = 1;
                    }
                }
                List<IDrawObject> allObjs = _canvas.DataModel.GetHitObjects(_canvas, ScreenPixelRectToUnitRect(), false);
                foreach (var obj in allObjs)
                {
                    DrawTools.RectBase rectBase = obj as DrawTools.RectBase;
                    if (rectBase == null)
                        continue;
                    Rectangle pixelRect = ScreenUtils.ConvertRect(ScreenUtils.ToScreenNormalized(_canvas, rectBase.GetBoundingRect(_canvas)));
                    for (int ii = pixelRect.Y; ii < pixelRect.Height + pixelRect.Y; ++ii)
                    {
                        for (int jj = pixelRect.X; jj < pixelRect.Width+pixelRect.X; ++jj)
                        {
                            m_pixelMatrix[jj, ii] = 0;
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
    }
}
