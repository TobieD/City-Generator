using System.Collections.Generic;
using System.Linq;
using Extensions;
using Voronoi;
using Voronoi.Helpers;

namespace CityGen
{
    public static class CityGenerator
    {

        public static bool UseRandomStartEndPoint = true;

        public static CityData GenerateCity(VoronoiDiagram voronoi)
        {
            var cityData = new CityData();

            cityData.MainRoad = GenerateMainRoad(voronoi);


            return cityData;
        }

        private static Road GenerateMainRoad(VoronoiDiagram voronoi)
        {
            //get all lines within bounds
            var lines = (from line in voronoi.HalfEdges let p1 = line.Point1 where MathHelpers.PointWithinBounds(p1, voronoi.Bounds) select line).ToList();
            var points = new List<Point>();
            foreach (var line in lines)
            {
                points.Add(line.Point1);
                points.Add(line.Point2);
            }
            //remove all double points
            points.FilterDoubleValues();

            var startEndPoints = UseRandomStartEndPoint ? GetRandomStartAndEndPoints(points) : GetMinMaxPoints(points);
            var startPoint = startEndPoints.Key;
            var endPoint = startEndPoints.Value;

            var roads = new List<Line>();

            //algorithm to find path from start to end
            var currentPoint = startPoint;
            while (currentPoint != endPoint)
            {
                var radius = 35.0f;
                var direction = (endPoint - currentPoint).Normalize();
                direction.X *= 35;
                direction.Y *= 35;

                var lookDirection = currentPoint + direction;

                //get all points in the direction of the end point
                var pointsInDistance = points.FindPointsNearPoint(lookDirection, radius);
                var newPoint = pointsInDistance.FindClosestPoint(currentPoint);

                while(newPoint == null)
                {
                    radius += 5.0f;
                    pointsInDistance = points.FindPointsNearPoint(lookDirection, radius);
                    newPoint = pointsInDistance.FindClosestPoint(currentPoint);
                }

                //create a new connection
                roads.Add(new Line(currentPoint, newPoint));
                currentPoint = newPoint;

                if (roads.Count > 50)
                  currentPoint = endPoint;
            }

            //add final line
            roads.Add(new Line(currentPoint, endPoint));

            //create the road
            var road = new Road
            {
                StartPoint = startPoint,
                EndPoint = endPoint,
                RoadLines = roads
            };

            return road;
        }

        private static Line FindLineThatShareAPoint(this List<Line> lines, Line l)
        {
            var shared = new List<Line>();

            if (!lines.Contains(l))
            {
                return null;
            }


            lines.Remove(l);

            foreach (var line in lines)
            {
                if (line.Point1 == l.Point2 || line.Point2 == l.Point2)
                {
                    shared.Add(line);
                }
            }


            return shared[0];
        }

        private static List<Point> FindPointsNearPoint(this List<Point> points, Point startPoint, double radius = 35.0)
        {
            var pointsInRadius = new List<Point>();

            foreach (var point in points)
            {
                var distance = MathHelpers.DistanceBetweenPoints(startPoint, point);
                if (distance < radius)
                    pointsInRadius.Add(point);
            }


            return pointsInRadius;
        }

        private static Point FindClosestPoint(this List<Point> points, Point start)
        {
            Point closest = null;

            var minDistance = double.MaxValue;

            foreach (var point in points)
            {
                var distance = MathHelpers.DistanceBetweenPoints(start, point);
                if (distance < minDistance && point != start)
                {
                    closest = point;
                    minDistance = distance;
                }

            }


            return closest;

        }

        private static KeyValuePair<Point, Point> GetRandomStartAndEndPoints(List<Point> validPoints )
        {
            //find a random start and end point
            var startPoint = validPoints.GetRandomValue();
            var endPoint = validPoints.GetRandomValue();
            while (startPoint == endPoint)
                endPoint = validPoints.GetRandomValue();

            //make sure startpoint.x is always < endpoint.x( direction is -->)
            if (startPoint > endPoint)
            {
                var t = startPoint;
                startPoint = endPoint;
                endPoint = t;
            }


            return new KeyValuePair<Point, Point>(startPoint,endPoint);
        }

        private static KeyValuePair<Point, Point> GetMinMaxPoints(List<Point> points)
        {
            var maxPoint = Point.Zero;
            var minPoint = Point.Max;

            foreach (var p in points)
            {
                if (p.X < minPoint.X && p.Y < minPoint.Y)
                {
                    minPoint = p;
                }

                if (p.X > maxPoint.X && p.Y > maxPoint.Y)
                {
                    maxPoint = p;
                }
            }

            return new KeyValuePair<Point, Point>(minPoint,maxPoint);
        }


    }

}
