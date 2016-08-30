using AStartPathFindAlgorithms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

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
        public AStartPathFinderWrapper(ICanvas canvas)
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
            _pathFinder.HeuristicEstimate = 4;
            _pathFinder.PunishChangeDirection = true;//only four direction is admitted
            _pathFinder.TieBreaker = true;
            _pathFinder.SearchLimit = 5000000;//important parm
            _pathFinder.DebugProgress = false;
            _pathFinder.ReopenCloseNodes = false;
            _pathFinder.DebugFoundPath = true;
        }
        bool EndPointIsValid(Point endPt)
        {
            if (endPt.IsEmpty)
                return false;

            if (endPt.X < 0 || endPt.X > m_pixelMatrix.GetUpperBound(0) || m_pixelMatrix[endPt.X, endPt.Y] == 0)
                return false;
            return true;
        }
        public List<UnitPoint> FindPath(UnitPoint startPt, UnitPoint endPt, bool passEndPoint = false)
        {
            if (_pathFinder == null)
                return null;
            Point pStart = ScreenUtils.ConvertPoint(_canvas.ToScreen(startPt));
            Point pEnd = ScreenUtils.ConvertPoint(_canvas.ToScreen(endPt));
            if (pStart == pEnd || !EndPointIsValid(pStart) || !EndPointIsValid(pEnd))
                return null;
            //if the canvas control'size has changed, we need to update the matrix
            if (NeedReconstructMatrix())
            {
                m_pixelMatrix = null;
                Init();
            }
            //-----------------------------------------------------------------------
            #region debug
            bool export = false;
            if (export)
            {
                int nItems = m_pixelMatrix.GetUpperBound(0) + 1;
                int maxX = -1, maxY = -1, minX = 100000, minY = 100000;
                for (int ii = 0; ii < nItems; ++ii)//row
                {
                    for (int jj = 0; jj < nItems; ++jj)//column
                    {
                        int val = m_pixelMatrix[jj, ii];
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
                PointF tmpPt = new PointF(minX, minY);
                UnitPoint pt1 = _canvas.ToUnit(tmpPt);
                tmpPt = new PointF(maxX, maxY);
                UnitPoint pt2 = _canvas.ToUnit(tmpPt);

                for (int ii = 0; ii < nItems; ++ii)
                {
                    for (int jj = 0; jj < nItems; ++jj)
                    {
                        int val = m_pixelMatrix[jj, ii];
                        Console.Write("{0},", val);
                    }
                    Console.Write("\n");
                }
            }
            #endregion
            //-----------------------------------------------------------------------
            _pathFinder.FindPathStop();
            List<PathFinderNode> path = _pathFinder.FindPath(pStart, pEnd);
            if (path == null || path.Count < 1)
                return null;
            List<PathFinderNode> cornerNodesPath = ExtractConnerNodes(path);
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
                        ptLeftBottom.X = (float)convertedPt.X;
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
            //List<UnitPoint> snapedPts = allPts;
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
            return new RectangleF(bottomRightPos.Point.X - (float)width, bottomRightPos.Point.Y, (float)width, (float)height);
        }

        bool NeedReconstructMatrix()
        {
            if (_canvas is CanvasWrapper)
            {
                int curSize = m_pixelMatrix.GetUpperBound(0) + 1;
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
                bool exportInfo = false;

                List<IDrawObject> allObjs = _canvas.DataModel.GetHitObjects(_canvas, ScreenPixelRectToUnitRect(), false);
                foreach (var obj in allObjs)
                {
                    DrawTools.RectBase rectBase = obj as DrawTools.RectBase;
                    if (rectBase == null)
                        continue;
                    Rectangle pixelRect = ScreenUtils.ConvertRect(ScreenUtils.ToScreenNormalized(_canvas, rectBase.GetExactBoundingRect(_canvas)));
                    pixelRect.Inflate(-1, -1);//make sure the point can lay down on the boundary of rectangle
                    PopulateValToMatrix(m_pixelMatrix, pixelRect);
                    //for (int ii = pixelRect.Top; ii < pixelRect.Bottom; ++ii)
                    //{
                    //    for (int jj = pixelRect.Left; jj < pixelRect.Right; ++jj)
                    //    {
                    //        m_pixelMatrix[jj, ii] = 0;//the path should not pass throught these point
                    //    }
                    //}

                    //const int threshold = 10;
                    //int[] leftXInterval = { pixelRect.Left, pixelRect.Left - threshold };
                    //int[] rightXInterval = { pixelRect.Right, pixelRect.Right + threshold };
                    //int[] topAndBottomXInterval = { pixelRect.Left, pixelRect.Right };

                    //int[] leftAndRightYInterval = { pixelRect.Top, pixelRect.Bottom };
                    //int[] topYInterval = { pixelRect.Top, pixelRect.Top - threshold };
                    //int[] bottomYInterval = { pixelRect.Bottom, pixelRect.Bottom + threshold };

                    //PopulateLinearBoundary(leftXInterval[0], leftXInterval[1], leftAndRightYInterval[0], leftAndRightYInterval[1], true);
                    //PopulateLinearBoundary(rightXInterval[0], rightXInterval[1], leftAndRightYInterval[0], leftAndRightYInterval[1], true);
                    //PopulateLinearBoundary(topYInterval[0], topYInterval[1], topAndBottomXInterval[0], topAndBottomXInterval[1], false);
                    //PopulateLinearBoundary(bottomYInterval[0], bottomYInterval[1], topAndBottomXInterval[0], topAndBottomXInterval[1], false);
                    //---------------------------------------------------
                    int nItems = m_pixelMatrix.GetUpperBound(0) + 1;
                    int maxX = -1, maxY = -1, minX = 100000, minY = 100000;

                    for (int ii = 0; ii < nItems && exportInfo; ++ii)//row
                    {
                        for (int jj = 0; jj < nItems; ++jj)//column
                        {
                            int val = m_pixelMatrix[jj, ii];
                            // Console.Write("{0} ", val);
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
                        //  Console.Write("\n");
                    }
                    //---------------------------------------------------

                }
                return true;
            }
            else
            {
                System.Diagnostics.Debug.Assert(false);
                return false;
            }
        }
        delegate int CalculateValFromDistance(int dist);
        void PopulateValToMatrix(byte[,] matrix, Rectangle rect)
        {
            int nRows = matrix.GetUpperBound(0) + 1;
            int nColms = matrix.GetUpperBound(1) + 1;
            for (int iRow = rect.Top; iRow <= rect.Bottom; ++iRow)
            {
                for (int iColm = rect.Left; iColm <= rect.Right; ++iColm)
                {
                    matrix[iColm, iRow] = 0;
                }
            }
            //return;
            CalculateValFromDistance calcVal = dist =>
            {
                System.Diagnostics.Debug.Assert(dist >= 0);
                int val = 50 - dist * 5;
                System.Diagnostics.Debug.Assert(val >= 0);
                return val;
            };
            const int maxGradientLayers = 5;
            for (int iRow = rect.Top; iRow < rect.Bottom; ++iRow)
            {
                //to left
                int countGradientLayers = 0;
                for (int iColm = rect.Left; iColm >= 0 && countGradientLayers < maxGradientLayers;
                    --iColm)
                {
                    int dist = rect.Left - iColm;
                    matrix[iColm, iRow] = (byte)calcVal(dist);
                    ++countGradientLayers;
                }
                //to right
                countGradientLayers = 0;
                for (int iColm = rect.Right; iColm <= nColms - 1 && countGradientLayers < maxGradientLayers;
                    ++iColm)
                {
                    int dist = iColm - rect.Right;
                    matrix[iColm, iRow] = (byte)calcVal(dist);
                    ++countGradientLayers;
                }
            }

            for (int iColm = rect.Left; iColm <= rect.Right; ++iColm)
            {
                //top
                int countGradientLayers = 0;
                for (int iRow = rect.Top; iRow > 0 && countGradientLayers < maxGradientLayers;
                    --iRow)
                {
                    int dist = rect.Top - iRow;
                    matrix[iColm, iRow] = (byte)calcVal(dist);
                    ++countGradientLayers;
                }
                //bottom
                countGradientLayers = 0;
                for (int iRow = rect.Bottom; iRow <= nRows - 1 && countGradientLayers < maxGradientLayers;
                    ++iRow)
                {
                    int dist = iRow - rect.Bottom;
                    matrix[iColm, iRow] = (byte)calcVal(dist);
                    ++countGradientLayers;
                }
            }
            //for(int iRows=0;iRows<nRows;++iRows)
            //{
            //    for(int iColms=0;iColms<nColms;++iColms)
            //    {
            //        System.Console.Write(matrix[iRows, iColms] + " ");
            //    }
            //    System.Console.Write("\n");
            //}
        }
        void PopulateLinearBoundary(int linearStart, int linearEnd, int constStart, int constEnd, bool linearX)
        {
            int tmpLinearStart, tmpLinearEnd, populateVal, linearDelta;
            const int maxPopulateVal = 80;
            if (linearStart < linearEnd)
            {
                tmpLinearStart = linearStart;
                tmpLinearEnd = linearEnd;
                populateVal = maxPopulateVal;
                linearDelta = -5;
            }
            else
            {
                tmpLinearStart = linearEnd;
                tmpLinearEnd = linearStart;
                populateVal = maxPopulateVal - (tmpLinearEnd - tmpLinearStart) * 5;
                linearDelta = 5;
            }

            for (int jj = constStart; jj < constEnd; ++jj)
            {
                int val = populateVal;
                for (int ii = tmpLinearStart; ii < tmpLinearEnd; ii++)
                {
                    System.Diagnostics.Debug.Assert(val > 0);
                    if (linearX)
                        m_pixelMatrix[ii, jj] = (byte)val;
                    else
                        m_pixelMatrix[jj, ii] = (byte)val;
                    val += linearDelta;
                }
            }
        }

        List<PathFinderNode> ExtractConnerNodes(List<PathFinderNode> originalPathNodes)
        {
            if (originalPathNodes.Count < 3)
                return originalPathNodes;

            {
                //validate the node, make sure the node can be walked thourgh.

                for (int ii = 0; ii < originalPathNodes.Count; ++ii)
                {
                    bool wrongPos = m_pixelMatrix[originalPathNodes[ii].X, originalPathNodes[ii].Y] == 0;
                    if (wrongPos)
                    {
                        int aa = 0;
                    }
                }
            }
            List<PathFinderNode> cornnerNodes = new List<PathFinderNode>();
            cornnerNodes.Add(originalPathNodes[0]);
            for (int ii = 1; ii < originalPathNodes.Count - 1; ++ii)
            {
                if ((originalPathNodes[ii].Y == originalPathNodes[ii - 1].Y && originalPathNodes[ii].X == originalPathNodes[ii + 1].X) ||
                    (originalPathNodes[ii].X == originalPathNodes[ii - 1].X && originalPathNodes[ii].Y == originalPathNodes[ii + 1].Y))
                {
                    cornnerNodes.Add(originalPathNodes[ii]);
                }
            }
            cornnerNodes.Add(originalPathNodes[originalPathNodes.Count - 1]);
            return cornnerNodes;
        }
        List<UnitPoint> SnapToStartEndNodes(List<UnitPoint> allNodes, UnitPoint ptStart, UnitPoint ptEnd)
        {
            //the first and last node don't equal the real start and end points.
            //we need to snap them here.
            System.Diagnostics.Debug.Assert(allNodes.Count >= 2);
            List<UnitPoint> modifiedNodes = new List<UnitPoint>();
            modifiedNodes.InsertRange(0, allNodes);
            if (allNodes.Count == 2)
            {
                modifiedNodes[0] = ptEnd;
                modifiedNodes[1] = ptStart;
                return modifiedNodes;
            }

            modifiedNodes[0] = ptEnd;
            //snap to start point
            UnitPoint tmpPoint = new UnitPoint();
            if (modifiedNodes[1].X == modifiedNodes[2].X)//constrain x
            {
                tmpPoint.X = modifiedNodes[1].X;
                tmpPoint.Y = ptEnd.Y;
                modifiedNodes[1] = tmpPoint;
            }
            else if (modifiedNodes[1].Y == modifiedNodes[2].Y)//constrain y
            {
                tmpPoint.X = ptEnd.X;
                tmpPoint.Y = modifiedNodes[1].Y;
                modifiedNodes[1] = tmpPoint;
            }
            //snap to end point
            int lastItemIndex = allNodes.Count - 1;
            modifiedNodes[lastItemIndex] = ptStart;
            tmpPoint = new UnitPoint();
            if (modifiedNodes[lastItemIndex - 1].X == modifiedNodes[lastItemIndex - 2].X)
            {
                tmpPoint.X = modifiedNodes[lastItemIndex - 1].X;
                tmpPoint.Y = ptStart.Y;
                modifiedNodes[lastItemIndex - 1] = tmpPoint;
            }
            else if (modifiedNodes[lastItemIndex - 1].Y == modifiedNodes[lastItemIndex - 2].Y)
            {
                tmpPoint.X = ptStart.X;
                tmpPoint.Y = modifiedNodes[lastItemIndex - 1].Y;
                modifiedNodes[lastItemIndex - 1] = tmpPoint;
            }
            return modifiedNodes;
        }
    }
}
