using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace Lab03___Rasterization
{
    interface IDrawable
    {
        void Draw(WriteableBitmap wbm);
        int GetVertexIndexOf(Point point);
        void MoveVertex(int vertexIndex, Vector offSet);
        int GetEdgeIndexOf(Point point);
        void MoveEdge(int edgeIndex, Vector offSet);
    }

    public abstract class Shape : IDrawable
    {
        protected List<Point> _points;
        protected int _thickness;
        protected Color _color = Color.FromArgb(255, 0, 0, 0);
        protected int _canvasHeight;
        protected int _canvasWidth;
        public abstract void Draw(WriteableBitmap wbm);
        public abstract int GetVertexIndexOf(Point point);
        public abstract void  MoveVertex(int vertexIndex, Vector offSet);
        public abstract int GetEdgeIndexOf(Point point);
        public abstract void MoveEdge(int edgeIndex, Vector offSet);
        


        protected double DistanceBetween(Point p1, Point p2)
        {
            return (int)Math.Round(Math.Sqrt(Math.Pow((p2.X - p1.X), 2) + Math.Pow((p2.Y - p1.Y), 2)));
        }

        protected double DistanceFromLine(Point onLine1, Point onLine2, Point exterior)
        {
            var denominator = DistanceBetween(onLine1, onLine2);
            var numerator = Math.Abs((onLine2.X - onLine1.X) * (onLine1.Y - exterior.Y) -
                                       (onLine1.X - exterior.X) * (onLine2.Y - onLine1.Y));

            return numerator / denominator;
        }

        protected bool IsInsideRectangle(Point vertex1, Point vertex2, Point p)
        {
            return (p.X > Math.Min(vertex1.X, vertex2.X) &&
                    p.X < Math.Max(vertex1.X, vertex2.X) &&
                    p.Y > Math.Min(vertex1.Y, vertex2.Y) &&
                    p.Y < Math.Max(vertex1.Y, vertex2.Y));
        }
    }

    public class Line : Shape
    {
        public Line(List<Point> points, int thickness = 1)
        {
            _points = points.GetRange(0, 2);
            _thickness = thickness;
        }

        public override void Draw(WriteableBitmap wbm)
        {
            _canvasHeight = wbm.PixelHeight;
            _canvasWidth = wbm.PixelWidth;
            
            double dy = _points[1].Y - _points[0].Y;
            double dx = _points[1].X - _points[0].X;

            try
            {
                wbm.Lock();
                
                if (dx != 0 && Math.Abs(dy/dx) < 1)
                {
                    double y = _points[0].Y;
                    double m = dy/dx;

                    if (dx > 0)
                    {
                        for (int x = (int)_points[0].X; x <= _points[1].X; ++x)
                        {
                            wbm.SetPixelColor(x, (int)Math.Round(y), _color);
                            y += m;
                        }
                    }
                    else
                    {
                        for (int x = (int)_points[0].X; x >= _points[1].X; --x)
                        {
                            wbm.SetPixelColor(x, (int)Math.Round(y), _color);
                            y -= m;
                        }
                    }
                }
                else if (dy != 0)
                {
                    double x = _points[0].X;
                    double m = dx/dy;

                    if (dy > 0)
                    {
                        for (int y = (int)_points[0].Y; y <= _points[1].Y; ++y)
                        {
                            wbm.SetPixelColor((int)Math.Round(x), y, _color);
                            x += m;
                        }
                    }
                    else
                    {
                        for (int y = (int)_points[0].Y; y >= _points[1].Y; --y)
                        {
                            wbm.SetPixelColor((int)Math.Round(x), y, _color);
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

        public override int GetVertexIndexOf(Point point)
        {
            foreach (var vertex in _points)
                if (DistanceBetween(vertex, point) < _thickness + 10)
                    return _points.IndexOf(vertex);

            return -1;
        }

        public override void MoveVertex(int vertexIndex, Vector offSet)
        {
            _points[vertexIndex] = Point.Add(_points[vertexIndex], offSet);
        }

        public override int GetEdgeIndexOf(Point point)
        {
            if (DistanceFromLine(_points[0], _points[1], point) < _thickness + 10 &&
                IsInsideRectangle(_points[0], _points[1], point))
                return 0;

            return -1;
        }

        public override void MoveEdge(int edgeIndex, Vector offSet)
        {
            var newP1 = Point.Add(_points[edgeIndex], offSet);
            var nextIndex = edgeIndex == _points.Count - 1 ? 0 : edgeIndex + 1;
            var newP2 = Point.Add(_points[nextIndex], offSet);

            if (IsInsideRectangle(new Point(0, 0), new Point(_canvasWidth, _canvasHeight), newP1) &&
                IsInsideRectangle(new Point(0, 0), new Point(_canvasWidth, _canvasHeight), newP2))
            {
                _points[edgeIndex] = newP1;
                _points[nextIndex] = newP2;
            }
        }

        public override string ToString()
        {
            return $"({_points[0].X}, {_points[0].Y})-({_points[1].X}, {_points[1].Y})";
        }
    }

    public class Polygon : Shape
    {
        public Polygon(List<Point> points, int thickness = 1)
        {
            _points = points;
            _thickness = thickness;
        }

        public override void Draw(WriteableBitmap wbm)
        {
            _canvasHeight = wbm.PixelHeight;
            _canvasWidth = wbm.PixelWidth;
            
            for (int i = 0; i < _points.Count; i++)
            {
                var endPoint = i < _points.Count - 1 ? _points[i + 1] : _points[0];
                var edge = new Line(new List<Point> {_points[i], endPoint});
                edge.Draw(wbm);
            }
        }

        public override int GetVertexIndexOf(Point point)
        {
            foreach (var vertex in _points)
                if (DistanceBetween(vertex, point) < _thickness + 10)
                    return _points.IndexOf(vertex);

            return -1;
        }

        public override void MoveVertex(int vertexIndex, Vector offSet)
        {
            _points[vertexIndex] = Point.Add(_points[vertexIndex], offSet);
        }

        public override int GetEdgeIndexOf(Point point)
        {
            for (int i = 0; i < _points.Count - 1; i++)
            {
                if (DistanceFromLine(_points[i], _points[i+1], point) < _thickness + 10 &&
                    IsInsideRectangle(_points[i], _points[i + 1], point))
                    return i;
            }

            if (DistanceFromLine(_points[^1], _points[0], point) < _thickness + 10 &&
                IsInsideRectangle(_points[^1], _points[0], point))
                return _points.Count - 1;

            return -1;
        }

        public override void MoveEdge(int edgeIndex, Vector offSet)
        {
            var newP1 = Point.Add(_points[edgeIndex], offSet);
            var nextIndex = edgeIndex == _points.Count - 1 ? 0 : edgeIndex + 1;
            var newP2 = Point.Add(_points[nextIndex], offSet);

            if (IsInsideRectangle(new Point(0, 0), new Point(_canvasWidth, _canvasHeight), newP1) &&
                IsInsideRectangle(new Point(0, 0), new Point(_canvasWidth, _canvasHeight), newP2))
            {
                _points[edgeIndex] = newP1;
                _points[nextIndex] = newP2;
            }
        }


        public override string ToString()
        {

            return $"POLYGON.TOSTRING NOT IMPLEMENTED YET"; // ({Points[0].X}, {Points[0].Y})-({Points[1].X}, {Points[1].Y})";
        }
    }
}