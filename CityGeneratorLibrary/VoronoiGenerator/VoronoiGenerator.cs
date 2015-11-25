using System;
using System.Collections.Generic;
using Points;
using Voronoi.Algorithms;

namespace Voronoi
{
    /// <summary>
    /// Different algorithms that can be used for generating a Voronoi Diagram
    /// </summary>
    public enum VoronoiAlgorithm
    {
        BoywerWatson, //first triangulate, then connect centeroids of connecting triangles
        Fortune, //Sweep line algorithm
    }

    //settings for generating the voronoi data
    public class GenerationSettings
    {
        // Seed info
        public bool UseSeed = false;
        public int Seed = 0;

        // Bounds
        public double StartX = 0;
        public double StartY = 0;
        public double Width = 2500;
        public double Length = 2500;
        
        // Amount of points to spawn
        public int Amount = 500;

        public bool UseCircle = false;
        public double CircleRadius = 25;


        // Algorithms to use
        public VoronoiAlgorithm VoronoiAlgorithm = VoronoiAlgorithm.BoywerWatson;
        public PointGenerationAlgorithm PointAlgorithm = PointGenerationAlgorithm.Simple;
    }

    public static class VoronoiGenerator
    {
        /// <summary>
        /// Create a Voronoi Diagram using a list of points and a specified algorithm to use
        /// </summary>
        public static VoronoiDiagram CreateVoronoi(List<Point> points, GenerationSettings settings)
        {
            VoronoiDiagram voronoi;

            var startX = settings.StartX;
            var startY = settings.StartX;
            var width = settings.Width;
            var length = settings.Length;


            //Select algorithm to use
            switch (settings.VoronoiAlgorithm)
            {
                // Voronoi according to Boywer-Watson Algorithm
                // http://paulbourke.net/papers/triangulate/
                case VoronoiAlgorithm.BoywerWatson:
                {
                    voronoi = new BowyerWatsonGenerator().GetVoronoi(points);
                        
                    break;;
                }

                // Voronoi according to Fortunes Algorithm
                // http://blog.ivank.net/fortunes-algorithm-and-implementation.html
                case VoronoiAlgorithm.Fortune:
                {
                    voronoi = new FortuneGenerator().GetVoronoi(points);
                        break;
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(settings.VoronoiAlgorithm), settings.VoronoiAlgorithm, null);
            }


            voronoi.Bounds = new Rectangle(startX, startY, width, length);
            voronoi.Sites = points;

            return voronoi;;
        }

    }
}