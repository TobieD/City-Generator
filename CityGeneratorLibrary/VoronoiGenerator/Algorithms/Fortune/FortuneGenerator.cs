using System.Collections.Generic;
using System.Linq;
using Helpers;
using Voronoi.Algorithms.FortuneHelpers;

namespace Voronoi.Algorithms
{
    /// <summary>
    /// Generate a voronoi diagram with an external library
    /// </summary>
    internal class FortuneGenerator
    {

        private VoronoiDiagram _voronoi;

        private Dictionary<Point, Cell> _siteCells = new Dictionary<Point, Cell>();

        //Generate the voronoi diagram using an external library
        public VoronoiDiagram GetVoronoi(List<Point> points)
        {
            _siteCells = new Dictionary<Point, Cell>();
            
            var nrPoints = points.Count;
           
            var dataPoints = new Vector[nrPoints];

            for (int i = 0; i < nrPoints; i++)
            {
                var point = points[i];
                if (_siteCells.ContainsKey(point))
                    continue;

                dataPoints[i] = new Vector(point.X,point.Y);

                var cell = new Cell {SitePoint = point};
                _siteCells.Add(point, cell);
            }

            //Create Voronoi Data using library
            var data = Fortune.ComputeVoronoiGraph(dataPoints);
            //data = BenTools.Mathematics.Fortune.FilterVG(data, 15);

            //Create Diagram
            _voronoi = new VoronoiDiagram();
            

            _voronoi.HalfEdges = GenerateLines(data);
            
            _voronoi.VoronoiCells = GenerateCells(data);
            _voronoi.Sites = points;
            
            return _voronoi;
        }

        private List<Cell> GenerateCells(VoronoiGraph data)
        {
            //go over all generated edges
            foreach (var edge in data.Edges)
            {
                //make sure it is a valid edge
                if (edge.IsInfinite || edge.IsPartlyInfinite)
                    continue;

                //site point to the left of the edge
                var pLeft = new Point(edge.LeftData[0],edge.LeftData[1]);

                //site point to the right of the edge
                var pRight = new Point(edge.RightData[0], edge.RightData[1]);

                //start and end points of the edge
                var p1 = new Point(edge.VVertexA[0], edge.VVertexA[1]);
                var p2 = new Point(edge.VVertexB[0], edge.VVertexB[1]);

                //is the sitepoint valid?
                if (_siteCells.ContainsKey(pLeft) == false || _siteCells.ContainsKey(pRight) == false)
                    continue;

                //Create a line from start till end
                var line = new Line(p1,p2);

                //Add this line to the left cell and store a reference to the cell to the line
                _siteCells[pLeft].AddPoint(p1);
                _siteCells[pLeft].AddPoint(p2);
                _siteCells[pLeft].AddLine(line);
                line.CellLeft = _siteCells[pLeft];

                //Add this line to the right cell and store a reference to the cell to the line
                _siteCells[pRight].AddPoint(p1);
                _siteCells[pRight].AddPoint(p2);
                _siteCells[pRight].AddLine(line);
                line.CellRight = _siteCells[pRight];

                line.bSharedBetweenCells = true;

            }

            //Filter out double cells
            _siteCells.Values.ToList().FilterDoubleValues();
            _voronoi.SiteCellPoints = _siteCells;

            return _siteCells.Values.ToList();
        }

        private List<Line> GenerateLines(VoronoiGraph data)
        {
            var lines = new List<Line>();
            var linePoints = new List<Point>();

            foreach (var edge in data.Edges)
            {
                if (edge.IsInfinite || edge.IsPartlyInfinite)
                    continue;

                var p1 = new Point(edge.VVertexA[0], edge.VVertexA[1]);
                var p2 = new Point(edge.VVertexB[0], edge.VVertexB[1]);

                var line = new Line(p1,p2); 
                lines.Add(line);
            }

            return lines;
        }
        
    }
}

