using System;
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

        public Line(Point p1, Point p2, int thickness = 1)
        {
            this.P1 = p1;
            this.P2 = p2;
            this.Thickness = thickness;
        }

        public void Draw(WriteableBitmap wbm)
        {
            double dy = P2.Y - P1.Y;
            double dx = P2.X - P1.X;
            double m = dy/dx;
            double y = P1.Y;

            try
            {
                wbm.Lock();
                if (dx > 0)
                {
                    for (int x = (int)P1.X; x <= P2.X; ++x)
                    {
                        wbm.SetPixelColor(x, (int)Math.Round(y), Color);
                        y += m;
                    }
                }
                else if (dx < 0)
                {
                    for (int x = (int)P1.X; x >= P2.X; --x)
                    {
                        wbm.SetPixelColor(x, (int)Math.Round(y), Color);
                        y -= m;
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
}