using System.Collections.Generic;
using System.Linq;
using Helpers;

namespace Voronoi.Algorithms
{    
    /// <summary>
    /// Helper class for generating a Voronoi Diagram using the Bowyer Watson algorithm
    /// </summary>
    public class BowyerWatsonGenerator
    {

        private VoronoiDiagram _voronoi;

        /// <summary>
        /// all points that will make cells
        /// </summary>
        public List<Point> _cellEdgePoints = new List<Point>();

        public VoronoiDiagram GetVoronoi(List<Point> points)
        {
            _voronoi = new VoronoiDiagram();

            _voronoi.Sites = points;

            //Triangulate points based on Delaunay Triangulation
            _voronoi.Triangulation = DelaunayTriangulation(points);

            //connect centroid points of all adjacent triangles
            _voronoi.HalfEdges = CreateVoronoiLines(_voronoi.Triangulation);
            _voronoi.VoronoiCells = CreateVoronoiCells(_voronoi.HalfEdges);
                
            return _voronoi;
        }

        /// <summary>
        /// Create Delaunay Triangulation of a given list of points
        /// </summary>
        public List<Triangle> DelaunayTriangulation(IList<Point> points)
        {
            //0. Need atleast 3 points before we can triangulate
            if (points.Count < 3)
                return null;

            //1. Create a Triangle List
            var triangles = new List<Triangle>();

            //2. Determine the Super Triangle (Encompasses all the sample points)
            //   Add triangle points at the end of the point list
            //   used to initialize the algorithm and will be removed later
            var superTriangle = MathHelpers.FindSuperTriangle(ref points);
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
                    for (var k = edges.Count - 1; k >= j + 1; k--)
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
                    var t = new Triangle(line.Start, line.End, point);
                    triangles.Add(t);
                }

                //3.5 Remove possible duplicate triangles
                for (int i = 0; i < triangles.Count; i++)
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

        /// <summary>
        /// Create a voronoi diagram in cell format
        /// </summary>
        private List<Cell> CreateVoronoiCells(List<Line> lines)
        {
            //Initialize Cell list
            var cells = new Dictionary<Point, Cell>();

            //Create a dictionary
            foreach (var point in _voronoi.Sites)
            {

                if(cells.ContainsKey(point))
                    continue;

                var c = new Cell();
                c.SitePoint = point;
                cells.Add(point,c);
            }

            
            foreach (var line in lines)
            {
                cells[line.Left].AddPoint(line.Start);
                cells[line.Left].AddPoint(line.End);
                cells[line.Left].AddLine(line);
                line.CellLeft = cells[line.Left];

                cells[line.Right].AddPoint(line.Start);
                cells[line.Right].AddPoint(line.End);
                cells[line.Right].AddLine(line);
                line.CellRight = cells[line.Right];
                line.bSharedBetweenCells = true;
            }

            _voronoi.SiteCellPoints = cells;
            
            return cells.Values.ToList();

        }

        /// <summary>
        /// Create voronoi diagram in lines
        /// </summary>
        private List<Line> CreateVoronoiLines(List<Triangle> triangles)
        {
            //Initialize Cell list
            var lines = new List<Line>();

            if (triangles == null || triangles.Count < 2)
                return lines;

            //Go over all triangles
            foreach (var triangle1 in triangles)
            {
                //compare triangle with other triangles
                foreach (var triangle2 in triangles)
                {
                    Line sharedLine = null;
                    //bug with the edge cases
                    if (!MathHelpers.HasSharedLineWith(triangle1, triangle2,ref sharedLine)) continue;

                    //when the triangles share a line connect the centeroid of the triangle
                    var circumT1 = MathHelpers.FindCentroidOfTriangle(triangle1);
                    var circumT2 = MathHelpers.FindCentroidOfTriangle(triangle2);


                    var line = new Line(circumT1, circumT2)
                    {
                        Left = sharedLine.Start,
                        Right = sharedLine.End
                    };

                    lines.Add(line);

                    //add edgepoints
                    _cellEdgePoints.Add(circumT1);
                    _cellEdgePoints.Add(circumT2);
                }
            }


            //filter out double points
            //_cellEdgePoints.FilterDoubleValues();

            //_voronoi.VoronoiCellPoints = _cellEdgePoints;
            return lines;
        }

    }
}
