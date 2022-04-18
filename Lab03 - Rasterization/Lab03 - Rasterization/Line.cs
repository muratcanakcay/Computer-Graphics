using System.Windows;

namespace Lab03___Rasterization
{
    public class Line
    {
        public Point P1 { get; set; }
        public Point P2 { get; set; }
        public int Thickness { get; set; }

        public Line(Point p2, Point p1, int thickness)
        {
            this.P2 = p2;
            this.P1 = p1;
            this.Thickness = thickness;
        }

        

    }
}