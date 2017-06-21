using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.Structure;
using System.Drawing;

namespace EmugCVSandbox
{
    public class DefectInfo
    {
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public int FarIndex { get; set; }
        public double Depth { get; set; }

        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }
        public Point FarPoint { get; set; }

        public double Gap
        {
            get
            {
                if (StartPoint != null && EndPoint != null) return ImageProcUtils.DistanceBetweenTwoPoints(StartPoint, EndPoint);
                else return double.NaN;
            }
        }

        public override string ToString()
        {
            return "[" + StartIndex.ToString() + ", " + EndIndex.ToString() + ", " + FarIndex.ToString() + ", " + Depth.ToString("0.00") + "]";
        }
    }

    /// <summary>
    /// Performs common contour ba methods.
    /// </summary>
    public class ContourAnalysis
    {

        private Dictionary<int, List<Point>> contours = new Dictionary<int, List<Point>>();
        private Dictionary<int, List<int>> convexHulls = new Dictionary<int, List<int>>();
        private Dictionary<int, List<DefectInfo>> defects = new Dictionary<int, List<DefectInfo>>();

        public Dictionary<int, List<Point>> Contours { get { return contours; } }
        public Dictionary<int, List<int>> ConvexHulls { get { return convexHulls; } }
        public Dictionary<int, List<DefectInfo>> Defects { get { return defects; } }

        //public Dictionary<int, List<Point>> GetConvexPoints()
        //{
        //    var points = new Dictionary<int, List<Point>>();
        //    foreach(int index in ConvexHulls.Keys)
        //    {
        //        List<Point> ptList = new List<Point>();
        //        foreach(int ptIdx in ConvexHulls[index])
        //        {
        //            ptList.Add(Contours[index][ptIdx]);
        //        }
        //        points[index] = (ptList);
        //    }
        //    return points;
        //}

        //public List<Point> GetConvexPoints(int contour_index)
        //{
        //    return GetConvexPoints()[contour_index];
        //}

        /// <summary>
        /// Creates new instance of ContourAnalysis with a white-object, black-background image.
        /// </summary>
        /// <param name="whiteBlackImage">White objects, black background image.</param>
        public ContourAnalysis(Image<Gray, byte> whiteBlackImage, double contour_approximation_epsilon = 1.0)
        {
            GenerateInfo(whiteBlackImage, contour_approximation_epsilon);
        }

        private void GenerateInfo(Image<Gray, byte> input, double contour_approximation_epsilon)
        {
            using (Image<Gray, byte> image = input.Copy())
            {
                VectorOfVectorOfPoint contoursVector = new VectorOfVectorOfPoint();
                var heirarchy = new Mat();
                CvInvoke.FindContours(image, contoursVector, heirarchy, Emgu.CV.CvEnum.RetrType.External, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);
                for (int i = 0; i < contoursVector.Size; i++)
                {
                    // Add contour
                    VectorOfPoint orig = contoursVector[i];
                    VectorOfPoint contour = new VectorOfPoint();
                    CvInvoke.ApproxPolyDP(orig, contour, contour_approximation_epsilon, true);
                    contours[i] = contour.ToArray().ToList();

                    // Get and add convex contour
                    VectorOfInt convex = new VectorOfInt();
                    CvInvoke.ConvexHull(contour, convex, true);
                    List<int> convexList = convex.ToArray().ToList();
                    // Sort ascending
                    convexList.Sort();
                    convex = new VectorOfInt(convexList.ToArray());
                    convexHulls[i] = convexList;

                    // Get and add convexity defects
                    Mat defectsMat = new Mat();
                    CvInvoke.ConvexityDefects(contour, convex, defectsMat);

                    List<DefectInfo> defectList = new List<DefectInfo>();
                    if (!defectsMat.IsEmpty)
                    {
                        Matrix<int> m = new Matrix<int>(defectsMat.Rows, defectsMat.Cols,
                                   defectsMat.NumberOfChannels);
                        defectsMat.CopyTo(m);

                        for (int j = 0; j < m.Rows; j++)
                        {
                            int startIdx = m.Data[j, 0];
                            int endIdx = m.Data[j, 1];
                            int farIdx = m.Data[j, 2];
                            double depth = m.Data[j, 3] / 256.0;

                            //if (depth < 200)
                            //{
                            Point startPoint = Contours[i][startIdx];
                            Point endPoint = Contours[i][endIdx];
                            Point farPoint = Contours[i][farIdx];

                            var newDefect = new DefectInfo
                            {
                                StartIndex = startIdx,
                                EndIndex = endIdx,
                                FarIndex = farIdx,
                                Depth = depth,
                                StartPoint = startPoint,
                                EndPoint = endPoint,
                                FarPoint = farPoint
                            };
                            defectList.Add(newDefect);
                            //}
                        }
                    }
                    defects[i] = defectList;
                }
            }
        }

        public VectorOfVectorOfPoint GetContoursVector()
        {
            VectorOfVectorOfPoint rtn = new VectorOfVectorOfPoint(Contours.Count);
            for (int i = 0; i < Contours.Count; i++)
            {
                rtn.Push(new VectorOfPoint(Contours[i].ToArray()));
            }
            return rtn;
        }

        public VectorOfVectorOfInt GetConvexHullsVector()
        {
            VectorOfVectorOfInt rtn = new VectorOfVectorOfInt(ConvexHulls.Count);
            for (int i = 0; i < ConvexHulls.Count; i++)
            {
                rtn.Push(new VectorOfInt(ConvexHulls[i].ToArray()));
            }
            return rtn;
        }

        public Dictionary<int, List<Point>> GetConvexHullContours()
        {
            var rtn = new Dictionary<int, List<Point>>();
            foreach (var hull in convexHulls)
            {
                rtn[hull.Key] = new List<Point>();
                foreach (var i in hull.Value)
                {
                    rtn[hull.Key].Add(Contours[hull.Key][i]);
                }
            }
            return rtn;
        }

        public List<Point> FillConvexDefects(int contourIdx, Func<DefectInfo, bool> lambda)
        {
            if (contourIdx >= Contours.Count)
            {
                return null;
            }

            List<int> rtnIndexes = new List<int>();
            for (int i = 0; i < Contours[contourIdx].Count; i++)
            {
                rtnIndexes.Add(i);
            }
            int maxIdx = rtnIndexes.Max();
            List<Point> ptList = new List<Point>();

            List<Point> contour = Contours[contourIdx];
            List<int> convexIndexes = ConvexHulls[contourIdx];
            List<DefectInfo> defects = Defects[contourIdx];

            List<DefectInfo> matches = defects.Where(lambda).ToList();
            //CTrace.WriteLine("Defect matches found: " + matches.Count.ToString());

            foreach (var defect in matches)
            {
                //CTrace.WriteLine("Defect Match: D = " + defect.Depth.ToString("0.00") + ", G = " + defect.Gap.ToString("0.00"));
                //CTrace.WriteLine(defect.ToString());
                int startIdx = defect.StartIndex;
                int endIdx = defect.EndIndex;

                int delta = endIdx - startIdx;

                if (delta >= 0)
                {
                    for (int i = startIdx + 1; i < endIdx; i++)
                    {
                        rtnIndexes.Remove(i);
                    }
                }
                else
                {
                    for (int i = startIdx + 1; i <= maxIdx; i++)
                    {
                        rtnIndexes.Remove(i);
                    }
                    for (int i = 0; i < endIdx; i++)
                    {
                        rtnIndexes.Remove(i);
                    }
                }
            }

            foreach (int i in rtnIndexes)
            {
                ptList.Add(Contours[contourIdx][i]);
            }
            return ptList;
        }
    }
}
