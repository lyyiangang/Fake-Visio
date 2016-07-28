using AStartPathFindAlgorithms;
using System.Collections.Generic;

namespace Canvas.AStartPathFindAlgorithms
{
    public enum eAstartPathFinderType
    {
        PathFinderFast,
        PathFinder,
    };
    class AStartPathFinderWrapper
    {
        private IPathFinder mPathFinder = null;
        public AStartPathFinderWrapper(eAstartPathFinderType type, byte[,] grid)
        {
            if(type==eAstartPathFinderType.PathFinderFast)
            {
                mPathFinder = new PathFinderFast(grid);

            }
            else if(type == eAstartPathFinderType.PathFinder)
            {
                mPathFinder = new PathFinder(grid);
            }
            mPathFinder.Formula = HeuristicFormula.Euclidean;
            mPathFinder.Diagonals = false;
            mPathFinder.HeavyDiagonals = false;
            mPathFinder.HeuristicEstimate = 2;
            mPathFinder.PunishChangeDirection = true;
            mPathFinder.TieBreaker = true;
            mPathFinder.SearchLimit = 50000;
            mPathFinder.DebugProgress = true;
            mPathFinder.ReopenCloseNodes = false;
            mPathFinder.DebugFoundPath = true;
        }
        public List<UnitPoint> FindPath(System.Drawing.Point startPt,System.Drawing.Point endPt,ICanvas canvas)
        {
            List<PathFinderNode> path = mPathFinder.FindPath(startPt,endPt);
            List<UnitPoint> allPts = new List<UnitPoint>();
            foreach(var tmpNode in path)
            {
                System.Drawing.PointF pt = new System.Drawing.PointF(tmpNode.X, tmpNode.Y);
                allPts.Add(canvas.ToUnit(pt));
            }
            return allPts;
        }
    }
}
