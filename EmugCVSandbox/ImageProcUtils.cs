using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;
using Emgu.CV.ML;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Emgu.CV.Util;

namespace EmugCVSandbox
{

    /// <summary>
    /// Contains useful methods for generic image processing.
    /// </summary>
    public static class ImageProcUtils
    {
        // Lock objects
        private static object scaleLock = new object();
        private static object fillLock = new object();


        /// <summary>
        /// Extracts a sub newImg from a bigger newImg using the defined dimensions
        /// </summary>
        public static Image<Gray, byte> GetSubImage(Image<Gray, byte> sourceImage, Point topLeftCorner, Size size)
        {
            Rectangle roi = new Rectangle(topLeftCorner, size);
            return GetSubImage(sourceImage, roi);
        }
        /// <summary>
        /// Extracts a sub newImg from a bigger newImg using the defined dimensions
        /// </summary>
        public static Image<Gray, byte> GetSubImage(Image<Gray, byte> sourceImage, Rectangle roi, bool fill_with_border_color = false)
        {
            if (roi.Top >= 0 && roi.Left >= 0 && roi.Bottom <= sourceImage.Height && roi.Right <= sourceImage.Width)
            {
                Image<Gray, byte> subImage = sourceImage.Copy(roi);
                return subImage;
            }
            else
            {
                // get displayColor of border pixel
                byte fill_value = 0;
                bool top = false;
                bool bottom = false;
                bool left = false;
                bool right = false;

                if (roi.Top < 0) top = true;
                if (roi.Bottom > sourceImage.Height) bottom = true;
                if (roi.Left < 0) left = true;
                if (roi.Right > sourceImage.Width) right = true;

                if (top)
                {
                    if (left)
                    {
                        fill_value = sourceImage.Data[0, 0, 0];
                    }
                    else if (right)
                    {
                        fill_value = sourceImage.Data[0, sourceImage.Width - 2, 0];
                    }
                    else
                    {
                        fill_value = sourceImage.Data[0, (roi.Left + roi.Right) / 2, 0];
                    }
                }
                else if (bottom)
                {
                    if (left)
                    {
                        fill_value = sourceImage.Data[sourceImage.Height - 2, 0, 0];
                    }
                    else if (right)
                    {
                        fill_value = sourceImage.Data[sourceImage.Height - 2, sourceImage.Width - 2, 0];
                    }
                    else
                    {
                        fill_value = sourceImage.Data[sourceImage.Height - 2, (roi.Left + roi.Right) / 2, 0];
                    }
                }
                else
                {
                    if (left)
                    {
                        fill_value = sourceImage.Data[(roi.Top + roi.Bottom) / 2, 0, 0];
                    }
                    else if (right)
                    {
                        fill_value = sourceImage.Data[(roi.Top + roi.Bottom) / 2, sourceImage.Width - 2, 0];
                    }
                }

                Image<Gray, byte> subImage = new Image<Gray, byte>(roi.Size);

                // fill empty space with displayColor of border pixel
                for (int h = roi.Top; h < roi.Bottom; h++)
                {
                    for (int w = roi.Left; w < roi.Right; w++)
                    {
                        int sub_h = h - roi.Top;
                        int sub_w = w - roi.Left;

                        if (h < 0 || h >= sourceImage.Height)
                        {
                            subImage.Data[sub_h, sub_w, 0] = 0;
                        }
                        else if (w < 0 || w >= sourceImage.Width)
                        {
                            subImage.Data[sub_h, sub_w, 0] = 0;
                        }
                        else
                        {
                            subImage.Data[sub_h, sub_w, 0] = sourceImage.Data[h, w, 0];
                        }
                    }
                }

                // replace all black with fill_value
                if (fill_with_border_color)
                {
                    for (int h = 0; h < subImage.Height; h++)
                    {
                        for (int w = 0; w < subImage.Width; w++)
                        {
                            if (subImage.Data[h, w, 0] == 0)
                            {
                                subImage.Data[h, w, 0] = fill_value;
                            }
                        }
                    }
                }
                return subImage;

            }
        }

        /// <summary>
        /// Resizes image, using Cubic interpolation.
        /// </summary>
        public static Image<Gray, TDepth> ResizeImage<TDepth>(Image<Gray, TDepth> sourceImage, Size newSize) where TDepth : new()
        {
            if (sourceImage != null && newSize.Width > 0 && newSize.Height > 0)
            {
                Image<Gray, TDepth> outImage = new Image<Gray, TDepth>(newSize);
                CvInvoke.Resize(sourceImage, outImage, newSize, 0, 0, Emgu.CV.CvEnum.Inter.Cubic);
                return outImage;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Performs an advanced binary close operation, using a dilate then erode.
        /// </summary>
        /// <param name="inputImage">Input image.</param>
        /// <param name="ext_iter">Number of dilate/erode loops.</param>
        /// <param name="int_iter">Number of iterations to run each dilate and erode operation.</param>
        /// <returns>Processed image.</returns>
        public static Image<Gray, byte> CloseImage(Image<Gray, byte> inputImage, int ext_iter, int int_iter)
        {
            Image<Gray, byte> outputImage = inputImage.Copy();
            for (int i = 0; i < ext_iter; i++)
            {
                outputImage._Dilate(int_iter);
                outputImage._Erode(int_iter);
            }
            return outputImage;
        }

        public static Image<Gray, byte> OpenImage(Image<Gray, byte> inputImage, int ext_iter, int int_iter)
        {
            Image<Gray, byte> outputImage = inputImage.Copy();
            for (int i = 0; i < ext_iter; i++)
            {
                outputImage._Erode(int_iter);
                outputImage._Dilate(int_iter);
            }
            return outputImage;
        }

        public static Image<Gray, byte> FillConvexHulls(Image<Gray, byte> inputImage)
        {
            Image<Gray, byte> outputImage = inputImage.Copy();
            ContourAnalysis ca = new ContourAnalysis(outputImage, 0.1);
            for (int i = 0; i < ca.Contours.Count; i++)
            {
                outputImage.FillConvexPoly(ca.GetConvexHullContours()[i].ToArray(), new Gray(255));
            }
            return outputImage;
        }

        public static Image<Gray, byte> AbsoluteDifference(Image<Gray, byte> inputImage, byte ref_val)
        {
            Image<Gray, byte> outputImage = inputImage.Copy();

            for (int h = 0; h < outputImage.Height; h++)
            {
                for (int w = 0; w < outputImage.Width; w++)
                {
                    byte abs_diff = (byte)Math.Abs(outputImage.Data[h, w, 0] - ref_val);
                    outputImage.Data[h, w, 0] = abs_diff;
                }
            }

            return outputImage;
        }

        /// <summary>
        /// Fills in black trainDataAll non-black objects touching the border pixels of an newImg
        /// </summary>
        /// <param name="procImg">newImg to be processed</param>
        public static Image<Gray, byte> RemoveBorderObjects(Image<Gray, byte> input)
        {
            Image<Gray, byte> outputImage = input.Copy();
            //remove top + bottom border objects
            for (int y = 0; y < outputImage.Height; y += outputImage.Height - 1)
            {
                for (int x = 0; x < outputImage.Width; x++)
                {
                    if (outputImage.Data[y, x, 0] == (byte)255)
                    {
                        Rectangle rect = new Rectangle();
                        CvInvoke.FloodFill(outputImage, new Image<Gray, byte>(outputImage.Width + 2, outputImage.Height + 2), new Point(x, y), new MCvScalar(0), out rect, new MCvScalar(128), new MCvScalar(128));
                    }
                }
            }

            //remove left + right border objects
            for (int x = 0; x < outputImage.Width; x += outputImage.Width - 1)
            {
                for (int y = 0; y < outputImage.Height; y++)
                {
                    if (outputImage.Data[y, x, 0] == (byte)255)
                    {
                        Rectangle rect = new Rectangle();
                        CvInvoke.FloodFill(outputImage, new Image<Gray, byte>(outputImage.Width + 2, outputImage.Height + 2), new Point(x, y), new MCvScalar(0), out rect, new MCvScalar(128), new MCvScalar(128));
                    }
                }
            }
            return outputImage;
        }

        /// <summary>
        /// Organizes a list of equally-sized images into a big grid-formatted newImg
        /// </summary>
        /// <typeparam name="TColor">Color of images</typeparam>
        /// <typeparam name="TDepth">Data depth of images</typeparam>
        /// <param name="imageList">List of images (must be equal in size)</param>
        /// <returns>Big newImg of smaller images in a grid</returns>
        public static Image<TColor, TDepth> StitchImages<TColor, TDepth>(List<Image<TColor, TDepth>> imageList)
            where TColor : struct, IColor
            where TDepth : new()
        {
            // Return if imageList is empty
            if (imageList.Count == 0)
            {
                return null;
            }

            // Ensure trainDataAll images have same dimensions and channel number
            Size imgSize = imageList[0].Size;
            int imgChNum = imageList[0].NumberOfChannels;

            foreach (Image<TColor, TDepth> img in imageList)
            {
                if (img.Size != imgSize || img.NumberOfChannels != imgChNum)
                {
                    return null;
                }
            }

            int imageNum = imageList.Count;
            int stitch_h = 0;
            int stitch_w = 0;

            // Get optimal vertical stack
            for (stitch_h = 0; (stitch_h * stitch_h) <= imageNum; stitch_h++)
            {
            }
            stitch_w = stitch_h;
            if (imageNum <= (stitch_h * (stitch_h - 1)))
            {
                stitch_h--;
            }


            int imageCounter = 0;
            Image<TColor, TDepth> output = new Image<TColor, TDepth>(stitch_w * imgSize.Width, stitch_h * imgSize.Height);
            for (int y = 0; y < stitch_h; y++)
            {
                for (int x = 0; x < stitch_w; x++)
                {
                    Rectangle roi = new Rectangle(new Point(x * imgSize.Width, y * imgSize.Height), imgSize);
                    output.ROI = roi;
                    Image<TColor, TDepth> subImage = new Image<TColor, TDepth>(imgSize);
                    if (imageCounter < imageList.Count)
                    {
                        imageList[imageCounter].CopyTo(subImage);
                    }
                    // Insert blank if no more images
                    else
                    {
                        IColor fillValue;
                        IColor textValue;
                        if (typeof(TColor) == typeof(Gray))
                        {
                            fillValue = new Gray(0);
                            textValue = new Gray(255);
                        }
                        else
                        {
                            fillValue = new Bgr(0, 0, 0);
                            textValue = new Bgr(255, 255, 255);
                        }
                        subImage.SetValue((TColor)fillValue);
                        //subImage.Draw("BLANK", new Point(10, imgSize.Height-10), Emgu.CV.CvEnum.FontFace.HersheyPlain, 1, (TColor) textValue);
                    }
                    subImage.CopyTo(output);
                    output.ROI = Rectangle.Empty;
                    imageCounter++;
                }
            }

            return output;

        }

        /// <summary>
        /// Displays a list of equally-sized images in a grid-like window
        /// </summary>
        /// <param name="title">Window name</param>
        /// <param name="images">List of images (must be equal in size)</param>
        public static void Imshow(string title, List<Image<Gray, byte>> images)
        {
            if (images != null && images.Count != 0)
            {
                Image<Gray, byte> stitch = StitchImages<Gray, byte>(images);
                CvInvoke.Imshow(title, stitch);
            }
        }

        /// <summary>
        /// Fills black holes in white blobs
        /// </summary>
        public static Image<Gray, byte> FillHoles(Image<Gray, byte> inputImg)
        {
            Image<Gray, byte> image = inputImg;
            Image<Gray, byte> mask = new Image<Gray, byte>(image.Size.Width + 2, image.Size.Height + 2);
            Rectangle maskROI = new Rectangle(new Point(1, 1), image.Size);
            mask.ROI = maskROI;
            Image<Gray, float> laplaceImg = image.Laplace(3);
            lock (fillLock)
            {
                image = laplaceImg.ConvertScale<byte>(1, 0);
            }
            image.Copy(mask);
            mask.ROI = Rectangle.Empty;
            image._ThresholdBinary(new Gray(128), new Gray(255));
            Rectangle rect = new Rectangle();
            CvInvoke.FloodFill(image, mask, new Point(0, 0), new MCvScalar(128), out rect, new MCvScalar(10), new MCvScalar(10));
            image._ThresholdToZeroInv(new Gray(200));
            image._ThresholdBinaryInv(new Gray(100), new Gray(255));
            image._Erode(1);
            return image;
        }

        public static Image<Gray, byte> FillHolesContours(Image<Gray, byte> image)
        {
            var resultImage = new Image<Gray, byte>(image.Size);
            ContourAnalysis ca = new ContourAnalysis(image, 1);
            var contours = ca.GetContoursVector();
            CvInvoke.DrawContours(resultImage, contours, -1, new MCvScalar(255), -1);
            //for (int i = 0; i < contours.Size; i++)
            //{
            //    resultImage.DrawPolyline(contours[i].ToArray(), true, new Gray(255), 1);
            //}
            return resultImage;
        }

        public static Image<Gray, byte> BlackLineImage(Image<Gray, byte> image)
        {
            byte fillValue = 128;
            int imageHeight = image.Height;
            int imageWidth = image.Width;

            for (int i = 0; i < imageWidth; i++)
            {
                image.Data[0, i, 0] = fillValue;
                image.Data[imageHeight - 1, i, 0] = fillValue;
            }
            for (int i = 0; i < imageHeight; i++)
            {
                image.Data[i, 0, 0] = fillValue;
                image.Data[i, imageWidth - 1, 0] = fillValue;
            }
            return image;
        }

        public static void ReplaceBgrChannel(Image<Bgr, byte> sourceImg, Image<Gray, byte> grayImg, Bgr mask)
        {
            if (sourceImg.Size != grayImg.Size)
            {
                return;
            }
            Size sz = grayImg.Size;

            for (int h = 0; h < sz.Height; h++)
            {
                for (int w = 0; w < sz.Width; w++)
                {
                    byte gray_byte = grayImg.Data[h, w, 0];
                    //if (gray_byte > 0)
                    //{
                    if (mask.Blue > 0) sourceImg.Data[h, w, 0] = gray_byte;
                    if (mask.Green > 0) sourceImg.Data[h, w, 1] = gray_byte;
                    if (mask.Red > 0) sourceImg.Data[h, w, 2] = gray_byte;
                    //}
                }
            }
        }

        /// <summary>
        /// Calculates a non-unique, Base64 hash string from a Bitmap.
        /// </summary>
        /// <param name="bp"></param>
        /// <returns>Base64 hash string</returns>
        public static string HashBitmap(Bitmap bp, bool unique = true)
        {
            // Get byte data of Bitmap

            byte[] rawData = new byte[bp.Width * bp.Height];
            BitmapData bpdata = bp.LockBits(new Rectangle(new Point(0, 0), bp.Size),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            Marshal.Copy(bpdata.Scan0, rawData, 0, bp.Width * bp.Height);
            bp.UnlockBits(bpdata);


            // Skip every other number of bytes
            int divider = 8192;
            int truncatedLength = rawData.Length / divider;
            byte[] truncatedData = new byte[truncatedLength];
            for (int i = 0; i < truncatedLength; i++)
            {
                truncatedData[i] = rawData[i * divider];
            }

            SHA1CryptoServiceProvider hasher = new SHA1CryptoServiceProvider();
            byte[] hash = null;
            if (unique)
            {
                hash = hasher.ComputeHash(rawData);
            }
            else
            {
                hash = hasher.ComputeHash(truncatedData);
            }
            string hexcode = Convert.ToBase64String(hash).Replace("/", "_");
            return hexcode;

        }

        public static double ConvexDefectNormalDistance(Point startPoint, Point endPoint, Point farPoint)
        {
            double D12 = DistanceBetweenTwoPoints(startPoint, endPoint);
            double D13 = DistanceBetweenTwoPoints(startPoint, farPoint);
            double D23 = DistanceBetweenTwoPoints(endPoint, farPoint);

            double P = (D12 + D13 + D23) / 2;
            double Area = Math.Sqrt(P * (P - D12) * (P - D13) * (P - D23));

            double normalDistance = (2 * Area) / D12;

            return normalDistance;
        }

        public static double DistanceBetweenTwoPoints(Point startPoint, Point endPoint)
        {
            int startX = startPoint.X;
            int startY = startPoint.Y;
            int endX = endPoint.X;
            int endY = endPoint.Y;

            double distance = Math.Sqrt(Math.Pow(startX - endX, 2) + Math.Pow(startY - endY, 2));
            return distance;
        }
    }
}
