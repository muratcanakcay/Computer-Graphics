using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace Lab05___3DModelling;

public class Line
{
    private List<Point> Points { get; }

    private Color Color { get; } = Colors.Gray;

    public Line(List<Point> points)
    {
        Points = points;
    }

    public void Draw(List<Pixel> drawingData)
    {
        var x0 = (int)(Points[0].X);
        var y0 = (int)(Points[0].Y);
        var x1 = (int)(Points[1].X);
        var y1 = (int)(Points[1].Y);

        double dy = y1 - y0;
        double dx = x1 - x0;

        if (dx != 0 && Math.Abs(dy/dx) < 1)
        {
            double y = y0;
            double m = dy/dx;

            if (dx > 0)
            {
                for (var x = x0; x <= x1; ++x)
                {
                    drawingData.Add(new Pixel(x, (int)Math.Round(y), Color));
                    y += m;
                }
            }
            else
            {
                for (var x = x0; x >= x1; --x)
                {
                    drawingData.Add(new Pixel(x, (int)Math.Round(y), Color));
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
                    drawingData.Add(new Pixel((int)Math.Round(x), y, Color));
                    x += m;
                }
            }
            else
            {
                for (var y = y0; y >= y1; --y)
                {
                    drawingData.Add(new Pixel((int)Math.Round(x), y, Color));
                    x -= m;
                }
            }
        }
    }

    public override string ToString()
    {
        return $"({Points[0].X}, {Points[0].Y})-({Points[1].X}, {Points[1].Y})\n";
    }
}

public class Triangle
{
    public List<Point3d> Points { get; }
    private WriteableBitmap? _fillImageWbm;
    private Phong _lightAttributes;
    public Triangle(Point3d v1, Point3d v2, Point3d v3)
    {
        Points = new List<Point3d>() { v1, v2, v3 };
    }

    public void Draw(List<Pixel> drawingData)
    {
        for (var i = 0; i < 3; i++)
        {
            var endPoint = i < 2 ? new Point(Points[i+1].Projected.X, Points[i+1].Projected.Y) : new Point(Points[0].Projected.X, Points[0].Projected.Y);
            var edge = new Line(new List<Point> { new Point(Points[i].Projected.X, Points[i].Projected.Y), endPoint });

            edge.Draw(drawingData);
        }
    }

    public void Fill(List<Pixel> drawingData, WriteableBitmap? texture, Phong lightAttributes)
    {
        if (texture == null && lightAttributes.IsIlluminated == false)
            return;
        
        _lightAttributes = lightAttributes;
        if (texture is not null) _fillImageWbm = texture;
        
        
        var et = CreateEdgeTable();
        if (et.Count == 0)
            return;

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
                if (et.Count == 0)
                {
                    Debug.WriteLine("DROPPED TRIANGLE!");
                    return;
                }
                    
                ++y;
                continue;
            }

            aet = aet.OrderBy(edge => edge.X).ToList();

            for (var i = 0; i < aet.Count; i+=2)
            {
                var x1 = (int)Math.Round(aet[i].X);
                var x2 = (int)Math.Round(aet[i+1].X);
                if (x1 < 0 || x2 < 0)
                    continue;

                FillBetweenEdges(drawingData, x1, x2, y);
            }

            ++y;

            aet.RemoveAll(edge => edge.YMax <= y);

            foreach (var edge in aet)
                edge.X += edge.InvM;
        }
    }
    
    public bool IsFacingCamera()
    {
        var v1 = new Vector3((float)(Points[1].Projected.X - Points[0].Projected.X), (float)(Points[1].Projected.Y - Points[0].Projected.Y), 0);
        var v2 = new Vector3((float)(Points[2].Projected.X - Points[0].Projected.X), (float)(Points[2].Projected.Y - Points[0].Projected.Y), 0);
        var result = Vector3.Cross(v1, v2);
        return result.Z > 0;
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
        const double threshold = 0.9;

        for (var i = 0; i < 3; i++)
        {
            var x1 = Points[i].Projected.X;
            var y1 = Points[i].Projected.Y;
            var x2 = (i == 2 ? Points[0].Projected.X : Points[i+1].Projected.X);
            var y2 = (i == 2 ? Points[0].Projected.Y : Points[i+1].Projected.Y);
            double dx = x2 - x1;
            double dy = y2 - y1;

            if (Math.Abs(dy) < threshold)
                continue; // horizontal edge

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

    private void FillBetweenEdges(List<Pixel> drawingData, int x1, int x2, int y)
    {
        Point p;
        Point3d pLeft, pRight;
        const double threshold = 1.5;

        // interpolate (x1, y)
        p = new Point(x1, y);
        if (p.DistanceFromLine(new Point(Points[0].Projected.X, Points[0].Projected.Y), new Point(Points[1].Projected.X, Points[1].Projected.Y)) < threshold)
            pLeft = InterpolatePoint(p, Points[0], Points[1]);
        else if (p.DistanceFromLine(new Point(Points[0].Projected.X, Points[0].Projected.Y), new Point(Points[2].Projected.X, Points[2].Projected.Y)) < threshold)
            pLeft = InterpolatePoint(p, Points[0], Points[2]);
        else
            pLeft = InterpolatePoint(p, Points[1], Points[2]);

        // interpolate (x2, y)
        p = new Point(x2, y);
        if (p.DistanceFromLine(new Point(Points[0].Projected.X, Points[0].Projected.Y), new Point(Points[1].Projected.X, Points[1].Projected.Y)) < threshold)
            pRight = InterpolatePoint(p, Points[0], Points[1]);
        else if (p.DistanceFromLine(new Point(Points[0].Projected.X, Points[0].Projected.Y), new Point(Points[2].Projected.X, Points[2].Projected.Y)) < threshold)
            pRight= InterpolatePoint(p, Points[0], Points[2]);
        else
            pRight = InterpolatePoint(p, Points[1], Points[2]);

        for (var x = x1; x < x2; x++)
        {
            // interpolate (x, y)
            p = new Point(x, y);
            var interpolatedPoint = InterpolatePoint(p, pLeft, pRight);

            Color modelColor;
            if (_lightAttributes.IsIlluminated)
                modelColor = CalculateIllumination(interpolatedPoint, _lightAttributes);
            else
                modelColor = _fillImageWbm.GetPixelColor((int)Math.Round(interpolatedPoint.TextureMap.X * _fillImageWbm.PixelWidth), (int)Math.Round((1-interpolatedPoint.TextureMap.Y) * _fillImageWbm.PixelHeight));
                
            

            drawingData.Add(new Pixel(x, y, modelColor));
        }
    }

    private static Color CalculateIllumination(Point3d point, Phong lightAttributes)
    {
        var p = new Vector3((float)point.Global.X, (float)point.Global.Y, (float)point.Global.Z);
        var n = new Vector3((float)point.Normal.X, (float)point.Normal.Y, (float)point.Normal.Z);
        var camera = lightAttributes.Camera;
        var light = lightAttributes.Light;

        var Ia = lightAttributes.Ia;
        var ka = lightAttributes.ka;
        var ks = lightAttributes.ks;
        var kd = lightAttributes.kd;

        var v = Vector3.Divide(Vector3.Subtract(camera, p), Vector3.Subtract(camera, p).Length());
        var li = Vector3.Divide(Vector3.Subtract(light, p), Vector3.Subtract(light, p).Length());
        var ri = Vector3.Subtract(Vector3.Multiply(2 * (Vector3.Dot(n, li)), n), li);

        var I = new Vector3(Ia * ka, Ia * ka, Ia * ka);
        I.X += (float)(kd * Ia * Math.Max(Vector3.Dot(n, li), 0) + ks * Ia * Math.Pow(Math.Max(Vector3.Dot(v, ri), 0), 20));
        I.Y += (float)(kd * Ia * Math.Max(Vector3.Dot(n, li), 0) + ks * Ia * Math.Pow(Math.Max(Vector3.Dot(v, ri), 0), 20));
        I.Z += (float)(kd * Ia * Math.Max(Vector3.Dot(n, li), 0) + ks * Ia * Math.Pow(Math.Max(Vector3.Dot(v, ri), 0), 20));
            
        var result = new Color()
        {
            A = lightAttributes.ModelColor.A,
            R = (byte)Clamp(lightAttributes.ModelColor.R * I.X),
            G = (byte)Clamp(lightAttributes.ModelColor.G * I.Y),
            B = (byte)Clamp(lightAttributes.ModelColor.B * I.Z)
        };

        return result;
    }

    public static int Clamp(double val)
    {
        return val switch
        {
            > 255 => 255,
            < 0 => 0,
            _ => (int)val
        };
    }

    private static Point3d InterpolatePoint(Point middleP, Point3d vertex1, Point3d vertex2)
    {
        var v1 = vertex1;
        var v2 = vertex2;

        if (v1.Projected.X > v2.Projected.X || v1.Projected.Y > v2.Projected.Y)
            (v1, v2) = (v2, v1);

        double t = Vector2.Distance(new Vector2((float)v1.Projected.X, (float)v1.Projected.Y), new Vector2((float)middleP.X, (float)middleP.Y)) /
                   Vector2.Distance(new Vector2((float)v1.Projected.X, (float)v1.Projected.Y), new Vector2((float)v2.Projected.X, (float)v2.Projected.Y));

        var interpolatedProjected = new Point4(
            (v2.Projected.X - v1.Projected.X) * t + v1.Projected.X, 
            (v2.Projected.Y - v1.Projected.Y) * t + v1.Projected.Y,
            (v2.Projected.Z - v1.Projected.Z) * t + v1.Projected.Z, 
            1);

        var u = v1.Projected.Z - v2.Projected.Z < 0.01 // compare floats
            ? t
            : ((1d/interpolatedProjected.Z) - (1d/v1.Projected.Z))/((1d/v2.Projected.Z) - (1d/v1.Projected.Z));

        var interpolatedGlobal = new Point4(
            u * (v2.Global.X - v1.Global.X) + v1.Global.X, 
            u * (v2.Global.Y - v1.Global.Y) + v1.Global.Y,
            u * (v2.Global.Z - v1.Global.Z) + v1.Global.Z, 
            1); // u * (v2.Global.W - v1.Global.W) + v1.Global.W);

        var interpolatedNormal = new Point4(
            u * (v2.Normal.X - v1.Normal.X) + v1.Normal.X, 
            u * (v2.Normal.Y - v1.Normal.Y) + v1.Normal.Y,
            u * (v2.Normal.Z - v1.Normal.Z) + v1.Normal.Z, 
            0); // u * (v2.Normal.W - v1.Normal.W) + v1.Normal.W);

        var interpolatedTextureCoordinates = new Point(
            u * (v2.TextureMap.X - v1.TextureMap.X) + v1.TextureMap.X, 
            u * (v2.TextureMap.Y - v1.TextureMap.Y) + v1.TextureMap.Y);

        return new Point3d
        {
            Projected = interpolatedProjected,
            Global = interpolatedGlobal,
            Normal = interpolatedNormal,
            TextureMap = interpolatedTextureCoordinates
        };
    }
}