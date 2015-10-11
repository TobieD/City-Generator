using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Voronoi
{
    public enum VoronoiAlgorithm
    {
        BoywerWatson,
        Lloyd,
        Fortune
    }

    public static class VoronoiCreator
    {
        private static int _height;
        private static int _width;

        /// <summary>
        /// Generate a given amount of points in a user defined rectangle
        /// </summary>
        public static List<Point> GenerateRandomPoints(int amount, Point startPoint, int width, int height,int seed)
        {
            //Create point list
            var points = new List<Point>();
            _height = height;
            _width = width;

            // Seed random
            var rnd = new Random(seed);

            //Generate points and add them to the collection
            for (var i = 0; i < amount; ++i)
            {
                var x = rnd.Next((int)startPoint.X, (int)startPoint.X + width);
                var y = rnd.Next((int)startPoint.Y, (int)startPoint.Y + height);

                var point = new Point(x, y);
                points.Add(point);
            }

            return points;
        }

        /// <summary>
        /// Create a Voronoi Diagram using a list of points and a specified algorithm to use
        /// </summary>
        public static VoronoiDiagram CreateVoronoi(List<Point> points, VoronoiAlgorithm algorithm)
        {
            //Create Voronoi Diagram
            var result = new VoronoiDiagram();

            //Select algorthm to use
            switch (algorithm)
            {
                case VoronoiAlgorithm.BoywerWatson:
                    result = Voronoi_BoywerWatson(points);
                    break;
                case VoronoiAlgorithm.Fortune:
                    result = Voronoi_Fortune(points);
                    break;

                case VoronoiAlgorithm.Lloyd:
                    result = Voronoi_Lloyd(points);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null);
            }

            //return the voronoi diagram
            return result;
        }

        /// <summary>
        /// Voronoi according to Boywer-Watson Algorithm
        /// http://paulbourke.net/papers/triangulate/
        /// </summary>
        private static VoronoiDiagram Voronoi_BoywerWatson(List<Point> points)
        {
            var diagram = new VoronoiDiagram();

            //Triangulae points based on Delaunay Triangulation
            diagram.Triangulation = DelaunayTriangulation(points);

            //connect centroid points of all adjacent triangles
            diagram.Lines = CreateVoronoiLines(diagram.Triangulation);

            return diagram;
        }

        /// <summary>
        /// Voronoi according to Fortunes Algorithm
        /// http://blog.ivank.net/fortunes-algorithm-and-implementation.html
        /// </summary>
        private static VoronoiDiagram Voronoi_Fortune(List<Point> points)
        {

            var fortuneHelper = new FortuneHelper(_width,_height);

            var dg = fortuneHelper.GetVoronoi(points);

            //return the diagram
            return dg;
        }

        /// <summary>
        /// Voronoi according to Fortunes Algorithm
        /// </summary>
        private static VoronoiDiagram Voronoi_Lloyd(List<Point> points)
        {
            //return the list of triangles
            return null;
        }

        #region Voronoi Helpers
        /// <summary>
        /// Create voronoi cells
        /// </summary>
        private static List<Cell> CreateVoronoiCells(List<Triangle> triangles )
        {
            //Initialize Cell list
            var cells = new List<Cell>();

            if (triangles == null || triangles.Count < 2 )
                return cells;

            //Go over all triangles
            foreach (var triangle1 in triangles)
            {
                var cell = new Cell();
                //compare triangle with other triangles
                for (int i = 1; i < triangles.Count; i++)
                {
                    var triangle2 = triangles[i];
                    
                    //when the triangles share a line connect the centeroid of the triangle
                    if (MathHelpers.HasSharedLineWith(triangle1, triangle2))
                    {
                        var circumT1 = MathHelpers.FindCentroidOfTriangle(triangle1);
                        var circumT2 = MathHelpers.FindCentroidOfTriangle(triangle2);

                        cell.AddPoint(circumT1);
                        cell.AddPoint(circumT2);
                    }
                }

                cells.Add(cell);

            }
  
            return cells;
        }

        /// <summary>
        /// Create voronoi diagram in lines
        /// </summary>
        private static List<Line> CreateVoronoiLines(List<Triangle> triangles)
        {
            //Initialize Cell list
            var lines = new List<Line>();

            if (triangles == null || triangles.Count < 2)
                return lines;

            //Go over all triangles
            foreach (var triangle1 in triangles)
            {
                //compare triangle with other triangles
                for (int i = 1; i < triangles.Count; i++)
                {
                    var triangle2 = triangles[i];

                    //when the triangles share a line connect the centeroid of the triangle
                    if (MathHelpers.HasSharedLineWith(triangle1, triangle2))
                    {
                        var circumT1 = MathHelpers.FindCentroidOfTriangle(triangle1);
                        var circumT2 = MathHelpers.FindCentroidOfTriangle(triangle2);

                        lines.Add(new Line(circumT1,circumT2));
                    }
                }
            }
            return lines;
        }

        /// <summary>
        /// Create DelaunayTriangulation of a given list of points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        private static List<Triangle> DelaunayTriangulation(IList<Point> points)
        {
             //0. Need atleast 3 points before we can triangulate
            if (points.Count < 3)
                return null;

            //1. Create a Triangle List
            var triangles = new List<Triangle>();

            //2. Determine the Super Triangle (Encompasses all the sample points)
            //   Add triangle points at the end of the point list
            //   used to initialize the algorithm and will be removed later
            var superTriangle= MathHelpers.FindSuperTriangle(ref points);
            triangles.Add(superTriangle);

            //3. Include each point one at the time into the existing triangulation
            foreach (var point in points)
            {
                //3.1 Initialize edge buffer
                var edges = new List<Line>();

                //3.2 if the actual point lies inside the circumcircle then the 3 edges of the triangle get added
                //    to the edge buffer and the triangle is removed from the list
                for (var triangleIndex = triangles.Count - 1; triangleIndex >= 0; triangleIndex--)
                {
                    var triangle = triangles[triangleIndex];

                    //3.2.1 is point in circle
                    if (MathHelpers.IsPointInCircle(point, triangle))
                    {
                        //3.2.2 add edges of current triangle
                        edges.AddRange(triangle.GetEdges());

                        //3.2.2 remove triangle
                        triangles.RemoveAt(triangleIndex);
                    }
                }

                //3.3 remove duplicate edges, this leaves the convec hull of the edges
                //    edges in this convex hull will be oriented counterclockwise
                for (var j = edges.Count - 2; j >= 0; j--)
                {
                    for (var k = edges.Count - 1; k >= j+1; k--)
                    {
                        //Get edges
                        var line1 = edges[j];
                        var line2 = edges[k];

                        if (line1 == line2)
                        {
                            //Remove duplicate edges
                            edges.RemoveAt(k);
                            edges.RemoveAt(j);
                            k--;
                        }
                    }
                }

                //3.4 Generate new counterclockwise oriented triangles filling the hole in the existing triangulation
                foreach (var line in edges)
                {
                    var t = new Triangle(line.Point1,line.Point2,point);
                    triangles.Add(t);
                }

                //3.5 Remove possible duplicate triangles
                for(int i = 0; i< triangles.Count; i++)
                {
                    var triangle = triangles[i];
                    if (triangle.HasDoublePoint())
                    {
                        triangles.RemoveAt(i);
                    }
                }
            }

            //4 Remove triangles sharing a point with the super triangle
            for (var i = triangles.Count - 1; i >= 0; i--)
            {
                if (MathHelpers.HasSharedPointWith(triangles[i], superTriangle))
                {
                    triangles.RemoveAt(i);
                }
            }

            //return the list of triangles
            return triangles;
        }
        #endregion
    }
}