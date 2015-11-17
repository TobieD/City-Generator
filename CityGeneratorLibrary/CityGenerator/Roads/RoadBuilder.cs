
using System;
using System.Collections.Generic;
using System.Linq;
using CityGenerator;
using Helpers;
using Voronoi;

namespace CityGenerator
{

    internal class RoadBuilder
    { 
        /// <summary>
        /// Build a road inside a district cell
        /// </summary>
        public Road BuildRoad(RoadSettings roadSettings,Cell cell)
        {

            //Edges are the bounds of the road and will be part of the road as well
            var edges = cell.Edges.ToList();

            //find the center of the cell as start point
            var start = MathHelpers.FindCenteroidOfCell(cell);
            start = cell.SitePoint;

            var e = edges.GetRandomValue();
            edges.Remove(e);

            start = e.Center();
                
            var end = MathHelpers.FindCenterOfLine(edges.GetRandomValue());
            
            //use recursion to generate all the lines of the road
            var road = GenerateRoad(null, edges, start,end,roadSettings.Branches);

            //all edges of the cell are part of the road
            foreach (var edge in cell.Edges)
            {
                road.Lines.Add(edge);
            }

            return road;

        }
        
        /// <summary>
        /// Keep generating roads untill branches are zero
        /// </summary>
       private Road GenerateRoad(Road road, List<Line> cellEdges , Point start,Point end, int branches)
        {
            //Create a new road if none exists
            if (road == null)
            {
                road = new Road();
            }

            //create part of the road
            var line = new Line(start,end);
            road.Lines.Add(line);

            //reduce branches
            branches--;
            if (branches + 1 <= 0 || cellEdges.Count < 1)
            {
                return road;
            }

            //start = MathHelpers.FindCenterOfLine(line);
            start = line.FindRandomPointOnLine(0.33,0.66);
            start = line.Center();
            var newEdge = cellEdges.GetRandomValue();
            cellEdges.Remove(newEdge);
            end = newEdge.FindRandomPointOnLine(0.33, 0.66);

            //generate a new road
            return GenerateRoad(road,cellEdges,start,end,branches);
        }
    }
}
