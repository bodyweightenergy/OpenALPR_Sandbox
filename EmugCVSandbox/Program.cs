using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using Emgu.CV.Structure;
using Tesseract;
using System.Drawing;

namespace EmugCVSandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            string filepath = @"C:\openalpr_64\data\soccer2.jpg";
            Image<Bgr, byte> inputImage = new Image<Bgr, byte>(filepath);
            inputImage = inputImage.Resize(1, Emgu.CV.CvEnum.Inter.Area);
            inputImage = inputImage.SmoothGaussian(3);
            Image<Gray, byte> grayImage = new Image<Gray, byte>(inputImage.Bitmap);
            Image<Bgr, byte> displayImage = new Image<Bgr, byte>(inputImage.Bitmap);
            using (var tesseract = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
            {
                for (int i = 0; i < 255; i += 5)
                {
                    Image<Bgr, byte> tempDisplayImage = new Image<Bgr, byte>(inputImage.Bitmap);
                    Image<Gray, byte> thresholdImage = grayImage.ThresholdBinary(new Gray(i), new Gray(255));
                    Pix p = PixConverter.ToPix(thresholdImage.Bitmap);
                    Image<Gray, byte> cannyImage = thresholdImage.Canny(200, 100);
                    BlobAnalysis ba = new BlobAnalysis(cannyImage);
                    int boxLimit = 20;
                    foreach (var blob in ba.Blobs.Values)
                    {
                        Rectangle box = blob.BoundingBox;
                        if (box.Width > boxLimit && box.Height > boxLimit)
                        {
                            Rect roi = new Rect(box.X, box.Y, box.Width, box.Height);
                            Page result = tesseract.Process(p, roi, PageSegMode.SingleLine);
                            float confidence = result.GetMeanConfidence();
                            string text = result.GetText().Trim().Replace(".","");
                            Bgr boxColor = new Bgr(0, 0, 255);
                            if (!String.IsNullOrWhiteSpace(text))
                            {
                                //if (IsNumber(text[0]) || IsLetter(text[0]))
                                //{
                                     if(confidence > 0.9)
                                     {
                                         boxColor = new Bgr(0, 255, 0);
                                         tempDisplayImage.Draw(text + " (" + confidence.ToString() + ")", box.Location, Emgu.CV.CvEnum.FontFace.HersheyPlain, 1, boxColor);
                                     } else if(confidence > 0.7)
                                     {
                                         boxColor = new Bgr(0, 255, 255);
                                         tempDisplayImage.Draw(text + " (" + confidence.ToString() + ")", box.Location, Emgu.CV.CvEnum.FontFace.HersheyPlain, 1, boxColor);
                                     }
                                    //Console.WriteLine("[{0}]({1}): {2} ({3})", i, blob.Label, text, confidence);
                                //}
                            }
                            result.Dispose();
                            tempDisplayImage.Draw(blob.BoundingBox, boxColor, 1);
                        }
                    }

                    CvInvoke.Imshow("threshold", thresholdImage);
                    CvInvoke.Imshow("canny", cannyImage);
                    CvInvoke.Imshow("display", tempDisplayImage);
                    CvInvoke.WaitKey();
                }
            }

            CvInvoke.DestroyAllWindows();
        }

        private static bool IsNumber(char character)
        {
            string numbers = "1234567890";
            return numbers.Contains(character);
        }

        private static bool IsLetter(char character)
        {
            string letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return letters.Contains(character);
        }
    }
}
