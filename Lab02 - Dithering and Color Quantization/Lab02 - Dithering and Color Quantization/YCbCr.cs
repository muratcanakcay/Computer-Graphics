using System;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace Lab02___Dithering_and_Color_Quantization
{
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