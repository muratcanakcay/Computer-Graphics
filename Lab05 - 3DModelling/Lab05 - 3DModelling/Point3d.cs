using System.Windows;
using System.Windows.Media;

namespace Lab05___3DModelling
{
    public struct Point3d
    {
        public Point4 Projected { get; set; }
        public Point4 Global { get; set; }
        public Point4 Normal { get; set; }
        public Point TextureMap { get; set; }
    }

    public class Point4
    {
        public double X;
        public double Y;
        public double Z;
        public double W;

        public Point4(double x, double y, double z, double w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
    }

    public class Pixel
    {
        public int X;
        public int Y;
        public Color Color;

        public Pixel(int x, int y, Color c)
        {
            X = x;
            Y = y;
            Color = c;
        }
    }
}