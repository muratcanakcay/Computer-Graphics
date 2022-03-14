using System;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace Lab01___Image_Filtering
{
    public class Kernel
    {
        public int[,] KernelMatrix { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public Point Anchor { get; set; }
        public int IntensityOffset { get; set; }
        public int D { get; set; }
        
        public Kernel(int[,] kernelMatrix, Point anchor, int intensityOffset = 0)
        {
            KernelMatrix = kernelMatrix;
            Height = kernelMatrix.GetLength(0);
            Width = kernelMatrix.GetLength(1);
            Anchor = anchor;
            IntensityOffset = intensityOffset;            

            if (anchor.X < 0 || anchor.X > Width || anchor.Y < 0 || anchor.Y > Height)
                throw new ArgumentException("Given anchor point is outside the kernel dimensions");

            // calculate D
            D = 0;
            for (var c = 0; c < Width; c++)
            {
                for (var r = 0; r < Height; r++)
                {
                    D += KernelMatrix[r, c];
                }
            }
            D = D == 0 ? 1 : D;
        }
    }
    public static class Kernels
    {
        public static readonly Kernel Blur = new(
            new[,]
            {
                { 1, 1, 1 }, 
                { 1, 1, 1 }, 
                { 1, 1, 1 }
            }, 
            new Point(1, 1), 
            0);

        public static readonly Kernel GaussianBlur = new(
            new[,]
            {
                { 0, 1, 0 }, 
                { 1, 4, 1 }, 
                { 0, 1, 0 }
            }, 
            new Point(1, 1), 
            0);

        public static readonly Kernel Sharpen = new(
            new[,]
            {
                { -1, -1, -1 }, 
                { -1, 9, -1 }, 
                { -1, -1, -1 }
            }, 
            new Point(1, 1), 
            0);

        public static readonly Kernel EdgeDetection = new(
            new[,]
            {
                { 0, 0, 0 }, 
                { 0, 1, -1 }, 
                { 0, 0, 0 }
            }, 
            new Point(1, 1), 
            128);

        public static readonly Kernel Emboss = new(
            new[,]
            {
                { -1, 0, 1 }, 
                { -1, 1, 1 }, 
                { -1, 0, 1 }
            }, 
            new Point(1, 1), 
            0);

        public static readonly Kernel Median = new(
            new[,]
            {
                { 1, 1, 1 }, 
                { 1, 1, 1 }, 
                { 1, 1, 1 }
            }, 
            new Point(1, 1), 
            0);
    }
}