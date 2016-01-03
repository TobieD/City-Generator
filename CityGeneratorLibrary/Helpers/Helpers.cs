using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using CityGenerator;
using Voronoi;
using Voronoi.Algorithms;

namespace Helpers
{
    /// <summary>
    /// Helper for random generation
    /// </summary>
    public static class RandomHelper
    {
        private static Random _rng;

        /// <summary>
        /// generate a random value between min and max
        /// </summary>
        public static int RandomInt(int min = int.MinValue, int max = int.MaxValue)
        {
            //seed random
            SeedRandom();

            return _rng.Next(min, max);
        }

        /// <summary>
        /// Generate a random boolean
        /// </summary>
        public static bool RandomBool()
        {
            SeedRandom();

            return (_rng.Next(0, 1) == 1);
        }

        /// <summary>
        /// Generate a random value between 0.0 and 1.0
        /// </summary>
        public static double RandomDouble(double min = 0.0, double max = 1.0)
        {
            SeedRandom();

            var rnd = _rng.NextDouble();

            //clamp values
            if (rnd < min)
                rnd = min;
            else if (rnd > max)
                rnd = max;

            return rnd;
        }

        /// <summary>
        /// Generate a random value from a list
        /// </summary>
        public static T RandomValueFromList<T>(IList<T> list)
        {
            SeedRandom();
            return list.ElementAt(_rng.Next(list.Count()));
        }

        /// <summary>
        /// Seed the random generation
        /// </summary>
        private static void SeedRandom()
        {
            if (_rng == null)
            {
                _rng = new Random(DateTime.Now.GetHashCode());
            }
        }
    }

    /// <summary>
    /// Math functions for the Voronoi and CityGenerator elements
    /// </summary>
    public static class MathHelpers
    {
        #region General

        /// <summary>
        /// Remove all double values from the list
        /// </summary>
        public static void FilterDoubleValues<T>(this IList<T> enumerable)
        {
            for (int i = 0; i < enumerable.Count; i++)
            {
                for (int l = 1; l < enumerable.Count - 1; l++)
                {
                    if (enumerable[i].Equals(enumerable[l]))
                        enumerable.Remove(enumerable[i]);
                }
            }
        }

        /// <summary>
        /// Returns a random value from a given list
        /// </summary>
        public static T GetRandomValue<T>(this IList<T> list)
        {
            return RandomHelper.RandomValueFromList(list);
        }

        /// <summary>
        /// Calculate the cross product of 2 points
        /// Formula:: ax * by - bx * ay
        /// </summary>
        public static double CrossProduct(Point a, Point b)
        {
            return (a.X * b.Y) - (b.X * a.Y);
        }

        /// <summary>
        /// do the power of a value
        /// Formula:: x *x 
        /// </summary>
        public static double Power(double x)
        {
            return x * x;
        }


        #endregion

        #region Point
        /// <summary>
        /// Calculate the distance between 2 points
        /// </summary>
        public static double DistanceBetweenPoints(Point p1, Point p2)
        {
            return Math.Sqrt(Power(p1.X - p2.X) + Power(p1.Y - p2.Y));
        }

        #endregion

        #region Line


        public static double Slope(this Line l)
        {
            return (l.End.Y - l.Start.Y)/(l.End.X - l.Start.X);
        }

        /// <summary>
        /// Find the line perpendicular with a given line in a given point
        /// </summary>
        public static Line FindPerpendicularBisectorOfLine(this Line l, Point p)
        {
            var p1 = Point.Zero;
            var p2 = Point.Zero;

            var dx = l.End.X - l.Start.X;
            var dy = l.End.Y - l.Start.Y;

            p1.X = -dy;
            p1.Y = dx;
            p2.X = dy;
            p2.Y = -dx;

            return new Line(p, p2);
        }

        /// <summary>
        /// Easier way to get the center of a line
        /// </summary>
        public static Point Center(this Line line)
        {
            return FindCenterOfLine(line);
        }
        /// <summary>
        /// Find the center point of a line
        /// </summary>
        public static Point FindCenterOfLine(Line l)
        {
            //take average
            var middlePoint = Point.Zero;
            middlePoint.X = (l.Start.X + l.End.X) / 2;
            middlePoint.Y = (l.Start.Y + l.End.Y) / 2;

            return middlePoint;
        }

        /// <summary>
        /// Find a random point on a given line
        /// </summary>
        public static Point FindRandomPointOnLine(this Line line, double min, double max)
        {
            //get points of line
            var p1 = line.Start;
            var p2 = line.End;

            var p = Point.Zero;
            var t = RandomHelper.RandomDouble(min,max);

            p.X = p1.X + t * (p2.X - p1.X);
            p.Y = p1.Y + t * (p2.Y - p1.Y);

            return p;

        }

        /// <summary>
        /// Find a point perpendicular with a line
        /// </summary>
        public static Point FindPerpendicularPointOnLine(this Line l, Point p3)
        {
            var p = Point.Zero;
            var p1 = l.Start;
            var p2 = l.End;
            
            //Convert line to normalized unity vector
            var dx = p2.X - p1.X;
            var dy = p2.Y - p1.Y;
            double mag = Math.Sqrt(dx*dx + dy*dy);

            dx /= mag;
            dy /= mag;

            //Translate the point and get the dot product
            double lambda = (dx*(p3.X - p1.X)) + (dy*(p3.Y - p1.Y));

            p.X = dy;
            p.Y = -dx;

            p.X = (dx*lambda) + p1.X;
            p.Y = (dy * lambda) + p1.Y;

            return p;
        }

        /// <summary>
        /// Extend a line with a given length
        /// </summary>
        public static Line ExtendLine(this Line line, double length)
        {
            var p1 = line.Start;
            var p2 = line.End;
            var lengthOfLine = DistanceBetweenPoints(p1, p2);

            var c = Point.Zero;
            var d = p1;

            c.X = p2.X + (p2.X - p1.X) / lengthOfLine * length;
            c.Y = p2.Y + (p2.Y - p1.Y) / lengthOfLine * length;

            if (RandomHelper.RandomBool())
            {
                d.X = p1.X + (p1.X - p2.X) / lengthOfLine * length;
                d.Y = p1.Y + (p1.Y - p2.Y) / lengthOfLine * length;
            }

            return new Line(d, c);
        }

        /// <summary>
        /// Determines if 2 lines intersect
        /// </summary>
        public static bool IntersectsWith(this Line line, Line otherLine)
        {
            //Check if bounding boxes intersect
            return LineTouchesOrCrossesLine(line, otherLine) &&
                LineTouchesOrCrossesLine(otherLine, line);
        }

        /// <summary>
        /// Determine if a given point is on a line
        /// </summary>
        public static bool IsPointOnLine(this Line l, Point p)
        {
            Line tmp = new Line(Point.Zero, new Point(l.End.X - l.Start.X, l.End.Y - l.Start.Y));
            Point tp = new Point(p.X - l.Start.X, p.Y - l.Start.Y);

            return Math.Abs(CrossProduct(tmp.End, tp)) < 0.000001;
        }

        /// <summary>
        /// Determine if a given point is to the right of a line
        /// </summary>
        public static bool IsPointRightOfLine(this Line l, Point p)
        {
            var tmp = new Line(Point.Zero, new Point(l.End.X - l.Start.X, l.End.Y - l.Start.Y));
            var tp = new Point(p.X - l.Start.X, p.Y - l.Start.Y);

            return CrossProduct(tmp.End, tp) < 0;
        }

        /// <summary>
        /// Determine if 2 lines touch or cross
        /// </summary>
        public static bool LineTouchesOrCrossesLine(Line a, Line b)
        {
            return a.IsPointOnLine(b.Start) ||
                a.IsPointOnLine(b.End)
                || (a.IsPointRightOfLine(b.Start) ^ a.IsPointRightOfLine(b.End));
        }

        public static Point GeneratePerpendicularPointOnLine(this Line l, Point c, double offset)
        {
            var p  = Point.Zero;
            var x1 = l.Start.X;
            var y1 = l.Start.Y;
            var x2 = l.End.X;
            var y2 = l.End.Y;



            return p;
        }

        public static Line GenerateOffsetParallel(this Line line, int offset = 20, bool bLeft = true)
        {
            var x1 = line.Start.X;
            var y1 = line.Start.Y;
            var x2 = line.End.X;
            var y2 = line.End.Y;

            var l = Math.Sqrt(Power(x1 - x2) + Power(y1 - y2));

            offset *= (bLeft) ? 1 : -1;

            var p3 = new Point(x1 + offset * (y2 - y1) / l, y1 + offset * (x1 - x2) / l);
            var p4 = new Point(x2 + offset * (y2 - y1) / l, y2 + offset * (x1 - x2) / l);

            return new Line(p3, p4);
        }

        public static Line GenerateOffsetParallelTowardsPoint(this Line line, int offset, Point focusPoint)
        {
            //generate both left and right offset line and take the centers
            var l = line.GenerateOffsetParallel(offset, true);
            var r = line.GenerateOffsetParallel(offset, false);
            
            //return the line that is closest to the focus pint
            return (DistanceBetweenPoints(l.Center(), focusPoint) < DistanceBetweenPoints(r.Center(), focusPoint))
                ? l
                : r;
        }

        /// <summary>
        /// Create random points near a line based on an offset and a division on the inside of the cell
        /// </summary>
        public static List<Point> GeneratePointsNearLineTowardsPoint(this Line line,Point p, double percentage, int offset,bool twoSided = false)
        {
            var points = new List<Point>();
            var p1X = line.Start.X;
            var p1Y = line.Start.Y;
            var p2X = line.End.X;
            var p2Y = line.End.Y;

            //generate an offset line left and right of the original line
            var l = Math.Sqrt(Power(p1X - p2X) + Power(p1Y - p2Y));

            //left off line
            var p3 = new Point(p1X + offset * (p2Y - p1Y) / l, p1Y + offset * (p1X - p2X) / l);
            var p4 = new Point(p2X + offset * (p2Y - p1Y) / l, p2Y + offset * (p1X - p2X) / l);

            var left = new Line(p3, p4);

            //flip for opposite side
            offset *= -1;  

            //right off line
            var p5 = new Point(p1X + offset * (p2Y - p1Y) / l, p1Y + offset * (p1X - p2X) / l);
            var p6 = new Point(p2X + offset * (p2Y - p1Y) / l, p2Y + offset * (p1X - p2X) / l);

            var right = new Line(p5, p6);

            double max = 1.0;

            //take the center of the left and right line
            var leftCenter = left.Center();
            var rightCenter = right.Center();

            //take the distance from left to cell sitepoint and right to cell sitepoint
            //line with the least distance is on the inside

            var leftDis = DistanceBetweenPoints(leftCenter, p);
            var rightDis = DistanceBetweenPoints(rightCenter, p);

            var innerLine = (leftDis < rightDis) ? left : right;

            //make a random point every percentage on the line
            for (double i = percentage; i < max; i+= percentage)
            {
                //Doesn't matter what side
                if (twoSided)
                {
                    points.Add(right.FindRandomPointOnLine(i, i));
                    points.Add(left.FindRandomPointOnLine(i, i));
                }
                else
                {
                    points.Add(innerLine.FindRandomPointOnLine(i, i));
                }

            }
            return points;
        }

        /// <summary>
        /// Find the intersection point of 2 lines
        /// </summary>
        public static Point FindIntersectionPoint(this Line l1, Line l2, ref bool parallel)
        {
            var ip = Point.Zero;

            var x1 = l1.Start.X; //Start
            var y1 = l1.Start.Y; //end
            var x2 = l1.End.X;
            var y2 = l1.End.Y;

            var x3 = l2.Start.X;
            var y3 = l2.Start.Y;
            var x4 = l2.End.X;
            var y4 = l2.End.Y;

            //Get Ax+By = C of line 1
            var a1 = y2 - y1;
            var b1 = x1 - x2;
            var c1 = a1 * x1 + b1 * y1;

            //Get Ax+By = C of line 2
            var a2 = y4 - y3;
            var b2 = x3 - x4;
            var c2 = a2 * x3 + b2 * y3;

            var delta = a1 * b2 - a2 * b1;
            if (delta == 0.0)
                parallel = true;

            ip.X = (b2 * c1 - b1 * c2) / delta;
            ip.Y = (a1 * c2 - a2 * c1) / delta;



            return ip;
        }

        /// <summary>
        /// get the bounding box of a line
        /// </summary>
        public static Point[] GetBoundingBox(this Line l)
        {
            var p = new Point[2];

            var leftX = (l.Start.X > l.End.X) ? l.End.X : l.Start.X;
            var rightX = (l.Start.X < l.End.X) ? l.End.X : l.Start.X;

            var leftY = (l.Start.Y < l.End.Y) ? l.End.Y : l.Start.Y;
            var rightY = (l.Start.Y > l.End.Y) ? l.End.Y : l.Start.Y;


            //left most point
            p[0] = new Point(leftX, leftY);
            p[1] = new Point(rightX, rightY);

            return p;
        }

        public static double Length(this Line l)
        {
            return DistanceBetweenPoints(l.Start,l.End);
        }

        #endregion

        #region Triangle

        /// <summary>
        /// given a triangle calculate the circle that passes through all 3 points
        /// </summary>
        public static Circle FindCircumCircleOfTriangle(Triangle t)
        {
            const double eps = 0.000001;
            var c = new Circle(Point.Zero, 0);
            var circleCenter = Point.Zero;

            //Calculate center of circle
            var m1 = -(t.Point2.X - t.Point1.X) / (t.Point2.Y - t.Point1.Y);
            var mx1 = (t.Point1.X + t.Point2.X) / 2;
            var my1 = (t.Point1.Y + t.Point2.Y) / 2;

            var m2 = -(t.Point3.X - t.Point2.X) / (t.Point3.Y - t.Point2.Y);
            var mx2 = (t.Point2.X + t.Point3.X) / 2;
            var my2 = (t.Point2.Y + t.Point3.Y) / 2;

            if (Math.Abs(t.Point2.Y - t.Point1.Y) < eps)
            {
                circleCenter.X = (t.Point2.X + t.Point1.X) / 2;
                circleCenter.Y = m2 * (circleCenter.X - mx2) + my2;
            }
            else if (Math.Abs(t.Point3.Y - t.Point2.Y) < eps)
            {
                circleCenter.X = (t.Point3.X + t.Point2.X) / 2;
                circleCenter.Y = m1 * (circleCenter.X - mx1) + my1;
            }
            else
            {
                circleCenter.X = (m1 * mx1 - m2 * mx2 + my2 - my1) / (m1 - m2);
                circleCenter.Y = m1 * (circleCenter.X - mx1) + my1;
            }

            //Set center of circle
            c.Center = circleCenter;

            //calculate radius of circle
            var dx = t.Point2.X - c.Center.X;
            var dy = t.Point2.Y - c.Center.Y;

            var rsqr = dx * dx + dy * dy;
            c.Radius = Math.Sqrt(rsqr);

            //return Circle
            return c;
        }

        /// <summary>
        /// determine if the two triangles share at least one point
        /// </summary>
        public static bool HasSharedPointWith(Triangle t1, Triangle t2)
        {
            //compare point 1 of triangle 1
            if (t1.Point1 == t2.Point1) return true;
            if (t1.Point1 == t2.Point2) return true;
            if (t1.Point1 == t2.Point3) return true;

            //compare point 2 of triangle 1
            if (t1.Point2 == t2.Point1) return true;
            if (t1.Point2 == t2.Point2) return true;
            if (t1.Point2 == t2.Point3) return true;

            //compare point 3 of triangle 4
            if (t1.Point3 == t2.Point1) return true;
            if (t1.Point3 == t2.Point2) return true;
            if (t1.Point3 == t2.Point3) return true;

            return false;
        }

        /// <summary>
        /// Determine if two triangles share at least 2 points or an edge
        /// </summary>
        public static bool HasSharedLineWith(Triangle t1, Triangle t2, ref Line sharedLine)
        {
            var a = t1.Point1;
            var b = t1.Point2;
            var c = t1.Point3;

            var f = t2.Point1;
            var g = t2.Point2;
            var e = t2.Point3;

            #region AB == Any edge
            //AB == FG
            if (a == f && b == g)
            {
                sharedLine = new Line(a, b);
                return true;
            }

            //AB == GF
            if (a == g && b == f)
            {
                sharedLine = new Line(a, b);
                return true;
            }

            //AB == FE
            if (a == f && b == e)
            {
                sharedLine = new Line(a, b);
                return true;
            }

            //AB == EF
            if (a == e && b == f)
            {
                sharedLine = new Line(a, b);
                return true;
            }

            //AB == GE
            if (a == g && b == e)
            {
                sharedLine = new Line(a, b);
                return true;
            }

            //AB == EG
            if (a == g && b == e)
            {
                sharedLine = new Line(a, b);
                return true;
            }
            #endregion

            #region AC == Any edge
            //AB == FG
            if (a == f && c == g)
            {
                sharedLine = new Line(a, c);
                return true;
            }

            //AB == GF
            if (a == g && c == f)
            {
                sharedLine = new Line(a, c);
                return true;
            }

            //AB == FE
            if (a == f && c == e)
            {
                sharedLine = new Line(a, c);
                return true;
            }

            //AB == EF
            if (a == e && c == f)
            {
                sharedLine = new Line(a, c);
                return true;
            }

            //AB == GE
            if (a == g && c == e)
            {
                sharedLine = new Line(a, c);
                return true;
            }

            //AB == EG
            if (a == g && c == e)
            {
                sharedLine = new Line(a, c);
                return true;
            }
            #endregion

            #region BC == Any edge
            //AB == FG
            if (b == f && c == g)
            {
                sharedLine = new Line(b, c);
                return true;
            }

            //AB == GF
            if (b == g && c == f)
            {
                sharedLine = new Line(b, c);
                return true;
            }

            //AB == FE
            if (a == f && c == e)
            {
                sharedLine = new Line(b, c);
                return true;
            }

            //AB == EF
            if (b == e && c == f)
            {
                sharedLine = new Line(b, c);
                return true;
            }

            //AB == GE
            if (b == g && c == e)
            {
                sharedLine = new Line(b, c);
                return true;
            }

            //AB == EG
            if (b == g && c == e)
            {
                sharedLine = new Line(b, c);
                return true;
            }
            #endregion

            return false;
        }

        /// <summary>
        ///Find the middle point of all lines of a triangle
        /// </summary>
        public static List<Point> GetMidpointsOfTriangle(Triangle t)
        {
            var points = new List<Point>();

            //go over all edges of the triangle
            foreach (var line in t.GetEdges())
            {
                //calculate midpoint of each edge and add it to the list
                points.Add(FindCenterOfLine(line));
            }

            return points;
        }

        /// <summary>
        /// Find the center point inside a triangle
        /// </summary>
        public static Point FindCentroidOfTriangle(Triangle t)
        {
            var p = Point.Zero;

            p.X = (t.Point1.X + t.Point2.X + t.Point3.X);
            p.X /= 3;

            p.Y = (t.Point1.Y + t.Point2.Y + t.Point3.Y);
            p.Y /= 3;


            p.X = Math.Round(p.X, 0);
            p.Y = Math.Round(p.Y, 0);


            return p;
        }

        /// <summary>
        /// Determine if a triangle has a point twice
        /// </summary>
        public static bool HasDoublePoint(this Triangle t)
        {
            var a = t.Point1;
            var b = t.Point2;
            var c = t.Point3;

            if (a == b) return true;
            if (a == c) return true;
            if (b == c) return true;

            return false;
        }

        /// <summary>
        /// Finda random point inside a triangle
        /// </summary>
        public static Point RandomPointInTriangle(this Triangle t)
        {
            var p = Point.Zero;

            //random point inside triangle
            var r1 = RandomHelper.RandomDouble();
            var r2 = RandomHelper.RandomDouble();

            p.X = (1 - Math.Sqrt(r1)) * t.Point1.X + (Math.Sqrt(r1) * (1 - r2)) * t.Point2.X + (Math.Sqrt(r1) * r2) * t.Point3.X;
            p.Y = (1 - Math.Sqrt(r1)) * t.Point1.Y + (Math.Sqrt(r1) * (1 - r2)) * t.Point2.Y + (Math.Sqrt(r1) * r2) * t.Point3.Y;
            
            return p;
        }

        #endregion

        #region Cell
        /// <summary>
        /// Find the center point of a cell
        /// </summary>
        public static Point Center(this Cell cell)
        {

            //add the first point at the end
            Point[] pts = new Point[cell.Points.Count + 1];
            cell.Points.CopyTo(pts, 0);
            pts[cell.Points.Count] = cell.Points[0];

            //find centroid
            double x = 0,
                   y = 0;

            for (int i = 0; i < cell.Points.Count; i++)
            {
                var secondFactor = pts[i].X * pts[i + 1].Y - pts[i + 1].X * pts[i].Y;

                x += (pts[i].X + pts[i + 1].X) * secondFactor;
                y += (pts[i].Y + pts[i + 1].Y) * secondFactor;
            }

            //divide by 6 times the polygon Area
            double area = cell.Area();
            x /= (6 * area);
            y /= (6 * area);

            if (x < 0)
            {
                x = -x;
                y = -y;
            }

            return new Point(x, y);

        }

        /// <summary>
        /// Find the area of a cell
        /// </summary>
        public static double Area(this Cell cell)
        {
            //add the first point at the end
            Point[] pts = new Point[cell.Points.Count + 1];
            cell.Points.CopyTo(pts, 0);
            pts[cell.Points.Count] = cell.Points[0];

            // Get the areas.
            double area = 0;
            for (int i = 0; i < cell.Points.Count; i++)
            {
                area +=
                    (pts[i + 1].X - pts[i].X) *
                    (pts[i + 1].Y + pts[i].Y) / 2;
            }

            return area;
        }


        public static Cell Inset(this Cell cell,int amount)
        {

            var insetCell = new Cell();
            insetCell.SitePoint = cell.SitePoint; //normally the site point should remain
            var c = cell.Center();

            //for every point of the cell move the point with the amount towards the center of the cell
            foreach (var p in cell.Points)
            {
                var yDiff = Math.Abs(p.Y - c.Y);
                var xDiff = Math.Abs(p.X - c.X);
                var distance = Math.Sqrt(Power(yDiff) + Power(xDiff));
                var unit = new Point(xDiff/distance,yDiff/distance);
                var newP = Point.Zero;

                var scaleX = p.X >= c.X ? -1 : 1;
                var scaleY = p.Y >= c.Y ? -1 : 1;


                unit.X *= scaleX;
                unit.Y *= scaleY;

                newP.X = p.X + unit.X*amount;
                newP.Y = p.Y + unit.Y * amount;

                insetCell.AddPoint(newP);
            }


            return insetCell;
        }

        #endregion

        #region Algorithms

        /// <summary>
        /// determine if a point is within the given bounds
        /// </summary>
        public static bool PointWithinBounds(Point p, Rectangle bounds)
        {
            return (p.X > bounds.Left && p.Y > bounds.Top && p.X < bounds.Right && p.Y < bounds.Bottom);
        }

        /// <summary>
        /// Find the triangle that contains all points
        /// </summary>
        public static Triangle FindSuperTriangle(ref IList<Point> points)
        {
            //1. find the maximum and minimum bounds of the super triangle
            var pMinX = points[0].X;
            var pMinY = points[0].Y;
            var pMaxX = points[0].X;
            var pMaxY = points[0].Y;

            for (var i = 1; i < points.Count; i++)
            {
                var p = points[i];

                //find min and max x
                if (p.X < pMinX)
                    pMinX = p.X;
                if (p.X > pMaxX)
                    pMaxX = p.X;

                //find min and max y
                if (p.Y < pMinY)
                    pMinY = p.Y;
                if (p.Y > pMaxY)
                    pMaxY = p.Y;
            }

            //3. calculate difference between min and max
            var dx = pMaxX - pMinX;
            var dy = pMaxY - pMinY;
            var dMax = (dx > dy) ? dx : dy;

            var pMidX = (pMaxX + pMinX) / 2;
            var pMidY = (pMaxY + pMinY) / 2;

            var pMin = Point.Zero;
            var pMid = Point.Zero;
            var pMax = Point.Zero;

            //4. Create points for the triangle
            pMin.X = pMidX - 2 * dMax;
            pMin.Y = pMidY - dMax;

            pMid.X = pMidX;
            pMid.Y = pMidY + 2 * dMax;

            pMax.X = pMidX + 2 * dMax;
            pMax.Y = pMidY - dMax;

            //6 Create Super Triangle from points
            return new Triangle(pMin, pMax, pMid);
        }

        /// <summary>
        /// Determine if the point lies within a circle
        /// </summary>
        public static bool IsPointInCircle(Point p, Triangle t)
        {
            var c = FindCircumCircleOfTriangle(t);

            //calculate radius of circle
            var dx = t.Point2.X - c.Center.X;
            var dy = t.Point2.Y - c.Center.Y;

            var rsqr = dx * dx + dy * dy;
            c.Radius = Math.Sqrt(rsqr);
            dx = p.X - c.Center.X;
            dy = p.Y - c.Center.Y;

            //check if point is in circle
            var drsqr = dx * dx + dy * dy;

            return drsqr <= rsqr;
        }

        /// <summary>
        /// find all center points of a given list of triangles
        /// </summary>
        public static List<Point> FindCenteroidsOfTriangles(List<Triangle> tlist)
        {
            return tlist.Select(FindCentroidOfTriangle).ToList();
        }

        /// <summary>
        /// Return all points near a given radius of a given point
        /// </summary>
        public static List<Point> FindPointsNearPoint(this List<Point> points, Point startPoint, double radius = 35.0)
        {
            var pointsInRadius = new List<Point>();

            foreach (var point in points)
            {
                var distance = DistanceBetweenPoints(startPoint, point);
                if (distance < radius)
                    pointsInRadius.Add(point);
            }


            return pointsInRadius;
        }

        /// <summary>
        /// out of a given list of points return the closest point to a given points
        /// </summary>
        public static Point FindClosestPoint(this List<Point> points, Point start)
        {
            Point closest = null;

            var minDistance = double.MaxValue;

            foreach (var point in points)
            {
                var distance = MathHelpers.DistanceBetweenPoints(start, point);
                if (distance < minDistance && point != start)
                {
                    closest = point;
                    minDistance = distance;
                }
            }
            
            return closest;

        }

        /// <summary>
        /// Convert a list of lines to a list of points
        /// </summary>
        public static List<Point> ToPoints(this List<Line> lines)
        {
            var points = new List<Point>();

            foreach (var line in lines)
            {
                points.Add(line.Start);
                points.Add(line.End);
            }

            return points;
        }

        /// <summary>
        /// Find all cells within the original generation bounds of a voronoi diagram
        /// </summary>
        /// <param name="voronoi"></param>
        /// <returns></returns>
        public static List<Cell> GetCellsInBounds(this VoronoiDiagram voronoi)
        {
            var cells = new List<Cell>();
            
            foreach (var cell in voronoi.VoronoiCells)
            {
                var cellCenter = cell.Center();

                //check if zone is in bounds (avoid generating infinitely large zones)
                if (cellCenter.X > voronoi.Bounds.Right || cellCenter.Y > voronoi.Bounds.Bottom
                    || cellCenter.X < voronoi.Bounds.Left || cellCenter.Y < voronoi.Bounds.Top)
                {
                    continue;
                }

                cells.Add(cell);
            }


            return cells;
        }

        /// <summary>
        /// Generate random points insidea  cell
        /// </summary>
        public static List<Point> GenerateRandomPoints(this Cell cell, int amount)
        {
            //Do an inset to avoid spawning points on the edges

            //Easier to take random points inside a triangle than a cell, so first triangulate the cell
            var triangles = new BowyerWatsonGenerator().DelaunayTriangulation(cell.Points);

            var points = new List<Point>();

            if (triangles.Count < 1)
            {
                return points;
            }

            var perTriangle = amount/triangles.Count;
            
            //for every triangle take the amount of points
            for (int i = 0; i < perTriangle; i++)
            {
                foreach (var t in triangles)
                {
                    var p = t.RandomPointInTriangle();
                    points.Add(p);
                }

            }

            return points;

        }

        public static Point GenerateRandomPointInCircle(Point origin, double radius,bool centered = false)
        {
            var p = Point.Zero;
            var angle = RandomHelper.RandomDouble()*Math.PI*2;
            var r = radius;

            if(centered)
            {
                r=  RandomHelper.RandomDouble()*radius;
            }
            else
            {
                r = Math.Sqrt(RandomHelper.RandomDouble()) * radius;
            }

            p.X = origin.X + r*Math.Cos(angle);
            p.Y = origin.Y + r * Math.Sin(angle);

            return p;
        }

        public static Rectangle GetCityBounds(CityData city)
        {
            double maxX = 0;
            double minX = double.MaxValue;
            double maxY = 0;
            double minY = double.MaxValue;

            //go over all cells and their edgepoints to find the bounds
            foreach (var district in city.Districts)
            {
                foreach (var cell in district.Cells)
                {
                    foreach (var point in cell.Points)
                    {
                        var x = point.X;
                        var y = point.Y;

                        if (x > maxX)
                            maxX = x;

                        if (x < minX)
                            minX = x;

                        if (y > maxY)
                            maxY = y;

                        if (y < minY)
                            minY = y;
                    }
                }
            }

            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        #endregion

    }

    public static class ListExtensions
    {
        public static void SortBySmallestLength(this IList<Road> l)
        {
            l = l.OrderBy(x => x.Length()).ToList();
        }
    }
}
