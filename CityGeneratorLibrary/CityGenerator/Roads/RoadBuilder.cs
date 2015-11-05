
using System;
using System.Collections.Generic;
using Helpers;
using Voronoi;

namespace CityGenerator
{
    internal class RoadBuilder
    {

        private List<Point> _pointsInBounds;
        private VoronoiDiagram _voronoi;

        private List<Road> _roads; 

        public List<Road> BuildRoads(VoronoiDiagram voronoi)
        {
            _voronoi = voronoi;
            //create list of roads
            _roads = new List<Road>();

            //find all line points in bounds
            _pointsInBounds = FindPointsInBounds();

            //generate roads
            var nrOfRoads = 1;
            for (int i = 0; i < nrOfRoads; i++)
            {
                //generate a startpoint for the road
                var start = _pointsInBounds.GetRandomValue();

                GenerateRoad(start,3,30);
            }

            return _roads;
        }

        /// <summary>
        /// Voronoi diagram can have points that are infinte
        /// We only need the points that are spawned within the orignal specified bounds
        /// </summary>
        private List<Point> FindPointsInBounds()
        {
            //go over all the voronoi lines to get the points in bounds
            var points = new List<Point>();

            foreach (var edge in _voronoi.HalfEdges)
            {
                var bInBounds = MathHelpers.PointWithinBounds(edge.Point1,_voronoi.Bounds);

                if (!bInBounds) continue;

                if (!points.Contains(edge.Point1))
                {
                    points.Add(edge.Point1);
                }

                if (!points.Contains(edge.Point2))
                {
                    points.Add(edge.Point2);
                }
            }

            return points;
        }

        private void GenerateRoad(Point start, int branches,int max = 75)
        {
            //find an end point and make sure the end point differs from the start
            var end = start;
            while (end == start)
            {
                end = _pointsInBounds.GetRandomValue();
            }

            //road generation algorithm
            var lines = GenerateLinesFromStartToEnd(start, ref end,max);
          

            //generate additional branches from this road
            var branch = branches - 1;
            
            if (branches > 0)
            {
                //find all unique points on this road
                var points = lines.ToPoints();

                for (int i = 0; i < branches; i++)
                {
                    var st = points.GetRandomValue();
                    max = 25;
                    
                    GenerateRoad(st, branch, max);
                    
                }
            }

            //create the road
            var road = new Road()
            {
                Start = start,
                End = end,
                Lines = lines
            };

            _roads.Add(road);

        }

        private List<Line> GenerateLinesFromStartToEnd(Point start, ref Point end, int max)
        {
            //connect all lines from start to end
            List<Line> lines = new List<Line>();

            var current = start;
            while (current != end)
            {
                var radius = 35.0;
                var range = 35.0;
                var direction = (end - start).Normalize();

                direction.X *= range;
                direction.Y *= range;

                var lookAt = current + direction;

                //get all points in the direction of the end point
                var nearPoints = _pointsInBounds.FindPointsNearPoint(lookAt, radius);

                //find the closest point to the current point
                var closest = nearPoints.FindClosestPoint(current);

                while (closest == null || closest == current)
                {
                    radius += 5.0f;
                    //range += 5.0f;
                    //direction.X *= range;
                    //direction.Y *= range;

                    lookAt = current + direction;
                    nearPoints = _pointsInBounds.FindPointsNearPoint(lookAt, radius);
                    closest = nearPoints.FindClosestPoint(current);

                    if (closest == current)
                    {
                        Console.Write("Endless Loop prevented\n");
                    }
                }

                //create the line
                lines.Add(new Line(current, closest));
                current = closest;

                //BUG sometimes the road gets stuck in an endless loop and this is a bandaid fix for now.
                if (lines.Count > max)
                {
                    end = current;
                }
            }


            //filter double lines out.
            var count = lines.Count;
            var indexToRemove = new List<int>();
            for (int i = 1; i < count; i++)
            {
                var l1 = lines[i-1];
                var l2 = lines[i];
                
                if(l1.Point1 == l2.Point2 && l1.Point2 == l2.Point1)
                {
                    indexToRemove.Add(i);
                }
            }

            for (int i = indexToRemove.Count - 1; i >= 0; i--)
            {
                var index = indexToRemove[i]; 
                lines.RemoveAt(index);
            }

            return lines;
        }
        

    }
}
