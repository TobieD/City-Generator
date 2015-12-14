
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


        //helpers for building roads
        private readonly RoadBuilder _roadBuilder = new RoadBuilder();
        private RoadSettings _roadSettings;

        private bool bEnableDebugMode = true;

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

            //sort all cells by their district type
            //and generate the roads
            //and generate the building sites
            foreach (var dc in _districtCells)
            {
                //sort by type
                if (dc.DistrictType != district.DistrictType)
                {
                    continue;
                }
                
                //add the cell
                district.Cells.Add(dc);

                //generate roads inside the district cell
                dc.Roads = _roadBuilder.BuildRoad(_roadSettings, dc);

                //Because the width of the building needs to be know the building site generation needs to be done in unity
                continue;

                double minDistance = 2;
                
                //Generate build points for every road inside the district cell
                foreach (var road in dc.Roads)
                {
                    //0. Get the offset line from this road towards the center of the cell
                    Line offsetLine = road.GenerateOffsetParallelTowardsPoint(settings.Offset, dc.SitePoint);

                    //1. Calculate the total length of the line
                    double totalLength = offsetLine.Length();
                    double lengthTraveled = minDistance*2;

                    //keep repeating until the end is reached
                    while (lengthTraveled < totalLength)
                    {
                        //3. get point on line using normalized values [0,1]
                        var pc = lengthTraveled/totalLength;
                        var p = offsetLine.FindRandomPointOnLine(pc, pc);

                        //4.Create q building site from this point
                        var bs = BuildingSite.FromPoint(p);
                        road.Buildings.Add(bs);

                        //5. travel along the line using the width of the building site
                        lengthTraveled += (minDistance + bs.Width );
                    }
                }

            }

            return district;
        }

        private List<DistrictCell> GenerateDistrictCells(CitySettings settings, VoronoiDiagram voronoi)
        {
            _roadSettings = settings.RoadSettings;
            bEnableDebugMode = settings.DebugMode;

            //Create a district cell from the voronoi cells
            var districtCells = new List<DistrictCell>();
            foreach (var cell in voronoi.GetCellsInBounds())
            {
                //ignore cells that are not valid
                if (cell.Edges.Count < 2)
                {
                    continue;
                }

                districtCells.Add(DistrictCell.FromCell(cell,settings.DistrictSettings[0].Type));
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
                var cellCenter = cell.SitePoint;

                //Calculate distance between current cell and the start cell
                var distance = MathHelpers.DistanceBetweenPoints(startCell.SitePoint, cellCenter);

                //if it is within range add the cell
                if (distance < radius)
                {
                    cell.DistrictType = tag;
                }
            }


        }
    }
}
