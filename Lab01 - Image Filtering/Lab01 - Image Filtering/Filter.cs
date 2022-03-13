using System;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace Lab01___Image_Filtering
{
    public interface IFilter
    {
        WriteableBitmap Apply(WriteableBitmap wbm);
    }

    public interface IFunctionFilter : IFilter
    {
    }

    public class Inversion : IFunctionFilter
    {
        public WriteableBitmap Apply(WriteableBitmap wbm)
        {
            var clone = wbm.Clone();
            var width = clone.PixelWidth;
            var height = clone.PixelHeight;

            try
            {
                wbm.Lock();
                clone.Lock();

                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        var oldColor = clone.GetPixelColor(x, y);

                        var newColor = Color.FromArgb(oldColor.A,
                            Math.Abs(oldColor.G - 255),
                            Math.Abs(oldColor.B - 255),
                            Math.Abs(oldColor.R - 255));

                        clone.SetPixelColor(x, y, newColor);

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
    }

    public class Brightness : IFunctionFilter
    {
        public int Coefficient { get; set; }

        public Brightness(int coefficient)
        {
            this.Coefficient = coefficient;
        }

        public WriteableBitmap Apply(WriteableBitmap wbm)
        {
            var clone = wbm.Clone();
            var width = clone.PixelWidth;
            var height = clone.PixelHeight;

            try
            {
                wbm.Lock();
                clone.Lock();
                    
                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        var oldColor = wbm.GetPixelColor(x, y);

                        var newColor = Color.FromArgb(oldColor.A, 
                            Math.Clamp(oldColor.R + Coefficient, 0, 255), 
                            Math.Clamp(oldColor.G + Coefficient, 0, 255), 
                            Math.Clamp(oldColor.B + Coefficient, 0, 255));

                        clone.SetPixelColor(x, y, newColor);
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
    }

    public class Contrast : IFunctionFilter
    {
        public double Coefficient { get; set; }

        public Contrast(double coefficient)
        {
            this.Coefficient = coefficient;
        }

        public WriteableBitmap Apply(WriteableBitmap wbm)
        {
            var clone = wbm.Clone();
            var width = wbm.PixelWidth;
            var height = wbm.PixelHeight;

            try
            {
                wbm.Lock();
                clone.Lock();

                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        var oldColor = wbm.GetPixelColor(x, y);

                        var newColor = Color.FromArgb(oldColor.A, 
                            (int)Math.Round(Math.Clamp((oldColor.R - 128) * Coefficient + 128, 0, 255)), 
                            (int)Math.Round(Math.Clamp((oldColor.G - 128) * Coefficient + 128, 0, 255)), 
                            (int)Math.Round(Math.Clamp((oldColor.B - 128) * Coefficient + 128, 0, 255)));

                        clone.SetPixelColor(x, y, newColor);
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
    }

    public class Gamma : IFunctionFilter
    {
        public double Coefficient { get; set; }

        public Gamma(double coefficient)
        {
            this.Coefficient = coefficient;
        }

        public WriteableBitmap Apply(WriteableBitmap wbm)
        {
            var clone = wbm.Clone();
            var width = wbm.PixelWidth;
            var height = wbm.PixelHeight;

            try
            {
                wbm.Lock();
                clone.Lock();

                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        
                        var pixelColor = wbm.GetPixelColor(x, y);

                        var red = (int)Math.Round(Math.Pow(pixelColor.R / 255D, Coefficient) * 255);
                        var blue = (int)Math.Round(Math.Pow(pixelColor.B / 255D, Coefficient) * 255);
                        var green = (int)Math.Round(Math.Pow(pixelColor.G / 255D, Coefficient) * 255);

                        var newColor = Color.FromArgb(pixelColor.A, red, green, blue);

                        clone.SetPixelColor(x, y, newColor);

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
    }
}