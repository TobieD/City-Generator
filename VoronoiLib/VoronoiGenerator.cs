using System;
using System.Collections.Generic;
using Voronoi.Algorithms.BowyerWatson;
using Voronoi.Algorithms.Fortune;
using FortuneGenerator = Voronoi.Algorithms.Fortune.FortuneGenerator;

namespace Voronoi
{
    /// <summary>
    /// Different algorithms that can be used for generating a Voronoi Diagram
    /// </summary>
    public enum VoronoiAlgorithm
    {
        BoywerWatson, //first triangulate, then connect centeroids of connecting triangles
        Fortune, //Sweep line algorithm
        Lloyd,//keeps iterating the voronoi diagram untill all cells are equaly divided
    }

    public static class VoronoiGenerator
    {
        private static int _height;
        private static int _width;

        /// <summary>
        /// Generate a given amount of points in a user defined rectangle
        /// </summary>
        public static List<Point> GenerateRandomPoints(int amount, Point startPoint, int width, int height,int seed)
        {
            //Create point list
            var points = new List<Point>();
            _height = height;
            _width = width;

            // Seed random
            var rnd = new Random(seed);

            //Generate points and add them to the collection
            for (var i = 0; i < amount; ++i)
            {
                var x = rnd.Next((int)startPoint.X, (int)startPoint.X + width);
                var y = rnd.Next((int)startPoint.Y, (int)startPoint.Y + height);

                var point = new Point(x, y);
                points.Add(point);
            }

            return points;
        }

        /// <summary>
        /// Create a Voronoi Diagram using a list of points and a specified algorithm to use
        /// </summary>
        public static VoronoiDiagram CreateVoronoi(List<Point> points, VoronoiAlgorithm algorithm)
        {
            //Select algorthm to use
            switch (algorithm)
            {
                case VoronoiAlgorithm.BoywerWatson:
                    return Voronoi_BoywerWatson(points);
                case VoronoiAlgorithm.Fortune:
                    return Voronoi_Fortune(points);

                case VoronoiAlgorithm.Lloyd:
                    return Voronoi_Lloyd(points);

                default:
                    throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null);
            }
        }

        /// <summary>
        /// Voronoi according to Boywer-Watson Algorithm
        /// http://paulbourke.net/papers/triangulate/
        /// </summary>
        private static VoronoiDiagram Voronoi_BoywerWatson(List<Point> points)
        {
            return new BowyerWatsonGenerator().GetVoronoi(points);
        }

        /// <summary>
        /// Voronoi according to Fortunes Algorithm
        /// http://blog.ivank.net/fortunes-algorithm-and-implementation.html
        /// </summary>
        private static VoronoiDiagram Voronoi_Fortune(List<Point> points)
        {
            return new FortuneGenerator().GetVoronoi(points);
        }

        /// <summary>
        /// Voronoi according to Lloyd Algorithm
        /// </summary>
        private static VoronoiDiagram Voronoi_Lloyd(List<Point> points)
        {
            //return the list of triangles
            return null;
        }
    }
}