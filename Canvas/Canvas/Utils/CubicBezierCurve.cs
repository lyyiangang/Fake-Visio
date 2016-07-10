using System.Diagnostics;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System;
using System.Drawing;

namespace Canvas
{
    class CubicBezierCurveCurve
    {
        float[] _xPos;
        float[] _yPos;
        //interval=[0.0f,1.0f]
        public CubicBezierCurveCurve(float[] ctrlPt1,float[] ctrlPt2,float[] ctrlPt3,float[] ctrlPt4)
        {
            _xPos=new float[4]{ctrlPt1[0],ctrlPt2[0],ctrlPt3[0],ctrlPt4[0]};
            _yPos=new float[4]{ctrlPt1[1],ctrlPt2[1],ctrlPt3[1],ctrlPt4[1]};
        }
        public int GetNCtrlPts()
        {
            return _xPos.Length;
        }
        public void SetPos(int index, float xx, float yy)
        {
            Debug.Assert(index < GetNCtrlPts() && index > -1);
            _xPos[index] = xx;
            _yPos[index] = yy;
        }
        //get the postion at parameter t
        public void Eval(float tt,ref float xx, ref float yy)
        {
            float[] xPos = { 0.0f};
            float[] yPos = { 0.0f };
            Eval(tt, 0, ref xPos, ref yPos);
            xx = xPos[0];
            yy = yPos[0];
        }
        //left bottom point
        public RectangleF GetBoundingBox()
        {
            float[] minXY = { _xPos[0],_yPos[0]};
            float[] maxXY = { _xPos[0],_yPos[0]};
            for(int ii=1;ii<_xPos.Length;++ii)
            {
                minXY[0] = Math.Min(_xPos[ii], minXY[0]);
                minXY[1] = Math.Min(_yPos[ii], minXY[1]);

                maxXY[0] = Math.Max(_xPos[ii], maxXY[0]);
                maxXY[1] = Math.Max(_yPos[ii], maxXY[1]);
            }
            float[] leftBottomPt = { minXY[0], minXY[1] };
            return new RectangleF(leftBottomPt[0], leftBottomPt[1], maxXY[0] - minXY[0], maxXY[1] - minXY[1]);
        }

        public bool PointOnCurve(float[] P,float epsilon)
        {
            float param = 0.0f, minSquareDistance = 0.0f;
            float[] closestPos = { 0.0f, 0.0f };
            ProjectPointToCurve(P, ref param, ref closestPos, ref minSquareDistance);
            return Math.Sqrt(minSquareDistance) < epsilon;
        }

        //f=Qt* QP
        //f'=Qtt*Qp +Qt*Qt
        //object function: min(QP* QP)
        //Q is the point on curve,the parameter is t
        //P is the test point
        public void ProjectPointToCurve(float[] P, ref float closestParam, ref float[] closestPos, ref float minSquareDistance)
        {
            const int nSegments = 4;
            const float epsilon = 1.0e-5f;
            float span = 1.0f / nSegments;
            float[] xDerives = new float[3];
            float[] yDerives = new float[3];
            List<float> allConvergenceParm = new List<float>();
            for (int ii = 0; ii < nSegments; ++ii)
            {
                float tt = 0.5f * span + ii * span;
                const int iterationTimes = 10;
                bool convergence = false;
                for (int jj = 0; jj < iterationTimes; ++jj)
                {
                    Eval(tt, 2, ref xDerives, ref yDerives);
                    float[] Qt = { xDerives[1], yDerives[1] };
                    float[] QP = { xDerives[0] - P[0], yDerives[0] - P[1] };
                    float fVal = Dot(Qt, QP);
                    float[] Qtt = { xDerives[2], yDerives[2] };
                    float fValDerives = Dot(Qtt, QP) + Dot(Qt, Qt);
                    float ttNext = tt - fVal / fValDerives;
                    convergence = System.Math.Abs(ttNext - tt) < epsilon;
                    tt = ttNext;
                    if (tt < 0.0f)
                        tt = 0.0f;
                    else if (tt > 1.0f)
                        tt = 1.0f;
                    if (convergence)
                    {
                        if (!ValueExistInList(allConvergenceParm, tt, epsilon))
                            allConvergenceParm.Add(tt);
                        break;
                    }
                }
            }
            //test wheter the start and end is the closest point
            if (allConvergenceParm.Count < 1)
            {
                allConvergenceParm.Add(0.0f);
                allConvergenceParm.Add(1.0f);
            }
            minSquareDistance = float.MaxValue;
            closestParam = 0.0f;
            foreach (var curParm in allConvergenceParm)
            {
                float[] Q = { 0.0f, 0.0f };
                Eval(curParm, ref Q[0], ref Q[1]);
                float[] QP = { Q[0] - P[0], Q[1] - P[1] };
                float tmpDistance = Dot(QP, QP);
                if (tmpDistance < minSquareDistance)
                {
                    minSquareDistance = tmpDistance;
                    closestParam = curParm;
                    closestPos[0] = Q[0];
                    closestPos[1] = Q[1];
                }
            }
        }

        public List<PointF> SamplePoints(int nPts)
        {
            Debug.Assert(nPts > 2);
            List<PointF> allPts = new List<PointF>();
            float stepSize = 1.0f / (nPts - 1);
            for(int ii=0;ii< nPts;++ii)
            {
                float tt = stepSize * ii;
                if (ii == nPts - 1)
                    tt = 1.0f;
                float[] pos = { 0.0f, 0.0f };
                Eval(tt, ref pos[0], ref pos[1]);
                PointF pt = new PointF(pos[0],pos[1]);
                allPts.Add(pt);
            }
            return allPts;
        }
        void Eval(float tt,int order,ref float[] xx,ref float[] yy)
        {
            Debug.Assert(order == 0 || order == 1 || order == 2 && xx.Length==yy.Length);
            if (tt > 1.0f)
                tt = 1.0f;
            else if (tt < 0.0f)
                tt = 0.0f;
            //https://en.wikipedia.org/wiki/B%C3%A9zier_curve
            float tTmp = 1 - tt;
            if(order>=0)
            {
                Debug.Assert(xx.Length >= 1);
                float[] tVec = { tTmp * tTmp * tTmp, 3 * tTmp * tTmp * tt, 3 * tTmp * tt * tt, tt * tt * tt };
                xx[0] = Dot(tVec, _xPos);
                yy[0] = Dot(tVec, _yPos);
            }
            if(order>=1)
            {
                Debug.Assert(xx.Length >= 2);
                float[] tVec = { 3 * tTmp * tTmp, 6 * tTmp * tt, 3 * tt * tt };
                float[] tmpX = { _xPos[1] - _xPos[0], _xPos[2] - _xPos[1], _xPos[3] - _xPos[2] };
                float[] tmpY = { _yPos[1] - _yPos[0], _yPos[2] - _yPos[1], _yPos[3] - _yPos[2] };
                xx[1] = Dot(tVec, tmpX);
                yy[1] = Dot(tVec, tmpY);
            }
            if (order==2)
            {
                Debug.Assert(xx.Length >= 3);
                float[] tVec = { 6 * tTmp, 6 * tt, 0.0f };
                float[] tmpX = { _xPos[2] - 2 * _xPos[1] + _xPos[0], _xPos[3] - 2 * _xPos[2] + _xPos[1],0.0f };
                float[] tmpY = { _yPos[2] - 2 * _yPos[1] + _yPos[0], _yPos[3] - 2 * _yPos[2] + _yPos[1],0.0f };
                xx[2] = Dot(tVec, tmpX);
                yy[2] = Dot(tVec, tmpY);
            }
        }

        float Dot(float[] vec1, float[] vec2)
        {
            Debug.Assert(vec1.Length == vec2.Length && vec1.Length>0);
            float val = 0.0f;
            for(int ii=0;ii<vec1.Length;++ii)
            {
                val += vec1[ii] * vec2[ii];
            }
            return val;
        }

        bool ValueExistInList(IEnumerable<float> arr,float val,float epsilon)
        {
            foreach(var item in arr)
            {
                if (System.Math.Abs(item - val) < epsilon)
                    return true;
            }
            return false;
        }
    }
}
