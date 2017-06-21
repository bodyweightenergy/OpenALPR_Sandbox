using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.ML;
using Emgu.Util;
using Emgu.CV.Features2D;
using Emgu.CV.Cvb;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using System.Drawing;
using System.Diagnostics;

namespace EmugCVSandbox
{
    public class BlobAnalysis
    {
        public Image<Gray, byte> ContainerImage { get; set; }
        public CvBlobDetector Detector { get; set; }
        public CvBlobs Blobs { get; set; }
        
        public BlobAnalysis()
        {

        }

        public BlobAnalysis(Image<Gray, byte> containerImage) : this()
        {
            ContainerImage = containerImage.Copy();
            Detector = new CvBlobDetector();
            Blobs = new CvBlobs();
            Detector.Detect(ContainerImage, Blobs);
        }

        /// <summary>
        /// Calculates the perimeter length of the specified blob.
        /// </summary>
        /// <param name="blob">Blob descriptor whose perimeter is to be measured.</param>
        /// <returns>Perimeter as double.</returns>
        /// <remarks>This function assumes the newImg is already in binary form.</remarks>
        public double Perimeter(CvBlob blob)
        {
            if (blob != null)
            {
                Point[] contour = GetContourSimple(blob);
                if (contour != null)
                {
                    if (contour.Length > 0)
                    {
                        VectorOfPoint contourVector = new VectorOfPoint(contour);
                        double perimeter = CvInvoke.ArcLength(contourVector, true);
                        return perimeter;
                    }
                }
            }
            return 0.0;
        }

        /// <summary>
        /// Calculates the area of the specified blob.
        /// </summary>
        /// <param name="blob">Blob descriptor whose area is to be measured.</param>
        /// <returns>Area as double.</returns>
        /// <remarks>This function assumes the newImg is already in binary form.</remarks>
        public double Area(CvBlob blob)
        {
            if (blob != null)
            {
                Point[] contour = GetContourSimple(blob);
                if (contour != null)
                {
                    if (contour.Length > 0)
                    {
                        //VectorOfPoint contourVector = new VectorOfPoint(contour);
                        //double area = CvInvoke.ContourArea((IInputArray)contourVector);
                        double area = blob.Area;
                        return area;
                    }
                }
            }
            return 0.0;
        }

        /// <summary>
        /// Calculates the circularity factor of the specified blob.
        /// </summary>
        /// <param name="blob">Blob descriptor whose area is to be measured.</param>
        /// <returns>Circularity Factor as double.</returns>
        /// <remarks>This function assumes the newImg is already in binary form.</remarks>
        public double CircularityFactor(CvBlob blob)
        {
            if (blob != null)
            {
                double perimeter = Perimeter(blob);
                double area = Area(blob);

                double cir_factor = (4.0 * Math.PI * area) / Math.Pow(perimeter, 2);
                return cir_factor;
            }
            return 0.0;
        }

        /// <summary>
        /// Gets the points that make the convex hull of the blob.
        /// </summary>
        /// <param name="blob">Blob descriptor</param>
        /// <returns>Array of Points making the Convex Hull contour</returns>
        public Point[] ConvexHull(CvBlob blob)
        {
            if (blob != null)
            {
                PointF[] hull = ConvexHullF(blob);
                if (hull != null)
                {
                    if (hull.Length > 0)
                    {
                        Point[] contour = Array.ConvertAll<PointF, Point>(hull, new Converter<PointF, Point>(x => new Point(Convert.ToInt32(x.X), Convert.ToInt32(x.Y))));
                        return contour;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the points (float) that make the convex hull of the blob.
        /// </summary>
        /// <param name="blob">Blob descriptor</param>
        /// <returns>Array of PointF making the Convex Hull contour</returns>
        public PointF[] ConvexHullF(CvBlob blob)
        {
            if (blob != null)
            {
                Point[] contour = GetContourSimple(blob);
                if (contour != null)
                {
                    if (contour.Length > 0)
                    {
                        //Point[] contour = GetContour(blob);
                        PointF[] contourF = Array.ConvertAll<Point, PointF>(contour, new Converter<Point, PointF>(pt => new PointF(pt.X, pt.Y)));
                        PointF[] hull = CvInvoke.ConvexHull(contourF, true);
                        return hull;
                    }
                }
            }
            return null;
        }

        public RotatedRect GetMinimumAreaRectangle(CvBlob blob)
        {
            if (blob != null)
            {
                Point[] contour = GetContourSimple(blob);
                if (contour != null)
                {
                    if (contour.Length > 0)
                    {
                        VectorOfPoint vec = new VectorOfPoint(contour);
                        return CvInvoke.MinAreaRect(vec);
                    }
                }
            }
            return RotatedRect.Empty;
        }

        public CvBlob ConvexHullBlob(CvBlob blob, out Image<Gray, byte> containerImage, out CvBlobs blobsOut)
        {
            if (blob != null)
            {
                CvBlobDetector detector = new CvBlobDetector();
                CvBlobs blobs = new CvBlobs();
                Point[] hull = ConvexHull(blob);
                if (hull != null)
                {
                    if (hull.Length > 0)
                    {
                        containerImage = GetContourImage(hull);
                        detector.Detect(containerImage, blobs);
                        blobsOut = blobs;
                        if (blobs.Count > 0)
                        {
                            return blobs.Values.ToList()[0];
                        }
                    }
                }
            }
            blobsOut = null;
            containerImage = null;
            return null;
        }

        public double AspectFactor(CvBlob blob)
        {
            if (blob != null)
            {
                Rectangle box = blob.BoundingBox;
                double w = box.Width;
                double h = box.Height;
                return 1.0 - (Math.Abs(w - h) / (w + h));
            }
            return 0.0;
        }

        public double Extent(CvBlob blob)
        {
            if (blob != null)
            {
                Rectangle box = blob.BoundingBox;
                int boxArea = box.Height * box.Width;
                int blobArea = blob.Area;
                double ratio = (double)blobArea / (double)boxArea;
                return ratio;
            }
            return 0.0;
        }

        public double EquivalentDiameter(CvBlob blob)
        {
            if (blob != null)
            {
                int blobArea = blob.Area;
                double dia = Math.Sqrt((4 * (double)blobArea) / Math.PI);
                return dia;
            }
            return 0.0;
        }

        public double ElongationFactor(CvBlob blob)
        {
            if (blob != null)
            {
                int blobArea = blob.Area;
                double M02 = blob.BlobMoments.M02;
                double M20 = blob.BlobMoments.M20;
                double M11 = blob.BlobMoments.M11;
                double x = M20 + M02;
                double y = 4 * Math.Pow(M11, 2) + Math.Pow((M20 - M02), 2);
                double sqrt_y = Math.Sqrt(y);
                double elongation = (x + sqrt_y) / (x - sqrt_y);
                return elongation;
            }
            return 0.0;
        }

        public double Compactness(CvBlob blob)
        {
            if (blob != null)
            {
                double firstMoment = blob.BlobMoments.U02;
                double secondMoment = blob.BlobMoments.U20;
                double area = Area(blob);

                double compactness = Math.Pow(area, 2) / 
                    (2.0 * Math.PI * Math.Sqrt(Math.Pow(firstMoment, 2) + Math.Pow(secondMoment, 2)));
                return compactness;
            }
            return 0.0;
        }

        public Image<Gray, byte> GetBlobImage(CvBlob blob)
        {
            if (blob != null)
            {
                Point[] contour = GetContourSimple(blob);
                Image<Gray, byte> outImg = GetContourImage(contour);
                return outImg.Copy();
            }
            return null;
        }

        public Image<Gray, byte> GetContourImage(Point[] contour)
        {
            if (contour != null && contour.Length > 0)
            {
                VectorOfPoint vec = new VectorOfPoint(contour);
                Rectangle boundingBox = CvInvoke.BoundingRectangle(vec);
                int graceDimension = 2;
                Size graceSize = new Size(boundingBox.Width + (graceDimension * 2), boundingBox.Height + (graceDimension * 2));
                Image<Gray, byte> outImg = new Image<Gray, byte>(graceSize);
                outImg.SetZero();
                Rectangle roi = new Rectangle(new Point(graceDimension, graceDimension), boundingBox.Size);
                outImg.ROI = roi;
                if (contour.Length > 0)
                {
                    Point offset = new Point(boundingBox.Location.X * -1, boundingBox.Location.Y * -1);
                    for (int i = 0; i < contour.Length; i++)
                    {
                        contour[i].Offset(offset);
                    }
                    outImg.DrawPolyline(contour, true, new Gray(255));
                    outImg.ROI = Rectangle.Empty;
                    outImg = ImageProcUtils.FillHoles(outImg);
                    outImg.ROI = roi;
                }
                return outImg.Copy();
            }
            else return null;
        }

        public Point[] GetContourSimple(CvBlob blob)
        {
            if (blob != null)
            {
                return blob.GetContour();
            }
            return null;
        }
    }
}
