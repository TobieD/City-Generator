using System.Collections.Generic;
using System.Linq;
using BenTools.Mathematics;
using CityGenerator.VoronoiGenerator.Helpers;

namespace CityGenerator.VoronoiGenerator.Algorithms.Fortune
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


                var cell = new Cell {CellPoint = point};
                _siteCells.Add(point, cell);
            }

            //Create Voronoi Data using library
            var data = BenTools.Mathematics.Fortune.ComputeVoronoiGraph(dataPoints);
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
            foreach (var edge in data.Edges)
            {
                if (edge.IsInfinite || edge.IsPartlyInfinite)
                    continue;

                var pLeft = new Point(edge.LeftData[0],edge.LeftData[1]);
                var pRight = new Point(edge.RightData[0], edge.RightData[1]);

                var p1 = new Point(edge.VVertexA[0], edge.VVertexA[1]);
                var p2 = new Point(edge.VVertexB[0], edge.VVertexB[1]);

                if (_siteCells.ContainsKey(pLeft) == false || _siteCells.ContainsKey(pRight) == false)
                    continue;

                _siteCells[pLeft].AddPoint(p1);
                _siteCells[pLeft].AddPoint(p2);

                _siteCells[pRight].AddPoint(p1);
                _siteCells[pRight].AddPoint(p2);

            }

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

