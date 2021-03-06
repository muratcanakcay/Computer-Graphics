using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Clipboard = System.Windows.Clipboard;
using Color = System.Drawing.Color;
using Point = System.Windows.Point;

namespace Lab04___Clipping_and_Filling
{
    internal interface IDrawable
    {
        List<Point> Points { get; }
        int Thickness { get; set; }
        Color Color { get; set; }
        Color? FillColor { get; set; }
        String? FillImage{ get; set; }
        bool IsClippingRectangle { get; set; }
        void Draw(WriteableBitmap wbm, bool isAntiAliased = false, bool isSuperSampled = false, int ssaa = 2, Rectangle? clip = null);
        int GetVertexIndexOf(Point point);
        void MoveVertex(int vertexIndex, Vector offSet);
        int GetEdgeIndexOf(Point point);
        void MoveEdge(int edgeIndex, Vector offSet);
        void MoveShape(Vector offset);
    }

    public abstract class Shape : IDrawable
    {
        protected const int GrabDistance = 10;
        protected string? _fillImage;
        protected WriteableBitmap? FillImageWbm;
        public List<Point> Points { get; }
        public int Thickness { get; set; }
        public Color Color { get; set; }
        public Color? FillColor { get; set; } 
        public string? FillImage
        {
            get => _fillImage;
            set
            {
                _fillImage = value;
                if (value is not null)
                {
                    try
                    {
                        FillImageWbm = new WriteableBitmap(new BitmapImage(
                            new Uri(new Uri(System.Reflection.Assembly.GetExecutingAssembly().Location), FillImage)));
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception.Message);
                    }
                }
                else
                {
                    FillImageWbm = null;
                }
            }
        }
        public bool IsClippingRectangle { get; set; } = false;

        protected Shape(List<Point> points, int thickness, Color color)
        {
            Points = points;
            Thickness = thickness;
            Color = color;
        }

        public abstract void Draw(WriteableBitmap wbm, bool isAntiAliased = false, bool isSuperSampled = false, int ssaa = 2, Rectangle? clip = null);

        public virtual int GetVertexIndexOf(Point point)
        {
            foreach (var vertex in Points)
                if (point.DistanceFromPoint(vertex) < Thickness + GrabDistance)
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
                if (point.DistanceFromLine(Points[i], Points[i+1]) < Thickness + GrabDistance &&
                    point.IsInsideRectangle(Points[i], Points[i+1], Thickness + GrabDistance))
                    return i;
            }

            if (point.DistanceFromLine(Points[^1], Points[0]) < Thickness + GrabDistance &&
                point.IsInsideRectangle(Points[^1], Points[0], Thickness + GrabDistance))
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

        protected static double Determinant(Point a, Point b, Point c)
        {
            return a.X*b.Y - a.X*c.Y - a.Y*b.X + a.Y*c.X + b.X*c.Y - b.Y*c.X;
        }
    }

    public class Line : Shape
    {
        public Line(List<Point> points, int thickness, Color color) : base(points, thickness, color)
        { }

        public override void Draw(WriteableBitmap wbm, bool isAntiAliased = false, bool isSuperSampled = false, int ssaa = 2, Rectangle? clip = null)
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
                var dXE = dy > 0 ? 2*(dy - dx) : 2*(dy + dx);
                var d = dy > 0 ? 2*dy - dx : 2*dy + dx;

                var x = (int)x0;
                var y = (int)y0;

                IntensifyPixel(wbm, x, y, 0);
                for (var i = 1; IntensifyPixel(wbm, x, y + i, 2*i*dx*invDenom); ++i)
                    ;
                for (var i = 1; IntensifyPixel(wbm, x, y - i, 2*i*dx*invDenom); ++i)
                    ;

                while (x < x1)
                {
                    ++x;
                    int twoVDx;

                    if (d <= 0) // move to E or SE
                    {
                        twoVDx = d + dx;
                        d += dy > 0 ? dE : dXE;
                        if (dy < 0)
                            --y;
                    }
                    else // move to E or NE
                    {
                        twoVDx = d - dx;
                        d += dy > 0 ? dXE : dE;
                        if (dy > 0)
                            ++y;
                    }

                    IntensifyPixel(wbm, x, y, twoVDx * invDenom);
                    for (var i = 1; IntensifyPixel(wbm, x, y + i, ((2*i*dx) - twoVDx) * invDenom); ++i)
                        ;
                    for (var i = 1; IntensifyPixel(wbm, x, y - i, ((2*i*dx) + twoVDx) * invDenom); ++i)
                        ;
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
                var dNX = dx > 0 ? 2*(dx - dy) : 2*(dx + dy);
                var d = dx > 0 ? 2*dx - dy : 2*dx + dy;

                var y = (int)y0;
                var x = (int)x0;

                IntensifyPixel(wbm, x, y, 0);
                for (var i = 1; IntensifyPixel(wbm, x + i, y, 2*i*dy*invDenom); ++i)
                    ;
                for (var i = 1; IntensifyPixel(wbm, x - i, y, 2*i*dy*invDenom); ++i)
                    ;

                while (y < y1)
                {
                    ++y;
                    int twoVDy;

                    if (d < 0) // move to N or NW
                    {
                        twoVDy = d + dy;
                        d += dx > 0 ? dN : dNX;
                        if (dx < 0)
                            --x;
                    }
                    else // move to N or NE
                    {
                        twoVDy = d - dy;
                        d += dx > 0 ? dNX : dN;
                        if (dx > 0)
                            ++x;
                    }

                    IntensifyPixel(wbm, x, y, twoVDy * invDenom);
                    for (var i = 1; IntensifyPixel(wbm, x + i, y, ((2*i*dy) - twoVDy) * invDenom); ++i)
                        ;
                    for (var i = 1; IntensifyPixel(wbm, x - i, y, ((2*i*dy) + twoVDy) * invDenom); ++i)
                        ;
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
                    var backgroundColor = wbm.GetPixelColor(x, y);
                    var newColor = Color.FromArgb(
                        255,
                        (int)(backgroundColor.R*(1-cov) + Color.R*cov),
                        (int)(backgroundColor.G*(1-cov) + Color.G*cov),
                        (int)(backgroundColor.B*(1-cov) + Color.B*cov));

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
            if (d >= r)
                return 0;

            return ((1/Math.PI) * Math.Acos(d/r)) - ((d / (Math.PI*r*r)) * Math.Sqrt((r*r) - (d*d)));
        }
        
        private enum Outcodes {
            LEFT = 1,
            RIGHT = 2,
            BOTTOM = 4,
            TOP = 8
        };

        private static byte ComputeOutcode(Point p, Rectangle clip)
        {
            byte outcode = 0;
            
            if (p.X > clip.Right) outcode |= (byte)Outcodes.RIGHT;
            else if (p.X < clip.Left) outcode |= (byte)Outcodes.LEFT;
            
            if (p.Y < clip.Bottom) outcode |= (byte)Outcodes.BOTTOM;
            else if (p.Y > clip.Top) outcode |= (byte)Outcodes.TOP;
            
            return outcode;
        }

        public void CohenSutherland(WriteableBitmap wbm, bool isAntiAliased, bool isSuperSampled, int ssaa, Rectangle? clip)
        {
            Draw(wbm, isAntiAliased, isSuperSampled, ssaa);
            
            if (clip == null) return;

            var accepted = false;
            var done = false;
            var p1 = Points[0];
            var p2 = Points[1];
            var outcode1 = ComputeOutcode(p1, clip);
            var outcode2 = ComputeOutcode(p2, clip);

            do
            {
                if ((outcode1 | outcode2) == 0) //trivially accepted
                {
                    accepted = true;
                    done = true;
                }
                else if ((outcode1 & outcode2) != 0) //trivially rejected
                {
                    accepted = false;
                    done = true;
                }
                else //subdivide
                {
                    byte outcodeOut = (outcode1 != 0) ? outcode1 : outcode2;
                    Point p;
                    if ( ( outcodeOut & (byte)Outcodes.BOTTOM ) != 0 ) 
                    {
                        p.X = p1.X + (p2.X - p1.X) * (clip.Bottom - p1.Y) / (p2.Y - p1.Y);
                        p.Y = clip.Bottom;
                    }
                    else if ( ( outcodeOut & (byte)Outcodes.TOP ) != 0 ) 
                    {
                        p.X = p1.X + (p2.X - p1.X) * (clip.Top - p1.Y) / (p2.Y - p1.Y);
                        p.Y = clip.Top;
                    }
                    else if ((outcodeOut & (byte)Outcodes.RIGHT) != 0)
                    {
                        p.Y = p1.Y + (p2.Y - p1.Y) * (clip.Right - p1.X) / (p2.X - p1.X);
                        p.X = clip.Right;
                    }
                    else if ((outcodeOut & (byte)Outcodes.LEFT) != 0)
                    {
                        p.Y = p1.Y + (p2.Y - p1.Y) * (clip.Left - p1.X) / (p2.X - p1.X);
                        p.X = clip.Left;
                    }

                    if (outcodeOut == outcode1)
                    {
                        p1 = p;
                        outcode1 = ComputeOutcode(p1, clip);
                    }
                    else 
                    {
                        p2 = p;
                        outcode2 = ComputeOutcode(p2, clip);
                    }
                }
            } while (!done);

            if (accepted)
            {
                var lineSegment = new Line(new List<Point> { p1, p2 }, Thickness+1, Color.FromKnownColor(KnownColor.Red));
                lineSegment.Draw(wbm, isAntiAliased, isSuperSampled, ssaa);
            }
        }

        public override string ToString()
        {
            return $"({Points[0].X}, {Points[0].Y})-({Points[1].X}, {Points[1].Y})\n";
        }
    }

    public class Polygon : Shape
    {
        public Polygon(List<Point> points, int thickness, Color color, Color? fillColor = null, string? fillImage = null) : base(points, thickness, color)
        {
            FillColor = fillColor;
            FillImage = fillImage;
        }

        public override void Draw(WriteableBitmap wbm, bool isAntiAliased = false, bool isSuperSampled = false, int ssaa = 2, Rectangle? clip = null)
        {
            if (FillColor != null || FillImage != null) FillPolygon(wbm);
            
            for (var i = 0; i < Points.Count; i++)
            {
                var endPoint = i < Points.Count-1 ? Points[i+1] : Points[0];
                var edge = new Line(new List<Point> { Points[i], endPoint }, Thickness, Color);

                edge.CohenSutherland(wbm, isAntiAliased, isSuperSampled, ssaa, IsClippingRectangle ? null : clip);
            }
        }

        private class EdgeData
        {
            public int YMax;
            public double X { get; set; }
            public double InvM;
        }

        private SortedDictionary<int, List<EdgeData>> CreateEdgeTable()
        {
            SortedDictionary<int, List<EdgeData>> et = new();

            for (var i = 0; i < Points.Count; i++)
            {
                var x1 = Points[i].X;
                var y1 = Points[i].Y;
                var x2 = ( i == Points.Count - 1 ? Points[0].X : Points[i+1].X );
                var y2 = ( i == Points.Count - 1 ? Points[0].Y : Points[i+1].Y );
                double dx = x2 - x1;
                double dy = y2 - y1;

                if (dy == 0) continue; // horizontal edge

                var xMin = y1 < y2 ? x1 : x2;
                var yMin = (int)Math.Round(Math.Min(y1, y2));
                var yMax = (int)Math.Round(Math.Max(y1, y2));

                var edge = new EdgeData()
                {
                    X = xMin,
                    YMax = yMax,
                    InvM = dx/dy
                };

                if (et.ContainsKey(yMin)) 
                {
                    et[yMin].Add(edge);
                }
                else
                {
                    var edgeBucket = new List<EdgeData> { edge };
                    et.Add(yMin, edgeBucket);
                }
            }

            return et;
        }

        private void FillPolygon(WriteableBitmap wbm)
        {
            var et = CreateEdgeTable();
            if (et.Count == 0) return;

            var y = et.Keys.First();
            List<EdgeData> aet = new();

            while (aet.Count != 0 || et.Count != 0)
            {
                if (et.ContainsKey(y))
                {
                    aet.AddRange(et[y]);
                    et.Remove(y);
                }

                // check for self-intersecting polygon issues
                if (aet.Count%2 == 1)
                {
                    if (et.Count == 0) return;
                    ++y;
                    continue;
                }

                aet = aet.OrderBy(edge => edge.X).ToList();

                for (var i = 0; i < aet.Count; i+=2)
                {
                    var x1 = (int)Math.Round(aet[i].X);
                    var x2 = (int)Math.Round(aet[i+1].X);
                    if (x1 < 0 || x2 < 0) continue;
                    
                    FillBetweenEdges(wbm, x1, x2, y);
                }

                ++y;

                aet.RemoveAll(edge => edge.YMax <= y);

                foreach (var edge in aet)
                    edge.X += edge.InvM;
            }
        }

        private void FillBetweenEdges(WriteableBitmap wbm, int x1, int x2, int y)
        {
            wbm.Lock();

            if (FillColor != null)
            {
                for (var x = x1; x < x2; x++)
                    wbm.SetPixelColor(x, y, (Color)(FillColor!));
            }
            else if (FillImageWbm != null) // fill with image
            {
                for (var x = x1; x < x2; x++)
                {
                    var imageColor = FillImageWbm.GetPixelColor(x%FillImageWbm.PixelWidth, y%FillImageWbm.PixelHeight);
                    wbm.SetPixelColor(x, y, imageColor);
                }
            }

            wbm.Unlock();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            for (var i = 0; i < Points.Count; i++)
                sb.Append($"P{i}: ({Points[i].X}, {Points[i].Y})\n");

            return sb.ToString();
        }
    }

    public class Rectangle : Polygon
    {
        public int Left =>
            (int)(Math.Abs(Points[0].X - Points[1].X) < 0.01
                ? (Points[1].X < Points[2].X ? Points[1].X : Points[2].X)
                : (Points[0].X < Points[1].X ? Points[0].X : Points[1].X));

        public int Bottom =>
            (int)(Math.Abs(Points[0].Y - Points[1].Y) < 0.01
                ? (Points[1].Y < Points[2].Y ? Points[1].Y : Points[2].Y)
                : (Points[0].Y < Points[1].Y ? Points[0].Y : Points[1].Y));

        public int Right => Left + (int)(Math.Abs(Points[0].X - Points[2].X));
        public int Top => Bottom + (int)(Math.Abs(Points[0].Y - Points[2].Y));

        public Rectangle(List<Point> points, int thickness, Color color, Color? fillColor = null,
            string? fillImage = null, bool isClipping = false) : base(points, thickness, color, fillColor, fillImage)
        {
            IsClippingRectangle = isClipping;
        }
        
        public override void MoveVertex(int vertexIndex, Vector offSet)
        {
            Points[vertexIndex] = Point.Add(Points[vertexIndex], offSet);

            var dx = new Vector(offSet.X, 0);
            var dy = new Vector(0, offSet.Y);

            switch (vertexIndex)
            {
                case 0:
                    Points[1] = Point.Add(Points[1], dy);
                    Points[3] = Point.Add(Points[3], dx);
                    break;
                case 1:
                    Points[0] = Point.Add(Points[0], dy);
                    Points[2] = Point.Add(Points[2], dx);
                    break;
                case 2:
                    Points[1] = Point.Add(Points[1], dx);
                    Points[3] = Point.Add(Points[3], dy);
                    break;
                case 3:
                    Points[0] = Point.Add(Points[0], dx);
                    Points[2] = Point.Add(Points[2], dy);
                    break;
            }
        }
        
        public override void MoveEdge(int edgeIndex, Vector offSet)
        {
            var nextIndex = edgeIndex == Points.Count - 1 ? 0 : edgeIndex + 1;

            var dx = new Vector(offSet.X, 0);
            var dy = new Vector(0, offSet.Y);

            switch (edgeIndex)
            {
                case 0:
                case 2:
                    Points[edgeIndex] = Point.Add(Points[edgeIndex], dy);
                    Points[nextIndex] = Point.Add(Points[nextIndex], dy);
                    break;
                case 1:
                case 3:
                    Points[edgeIndex] = Point.Add(Points[edgeIndex], dx);
                    Points[nextIndex] = Point.Add(Points[nextIndex], dx);
                    break;
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
        private int Radius => (int)Math.Round(Points[0].DistanceFromPoint(Points[1]));
        public Circle(List<Point> points, int thickness, Color color) : base(points, thickness, color) { }

        public override void Draw(WriteableBitmap wbm, bool isAntiAliased = false, bool isSuperSampled = false, int ssaa = 2, Rectangle? clip = null)
        {
            var SSAA = isSuperSampled ? ssaa : 1;

            var x = 0;
            var y = SSAA * Radius;
            var d = 1 - (SSAA * Radius);
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
            if (point.DistanceFromPoint(Center) < Radius - Thickness - GrabDistance ||
                point.DistanceFromPoint(Center) > Radius + Thickness + GrabDistance)
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
            return $"Center:({Center.X}, {Center.Y}) - Radius:{Radius})\n";
        }
    }

    public class CircleArc : Shape // TODO: correctly implement Edge and Vertex movement
    {
        private Point Center => Points[0];
        private int Radius => (int)Math.Round(Points[0].DistanceFromPoint(Points[1]));
        public CircleArc(List<Point> points, int thickness, Color color) : base(points, thickness, color) { }

        public override void Draw(WriteableBitmap wbm, bool isAntiAliased = false, bool isSuperSampled = false, int ssaa = 2, Rectangle? clip = null)
        {
            var SSAA = isSuperSampled ? ssaa : 1;

            var x = 0;
            var y = SSAA * Radius;
            var d = 1 - (SSAA * Radius);
            var xC = (int)(SSAA * Center.X);
            var yC = (int)(SSAA * Center.Y);

            var det = Determinant(Center, Points[1], Points[2]);

            try
            {
                wbm.Lock();

                var plusMinus = new[] { -1, 1 };

                foreach (var i in plusMinus)
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

                    foreach (var i in plusMinus)
                        foreach (var j in plusMinus)
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
            if (point.DistanceFromPoint(Center) < Radius - Thickness - GrabDistance ||
                point.DistanceFromPoint(Center) > Radius + Thickness + GrabDistance)
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
            return $"Center:({Center.X}, {Center.Y}) - Radius:{Radius} - P1:({Points[1].X}, {Points[1].Y}) - P2: ({Points[2].X}, {Points[2].Y})\n";
        }
    }
}