using AStartPathFindAlgorithms;

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
        public AStartPathFinderWrapper(eAstartPathFinderType type)
        {
            if(type==eAstartPathFinderType.PathFinderFast)
            {
             //   mPathFinder = new PathFinderFast(PnlGUI.Matrix);

            }
            else if(type == eAstartPathFinderType.PathFinder)
            {
                 //   mPathFinder = new PathFinder(PnlGUI.Matrix);

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
        //public List<PathFinderNode> StartFind()
        //{
        //    List<PathFinderNode> path = mPathFinder.FindPath(PnlGUI.Start, PnlGUI.End);

        //}
    }
}
