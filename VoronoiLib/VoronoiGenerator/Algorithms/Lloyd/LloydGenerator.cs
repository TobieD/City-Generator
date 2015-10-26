using System;
using System.Collections.Generic;
using System.Linq;
using Voronoi.Helpers;

namespace Voronoi.Algorithms.Fortune
{
    /// <summary>
    /// Generate a voronoi diagram with the lloyd algorithm
    /// divides all the cells of a voronoi diagram in equally sized cells
    /// </summary>
    internal class LloydGenerator
    {

        private VoronoiDiagram _voronoi;

        public VoronoiDiagram GetVoronoi(List<Voronoi.Point> points)
        {
            _voronoi = new VoronoiDiagram();
            
            var fortune = new FortuneGenerator();

            var iterations = 15;
            var sites = points;
            _voronoi.Sites = points;

            for (int i = 0; i < iterations; i++)
            {
                //calculate the diagram of the points
                _voronoi = fortune.GetVoronoi(sites);

                //remove old sites
                sites = new List<Point>();

                //take the centers of the cell as the new site points.
                foreach (var voronoiCell in _voronoi.VoronoiCells)
                {
                    Point newSite = MathHelpers.FindCenteroidOfCell(voronoiCell);
                    //if(newSite.X == double.NaN || newSite.Y == double.NaN) continue;
                    sites.Add(newSite);
                }
            }



            return _voronoi;
        }

    }
}

