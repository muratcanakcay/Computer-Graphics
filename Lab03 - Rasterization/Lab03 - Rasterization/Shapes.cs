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
        List<Point> GetPoints();
        bool SetPoints(List<Point> points);
    }

    public class Line : IDrawable
    {
        public List<Point> Points { get; set; }
        public int Thickness { get; set; }
        public Color Color { get; set; } = Color.FromArgb(255, 0, 0, 0);

        public Line(List<Point> points, int thickness = 1)
        {
            Points = points.GetRange(0, 2);
            this.Thickness = thickness;
        }

        public void Draw(WriteableBitmap wbm)
        {
            double dy = Points[1].Y - Points[0].Y;
            double dx = Points[1].X - Points[0].X;

            try
            {
                wbm.Lock();
                
                if (dx != 0 && Math.Abs(dy/dx) < 1)
                {
                    double y = Points[0].Y;
                    double m = dy/dx;

                    if (dx > 0)
                    {
                        for (int x = (int)Points[0].X; x <= Points[1].X; ++x)
                        {
                            wbm.SetPixelColor(x, (int)Math.Round(y), Color);
                            y += m;
                        }
                    }
                    else
                    {
                        for (int x = (int)Points[0].X; x >= Points[1].X; --x)
                        {
                            wbm.SetPixelColor(x, (int)Math.Round(y), Color);
                            y -= m;
                        }
                    }
                }
                else if (dy != 0)
                {
                    double x = Points[0].X;
                    double m = dx/dy;

                    if (dy > 0)
                    {
                        for (int y = (int)Points[0].Y; y <= Points[1].Y; ++y)
                        {
                            wbm.SetPixelColor((int)Math.Round(x), y, Color);
                            x += m;
                        }
                    }
                    else
                    {
                        for (int y = (int)Points[0].Y; y >= Points[1].Y; --y)
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

        public List<Point> GetPoints()
        {
            return Points;
        }

        public bool SetPoints(List<Point> points)
        {
            Points = points;
            return true;
        }

        public override string ToString()
        {
            return $"({Points[0].X}, {Points[0].Y})-({Points[1].X}, {Points[1].Y})";
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

        public List<Point> GetPoints()
        {
            return Points;
        }

        public bool SetPoints(List<Point> points)
        {
            Points = points;
            return true;
        }

        public override string ToString()
        {

            return $"POLYGON.TOSTRING NOT IMPLEMENTED YET"; // ({Points[0].X}, {Points[0].Y})-({Points[1].X}, {Points[1].Y})";
        }
    }
}