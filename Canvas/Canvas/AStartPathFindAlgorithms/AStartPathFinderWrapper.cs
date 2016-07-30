using AStartPathFindAlgorithms;
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
        public AStartPathFinderWrapper(eAstartPathFinderType type,ICanvas canvas )
        {
            if(type==eAstartPathFinderType.PathFinderFast)
            {
                _pathFinder = new PathFinderFast(canvas.PixelMatrix);
            }
            else if(type == eAstartPathFinderType.PathFinder)
            {
                _pathFinder = new PathFinder(canvas.PixelMatrix);
            }
            _canvas = canvas;
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
        //void InitPixelMatrix()
        //{
        //    int nx = _canvas.ClientRectangle.Width;
        //    int ny = _canvas.ClientRectangle.Height;
        //    if (m_pixelMatrix == null)
        //        m_pixelMatrix = new byte[nx, ny];
        //    for (int ii = 0; ii < nx; ++ii)
        //    {
        //        for (int jj = 0; jj < ny; ++jj)
        //        {//init
        //            m_pixelMatrix[ii, jj] = 1;
        //        }
        //    }

        //    //List<IDrawObject> allObjs = _canvas.DataModel.GetHitObjects(_canvas.can, ScreenPixelRectToUnitRect(), false);
        //    //foreach (var obj in allObjs)
        //    //{
        //    //    DrawTools.RectBase rectBase = obj as DrawTools.RectBase;
        //    //    if (rectBase == null)
        //    //        continue;
        //    //    Rectangle pixelRect = ScreenUtils.ConvertRect(ScreenUtils.ToScreen(m_canvaswrapper, rectBase.GetBoundingRect(m_canvaswrapper)));
        //    //    for (int ii = pixelRect.Y; ii < pixelRect.Height + pixelRect.Y; ++ii)
        //    //    {
        //    //        for (int jj = pixelRect.X; jj < pixelRect.Width; ++jj)
        //    //        {
        //    //            m_pixelMatrix[jj, ii] = 5;
        //    //        }
        //    //    }
        //    //}
        //}
        public List<UnitPoint> FindPath(UnitPoint startPt, UnitPoint endPt)
        {
            Point pStart = ScreenUtils.ConvertPoint(_canvas.ToScreen(startPt));
            Point pEnd = ScreenUtils.ConvertPoint(_canvas.ToScreen(endPt));
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
            _pathFinder.FindPathStop();
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
