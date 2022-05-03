using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;
using Point = System.Windows.Point;

namespace Lab03___Rasterization
{
    
    internal interface IDrawable
    {
        int Thickness { get; set; }
        Color Color { get; set; }
        List<Point> Points { get; }
        void Draw(WriteableBitmap wbm, bool isAntiAliased = false, bool isSuperSampled = false, int ssaa = 2);
        int GetVertexIndexOf(Point point);
        void MoveVertex(int vertexIndex, Vector offSet);
        int GetEdgeIndexOf(Point point);
        void MoveEdge(int edgeIndex, Vector offSet);
        void MoveShape(Vector offset);
    }

    public abstract class Shape : IDrawable
    {
        public List<Point> Points { get; }
        public int Thickness { get; set; }
        public Color Color { get; set; }
        protected const int GrabDistance = 10;

        protected Shape(List<Point> points, int thickness, Color color)
        {
            Points = points;
            Thickness = thickness;
            Color = color;
        }
        
        public abstract void Draw(WriteableBitmap wbm, bool isAntiAliased = false, bool isSuperSampled = false, int ssaa = 2);

        public virtual int GetVertexIndexOf(Point point)
        {
            foreach (var vertex in Points)
                if (DistanceBetween(vertex, point) < Thickness + GrabDistance)
                    return Points.IndexOf(vertex);

            return -1;
        }
        
        public virtual void MoveVertex(int vertexIndex, Vector offSet)
        {
            Points[vertexIndex] = Point.Add(Points[vertexIndex], offSet);
        }

        public virtual int GetEdgeIndexOf(Point point)
        {
            for (var i = 0; i < Points.Count - 1; i++)
            {
                if (DistanceFromLine(Points[i], Points[i+1], point) < Thickness + GrabDistance &&
                    IsInsideRectangle(Points[i], Points[i+1], point, Thickness + GrabDistance))
                    return i;
            }

            if (DistanceFromLine(Points[^1], Points[0], point) < Thickness + GrabDistance &&
                IsInsideRectangle(Points[^1], Points[0], point, Thickness + GrabDistance))
                return Points.Count - 1;

            return -1;
        }
        
        public virtual void MoveEdge(int edgeIndex, Vector offSet)
        {
            var nextIndex = edgeIndex == Points.Count - 1 ? 0 : edgeIndex + 1;

            Points[edgeIndex] = Point.Add(Points[edgeIndex], offSet);
            Points[nextIndex] = Point.Add(Points[nextIndex], offSet);
        }

        public void MoveShape(Vector offset)
        {
            for (var i = 0; i < Points.Count; i++)
                Points[i] = Point.Add(Points[i], offset);
        }

        protected static double DistanceBetween(Point p1, Point p2)
        {
            var dx = p2.X - p1.X;
            var dy = p2.Y - p1.Y;
            return Math.Round(Math.Sqrt(dx*dx + dy*dy));
        }

        private static double DistanceFromLine(Point onLine1, Point onLine2, Point exterior)
        {
            var denominator = DistanceBetween(onLine1, onLine2);
            var numerator = Math.Abs((onLine2.X - onLine1.X) * (onLine1.Y - exterior.Y) -
                                       (onLine1.X - exterior.X) * (onLine2.Y - onLine1.Y));

            return numerator / denominator;
        }

        private static bool IsInsideRectangle(Point vertex1, Point vertex2, Point p, int offSet = 0)
        {
            // offSet value is used to increase the rectangle size by 2*offSet on each edge
            return (p.X > Math.Min(vertex1.X, vertex2.X) - offSet &&
                    p.X < Math.Max(vertex1.X, vertex2.X) + offSet &&
                    p.Y > Math.Min(vertex1.Y, vertex2.Y) - offSet &&
                    p.Y < Math.Max(vertex1.Y, vertex2.Y) + offSet);
        }

        protected static double Determinant(Point a, Point b, Point c)
        {
            return a.X*b.Y - a.X*c.Y - a.Y*b.X + a.Y*c.X + b.X*c.Y - b.Y*c.X;
        }
    }

    public class Line : Shape 
    {
        public Line(List<Point> points, int thickness, Color color) : base(points, thickness, color) {}

        public override void Draw(WriteableBitmap wbm, bool isAntiAliased = false, bool isSuperSampled = false, int ssaa = 2)
        {
            if (isAntiAliased)
            {
                DrawAntiAliased(wbm);
                return;
            }

            var SSAA = isSuperSampled ? ssaa : 1;

            var x0 = (int)(SSAA * Points[0].X);
            var y0 = (int)(SSAA * Points[0].Y);
            var x1 = (int)(SSAA * Points[1].X);
            var y1 = (int)(SSAA * Points[1].Y);

            double dy = y1 - y0;
            double dx = x1 - x0;

            try
            {
                wbm.Lock();
                
                if (dx != 0 && Math.Abs(dy/dx) < 1)
                {
                    double y = y0;
                    double m = dy/dx;

                    if (dx > 0)
                    {
                        for (var x = x0; x <= x1; ++x)
                        {
                            wbm.ApplyBrush(x, (int)Math.Round(y), SSAA * Thickness, Color);
                            y += m;
                        }
                    }
                    else
                    {
                        for (var x = x0; x >= x1; --x)
                        {
                            wbm.ApplyBrush(x, (int)Math.Round(y), SSAA * Thickness, Color);
                            y -= m;
                        }
                    }
                }
                else if (dy != 0)
                {
                    double x = x0;
                    double m = dx/dy;

                    if (dy > 0)
                    {
                        for (var y = y0; y <= y1; ++y)
                        {
                            wbm.ApplyBrush((int)Math.Round(x), y, SSAA * Thickness, Color);
                            x += m;
                        }
                    }
                    else
                    {
                        for (var y = y0; y >= y1; --y)
                        {
                            wbm.ApplyBrush((int)Math.Round(x), y, SSAA * Thickness, Color);
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

        private void DrawAntiAliased(WriteableBitmap wbm) // using Gupta-Sproull algorithm for lines
        {
            var x0 = Points[0].X;
            var y0 = Points[0].Y;
            var x1 = Points[1].X;
            var y1 = Points[1].Y;
            var dx = (int)(x1 - x0);
            var dy = (int)(y1 - y0);
            var invDenom = 1 / (2 * Math.Sqrt(dx*dx + dy*dy));

            if (dx != 0 && Math.Abs(dy/dx) < 1)
            {
                if (x1 < x0) // swap initial and endpoint for drawing
                {
                    x0 = x1;
                    y0 = y1;
                    x1 = Points[0].X;
                    y1 = Points[0].Y;
                    dx = -dx;
                    dy = -dy;
                }

                var dE = 2 * dy;
                var dXE = y1 > y0 ? 2*(dy - dx) : 2*(dy + dx);
                var d = y1 > y0 ? 2*dy - dx : 2*dy + dx;

                var x = (int)x0;
                var y = (int)y0;

                IntensifyPixel(wbm, x, y, 0);
                for (var i = 1; IntensifyPixel(wbm, x, y + i, 2*i*dx*invDenom); ++i);
                for (var i = 1; IntensifyPixel(wbm, x, y - i, 2*i*dx*invDenom); ++i);

                while (x < x1)
                {
                    ++x;
                    int twoVDx;

                    if (d <= 0) // move to E or SE
                    {
                        twoVDx = d + dx;
                        d += y1 > y0 ? dE : dXE;
                        if (y1 < y0) --y;
                    }
                    else // move to E or NE
                    {
                        twoVDx = d - dx;
                        d += y1 > y0 ? dXE : dE;
                        if (y1 > y0) ++y;
                    }

                    IntensifyPixel(wbm, x, y, twoVDx * invDenom);
                    for (var i = 1; IntensifyPixel(wbm, x, y + i, ((2*i*dx) - twoVDx) * invDenom); ++i);
                    for (var i = 1; IntensifyPixel(wbm, x, y - i, ((2*i*dx) + twoVDx) * invDenom); ++i);
                }
            }
            else if (dy != 0)
            {
                if (y1 < y0)  // swap initial and endpoint for drawing
                {
                    y0 = y1;
                    x0 = x1;
                    y1 = Points[0].Y;
                    x1 = Points[0].X;
                    dy = -dy;
                    dx = -dx;
                }

                var dN = 2 * dx;
                var dNX = x1 > x0 ? 2*(dx - dy) : 2*(dx + dy);
                var d = x1 > x0 ? 2*dx - dy : 2*dx + dy;

                var y = (int)y0;
                var x = (int)x0;

                IntensifyPixel(wbm, x, y, 0);
                for (var i = 1; IntensifyPixel(wbm, x + i, y, 2*i*dy*invDenom); ++i);
                for (var i = 1; IntensifyPixel(wbm, x - i, y, 2*i*dy*invDenom); ++i);

                while (y < y1)
                {
                    ++y;
                    int twoVDy;

                    if (d < 0) // move to N or NW
                    {
                        twoVDy = d + dy;
                        d += x1 > x0 ? dN : dNX;
                        if (x1 < x0) --x;
                    }
                    else // move to N or NE
                    {
                        twoVDy = d - dy;
                        d += x1 > x0 ? dNX : dN;
                        if (x1 > x0) ++x;
                    }

                    IntensifyPixel(wbm, x, y, twoVDy * invDenom);
                    for (var i = 1; IntensifyPixel(wbm, x + i, y, ((2*i*dy) - twoVDy) * invDenom); ++i);
                    for (var i = 1; IntensifyPixel(wbm, x - i, y, ((2*i*dy) + twoVDy) * invDenom); ++i);
                }
            }
        }

        private bool IntensifyPixel(WriteableBitmap wbm, int x, int y, double distance)
        {
            const double r = 0.5f;
            var cov = Coverage(distance, r);

            if (cov > 0)
            {
                try
                {
                    wbm.Lock();
                    var newColor = Color.FromArgb(
                        255,
                        (int)(255 - (255-Color.R) * cov),
                        (int)(255 - (255-Color.G) * cov),
                        (int)(255 - (255-Color.B) * cov));
                    wbm.SetPixelColor(x, y, newColor);
                }
                finally
                {
                    wbm.Unlock();
                }
            }

            return cov>0;
        }

        private double Coverage(double D, double r)
        {
            var w = Thickness - 0.5d;

            if (w >= r)
            {
                if (w <= D)
                    return Cov(D - w, r);
                if (D < w)
                    return 1 - Cov(w - D, r);
            }
            else
            {
                if (D <= w)
                    return 1 - Cov(w - D, r) - Cov(w + D, r);
                if (w < D && D <= r-w)
                    return Cov(D - w, r) - Cov(D + w, r);
                if (r - w < D && D <= r + w)
                    return Cov(D - w, r);
            }

            return 0;
        }

        private static double Cov(double d, double r)
        {
            if (d >= r) return 0;

            return ((1/Math.PI) * Math.Acos(d/r)) - ((d / (Math.PI*r*r)) * Math.Sqrt((r*r) - (d*d)));
        }

        public override string ToString()
        {
            return $"({Points[0].X}, {Points[0].Y})-({Points[1].X}, {Points[1].Y})";
        }
    }

    public class Polygon : Shape
    {
        public Polygon(List<Point> points, int thickness, Color color) : base(points, thickness, color) {}

        public override void Draw(WriteableBitmap wbm, bool isAntiAliased = false, bool isSuperSampled = false, int ssaa = 2)
        {
            for (var i = 0; i < Points.Count; i++)
            {
                var endPoint = i < Points.Count-1 ? Points[i+1] : Points[0];
                var edge = new Line(new List<Point> {Points[i], endPoint}, Thickness, Color);
                edge.Draw(wbm, isAntiAliased, isSuperSampled, ssaa);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            
            for (var i = 0; i < Points.Count; i++)
                sb.Append($"P{i}: ({Points[i].X}, {Points[i].Y})\n");            

            return sb.ToString();
        }
    }

    public class Circle : Shape
    {
        private Point Center => Points[0];
        private int Radius => (int)Math.Round(DistanceBetween(Points[0], Points[1]));
        public Circle(List<Point> points, int thickness, Color color) : base(points, thickness, color) {}
        
        public override void Draw(WriteableBitmap wbm, bool isAntiAliased = false, bool isSuperSampled = false, int ssaa = 2)
        {
            var SSAA = isSuperSampled ? ssaa : 1;
            
            var x  = 0;
            var y  = SSAA * Radius;
            var d  = 1 - (SSAA * Radius);
            var xC = (int)(SSAA * Center.X);
            var yC = (int)(SSAA * Center.Y);
            
            var plusMinus = new[] { -1, 1 };

            try
            {
                wbm.Lock();

                foreach (var i in plusMinus)
                {
                    wbm.ApplyBrush(xC + x, yC + i*y, SSAA * Thickness, Color);
                    wbm.ApplyBrush(xC + i*y, yC + x, SSAA * Thickness, Color);
                }

                while (y > x)
                {
                    if (d < 0)
                        //move to E
                        d += 2*x + 3;
                    else //move to NE
                    {
                        d += 2*x - 2*y + 5;
                        --y;
                    }

                    ++x;

                    foreach (var i in plusMinus)
                        foreach (var j in plusMinus)
                        {
                            wbm.ApplyBrush(xC + i*x, yC + j*y, SSAA*Thickness, Color);
                            wbm.ApplyBrush(xC + i*y, yC + j*x, SSAA*Thickness, Color);
                        }
                }
            }
            finally
            {
                wbm.Unlock();
            }
        }

        public override int GetVertexIndexOf(Point point)
        {
            return -1; // circle has no vertices
        }
        
        public override int GetEdgeIndexOf(Point point)
        {
            if (DistanceBetween(Center, point) < Radius - Thickness - GrabDistance || 
                DistanceBetween(Center, point) > Radius + Thickness + GrabDistance)
                return -1;
            
            // change edgePoint to the closest point on circle to the clicked point
            var v = Point.Subtract(point, Points[0]); 
            var vUnit = v / v.Length;
            var newEdgePoint = Point.Add(Center, Radius * vUnit);
            Points[1] = newEdgePoint;

            return 1; // circle has only one edge with index 1
        }

        public override void MoveEdge(int edgeIndex, Vector offSet)
        {
            Points[edgeIndex] = Point.Add(Points[edgeIndex], offSet);
        }

        public override string ToString()
        {
            return $"Center:({Center.X}, {Center.Y}) - Radius:{Radius})";
        }
    }

    public class CircleArc : Shape // TODO: correctly implement Edge and Vertex movement
    {
        private Point Center => Points[0];
        private int Radius => (int)Math.Round(DistanceBetween(Points[0], Points[1]));
        public CircleArc(List<Point> points, int thickness, Color color) : base(points, thickness, color) {}
        
        public override void Draw(WriteableBitmap wbm, bool isAntiAliased = false, bool isSuperSampled = false, int ssaa = 2)
        {
            var SSAA = isSuperSampled ? ssaa : 1;
            
            var x  = 0;
            var y  = SSAA * Radius;
            var d  = 1 - (SSAA * Radius);
            var xC = (int)(SSAA * Center.X);
            var yC = (int)(SSAA * Center.Y);

            var det = Determinant(Center, Points[1], Points[2]);

            try
            {
                wbm.Lock();
                
                var plusMinus = new[] { -1, 1 };

                foreach(var i in plusMinus)
                    if (det > 0)
                    {
                        if (Determinant(Center, Points[1], new Point(Center.X + x, Center.Y + i*y)) > 0 &&
                            Determinant(Center, Points[2], new Point(Center.X + x, Center.Y + i*y)) < 0)
                            wbm.ApplyBrush(xC + x, yC + i*y, SSAA*Thickness, Color);

                        if (Determinant(Center, Points[1], new Point(Center.X + i*y, Center.Y + x)) > 0 &&
                            Determinant(Center, Points[2], new Point(Center.X + i*y, Center.Y + x)) < 0)
                            wbm.ApplyBrush(xC + i*y, yC + x, SSAA*Thickness, Color);
                    }
                    else
                    {
                        if (Determinant(Center, Points[1], new Point(Center.X + x, Center.Y + i*y)) > 0 ||
                            Determinant(Center, Points[2], new Point(Center.X + x, Center.Y + i*y)) < 0)
                            wbm.ApplyBrush(xC + x, yC + y, SSAA*Thickness, Color);

                        if (Determinant(Center, Points[1], new Point(Center.X + i*y, Center.Y + x)) > 0 ||
                            Determinant(Center, Points[2], new Point(Center.X + i*y, Center.Y + x)) < 0)
                            wbm.ApplyBrush(xC + i*y, yC + x, SSAA*Thickness, Color);
                    }

                while (y > x)
                {
                    if (d < 0)
                        //move to E
                        d += 2*x + 3;
                    else //move to NE
                    {
                        d += 2*x - 2*y + 5;
                        --y;
                    }

                    ++x;

                    foreach(var i in plusMinus)
                        foreach(var j in plusMinus)
                            if (det > 0)
                            {

                                if (Determinant(Center, Points[1], new Point(Center.X + i*x, Center.Y + j*y)) > 0 &&
                                    Determinant(Center, Points[2], new Point(Center.X + i*x, Center.Y + j*y)) < 0)
                                    wbm.ApplyBrush(xC + i*x, yC + j*y, SSAA*Thickness, Color);

                                if (Determinant(Center, Points[1], new Point(Center.X + i*y, Center.Y + j*x)) > 0 &&
                                    Determinant(Center, Points[2], new Point(Center.X + i*y, Center.Y + j*x)) < 0)
                                    wbm.ApplyBrush(xC + i*y, yC + j*x, SSAA*Thickness, Color);
                            }
                            else
                            {
                                if (Determinant(Center, Points[1], new Point(Center.X + i*x, Center.Y + j*y)) > 0 ||
                                    Determinant(Center, Points[2], new Point(Center.X + i*x, Center.Y + j*y)) < 0)
                                    wbm.ApplyBrush(xC + i*x, yC + j*y, SSAA*Thickness, Color);

                                if (Determinant(Center, Points[1], new Point(Center.X + i*y, Center.Y + j*x)) > 0 ||
                                    Determinant(Center, Points[2], new Point(Center.X + i*y, Center.Y + j*x)) < 0)
                                    wbm.ApplyBrush(xC + i*y, yC + j*x, SSAA*Thickness, Color);
                            }
                }
            }
            finally
            {
                wbm.Unlock();
            }
        }

        public override int GetVertexIndexOf(Point point)
        {
            return -1; // circle has no vertices
        }
        
        public override int GetEdgeIndexOf(Point point)
        {
            if (DistanceBetween(Center, point) < Radius - Thickness - GrabDistance || 
                DistanceBetween(Center, point) > Radius + Thickness + GrabDistance)
                return -1;
            
            // change edgePoint to -> the point on circle that is closest to the clicked point
            var v = Point.Subtract(point, Points[0]); 
            var vUnit = v / v.Length;
            var newEdgePoint = Point.Add(Center, Radius * vUnit);
            Points[1] = newEdgePoint;

            return 1; // circle has only one edge with index 1
        }

        public override void MoveEdge(int edgeIndex, Vector offSet)
        {
            Points[edgeIndex] = Point.Add(Points[edgeIndex], offSet);
        }

        public override string ToString()
        {
            return $"Center:({Center.X}, {Center.Y}) - Radius:{Radius} - P1:({Points[1].X}, {Points[1].Y} - P2: ({Points[2].X}, {Points[2].Y}))";
        }
    }
}