using System;
using System.Collections.Generic;

namespace Voronoi
{
    /// <summary>
    /// Represents a 2D point
    /// </summary>
    public class Point
    {
        public double X;
        public double Y;

        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }

        //Statics
        public static Point Zero => new Point(0, 0);
        public static Point Max => new Point(double.MaxValue, double.MaxValue);
        

        #region Operator Overloading
        public override string ToString()
        {
            return $"[{X},{Y}]";
        }

        /// <summary>A hash code for the point.</summary>
        public override int GetHashCode()
        {
            int xHc = this.X.ToString().GetHashCode();
            int yHc = this.Y.ToString().GetHashCode();

            return xHc ^ yHc;
        }

        /// <summary>Tests if two points are considered equal.</summary>
        public override bool Equals(object obj)
        {
            return this == (Point)obj;
        }

        /// <summary>Tests if two points are considered equal.</summary>
        public static bool operator ==(Point left, Point right)
        {
            if (((object)left) == ((object)right))
            {
                return true;
            }

            if ((((object)left) == null) || (((object)right) == null))
            {
                return false;
            }

            // Just compare x and y here...
            if (left.X != right.X) return false;
            if (left.Y != right.Y) return false;

            return true;
        }

        /// <summary>Tests if two points are considered equal.</summary>
        public static bool operator !=(Point left, Point right)
        {
            return !(left == right);
        }


        public static bool operator >(Point left, Point right)
        {
            return (left.X > right.X );
        }

        public static bool operator <(Point left, Point right)
        {
            return (left.X < right.X);
        }

        public static Point operator -(Point left, Point right)
        {
            return new Point(left.X - right.X,left.Y - right.Y);
        }

        public static Point operator +(Point left, Point right)
        {
            return new Point(left.X + right.X, left.Y + right.Y);
        }

        public Point Normalize()
        {
            double distance = Math.Sqrt(this.X*this.X + this.Y*this.Y);
            return new Point(this.X/distance, this.Y/distance);
        }

        #endregion
    }

    
    

    /// <summary>
    /// Consists of 2 points
    /// </summary>
    public class Line
    {
        public Point Start;
        public Point End;


        public Point Left;
        public Point Right;
        public Point Intersect;

        public bool Intersected = false;

        public Line(Point p1, Point p2)
        {
            Start = p1;
            End = p2;
        }
        #region Operator Overloading

        public override int GetHashCode()
        {
            return this.Start.GetHashCode() ^ this.End.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this == (Line)obj;
        }

        /// <summary>
        /// Test if two lines are equal
        /// </summary>
        public static bool operator ==(Line left, Line right)
        {
            //exact same points
            if (((object)left) == ((object)right))
                return true;

            //one is not initialized correctly
            if ((((object) left) == null) || (((object) right) == null))
                return false;

            return ((left.Start == right.Start && left.End == right.End) ||
                   (left.Start == right.End && left.End == right.Start));
        }

        /// <summary>
        /// Test if two lines are eual
        /// </summary>
        public static bool operator !=(Line left, Line right)
        {
            return !(left == right);
        }

        public static bool operator >(Line left, Line right)
        {
            return (left.Start > right.Start && left.End > right.End);
        }

        public static bool operator <(Line left, Line right)
        {
            return (left.Start < right.Start && left.End < right.End)
            ;
        }


        public override string ToString()
        {
            return $"Line: {Start} - {End}";
        }

        #endregion
    }

    /// <summary>
    /// Consists of 3 points that are connected
    /// </summary>
    public class Triangle
    {
       public Point Point1;
       public Point Point2;
       public Point Point3;

        public Triangle(Point point1, Point point2, Point point3)
        {
            Point1 = point1;
            Point2 = point2;
            Point3 = point3;
        }

        /// <summary>
        /// return the edges of the triangle as a line list
        /// </summary>
        /// <returns></returns>
        public List<Line> GetEdges()
        {
            var l1 = new Line(Point1, Point2);
            var l2 = new Line(Point2, Point3);
            var l3 = new Line(Point3, Point1);

            return new List<Line>()
            {
                l1,l2,l3
            };
        } 
    }

    public class Rectangle
    {
        public double Left;
        public double Right;

        public double Top;
        public double Bottom;

        public Rectangle(double left, double top, double width, double height)
        {
            Left = left;
            Right = left + width;

            Top = top;
            Bottom = top + height;
        }
    }

    public class Circle
    {
        public Point Center;
        public double Radius;

        public Circle(Point center, double radius)
        {
            Center = center;
            Radius = radius;
        }
    }

    /// <summary>
    /// A cell has an infinite amount of points that are all connected to eachother
    /// </summary>
    public class Cell
    {
        /// <summary>
        /// the points that create the edges of the cell
        /// </summary>
        public List<Point> Points;
        /// <summary>
        ///the point that the cell is build around
        /// /// </summary>
        public Point SitePoint;

        public List<Line> Edges; 

        public Cell()
        {
            Points = new List<Point>();
            Edges = new List<Line>();
        }

        public Cell(IEnumerable<Point> points)
        {
            Points = new List<Point>();
            Edges = new List<Line>();

            foreach (var point in points)
            {
                AddPoint(point);
            }
        }

        public void AddPoint(Point p)
        { 

            if(Points.Contains(p))
                return;

            Points.Add(p);

            SortAlgorithms.ReferencePoint = SitePoint;
            Points.Sort(new Comparison<Point>(SortAlgorithms.SortClockwise));
        }

        public void AddLine(Line l)
        {
            Edges.Add(l);
            //Points.Add(l.Point1);
            //Points.Add(l.Point2);
        }
    }

    internal static class SortAlgorithms
    {
        public static Point ReferencePoint ;

        public static int SortClockwise(Point p1, Point p2)
        {
            var aTan1 = Math.Atan2(p1.Y - ReferencePoint.Y, p1.X - ReferencePoint.X);
            var aTan2 = Math.Atan2(p2.Y - ReferencePoint.Y, p2.X - ReferencePoint.X);

            if (aTan1 < aTan2) return -1;
            else if (aTan1 > aTan2) return 1;

            return 0;
        }
    }
    
    /// <summary>
    /// Stores all results of a voronoi Diagram
    /// </summary>
    public class VoronoiDiagram
    {
        public List<Point> Sites; //points used to generate the Voronoi Diagram 
        public List<Triangle> Triangulation; //possible triangulation required
        public List<Line> HalfEdges; //voronoi diagram in line format
        public List<Cell> VoronoiCells; //voronoi diagram in Cell format 
        public Dictionary<Point, Cell> SiteCellPoints; //store the cell with their corresponding site

        public Rectangle Bounds;

        public VoronoiDiagram()
        {
            Triangulation = new List<Triangle>();
            HalfEdges = new List<Line>();
            VoronoiCells = new List<Cell>();
            SiteCellPoints = new Dictionary<Point,Cell>();
        }

        public void Clear()
        {
            Triangulation.Clear();
            HalfEdges.Clear();   
            VoronoiCells.Clear();
            SiteCellPoints.Clear();
        }
    }
}