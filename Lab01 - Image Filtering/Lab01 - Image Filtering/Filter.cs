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
                        var pixelColor = clone.GetPixelColor(x, y);

                        var newColor = Color.FromArgb(pixelColor.A,
                            Math.Abs(pixelColor.G - 255),
                            Math.Abs(pixelColor.B - 255),
                            Math.Abs(pixelColor.R - 255));

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