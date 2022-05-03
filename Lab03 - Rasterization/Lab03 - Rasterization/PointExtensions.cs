using System;
using Point = System.Windows.Point;

namespace Lab03___Rasterization
{
    public static class PointExtensions
    {
        public static double DistanceFrom(this Point point, Point otherPoint)
        {
            var dx = otherPoint.X - point.X;
            var dy = otherPoint.Y - point.Y;
            return Math.Round(Math.Sqrt(dx*dx + dy*dy));
        }

        public static double DistanceFromLine(this Point point,  Point pointOnLine1, Point pointOnLine2)
        {
            var denominator = pointOnLine1.DistanceFrom(pointOnLine2);
            var numerator = Math.Abs((pointOnLine2.X - pointOnLine1.X) * (pointOnLine1.Y - point.Y) -
                                     (pointOnLine1.X - point.X) * (pointOnLine2.Y - pointOnLine1.Y));

            return numerator / denominator;
        }

        public static bool IsInsideRectangle(this Point point, Point cornerPoint1, Point cornerPoint2, int offSet = 0)
        {
            // offSet value is used to increase the rectangle size by 2*offSet on each edge
            return (point.X > Math.Min(cornerPoint1.X, cornerPoint2.X) - offSet &&
                    point.X < Math.Max(cornerPoint1.X, cornerPoint2.X) + offSet &&
                    point.Y > Math.Min(cornerPoint1.Y, cornerPoint2.Y) - offSet &&
                    point.Y < Math.Max(cornerPoint1.Y, cornerPoint2.Y) + offSet);
        }
    }
}