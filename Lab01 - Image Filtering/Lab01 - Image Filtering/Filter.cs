using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

namespace Lab01___Image_Filtering
{
    public interface IFilter
    {
        WriteableBitmap ApplyTo(WriteableBitmap wbm);
    }

    public class Inversion : IFilter
    {
        public WriteableBitmap ApplyTo(WriteableBitmap wbm)
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

        public override string ToString()
        {
            return "Inversion";
        }
    }

    public class Brightness : IFilter
    {
        public int Coefficient { get; set; }

        public Brightness(int coefficient)
        {
            this.Coefficient = coefficient;
        }

        public WriteableBitmap ApplyTo(WriteableBitmap wbm)
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

        public override string ToString()
        {
            return "Brightness";
        }
    }

    public class Contrast : IFilter
    {
        public double Coefficient { get; set; }

        public Contrast(double coefficient)
        {
            this.Coefficient = coefficient;
        }

        public WriteableBitmap ApplyTo(WriteableBitmap wbm)
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

        public override string ToString()
        {
            return "Contrast";
        }
    }

    public class Gamma : IFilter
    {
        public double Coefficient { get; set; }

        public Gamma(double coefficient)
        {
            this.Coefficient = coefficient;
        }

        public WriteableBitmap ApplyTo(WriteableBitmap wbm)
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

        public override string ToString()
        {
            return "Gamma";
        }
    }

    public class Convolution : IFilter
    {
        public Kernel Kernel { get; set; }

        public Convolution(Kernel kernel)
        {
            this.Kernel= kernel;
        }

        public WriteableBitmap ApplyTo(WriteableBitmap wbm)
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
                        var newColor = CalculateColorFromKernel(wbm, Kernel, x, y);
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

        private Color CalculateColorFromKernel(WriteableBitmap wbm, Kernel kernel, int x, int y)
        {
            var redChannel = 0;
            var blueChannel = 0;
            var greenChannel = 0;
            var readPixel = new Point();
            var pixelColor = Color.Empty;
            
            for (var c = 0; c < kernel.Width; c++)
            {
                for (var r = 0; r < kernel.Height; r++)
                {
                    readPixel.X = x + c - kernel.Anchor.X;
                    readPixel.Y = y + r - kernel.Anchor.Y;

                    // mirror edges
                    if (readPixel.X < 0)
                        readPixel.X = -readPixel.X;

                    if (readPixel.X > wbm.PixelWidth - 1)
                        readPixel.X = 2 * wbm.PixelWidth - readPixel.X;

                    if (readPixel.Y < 0)
                        readPixel.Y = -readPixel.Y;

                    if (readPixel.Y >= wbm.PixelHeight)
                        readPixel.Y = 2 * wbm.PixelHeight - readPixel.Y;

                    pixelColor = wbm.GetPixelColor(readPixel.X, readPixel.Y);
                    var kernelIntensity = kernel.KernelMatrix[r, c];

                    redChannel += pixelColor.R * kernelIntensity;
                    greenChannel += pixelColor.G * kernelIntensity;
                    blueChannel += pixelColor.B * kernelIntensity;
                }
            }

            return Color.FromArgb(pixelColor.A, 
                Math.Clamp(redChannel / kernel.D + kernel.IntensityOffset, 0, 255), 
                Math.Clamp(greenChannel / kernel.D + kernel.IntensityOffset, 0, 255), 
                Math.Clamp(blueChannel / kernel.D + kernel.IntensityOffset, 0, 255));
        }
    }

    public class Blur : Convolution
    {
        public Blur() : base(Kernels.Blur)
        { }

        public override string ToString()
        {
            return "Blur";
        }
    }

    public class GaussianBlur : Convolution
    {
        public GaussianBlur() : base(Kernels.GaussianBlur)
        { }

        public override string ToString()
        {
            return "Gaussian Blur";
        }
    }

    public class Sharpen : Convolution
    {
        public Sharpen() : base(Kernels.Sharpen)
        { }

        public override string ToString()
        {
            return "Sharpen";
        }
    }

    public class EdgeDetection: Convolution
    {
        public EdgeDetection() : base(Kernels.EdgeDetection)
        { }

        public override string ToString()
        {
            return "Edge Detection";
        }
    }

    public class Emboss : Convolution
    {
        public Emboss() : base(Kernels.Emboss)
        { }

        public override string ToString()
        {
            return "Emboss";
        }
    }

    public class CustomFunction : IFilter
    {
        public System.Windows.Point[] NodeList { get; set; }

        public CustomFunction(System.Windows.Point[] nodeList)
        {
            this.NodeList = nodeList;
        }

        public WriteableBitmap ApplyTo(WriteableBitmap wbm)
        {
            var clone = wbm.Clone();
            var width = clone.PixelWidth;
            var height = clone.PixelHeight;

            try
            {
                wbm.Lock();
                clone.Lock();

                int FilterFunction(int channelValue)
                {
                    for (var i = 0; i < NodeList.Length - 1; i++)
                    {
                        var p1 = NodeList[i];
                        p1.Y = 255 - p1.Y;
                        var p2 = NodeList[i + 1];
                        p2.Y = 255 - p2.Y;

                        if (channelValue >= p1.X && channelValue <= p2.X)
                        {
                            double slope = (double)(p2.Y - p1.Y) / (p2.X - p1.X);
                            double yIntercept = p1.Y - slope * p1.X;

                            return (int)Math.Round(slope * channelValue + yIntercept);
                        }
                    }

                    throw new Exception("Color channel value out of bounds in custom filter");
                }

                for (var x = 0; x < width; x++)
                {
                    for (var y = 0; y < height; y++)
                    {
                        var oldColor = wbm.GetPixelColor(x, y);

                        var newColor = Color.FromArgb(oldColor.A, 
                            Math.Clamp(FilterFunction(oldColor.R), 0, 255), 
                            Math.Clamp(FilterFunction(oldColor.G), 0, 255), 
                            Math.Clamp(FilterFunction(oldColor.B), 0, 255));

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

        public override string ToString()
        {
            return "Custom Function";
        }
    }

    public class Median : IFilter
    {
        public Kernel Kernel { get; set; } = Kernels.Median;
        
        public WriteableBitmap ApplyTo(WriteableBitmap wbm)
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
                        var newColor = CalculateMedianFromKernel(wbm, Kernel, x, y);
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

        private Color CalculateMedianFromKernel(WriteableBitmap wbm, Kernel kernel, int x, int y)
        {
            List<int> redChannel = new List<int>();
            List<int> greenChannel = new List<int>();
            List<int> blueChannel = new List<int>();

            var readPixel = new Point();
            var pixelColor = Color.Empty;
            
            for (var c = 0; c < kernel.Width; c++)
            {
                for (var r = 0; r < kernel.Height; r++)
                {
                    readPixel.X = x + c - kernel.Anchor.X;
                    readPixel.Y = y + r - kernel.Anchor.Y;

                    // mirror edges
                    if (readPixel.X < 0)
                        readPixel.X = -readPixel.X;

                    if (readPixel.X > wbm.PixelWidth - 1)
                        readPixel.X = 2 * wbm.PixelWidth - readPixel.X;

                    if (readPixel.Y < 0)
                        readPixel.Y = -readPixel.Y;

                    if (readPixel.Y >= wbm.PixelHeight)
                        readPixel.Y = 2 * wbm.PixelHeight - readPixel.Y;

                    pixelColor = wbm.GetPixelColor(readPixel.X, readPixel.Y);

                    redChannel.Add(pixelColor.R);
                    greenChannel.Add(pixelColor.G);
                    blueChannel.Add(pixelColor.B);
                }
            }

            redChannel.Sort();
            greenChannel.Sort();
            blueChannel.Sort();

            int median = redChannel.Count / 2;

            return Color.FromArgb(pixelColor.A,
                redChannel[median],
                greenChannel[median],
                blueChannel[median]);
        }

        public override string ToString()
        {
            return "Median";
        }
    }
}