
using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using Voronoi;
using Voronoi.Algorithms;

namespace CityGenerator
{
    /// <summary>
    /// Helper for generating districts
    /// </summary>
    internal class DistrictBuilder
    {
        private List<DistrictCell> _districtCells;
        private BowyerWatsonGenerator _triangulator = new BowyerWatsonGenerator();
        private RoadBuilder _roadBuilder = new RoadBuilder();

        private RoadSettings _roadSettings;

        private bool bEnableDebugMode = false;

        public List<District> CreateCityDistricts(CitySettings settings,VoronoiDiagram voronoi)
        {
            //create districts cells from the voronoi cells
            _districtCells = GenerateDistrictCells(settings, voronoi);
            
            //create districts from the  corresponding district cells
            return settings.DistrictSettings.Select(CreateDistrict).ToList();
        }

        /// <summary>
        /// find all district cells with the same type and make a district from them
        /// </summary>
        private District CreateDistrict(DistrictSettings settings)
        {
            var district = new District {DistrictType = settings.Type };
            var buildpoints = new List<Point>();

            //find all cells with the corresponding district type
            foreach (var dc in _districtCells)
            {
                if (dc.DistrictType != district.DistrictType)
                {
                    continue;
                }
                
                district.Cells.Add(dc);

                //generate roads inside the district cell
                dc.Roads = _roadBuilder.BuildRoad(_roadSettings, dc);

                if (dc.Roads.Count < 1)
                {
                    continue;
                }

                dc.Roads.SortBySmallestLength();

                //Generate build sites near the generated roads starting from the smallest line
                var percentage = settings.Percentage/100;

                //the first entry is always the shortest
                var shortest = dc.Roads[0].RoadLine;
                var p = shortest.FindRandomPointOnLine(percentage, percentage);

                //make sure the distance between building points is always equal to this
                var uniformDistanceBetweenPoints = MathHelpers.DistanceBetweenPoints(shortest.Start, p);

                //Generate build points for every road inside the district cell
                foreach (var road in dc.Roads)
                {
                    var length = road.RoadLine.Length();

                    var pc = uniformDistanceBetweenPoints / length;

                    //the points will be generated with a specified interval and a specified offset from the road
                    road.BuildSites.AddRange(road.RoadLine.GeneratePointsNearLineOfCell(dc.Cell,pc, settings.Offset));
                    buildpoints.AddRange(road.BuildSites);

                }
                
                //Only create one cell
                if(bEnableDebugMode)
                    break;
            }

            return district;
        }

        private List<DistrictCell> GenerateDistrictCells(CitySettings settings, VoronoiDiagram voronoi)
        {
            _roadSettings = settings.RoadSettings;
            bEnableDebugMode = settings.DebugMode;

            //Create a district cell from the voronoi cells
            var cells = voronoi.GetCellsInBounds();
            //var districtCells = cells.Select(cell => new DistrictCell(settings.DistrictSettings[0].Type, cell)).ToList();


            var districtCells = new List<DistrictCell>();

            foreach (var cell in cells)
            {

                if (cell.Edges.Count < 2)
                {
                    continue;
                }

                districtCells.Add(new DistrictCell(settings.DistrictSettings[0].Type,cell));
            }


            //tag random cells
            foreach (var setting in settings.DistrictSettings)
            {
                for (int i = 0; i < setting.Frequency; ++i)
                {
                    //Get a random start cell from the voronoi
                    var startCell = districtCells.GetRandomValue();

                    //size is a ratio of the width and length of the plane
                    var size = setting.Size * ((voronoi.Bounds.Right + voronoi.Bounds.Bottom)/8);

                    //tag cell
                    districtCells.TagCells(startCell, size, setting.Type);
                }
            }

            return districtCells;
        }
        
    }

    internal static class CellTagger
    {
        public static void TagCells(this List<DistrictCell> cells, DistrictCell startCell, double radius,string tag)
        {
            //go over all cells
            foreach (var cell in cells)
            {
                //get cell center
                var cellCenter = cell.Cell.SitePoint;

                //Calculate distance between current cell and the start cell
                var distance = MathHelpers.DistanceBetweenPoints(startCell.Cell.SitePoint, cellCenter);

                //if it is within range add the cell
                if (distance < radius)
                {
                    cell.DistrictType = tag;
                }
            }


        }
    }
}
