
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
                if (dc.DistrictType == district.DistrictType)
                {
                    //generate a road inside the district cell
                    dc.Road = _roadBuilder.BuildRoad(_roadSettings, dc);

                    dc.Road.Lines.SortBySmallestLength();

                    //Generate build sites near the generated roads starting from the smallest line
                    var percentage = settings.Percentage/100;

                    //the first entry is always the shortest
                    var shortest = dc.Road.Lines[0];
                    var p = shortest.FindRandomPointOnLine(percentage, percentage);

                    //make sure the distance between building points is always equal to this
                    var uniformDistanceBetweenPoints = MathHelpers.DistanceBetweenPoints(shortest.Start, p);

                    foreach (var l in dc.Road.Lines)
                    {
                        var length = l.Length();

                        var pc = uniformDistanceBetweenPoints / length;

                        dc.BuildSites.AddRange(l.GeneratePointsNearLine(pc, settings.Offset));

                    }

                    buildpoints.AddRange(dc.BuildSites);
                    district.Cells.Add(dc);
                }

                //Only create one cell
                if(bEnableDebugMode)
                    break;
            }

            //filter out build sites that are too close together

            return district;
        }

        private List<DistrictCell> GenerateDistrictCells(CitySettings settings, VoronoiDiagram voronoi)
        {
            _roadSettings = settings.RoadSettings;
            bEnableDebugMode = settings.DebugMode;

            //Create a district cell from the voronoi cells
            var cells = voronoi.GetCellsInBounds();
            var districtCells = cells.Select(cell => new DistrictCell(settings.DistrictSettings[0].Type, cell)).ToList();

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
