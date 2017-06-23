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
        //static void Main(string[] args)
        //{
        //    string filepath = @"C:\openalpr_64\data\soccer2.jpg";
        //    Image<Bgr, byte> inputImage = new Image<Bgr, byte>(filepath);
        //    inputImage = inputImage.Resize(1, Emgu.CV.CvEnum.Inter.Area);
        //    //inputImage = inputImage.SmoothGaussian(3);
        //    Image<Gray, byte> grayImage = new Image<Gray, byte>(inputImage.Bitmap);
        //    Image<Bgr, byte> displayImage = new Image<Bgr, byte>(inputImage.Bitmap);
        //    using (var tesseract = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
        //    {
        //        for (int i = 0; i < 255; i += 5)
        //        {
        //            Image<Bgr, byte> tempDisplayImage = new Image<Bgr, byte>(inputImage.Bitmap);
        //            Image<Gray, byte> thresholdImage = grayImage.ThresholdBinaryInv(new Gray(i), new Gray(255));
        //            Pix p = PixConverter.ToPix(thresholdImage.Bitmap);
        //            Image<Gray, byte> cannyImage = thresholdImage.Canny(200, 100);
        //            BlobAnalysis ba = new BlobAnalysis(cannyImage);
        //            int boxLimit = 30;
        //            foreach (var blob in ba.Blobs.Values)
        //            {
        //                Rectangle box = new Rectangle(Math.Max(blob.BoundingBox.X-10, 0), Math.Max(blob.BoundingBox.Y-10, 0), blob.BoundingBox.Width + 20, blob.BoundingBox.Height + 20);
        //                try
        //                {
        //                    if (box.Width > boxLimit && box.Height > boxLimit)
        //                    {
        //                        Rect roi = new Rect(box.X, box.Y, box.Width, box.Height);
        //                        Page result = tesseract.Process(p, roi, PageSegMode.SingleChar);
        //                        float confidence = result.GetMeanConfidence();
        //                        string text = result.GetText().Trim(); //.Replace(".", "");
        //                        Bgr boxColor = new Bgr(0, 0, 255);
        //                        if (!String.IsNullOrWhiteSpace(text))
        //                        {
        //                            if (confidence > 0.95)
        //                            {
        //                                boxColor = new Bgr(0, 255, 0);
        //                                //Console.WriteLine("[{0}]({1}): {2} ({3})", i, blob.Label, text, confidence);
        //                            }
        //                            else if (confidence > 0.8)
        //                            {
        //                                boxColor = new Bgr(0, 255, 255);
        //                            }
        //                        }
        //                        //Console.WriteLine(text + " (" + result.GetMeanConfidence() + ")");
        //                        result.Dispose();
        //                        tempDisplayImage.Draw(blob.BoundingBox, new Bgr(0, 0, 0), 3);
        //                        tempDisplayImage.Draw(blob.BoundingBox, boxColor, 1);
        //                        if (!String.IsNullOrWhiteSpace(text) && confidence > 0.8)
        //                        {
        //                            string displayText = text;
        //                            tempDisplayImage.Draw(displayText, box.Location, Emgu.CV.CvEnum.FontFace.HersheyPlain, 1, new Bgr(0, 0, 0), 3);
        //                            tempDisplayImage.Draw(displayText, box.Location, Emgu.CV.CvEnum.FontFace.HersheyPlain, 1, boxColor);
        //                        }
        //                    }
        //                }
        //                catch (Exception ex)
        //                {

        //                }
        //            }

        //            CvInvoke.Imshow("threshold", thresholdImage);
        //            //CvInvoke.Imshow("canny", cannyImage);
        //            CvInvoke.Imshow("display", tempDisplayImage);
        //            CvInvoke.WaitKey();
        //        }
        //    }

        //    CvInvoke.DestroyAllWindows();
        //}

        //private static bool IsNumber(char character)
        //{
        //    string numbers = "1234567890";
        //    return numbers.Contains(character);
        //}

        //private static bool IsLetter(char character)
        //{
        //    string letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        //    return letters.Contains(character);
        //}

        static void Main(string[] args)
        {
            Action<string> consumeAction = new Action<string>(s => Console.WriteLine(s + " consumed."));
            ProducerConsumer<string> tq = new ProducerConsumer<string>(8, consumeAction);
            for (int i = 0; i < 100; i++ )
            {
                tq.EnqueueTask(i.ToString());
            }
        }
    }
}
