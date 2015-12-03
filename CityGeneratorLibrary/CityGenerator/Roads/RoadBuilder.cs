using System.Collections.Generic;
using System.Linq;
using Helpers;
using Voronoi;

using SplitLine = System.Collections.Generic.KeyValuePair<Voronoi.Line, Voronoi.Line>;

namespace CityGenerator
{

    internal class RoadBuilder
    { 
        /// <summary>
        /// Build a road inside a district cell
        /// </summary>
        public List<Road> BuildRoad(RoadSettings roadSettings,DistrictCell cell)
        {
            //Edges are the bounds of the road and will be part of the road as well
            var edges = cell.Cell.Edges.ToList();

            //find the longest line in the cell as a start line
            var longest = FindLongestLineInCell(edges);

            //a cell edge can be shared so first remove the line from one of the shared cells
            //all edges of the cell are part of the road
            var roads = new List<Road>();
            foreach (var edge in cell.Cell.Edges)
            {
                //add the original side edge
                roads.Add(new Road(edge));
            }


            if (roadSettings.GenerateInnerRoads)
            {
                //Create all the parts of the road
                roads.AddRange(GenerateRoad(roads, new Road(longest.Key), new Road(longest.Value), roadSettings.Branches));
            }

            return roads;

        }
        
        /// <summary>
        /// Keep generating roads until branches are zero
        /// </summary>
        private List<Road> GenerateRoad(List<Road> roads , Road startLine ,Road endLine, int branches)
        {
            //find a new start and end point
            const double min = 0.33;
            const double max = 1 - min;
            var start = startLine.RoadLine.FindRandomPointOnLine(min, max);
            var end = endLine.RoadLine.Center();

            //When an intersection occurs break the line     
            HandlePossibleRoadIntersections(new Line(start,end),startLine,endLine, roads);

            //reduce branches to end recursion
            branches--;
            if (branches + 1 <= 0)
            {
                return roads;
            }

            //From the pool of lines choose 2 random ones that aren't the same
            var road1 = roads.GetRandomValue();
            var road2 = roads.GetRandomValue();
            //make sure both are unique
            while (road1 == road2)
            {
                road2 = roads.GetRandomValue();
            }

            //generate a new road by connecting 2 roads
            return GenerateRoad(roads, road1, road2, branches);
        }

        private SplitLine FindLongestLineInCell(List<Line> edges)
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
            //edges.Remove(e1);
            //edges.Remove(e2);

            return new SplitLine(e1, e2);
        }

        private void HandlePossibleRoadIntersections(Line line, Road startLine, Road endLine,List<Road> roads)
        {
            bool flipped = false;

            //check for possible intersection, 
            //if an intersection happens the end point becomes the point of intersection
            //has to be reverse because otherwise the lines will intersect with the edges
            for(var i = roads.Count-1; i> 0; i--)
            {
                var l = roads[i].RoadLine;

                //if the line intersects
                if (!line.IntersectsWith(l)) continue;

                //find the intersection point
                bool parallel = false;
                var ip = line.FindIntersectionPoint(l, ref parallel);

                //parrallel lines don't intersect so ignore
                if (parallel) continue;

                //Create the new line from the intersection
                line = CreateIntersectedLine(line,ip,ref flipped);

                //Split the line that is intersected with in 2 new lines
                endLine = roads[i];

                //stop at the first intersection
                break;
            }

            //Add the new road
            roads.Add(new Road(line));

            return;

            //remove the road that was intersected(endline) with
            roads.Remove(endLine);
            roads.Remove(startLine);



            //split start line
            var splitStart = SplitLine(startLine.RoadLine, line.Start);
            roads.Add(new Road(splitStart.Key));
            roads.Add(new Road(splitStart.Value));

            var splitEnd = SplitLine(endLine.RoadLine, line.End);
            roads.Add(new Road(splitEnd.Key));
            roads.Add(new Road(splitEnd.Value));


        }

        private Line CreateIntersectedLine(Line newLine,Point ip, ref bool flipped)
        {
            var start = newLine.Start;
            var end = newLine.End;

            //Check if the new line will not be too small
            //if it is too small switch the start point with the previous end point
            var totalDistance = MathHelpers.DistanceBetweenPoints(start, end);
            var newDistance = MathHelpers.DistanceBetweenPoints(start, ip);

            //Create the new line
            //swap occurs when the new distance is smaller than 1/3 of the original distance
            flipped = newDistance < (totalDistance*0.33);
            var line = flipped? new Line(ip,end) : new Line(start, ip);
            

            return line;
        }

        private SplitLine SplitLine(Line line, Point ip)
        {

            var start = line.Start;
            var end = line.End;

            return new SplitLine(new Line(start,ip),new Line(ip,end));



        }
        
    }
}
