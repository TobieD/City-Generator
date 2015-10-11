using System.Collections.Generic;
using System.Linq;

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
        #endregion
    }

    /// <summary>
    /// Consists of 2 points
    /// </summary>
    public class Line
    {
        public Point Point1;
        public Point Point2;

        public Line(Point p1, Point p2)
        {
            Point1 = p1;
            Point2 = p2;
        }
        #region Operator Overloading

        public override int GetHashCode()
        {
            return this.Point1.GetHashCode() ^ this.Point2.GetHashCode();
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

            return ((left.Point1 == right.Point1 && left.Point2 == right.Point2) ||
                   (left.Point1 == right.Point2 && left.Point2 == right.Point1));
        }

        /// <summary>
        /// Test if two lines are eual
        /// </summary>
        /// <remarks>Gives stack overflow error</remarks>
        public static bool operator !=(Line left, Line right)
        {
            return left != right;
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
        public List<Point> Points;

        public Cell()
        {
            Points = new List<Point>();
        }

        public Cell(IEnumerable<Point> points)
        {
            Points = new List<Point>();

            foreach (var point in points)
            {
                AddPoint(point);
            }
        }

        public void AddPoint(Point p)
        { 
            Points.Add(p);
        }

        public void AddLine(Line l)
        {
            Points.Add(l.Point1);
            Points.Add(l.Point2);
        }
    }
    
    /// <summary>
    /// Stores all results of a voronoi Diagram
    /// </summary>
    public class VoronoiDiagram
    {
        public List<Point> Points; //points used to generate the Voronoi Diagram 
        public List<Triangle> Triangulation; //possible triangulation required
        public List<Line> Lines; //voronoi diagram in line format
        public List<Cell> VoronoiCells; //voronou diagram in Cell format 

        public VoronoiDiagram()
        {
            Triangulation = new List<Triangle>();
            Lines = new List<Line>();
            VoronoiCells = new List<Cell>();
        }

        public void Clear()
        {
            Triangulation.Clear();
            Lines.Clear();   
            VoronoiCells.Clear();
        }
    }
}