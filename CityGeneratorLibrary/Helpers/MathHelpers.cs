using System;
using System.Collections.Generic;
using System.Linq;
using Voronoi;

namespace Helpers
{

    public static class RandomGen
    {

        public static int Random(int min, int max)
        {
            SeedRandom();

            return _rng.Next(min, max);
        }

        public static bool RandomBool()
        {
            SeedRandom();

            var i = _rng.Next(0, 1);

            return (i == 1) ? true : false;


        }

        private static Random _rng;

        private static void SeedRandom()
        {
            if (_rng == null)
            {
                _rng = new Random(DateTime.Now.GetHashCode());
            }
        }
    }


    public static class MathHelpers
    {
        /// <summary>
        /// Find the triangle that contains all points
        /// </summary>
        /// <returns></returns>
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

            ////5.Add points to the list
            //points.Add(pMin);
            //points.Add(pMid);
            //points.Add(pMax);

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
        /// given a triangle calculate the circle that passes through all 3 points
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
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
        /// Determine if two triangles share atleast 2 points
        /// </summary>
        public static bool HasSharedLineWith(Triangle t1, Triangle t2, ref Line sharedLine)
        {
            var a =t1.Point1;
            var b =t1.Point2;
            var c =t1.Point3;
                        
            var f =t2.Point1;
            var g =t2.Point2;
            var e =t2.Point3;

            #region AB == Any edge
            //AB == FG
            if (a == f && b == g)
            {
                sharedLine = new Line(a,b);
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
            if (a ==e && b == f)
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

        public static Line FindPerpendicularBisectorOfLine(Line l)
        {
            var p1 = Point.Zero;
            var p2 = Point.Zero;

            #region old
            ////1. find midpoint
            //var mp = FindCenterOfLine(l);

            ////2. Find slope
            //var slope = (l.Point2.Y - l.Point1.Y) / (l.Point2.X - l.Point1.X);
            //var negReci = -1 / slope;

            ////3.

            //var b = mp.Y - (negReci * mp.X);

            //p1 = mp;
            //p2.X = b / slope;
            //p2.Y = (negReci * p2.X) + (b);
            #endregion

            var dx = l.Point2.X - l.Point1.X;
            var dy = l.Point2.Y - l.Point1.Y;

            p1.X = -dy;
            p1.Y = dx;
            p2.X = dy;
            p2.Y = -dx;

            return new Line(FindCenterOfLine(l), p2);
        }

        public static Point FindCenterOfLine(Line l)
        {
            //take average
            var middlePoint = Point.Zero;
            middlePoint.X = (l.Point1.X + l.Point2.X) / 2;
            middlePoint.Y = (l.Point1.Y + l.Point2.Y) / 2;

            return middlePoint;
        }

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

        public static List<Point> FindCenteroidsOfTriangles(List<Triangle> tlist)
        {

            var plist = new List<Point>();
            foreach (var t in tlist)
            {
                plist.Add(FindCentroidOfTriangle(t));
            }

            return plist;
        }

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

        public static double DistanceBetweenPoints(Point p1, Point p2)
        {
            return Math.Sqrt((p1.X - p2.X)*(p1.X - p2.X) + (p1.Y - p2.Y)*(p1.Y - p2.Y));
        }

        public static double CounterClockWise(Point p1, Point p2, Point p3)
        {
            return (p2.X - p1.X)*(p3.Y - p1.Y) - (p2.Y - p1.Y)*(p3.X - p1.X);
        }

        public static Point FindCenteroidOfCell(Cell cell)
        {

            //if(!(cell.Points.Count > 0))
            //    return new Point(double.NaN,double.NaN);


           //add the first point at the end
            Point[] pts = new Point[cell.Points.Count + 1];   
            cell.Points.CopyTo(pts,0);
            pts[cell.Points.Count] = cell.Points[0];

            //find centroid
            double x = 0,
                   y = 0;

            for (int i = 0; i < cell.Points.Count; i++)
            {
                var secondFactor = pts[i].X*pts[i + 1].Y - pts[i + 1].X*pts[i].Y;

                x += (pts[i].X + pts[i + 1].X)*secondFactor;
                y += (pts[i].Y + pts[i + 1].Y) * secondFactor;
            }

            //divide by 6 times the polygon Area
            double area = FindAreaOfCell(cell);
            x /= (6*area);
            y /= (6 * area);

            if (x < 0)
            {
                x = -x;
                y = -y;
            }

            return new Point(x,y);

        }

        public static double FindAreaOfCell(Cell cell)
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

        public static bool PointWithinBounds(Point p, Rectangle bounds)
        {
            return (p.X > bounds.Left && p.Y > bounds.Top && p.X < bounds.Right && p.Y < bounds.Bottom);
        }

        public static List<Point> FindPointsNearPoint(this List<Point> points, Point startPoint, double radius = 35.0)
        {
            var pointsInRadius = new List<Point>();

            foreach (var point in points)
            {
                var distance = MathHelpers.DistanceBetweenPoints(startPoint, point);
                if (distance < radius)
                    pointsInRadius.Add(point);
            }


            return pointsInRadius;
        }

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

        private static Random _rng;

        public static T GetRandomValue<T>(this IEnumerable<T> list)
        {
            SeedRng();

            var count = list.Count();
            return list.ElementAt(_rng.Next(count));
        }

        private static void SeedRng()
        {
            if (_rng == null)
                _rng = new Random(DateTime.Now.GetHashCode());
        }


        public static List<Point> ToPoints(this List<Line> lines)
        {
            var points = new List<Point>();

            foreach (var line in lines)
            {
                points.Add(line.Point1);
                points.Add(line.Point2);
            }


            return points;
        }


        public static Point RandomPointInTriangle(this Triangle t)
        {

            SeedRng();

            var p = Point.Zero;

            //random point inside triangle
            var r1 = _rng.NextDouble();
            var r2 = _rng.NextDouble();

            p.X = (1 - Math.Sqrt(r1))*t.Point1.X + (Math.Sqrt(r1)*(1 - r2))*t.Point2.X + (Math.Sqrt(r1)*r2)*t.Point3.X;
            p.Y = (1 - Math.Sqrt(r1)) * t.Point1.Y + (Math.Sqrt(r1) * (1 - r2)) * t.Point2.Y + (Math.Sqrt(r1) * r2) * t.Point3.Y;



            return p;
        }


        public static Point FindRandomPointOnLine(this Line line, double min, double max)
        {
            //get points of line
            var p1 = line.Point1;
            var p2 = line.Point2;

            var p = Point.Zero;

            var t =_rng.NextDouble();

            

            if (t < min)
            {
                t = min;
            }
            else if (t > max)
                t = max;

            p.X = p1.X + t*(p2.X - p1.X);
            p.Y = p1.Y + t * (p2.Y - p1.Y);

            return p;

        }

        public static Point FindPerpendicularPointOnLine(this Line l, Point p3)
        {

            var p = Point.Zero;
            var p1 = l.Point1;
            var p2 = l.Point2;



            var dx = p2.X - p1.X;
            var dy = p2.Y - p1.Y;

            p.X = dy;
            p.Y = -dx;


            return p3 + p;
        }

        public static List<Cell> GetCellsInBounds(this VoronoiDiagram voronoi)   
        {
            var cells = new List<Cell>();


            foreach (var cell in voronoi.VoronoiCells)
            {
                var cellCenter = MathHelpers.FindCenteroidOfCell(cell);

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


        public static Line ExtendLine(this Line line, double length)
        {

            var p1 = line.Point1;
            var p2 = line.Point2;


            var lengthOfLine = DistanceBetweenPoints(p1, p2);

            var c = Point.Zero;
            var d = p1;
            
            c.X = p2.X + (p2.X - p1.X)/lengthOfLine*length;
            c.Y = p2.Y + (p2.Y - p1.Y) / lengthOfLine * length;

            if (RandomGen.RandomBool())
            {
                d.X = p1.X + (p1.X - p2.X)/lengthOfLine*length;
                d.Y = p1.Y + (p1.Y - p2.Y)/lengthOfLine*length;
            }



            return new Line(d,c);
        }

        public static Point Center(this Line line)

        {
            return FindCenterOfLine(line);
        }

    }
}
