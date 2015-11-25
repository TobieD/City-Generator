
using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using Voronoi;

namespace CityGenerator
{

    internal class RoadBuilder
    { 
        /// <summary>
        /// Build a road inside a district cell
        /// </summary>
        public Road BuildRoad(RoadSettings roadSettings,DistrictCell cell)
        {
            //Create the road for the cell
            var road = new Road();

            //Edges are the bounds of the road and will be part of the road as well
            var edges = cell.Cell.Edges.ToList();

            //find the longest line in the cell as a start line
            var longest = FindLongestLineInCell(edges);
            var start = longest.Start;
            var end = longest.End;

            //Remove all edges from the cell that are smaller than 1/4th of the longest
            //this avoids having multiple roads starting close together

            var minLength = longest.Length()*0.25;
            for(int i = 0; i < edges.Count; ++i)
            {
                
                if (edges[i].Length() < minLength)
                {
                    edges.Remove(edges[i]);
                }

            }

            //Create all the parts of the road
            road.Lines.AddRange(GenerateRoad(null, edges, start, end, roadSettings.Branches));

            //all edges of the cell are part of the road
            foreach (var edge in cell.Cell.Edges)
            {
                //add the original side edge
                //road.Lines.Add(edge);
            } 

            return road;

        }
        
        /// <summary>
        /// Keep generating roads until branches are zero
        /// </summary>
        private List<Line> GenerateRoad(List<Line> lines, List<Line> cellEdges , Point start,Point end, int branches)
        {
            //Create a new road if none exists
            if (lines == null)
            {
                lines = new List<Line>();
            }

            //Create the new line
            var line = new Line(start, end);

            //check for possible intersection, 
            //if an intersection happens the end point becomes the point of intersection
            foreach (var l in lines)
            {
                //exit on first intersection
                //make the new endpoint the intersection point
                if (line.IntersectsWith(l))
                {
                    bool parallel = false;
                    var ip = line.FindIntersectionPoint(l,ref parallel);

                    if(parallel)
                        continue;

                    line.End = ip;

                    line.Intersected = true;
                    line.Left = end; //original generated end point
                    line.Right = start; //point of intersection
                    line.Intersect = ip;

                    //Check if the new line will not be too small
                    //if it is too small switch the start point with the previous end point
                    //distance of the original line
                    double totalDistance = MathHelpers.DistanceBetweenPoints(start, end);
                    //distance of the start point till the intersect point
                    double newDistance = MathHelpers.DistanceBetweenPoints(start, ip);

                    //swap occurs when the new distance is smaller than 1/3 of the original distance
                    if (newDistance < (totalDistance * 0.33))
                    {
                        line.Start = end;
                    }

                    break;
                }

            }


            //Add the line to the roads
            lines.Add(line);

            //reduce branches to end recursion
            branches--;
            if (branches + 1 <= 0 || cellEdges.Count < 1)
            {
                return lines;
            }

            //find new start and end edges
            line = cellEdges.GetRandomValue();
            var newEdge = cellEdges.GetRandomValue();

            while (newEdge == line)
                newEdge = cellEdges.GetRandomValue();

            double min = 0.33;
            double max = 1 - min;

            //Generate a new start and endpoint random on the lines
            start = line.FindRandomPointOnLine(min, max);
            end = newEdge.FindRandomPointOnLine(min, max);

            end = newEdge.Center();
            
            //generate a new road
            return GenerateRoad(lines, cellEdges,start,end, branches);
        }

        private Line FindLongestLineInCell(List<Line> edges)
        {
            Point start = Point.Zero, end = Point.Zero;
            double maxDistance = 0;

            Line e1 = null;
            Line e2 = null;
            for (int x = 0; x < edges.Count; ++x)
            {
                for (int y = 1; y < edges.Count; ++y)
                {
                    var centerLine1 = edges[x].FindRandomPointOnLine(0.33,0.66);
                    var centerLine2 = edges[y].FindRandomPointOnLine(0.33,0.66);

                    var distance = MathHelpers.DistanceBetweenPoints(centerLine1, centerLine2);


                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        start = centerLine1;
                        end = centerLine2;
                        e1 = edges[x];
                        e2 = edges[y];
                    }
                }

            }

            //remove the edges used for the line
            edges.Remove(e1);
            //edges.Remove(e2);

            return new Line(start,end);
        }
    }
}
