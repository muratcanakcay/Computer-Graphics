using System;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace Lab02___Dithering_and_Color_Quantization
{
    public abstract class Dithering: IEffect
    {
        public abstract WriteableBitmap ApplyTo(WriteableBitmap wbm);
    }

    public class RandomDithering: Dithering
    {
        public int K { get; set; }

        public RandomDithering(int k)
        {
            this.K = k;
        }
        
        public override WriteableBitmap ApplyTo(WriteableBitmap wbm)
        {
            var clone = wbm.Clone();
            var width = clone.PixelWidth;
            var height = clone.PixelHeight;
            var rng = new Random();
            var intervalLength = (255f / (K - 1));

            try
            {
                wbm.Lock();
                clone.Lock();

                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        var oldColor = clone.GetPixelColor(x, y);
                        var oldR = oldColor.R;
                        var oldG = oldColor.G;
                        var oldB = oldColor.B;

                        var newR = 0.0;
                        var newG = 0.0;
                        var newB = 0.0;

                        var boundariesArrayR = new int[K - 1];
                        var boundariesArrayG = new int[K - 1];
                        var boundariesArrayB = new int[K - 1];

                        for (var i = 0; i < K - 1; i++)
                        {
                            var startTempBound = i * 255.0 / (K - 1);
                            var endTempBound = (i + 1) * 255.0 / (K - 1);
                            boundariesArrayR[i] = rng.Next((int)startTempBound, (int)endTempBound);
                            boundariesArrayG[i] = rng.Next((int)startTempBound, (int)endTempBound);
                            boundariesArrayB[i] = rng.Next((int)startTempBound, (int)endTempBound);
                        }

                        if (oldR == oldG && oldG == oldB) // if greyscale
                        {
                            for (var i = 0; i < K - 1; i++)
                            {
                                if (oldR >= boundariesArrayR[K - 2])
                                {
                                    newR = 255.0;
                                    break;
                                }
                                if (oldR < boundariesArrayR[i])
                                {
                                    newR = 255.0 * i / (K - 1);
                                    break;
                                }
                            }

                            clone.SetPixelColor(x, y,
                                Color.FromArgb(oldColor.A, (int)newR, (int)newR,
                                    (int)newR));
                        }
                        else // if RGB
                        {
                            for (var i = 0; i < K - 1; i++)
                            {
                                if (oldR >= boundariesArrayR[K - 2])
                                {
                                    newR = 255.0;
                                    break;
                                }
                                if (oldR < boundariesArrayR[i])
                                {
                                    newR = 255.0 * i / (K - 1);
                                    break;
                                }
                            }

                            for (var i = 0; i < K - 1; i++)
                            {
                                if (oldG >= boundariesArrayG[K - 2])
                                {
                                    newG = 255.0;
                                    break;
                                }
                                if (oldG < boundariesArrayG[i])
                                {
                                    newG = 255.0 * i / (K - 1);
                                    break;
                                }
                            }

                            for (var i = 0; i < K - 1; i++)
                            {
                                if (oldB >= boundariesArrayB[K - 2])
                                {
                                    newB = 255.0;
                                    break;
                                }

                                if (oldB < boundariesArrayB[i])
                                {
                                    newB = 255.0 * i / (K - 1);
                                    break;
                                }
                            }
                            
                            clone.SetPixelColor(x, y, Color.FromArgb(oldColor.A, (int)newR, (int)newG, (int)newB));
                        }


                        //var oldColor = clone.GetPixelColor(x, y);
                        //var randomThresholdR = (int)Math.Round(rng.NextDouble() * intervalLength);
                        //var randomThresholdB = (int)Math.Round(rng.NextDouble() * intervalLength);
                        //var randomThresholdG = (int)Math.Round(rng.NextDouble() * intervalLength);
                        //var posR = (int)Math.Truncate(oldColor.R / intervalLength) + 1;
                        //var posG = (int)Math.Truncate(oldColor.G / intervalLength) + 1;
                        //var posB = (int)Math.Truncate(oldColor.B / intervalLength) + 1;


                        //if (oldColor.R == oldColor.B && oldColor.B == oldColor.G) // grey pixel
                        //{
                        //    int greyIntensity;

                        //    if (oldColor.R % intervalLength > randomThresholdR)
                        //    {
                        //        greyIntensity = posR == K ? 255 : Math.Clamp((int)Math.Round(posR * intervalLength), 0, 255);
                        //    }
                        //    else
                        //    {
                        //        greyIntensity = posR == 1 ? 0 : Math.Clamp((int)Math.Round((posR-1) * intervalLength), 0, 255);
                        //    }

                        //    var newColor = Color.FromArgb(oldColor.A,
                        //        greyIntensity,
                        //        greyIntensity,
                        //        greyIntensity);

                        //    clone.SetPixelColor(x, y, newColor);
                        //}
                        //else
                        //{
                        //    int newR;
                        //    int newG;
                        //    int newB;

                        //    newR = oldColor.R > randomThresholdR ?
                        //        newR = posR == K ?
                        //            255 :
                        //            Math.Clamp((int)Math.Round(posR * intervalLength), 0, 255) :
                        //        newR = posR == 1 ?
                        //            0 :
                        //            Math.Clamp((int)Math.Round((posR-1) * intervalLength), 0, 255);

                        //    newG = oldColor.G > randomThresholdG ?
                        //        newG = posG == K ?
                        //            255 :
                        //            Math.Clamp((int)Math.Round(posG * intervalLength), 0, 255) :
                        //        newG = posG == 1 ?
                        //            0 :
                        //            Math.Clamp((int)Math.Round((posG-1) * intervalLength), 0, 255);

                        //    newB = oldColor.B > randomThresholdB ?
                        //        newB = posB == K ?
                        //            255 :
                        //            Math.Clamp((int)Math.Round(posB * intervalLength), 0, 255) :
                        //        newB = posB == 1 ?
                        //            0 :
                        //            Math.Clamp((int)Math.Round((posB-1) * intervalLength), 0, 255);

                        //    var newColor = Color.FromArgb(oldColor.A,
                        //        newR,
                        //        newG,
                        //        newB);

                        //    clone.SetPixelColor(x, y, newColor);
                        //}
                    }
                }
            }
            finally
            {
                wbm.Unlock();
                clone.Unlock();
            }

            return clone;
        }

        public override string ToString()
        {
            return "Random Dithering";
        }
    }

    public class YCbCrDithering: Dithering
    {
        public int K { get; set; }

        public YCbCrDithering(int k)
        {
            this.K = k;
        }
        
        public override WriteableBitmap ApplyTo(WriteableBitmap wbm)
        {
            var clone = wbm.Clone();
            var width = clone.PixelWidth;
            var height = clone.PixelHeight;
            var rng = new Random();
            
            try
            {
                wbm.Lock();
                clone.Lock();

                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        var oldColor = clone.GetPixelColor(x, y);
                        var oldR = oldColor.R;
                        var oldG = oldColor.G;
                        var oldB = oldColor.B;

                        // convert to Y'CbCr
                        var Y = 0.299*oldR + 0.587*oldG + 0.114*oldB;
                        var Cb = 128 - 0.169*oldR - 0.331*oldG + 0.5*oldB;
                        var Cr = 128 + 0.5*oldR - 0.419*oldG - 0.081*oldB;

                        var newY = 0.0;
                        var newCb = 0.0;
                        var newCr = 0.0;

                        var boundariesArrayY = new int[K - 1];
                        var boundariesArrayCb = new int[K - 1];
                        var boundariesArrayCr = new int[K - 1];

                        for (var i = 0; i < K - 1; i++)
                        {
                            var startTempBound = i * 255.0 / (K - 1);
                            var endTempBound = (i + 1) * 255.0 / (K - 1);
                            boundariesArrayY[i] = rng.Next((int)startTempBound, (int)endTempBound);
                            boundariesArrayCb[i] = rng.Next((int)startTempBound, (int)endTempBound);
                            boundariesArrayCr[i] = rng.Next((int)startTempBound, (int)endTempBound);
                        }

                        for (var i = 0; i < K - 1; i++)
                        {
                            if (Y >= boundariesArrayY[K - 2])
                            {
                                newY = 255.0;
                                break;
                            }
                            if (Y < boundariesArrayY[i])
                            {
                                newY = 255.0 * i / (K - 1);
                                break;
                            }
                        }

                        for (var i = 0; i < K - 1; i++)
                        {
                            if (Cb >= boundariesArrayCb[K - 2])
                            {
                                newCb = 255.0;
                                break;
                            }
                            if (Cb < boundariesArrayCb[i])
                            {
                                newCb = 255.0 * i / (K - 1);
                                break;
                            }
                        }

                        for (var i = 0; i < K - 1; i++)
                        {
                            if (Cr >= boundariesArrayCr[K - 2])
                            {
                                newCr = 255.0;
                                break;
                            }

                            if (Cr < boundariesArrayCr[i])
                            {
                                newCr = 255.0 * i / (K - 1);
                                break;
                            }
                        }

                        // convert back to RGB
                        var newR = newY + 1.402 * (newCr - 128);
                        var newG = newY - 0.344 * (newCb - 128) - 0.714 * (newCr - 128);
                        var newB = newY + 1.772*(newCb - 128);

                        newR = Math.Clamp((int)newR, 0, 255);
                        newG = Math.Clamp((int)newG, 0, 255);
                        newB = Math.Clamp((int)newB, 0, 255);

                        clone.SetPixelColor(x, y, Color.FromArgb(oldColor.A, (int)newR, (int)newG, (int)newB));
                    }
                }
            }
            finally
            {
                wbm.Unlock();
                clone.Unlock();
            }

            return clone;
        }

        public override string ToString()
        {
            return "YCbCr Dithering";
        }
    }
}