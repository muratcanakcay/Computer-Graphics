using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace Lab03___Rasterization
{
    
    internal interface IDrawable
    {
        uint Thickness { get; set; }
        Color Color { get; set; }
        List<Point> Points { get; }
        void Draw(WriteableBitmap wbm);
        int GetVertexIndexOf(Point point);
        void MoveVertex(int vertexIndex, Vector offSet);
        int GetEdgeIndexOf(Point point);
        void MoveEdge(int edgeIndex, Vector offSet);
        void MoveShape(Vector offset);
    }

    public abstract class Shape : IDrawable
    {
        public List<Point> Points { get; protected set; }
        public uint Thickness { get; set; }
        public Color Color { get; set; }
        protected const uint GrabDistance = 10;

        protected Shape(List<Point> points, uint thickness, Color color)
        {
            Points = points;
            Thickness = thickness;
            Color = color;
        }
        
        public abstract void Draw(WriteableBitmap wbm);

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
                    IsInsideRectangle(Points[i], Points[i + 1], point, Thickness + GrabDistance))
                    return i;
            }

            if (DistanceFromLine(Points[^1], Points[0], point) < Thickness + GrabDistance &&
                IsInsideRectangle(Points[^1], Points[0], point, Thickness + GrabDistance))
                return Points.Count - 1;

            return -1;
        }
        
        public virtual void MoveEdge(int edgeIndex, Vector offSet)
        {
            var newP1 = Point.Add(Points[edgeIndex], offSet);
            var nextIndex = edgeIndex == Points.Count - 1 ? 0 : edgeIndex + 1;
            var newP2 = Point.Add(Points[nextIndex], offSet);

            Points[edgeIndex] = newP1;
            Points[nextIndex] = newP2;
        }

        public void MoveShape(Vector offset)
        {
            for (int i = 0; i < Points.Count; i++)
            {
                var newP = Point.Add(Points[i], offset);
                Points[i] = newP;
            }
        }

        protected static double DistanceBetween(Point p1, Point p2)
        {
            return Math.Round(Math.Sqrt(Math.Pow((p2.X - p1.X), 2) + Math.Pow((p2.Y - p1.Y), 2)));
        }

        protected static double DistanceFromLine(Point onLine1, Point onLine2, Point exterior)
        {
            var denominator = DistanceBetween(onLine1, onLine2);
            var numerator = Math.Abs((onLine2.X - onLine1.X) * (onLine1.Y - exterior.Y) -
                                       (onLine1.X - exterior.X) * (onLine2.Y - onLine1.Y));

            return numerator / denominator;
        }

        protected static bool IsInsideRectangle(Point vertex1, Point vertex2, Point p, uint offSet = 0)
        {
            // offSet value is used to increase the rectangle size by 2*offSet on each edge
            return (p.X > Math.Min(vertex1.X, vertex2.X) - offSet &&
                    p.X < Math.Max(vertex1.X, vertex2.X) + offSet &&
                    p.Y > Math.Min(vertex1.Y, vertex2.Y) - offSet &&
                    p.Y < Math.Max(vertex1.Y, vertex2.Y) + offSet);
        }

        protected static double Determinant(Point a, Point b, Point c)
        {
            return a.X * b.Y - a.X * c.Y - a.Y * b.X + a.Y * c.X + b.X * c.Y - b.Y * c.X;
        }
    }

    public class Line : Shape
    {
        public Line(List<Point> points, uint thickness, Color color) : base(points, thickness, color) {}

        public override void Draw(WriteableBitmap wbm)
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
                        for (var x = (int)Points[0].X; x <= Points[1].X; ++x)
                        {
                            wbm.ApplyBrush(x, (int)Math.Round(y), Thickness, Color);
                            //wbm.SetPixelColor(x, (int)Math.Round(y), Color);
                            y += m;
                        }
                    }
                    else
                    {
                        for (var x = (int)Points[0].X; x >= Points[1].X; --x)
                        {
                            wbm.ApplyBrush(x, (int)Math.Round(y), Thickness, Color);
                            //wbm.SetPixelColor(x, (int)Math.Round(y), Color);
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
                        for (var y = (int)Points[0].Y; y <= Points[1].Y; ++y)
                        {
                            wbm.ApplyBrush((int)Math.Round(x), y, Thickness, Color);
                            //wbm.SetPixelColor((int)Math.Round(x), y, Color);
                            x += m;
                        }
                    }
                    else
                    {
                        for (var y = (int)Points[0].Y; y >= Points[1].Y; --y)
                        {
                            wbm.ApplyBrush((int)Math.Round(x), y, Thickness, Color);
                            //wbm.SetPixelColor((int)Math.Round(x), y, Color);
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
            return $"({Points[0].X}, {Points[0].Y})-({Points[1].X}, {Points[1].Y})";
        }
    }

    public class Polygon : Shape
    {
        public Polygon(List<Point> points, uint thickness, Color color) : base(points, thickness, color) {}

        public override void Draw(WriteableBitmap wbm)
        {
            for (var i = 0; i < Points.Count; i++)
            {
                var endPoint = i < Points.Count - 1 ? Points[i + 1] : Points[0];
                var edge = new Line(new List<Point> {Points[i], endPoint}, Thickness, Color);
                edge.Draw(wbm);
            }
        }

        public override string ToString()
        {

            return $"POLYGON.TOSTRING NOT IMPLEMENTED YET"; // ({Points[0].X}, {Points[0].Y})-({Points[1].X}, {Points[1].Y})";
        }
    }

    public class Circle : Shape
    {
        public Point Center => Points[0];
        public int Radius => (int)Math.Round(DistanceBetween(Points[0], Points[1]));
        public Circle(List<Point> points, uint thickness, Color color) : base(points, thickness, color) {}
        
        public override void Draw(WriteableBitmap wbm)
        {
            int x = 0;
            int y = Radius;
            int d = 1-Radius;

            try
            {
                wbm.Lock();
                
                wbm.ApplyBrush((int)Center.X + x, (int)Center.Y + y, Thickness, Color);
                wbm.ApplyBrush((int)Center.X + x, (int)Center.Y - y, Thickness, Color);

                wbm.ApplyBrush((int)Center.X + y, (int)Center.Y + x, Thickness, Color);
                wbm.ApplyBrush((int)Center.X - y, (int)Center.Y + x, Thickness, Color);
                
                while (y > x)
                {
                    if (d < 0)
                        //move to E
                        d += 2 * x + 3;
                    else //move to NE
                    {
                        d += 2 * x - 2 * y + 5;
                        --y;
                    }

                    ++x;
                    
                    wbm.ApplyBrush((int)Center.X + x, (int)Center.Y + y, Thickness, Color);
                    wbm.ApplyBrush((int)Center.X + x, (int)Center.Y - y, Thickness, Color);
                    
                    wbm.ApplyBrush((int)Center.X - x, (int)Center.Y + y, Thickness, Color);
                    wbm.ApplyBrush((int)Center.X - x, (int)Center.Y - y, Thickness, Color);
                    
                    wbm.ApplyBrush((int)Center.X + y, (int)Center.Y + x, Thickness, Color);
                    wbm.ApplyBrush((int)Center.X - y, (int)Center.Y + x, Thickness, Color);
                    
                    wbm.ApplyBrush((int)Center.X + y, (int)Center.Y - x, Thickness, Color);
                    wbm.ApplyBrush((int)Center.X - y, (int)Center.Y - x, Thickness, Color);
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
            if (DistanceBetween(Center, point) < Radius + Thickness + GrabDistance &&
                DistanceBetween(Center, point) > Radius - Thickness - GrabDistance)
            {
                // change edgePoint to the closest point on circle to the clicked point
                var v = Point.Subtract(point, Points[0]); 
                var vUnit = v / v.Length;
                var newEdgePoint = Point.Add(Center, Radius * vUnit);
                Points[1] = newEdgePoint;

                return 1; // circle has only one edge with index 1
            }

            return -1;
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
        public Point Center => Points[0];
        public int Radius => (int)Math.Round(DistanceBetween(Points[0], Points[1]));
        public CircleArc(List<Point> points, uint thickness, Color color) : base(points, thickness, color) {}
        
        public override void Draw(WriteableBitmap wbm)
        {
            int x = 0;
            int y = Radius;
            int d = 1-Radius;
            double det = Determinant(Center, Points[1], Points[2]);

            try
            {
                wbm.Lock();

                if (det > 0)
                {
                    if (Determinant(Center, Points[1], new Point((int)Center.X + x, (int)Center.Y + y)) > 0 &&
                        Determinant(Center, Points[2], new Point((int)Center.X + x, (int)Center.Y + y)) < 0)
                        wbm.ApplyBrush((int)Center.X + x, (int)Center.Y + y, Thickness, Color);
                    
                    if (Determinant(Center, Points[1], new Point((int)Center.X + x, (int)Center.Y - y)) > 0 &&
                        Determinant(Center, Points[2], new Point((int)Center.X + x, (int)Center.Y - y)) < 0)
                        wbm.ApplyBrush((int)Center.X + x, (int)Center.Y - y, Thickness, Color);

                    if (Determinant(Center, Points[1], new Point((int)Center.X + y, (int)Center.Y + x)) > 0 &&
                        Determinant(Center, Points[2], new Point((int)Center.X + y, (int)Center.Y + x)) < 0)
                        wbm.ApplyBrush((int)Center.X + y, (int)Center.Y + x, Thickness, Color);

                    if (Determinant(Center, Points[1], new Point((int)Center.X - y, (int)Center.Y + x)) > 0 &&
                        Determinant(Center, Points[2], new Point((int)Center.X - y, (int)Center.Y + x)) < 0)
                        wbm.ApplyBrush((int)Center.X - y, (int)Center.Y + x, Thickness, Color);
                }
                else
                {
                    if (Determinant(Center, Points[1], new Point((int)Center.X + x, (int)Center.Y + y)) > 0 ||
                        Determinant(Center, Points[2], new Point((int)Center.X + x, (int)Center.Y + y)) < 0)
                        wbm.ApplyBrush((int)Center.X + x, (int)Center.Y + y, Thickness, Color);
                    
                    if (Determinant(Center, Points[1], new Point((int)Center.X + x, (int)Center.Y - y)) > 0 ||
                        Determinant(Center, Points[2], new Point((int)Center.X + x, (int)Center.Y - y)) < 0)
                        wbm.ApplyBrush((int)Center.X + x, (int)Center.Y - y, Thickness, Color);

                    if (Determinant(Center, Points[1], new Point((int)Center.X + y, (int)Center.Y + x)) > 0 ||
                        Determinant(Center, Points[2], new Point((int)Center.X + y, (int)Center.Y + x)) < 0)
                        wbm.ApplyBrush((int)Center.X + y, (int)Center.Y + x, Thickness, Color);

                    if (Determinant(Center, Points[1], new Point((int)Center.X - y, (int)Center.Y + x)) > 0 ||
                        Determinant(Center, Points[2], new Point((int)Center.X - y, (int)Center.Y + x)) < 0)
                        wbm.ApplyBrush((int)Center.X - y, (int)Center.Y + x, Thickness, Color);
                }

                while (y > x)
                {
                    if (d < 0)
                        //move to E
                        d += 2 * x + 3;
                    else //move to NE
                    {
                        d += 2 * x - 2 * y + 5;
                        --y;
                    }

                    ++x;

                    if (det > 0)
                    {
                        if (Determinant(Center, Points[1], new Point((int)Center.X + x, (int)Center.Y + y)) > 0 &&
                            Determinant(Center, Points[2], new Point((int)Center.X + x, (int)Center.Y + y)) < 0)
                            wbm.ApplyBrush((int)Center.X + x, (int)Center.Y + y, Thickness, Color);
                        
                        if (Determinant(Center, Points[1], new Point((int)Center.X + x, (int)Center.Y - y)) > 0 &&
                            Determinant(Center, Points[2], new Point((int)Center.X + x, (int)Center.Y - y)) < 0)
                            wbm.ApplyBrush((int)Center.X + x, (int)Center.Y - y, Thickness, Color);

                        if (Determinant(Center, Points[1], new Point((int)Center.X - x, (int)Center.Y + y)) > 0 &&
                            Determinant(Center, Points[2], new Point((int)Center.X - x, (int)Center.Y + y)) < 0)
                            wbm.ApplyBrush((int)Center.X - x, (int)Center.Y + y, Thickness, Color);
                        
                        if (Determinant(Center, Points[1], new Point((int)Center.X - x, (int)Center.Y - y)) > 0 &&
                            Determinant(Center, Points[2], new Point((int)Center.X - x, (int)Center.Y - y)) < 0)
                            wbm.ApplyBrush((int)Center.X - x, (int)Center.Y - y, Thickness, Color);

                        if (Determinant(Center, Points[1], new Point((int)Center.X + y, (int)Center.Y + x)) > 0 &&
                            Determinant(Center, Points[2], new Point((int)Center.X + y, (int)Center.Y + x)) < 0)
                            wbm.ApplyBrush((int)Center.X + y, (int)Center.Y + x, Thickness, Color);

                        if (Determinant(Center, Points[1], new Point((int)Center.X - y, (int)Center.Y + x)) > 0 &&
                            Determinant(Center, Points[2], new Point((int)Center.X - y, (int)Center.Y + x)) < 0)
                            wbm.ApplyBrush((int)Center.X - y, (int)Center.Y + x, Thickness, Color);

                        if (Determinant(Center, Points[1], new Point((int)Center.X + y, (int)Center.Y - x)) > 0 &&
                            Determinant(Center, Points[2], new Point((int)Center.X + y, (int)Center.Y - x)) < 0)
                            wbm.ApplyBrush((int)Center.X + y, (int)Center.Y - x, Thickness, Color);

                        if (Determinant(Center, Points[1], new Point((int)Center.X - y, (int)Center.Y - x)) > 0 &&
                            Determinant(Center, Points[2], new Point((int)Center.X - y, (int)Center.Y - x)) < 0)
                            wbm.ApplyBrush((int)Center.X - y, (int)Center.Y - x, Thickness, Color);
                    }
                    else
                    {
                        if (Determinant(Center, Points[1], new Point((int)Center.X + x, (int)Center.Y + y)) > 0 ||
                            Determinant(Center, Points[2], new Point((int)Center.X + x, (int)Center.Y + y)) < 0)
                            wbm.ApplyBrush((int)Center.X + x, (int)Center.Y + y, Thickness, Color);
                        
                        if (Determinant(Center, Points[1], new Point((int)Center.X + x, (int)Center.Y - y)) > 0 ||
                            Determinant(Center, Points[2], new Point((int)Center.X + x, (int)Center.Y - y)) < 0)
                            wbm.ApplyBrush((int)Center.X + x, (int)Center.Y - y, Thickness, Color);

                        if (Determinant(Center, Points[1], new Point((int)Center.X - x, (int)Center.Y + y)) > 0 ||
                            Determinant(Center, Points[2], new Point((int)Center.X - x, (int)Center.Y + y)) < 0)
                            wbm.ApplyBrush((int)Center.X - x, (int)Center.Y + y, Thickness, Color);
                        
                        if (Determinant(Center, Points[1], new Point((int)Center.X - x, (int)Center.Y - y)) > 0 ||
                            Determinant(Center, Points[2], new Point((int)Center.X - x, (int)Center.Y - y)) < 0)
                            wbm.ApplyBrush((int)Center.X - x, (int)Center.Y - y, Thickness, Color);

                        if (Determinant(Center, Points[1], new Point((int)Center.X + y, (int)Center.Y + x)) > 0 ||
                            Determinant(Center, Points[2], new Point((int)Center.X + y, (int)Center.Y + x)) < 0)
                            wbm.ApplyBrush((int)Center.X + y, (int)Center.Y + x, Thickness, Color);

                        if (Determinant(Center, Points[1], new Point((int)Center.X - y, (int)Center.Y + x)) > 0 ||
                            Determinant(Center, Points[2], new Point((int)Center.X - y, (int)Center.Y + x)) < 0)
                            wbm.ApplyBrush((int)Center.X - y, (int)Center.Y + x, Thickness, Color);

                        if (Determinant(Center, Points[1], new Point((int)Center.X + y, (int)Center.Y - x)) > 0 ||
                            Determinant(Center, Points[2], new Point((int)Center.X + y, (int)Center.Y - x)) < 0)
                            wbm.ApplyBrush((int)Center.X + y, (int)Center.Y - x, Thickness, Color);

                        if (Determinant(Center, Points[1], new Point((int)Center.X - y, (int)Center.Y - x)) > 0 ||
                            Determinant(Center, Points[2], new Point((int)Center.X - y, (int)Center.Y - x)) < 0)
                            wbm.ApplyBrush((int)Center.X - y, (int)Center.Y - x, Thickness, Color);
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
            if (DistanceBetween(Center, point) < Radius + Thickness + GrabDistance &&
                DistanceBetween(Center, point) > Radius - Thickness - GrabDistance)
            {
                // change edgePoint to -> the point on circle that is closest to the clicked point
                var v = Point.Subtract(point, Points[0]); 
                var vUnit = v / v.Length;
                var newEdgePoint = Point.Add(Center, Radius * vUnit);
                Points[1] = newEdgePoint;

                return 1; // circle has only one edge with index 1
            }

            return -1;
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