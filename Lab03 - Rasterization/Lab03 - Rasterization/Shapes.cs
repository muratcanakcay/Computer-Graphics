using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace Lab03___Rasterization
{
    
    internal interface IDrawable
    {
        void Draw(WriteableBitmap wbm);
        int GetVertexIndexOf(Point point);
        void MoveVertex(int vertexIndex, Vector offSet);
        int GetEdgeIndexOf(Point point);
        void MoveEdge(int edgeIndex, Vector offSet);
    }

    public abstract class Shape : IDrawable
    {
        private const uint GrabDistance = 10;
        protected List<Point> Points = new();
        protected uint Thickness;
        protected Color Color = Color.FromArgb(255, 0, 0, 0);
        protected int CanvasHeight;
        protected int CanvasWidth;
        
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

            // checking for canvas boundaries not needed if setPixelColor just returns without throwing an exception
            //if (IsInsideRectangle(new Point(0, 0), new Point(CanvasWidth, CanvasHeight), newP1) &&
            //    IsInsideRectangle(new Point(0, 0), new Point(CanvasWidth, CanvasHeight), newP2))
            {
                Points[edgeIndex] = newP1;
                Points[nextIndex] = newP2;
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
    }

    public class Line : Shape
    {
        public Line(List<Point> points, uint thickness = 1)
        {
            Points = points.GetRange(0, 2);
            Thickness = thickness;
        }

        public override void Draw(WriteableBitmap wbm)
        {
            CanvasHeight = wbm.PixelHeight;
            CanvasWidth = wbm.PixelWidth;
            
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
                            wbm.SetPixelColor(x, (int)Math.Round(y), Color);
                            y += m;
                        }
                    }
                    else
                    {
                        for (var x = (int)Points[0].X; x >= Points[1].X; --x)
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
                        for (var y = (int)Points[0].Y; y <= Points[1].Y; ++y)
                        {
                            wbm.SetPixelColor((int)Math.Round(x), y, Color);
                            x += m;
                        }
                    }
                    else
                    {
                        for (var y = (int)Points[0].Y; y >= Points[1].Y; --y)
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
            return $"({Points[0].X}, {Points[0].Y})-({Points[1].X}, {Points[1].Y})";
        }
    }

    public class Polygon : Shape
    {
        public Polygon(List<Point> points, uint thickness = 1)
        {
            Points = points;
            Thickness = thickness;
        }

        public override void Draw(WriteableBitmap wbm)
        {
            CanvasHeight = wbm.PixelHeight;
            CanvasWidth = wbm.PixelWidth;
            
            for (var i = 0; i < Points.Count; i++)
            {
                var endPoint = i < Points.Count - 1 ? Points[i + 1] : Points[0];
                var edge = new Line(new List<Point> {Points[i], endPoint});
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
        public double Radius => DistanceBetween(Points[0], Points[1]);
        public Circle(List<Point> points, uint thickness = 1)
        {
            Points = points;
            Thickness = thickness;
        }
        
        public override void Draw(WriteableBitmap wbm)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {

            return $"Center:({Center.X}, {Center.Y})-Radius:{Radius})";
        }
    }
}