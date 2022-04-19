using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace Lab03___Rasterization
{
    interface IDrawable
    {
        void Draw(WriteableBitmap wbm);
    }

    public class Line : IDrawable
    {
        public Point P1 { get; set; }
        public Point P2 { get; set; }
        public int Thickness { get; set; }
        public Color Color { get; set; } = Color.FromArgb(0, 0, 0, 1);

        public Line(List<Point> points, int thickness = 1)
        {
            this.P1 = points[0];
            this.P2 = points[1];
            this.Thickness = thickness;
        }

        public void Draw(WriteableBitmap wbm)
        {
            double dy = P2.Y - P1.Y;
            double dx = P2.X - P1.X;

            try
            {
                wbm.Lock();
                
                if (dx != 0 && Math.Abs(dy/dx) < 1)
                {
                    double y = P1.Y;
                    double m = dy/dx;

                    if (dx > 0)
                    {
                        for (int x = (int)P1.X; x <= P2.X; ++x)
                        {
                            wbm.SetPixelColor(x, (int)Math.Round(y), Color);
                            y += m;
                        }
                    }
                    else
                    {
                        for (int x = (int)P1.X; x >= P2.X; --x)
                        {
                            wbm.SetPixelColor(x, (int)Math.Round(y), Color);
                            y -= m;
                        }
                    }
                }
                else if (dy != 0)
                {
                    double x = P1.X;
                    double m = dx/dy;

                    if (dy > 0)
                    {
                        for (int y = (int)P1.Y; y <= P2.Y; ++y)
                        {
                            wbm.SetPixelColor((int)Math.Round(x), y, Color);
                            x += m;
                        }
                    }
                    else
                    {
                        for (int y = (int)P1.Y; y >= P2.Y; --y)
                        {
                            wbm.SetPixelColor((int)Math.Round(x), y, Color);
                            x -= m;
                        }
                    }
                    
                }
            }
            finally
            {
                wbm.Unlock();
            }
        }

        public override string ToString()
        {
            return $"({P1.X}, {P1.Y})-({P2.X}, {P2.Y})";
        }
    }

    public class Polygon : IDrawable
    {
        public List<Point> Points { get; set; }
        public int Thickness { get; set; }
        public Color Color { get; set; } = Color.FromArgb(0, 0, 0, 1);

        public Polygon(List<Point> points, int thickness = 1)
        {
            this.Points = points;
            this.Thickness = thickness;
        }

        public void Draw(WriteableBitmap wbm)
        {
            for (int i = 0; i < Points.Count; i++)
            {
                var endPoint = i < Points.Count - 1 ? Points[i + 1] : Points[0];
                var edge = new Line(new List<Point> {Points[i], endPoint});
                edge.Draw(wbm);
            }
        }

        public override string ToString()
        {

            return $"NOT READY YET"; // ({P1.X}, {P1.Y})-({P2.X}, {P2.Y})";
        }
    }
}