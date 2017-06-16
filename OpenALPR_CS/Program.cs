using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using openalprnet;
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace OpenALPR_CS
{
    class Program
    {
        private static AlprNet alpr;
        private static Capture cap;
        private static Image<Bgr, byte> frame = new Image<Bgr, byte>(1,1);
        private static Mat frameInput = new Mat();
        private static UMat frameInputU = new UMat();
        private static UMat frameOutput = new UMat();
        private static int frameIndex = 0;
        private static FpsCounter fps = new FpsCounter(20);
        private static string plateOutputDirectory = @"C:\opencv\data\plates";

        static void Main(string[] args)
        {
            if(!Directory.Exists(plateOutputDirectory))
            {
                Directory.CreateDirectory(plateOutputDirectory);
            }

            CvInvoke.UseOpenCL = true;

            alpr = new AlprNet("us", @"C:\openalpr_64\openalpr.conf", @"C:\openalpr_64\runtime_data");
            if (!alpr.IsLoaded())
            {
                Console.WriteLine("OpenAlpr failed to load!");
                return;
            }
            // Optionally apply pattern matching for a particular region
            alpr.DefaultRegion = "ks";
            alpr.TopN = 1;


            cap = new Capture(@"c:\opencv\data\honda.mp4");
            //cap = new Capture();
            int skipFrame = 0;
            int skipFPS = 0;

            while (true)
            {
                fps.Restart();

                frameInput = cap.QueryFrame();
                skipFrame++;
                skipFPS++;
                if (skipFrame % 1 == 0)
                {
                    if (frameInput != null)
                    {
                        frameIndex++;
                        frameInputU = frameInput.ToUMat(Emgu.CV.CvEnum.AccessType.ReadWrite);
                        //frame = frameInput.ToImage<Bgr, byte>();
                        //umat = frame.ToUMat();
                        CvInvoke.Sobel(frameInputU, frameOutput, Emgu.CV.CvEnum.DepthType.Cv8U, 3, 3, 31);
                        //frame = frame.Rotate(270, new Bgr(0, 0, 0));
                        //frame = frame.Resize(0.5, Emgu.CV.CvEnum.Inter.Area);
                        //FindPlates(frame);
                    }
                    else
                    {
                        Console.WriteLine("No more frames.");
                        break;
                    }
                }

                fps.Stop();
                if(skipFPS % 10 == 0)
                {
                    Console.WriteLine(fps.GetFPS().ToString("0.00") + " fps");
                }
            }
            cap.Stop();
            Console.WriteLine("No more capture.");
            //CvInvoke.WaitKey();

        }

        private static void FindPlates(Image<Bgr, byte> input)
        {
            //Rectangle ROI = new Rectangle(0, (int)(input.Height * 0.1), input.Width, (int)(input.Height * 0.9));
            //frame.Draw(ROI, new Bgr(0, 255, 0), 1);
            var results = alpr.Recognize(input.Bitmap); //, new List<Rectangle>(){ROI});
            

            //if(results.Plates.Count == 0 ) Console.WriteLine("No plates found.");
            foreach (var result in results.Plates)
            {
                // Save plate region to file
                frame.ROI = GetBoundingRectangle(result.PlatePoints);
                frame.Save(Path.Combine(plateOutputDirectory, result.BestPlate.Characters + "_" + DateTime.Now.ToString("yyyymmdd_HHMMssfff") + ".png"));
                frame.ROI = Rectangle.Empty;

                frame.Draw(result.PlatePoints.ToArray(), new Bgr(0, 0, 255), 2);
                frame.Draw(result.BestPlate.Characters, result.PlatePoints[0], Emgu.CV.CvEnum.FontFace.HersheySimplex, 1, new Bgr(0, 0, 0), 5);
                frame.Draw(result.BestPlate.Characters, result.PlatePoints[0], Emgu.CV.CvEnum.FontFace.HersheySimplex, 1, new Bgr(0, 0, 255), 3);
                Console.WriteLine("[" + frameIndex.ToString() + "] Plate: " + result.BestPlate.Characters + " (" + result.BestPlate.OverallConfidence.ToString("0.") + "%) " + " T:" + result.ProcessingTimeMs.ToString("0.00") );
                
                //Console.WriteLine("Plate {0}: {1} result(s)", result.PlateIndex, result.TopNPlates.Count);
                //Console.WriteLine("  Processing Time: {0} msec(s)", result.ProcessingTimeMs);
                //foreach (var plate in result.TopNPlates)
                //{
                //    Console.WriteLine("  - {0}\t Confidence: {1}\tMatches Template: {2}", plate.Characters,
                //                      plate.OverallConfidence, plate.MatchesTemplate);
                //}
            }

            CvInvoke.Imshow("input", input);
            CvInvoke.WaitKey(20);
        }

        private static Rectangle GetBoundingRectangle(List<Point> points)
        {
            int minx = Int32.MaxValue;
            int maxx = Int32.MinValue;
            int miny = Int32.MaxValue;
            int maxy = Int32.MinValue;

            foreach(var point in points)
            {
                minx = Math.Min(minx, point.X);
                maxx = Math.Max(maxx, point.X);
                miny = Math.Min(miny, point.Y);
                maxy = Math.Max(maxy, point.Y);
            }
            var output = new Rectangle(minx, miny, maxx - minx, maxy - miny);
            return output;
        }
    }
}
