using System;
using System.Collections.Generic;
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
    }

    public abstract class Shape : IDrawable
    {
        protected List<Point> _points;
        protected int _thickness;
        protected Color _color = Color.FromArgb(255, 0, 0, 0);
        public abstract void Draw(WriteableBitmap wbm);
        public abstract int GetVertexIndexOf(Point point);
        public abstract void  MoveVertex(int vertexIndex, Vector offSet);
        public abstract int GetEdgeIndexOf(Point point);
        


        protected int DistanceBetween(Point p1, Point p2)
        {
            return (int)Math.Round(Math.Sqrt(Math.Pow((p2.X - p1.X), 2) + Math.Pow((p2.Y - p1.Y), 2)));
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
        

        public override string ToString()
        {

            return $"POLYGON.TOSTRING NOT IMPLEMENTED YET"; // ({Points[0].X}, {Points[0].Y})-({Points[1].X}, {Points[1].Y})";
        }
    }
}