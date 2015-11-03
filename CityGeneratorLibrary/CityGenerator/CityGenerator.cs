using System.Collections.Generic;
using System.Linq;
using Helpers;
using Voronoi;

namespace CityGenerator
{
    public static class CityBuilder
    {
        public static bool UseRandomStartEndPoint = true;

        public static CityData GenerateCity(VoronoiDiagram voronoi)
        {
            var cityData = new CityData();

            //Divide the voronoi cells into districts
            cityData.Zones = CreateCityZones(voronoi);

            //Create a long road from point A to point B
            cityData.MainRoad = GenerateMainRoad(voronoi);

            //Create smaller roads branching off from the main road
            //cityData.RoadBranches = GenerateBranchRoads(cityData.MainRoad,voronoi,10);

            
            return cityData;
        }

        #region Zone Generation

        private static List<Zone> CreateCityZones(VoronoiDiagram voronoi)
        {
            var zoneRange = 500.0;

            //make zones of all cells
            var zones = voronoi.VoronoiCells.Select(cell => new Zone
            {
                Type = ZoneType.Farm, ZoneBounds = cell
            }).ToList();


            int type = 0;
            //int iterations = 20;
            while (zoneRange > 0.0)
            {
                zones.TagZones(zoneRange, type);

                zoneRange -= 25.0;

                type++;
                if (type >= 4)
                {
                    type = 0;
                }
            }


            return zones;
        }

        private static void TagZones(this List<Zone> zones, double radius, int type)
        {
            //pick a random cell as the start zone 
            var startZone = zones.GetRandomValue();
            startZone.Type = (ZoneType)type;

            //make all zones within a distance the same cell
            foreach (var zone in zones)
            {
                var currZonePoint = zone.ZoneBounds.CellPoint;
                var distance = MathHelpers.DistanceBetweenPoints(startZone.ZoneBounds.CellPoint, currZonePoint);

                if (distance < radius)
                {
                    zone.Type = startZone.Type;
                }
            }
        }

        #endregion

        #region Road Generation
        public static Road GenerateMainRoad(VoronoiDiagram voronoi)
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

            //take any points from the voronoi diagram
            var startEndPoints = UseRandomStartEndPoint ? GetRandomStartAndEndPoints(points) : GetMinMaxPoints(points);
            var startPoint = startEndPoints.Key;
            var endPoint = startEndPoints.Value;
            
            var mainRoad = GenerateRoad(points, startPoint, endPoint);

            return mainRoad;
        }

        private static Road GenerateRoad(List<Point> points, Point startPoint, Point endPoint)
        {
            //algorithm to find path from start to end
            var currentPoint = startPoint;
            //the lines making the road
            List<Line> roads = new List<Line>();
            while (currentPoint != endPoint || currentPoint == null)
            {
                
                var radius = 35.0f;
                var direction = (endPoint - currentPoint).Normalize();
                direction.X *= 35;
                direction.Y *= 35;

                var lookDirection = currentPoint + direction;

                //get all points in the direction of the end point
                var pointsInDistance = points.FindPointsNearPoint(lookDirection, radius);
                var newPoint = pointsInDistance.FindClosestPoint(currentPoint);

                while (newPoint == null)
                {
                    radius += 5.0f;
                    pointsInDistance = points.FindPointsNearPoint(lookDirection, radius);
                    newPoint = pointsInDistance.FindClosestPoint(currentPoint);
                }

                //create a new connection
                roads.Add(new Line(currentPoint, newPoint));
                currentPoint = newPoint;

                if (roads.Count > 75)
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

        private static List<Road> GenerateBranchRoads(Road mainRoad,VoronoiDiagram voronoi, int branches)
        {
            var roads = new List<Road>();

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

            var mainRoadPoints = mainRoad.RoadLines.ToPoints();

            for (int i = 0; i < branches; i++)
            {
                var start = mainRoadPoints.GetRandomValue();

                var end = points.GetRandomValue();

                while (mainRoadPoints.Contains(end))
                {
                    end = points.GetRandomValue();
                }
                roads.Add(GenerateRoad(mainRoadPoints,start,end));
            }

            return roads;
        }

        #endregion

        #region Helpers

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

        private static KeyValuePair<Point, Point> GetRandomStartAndEndPoints(List<Point> validPoints)
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


            return new KeyValuePair<Point, Point>(startPoint, endPoint);
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

            return new KeyValuePair<Point, Point>(minPoint, maxPoint);
        }

        private static List<Point> ToPoints(this List<Line> lines)
        {
            var points = new List<Point>();

            foreach (var line in lines)
            {
                if (!points.Contains(line.Point1))
                {
                    points.Add(line.Point1);
                }

                if (!points.Contains(line.Point2))
                {
                    points.Add(line.Point2);
                }

            }

            return points;
        }

        #endregion


    }

}
