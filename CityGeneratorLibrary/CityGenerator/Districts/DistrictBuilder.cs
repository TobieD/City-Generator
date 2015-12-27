
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
        private CitySettings _citySettings;

        //helpers for building roads
        private readonly RoadBuilder _roadBuilder = new RoadBuilder();

        public List<District> CreateCityDistricts(CitySettings settings,VoronoiDiagram voronoi)
        {
            _citySettings = settings;

            //create districts cells from the voronoi cells
            _districtCells = GenerateDistrictCells(voronoi);
            
            //create districts from the  corresponding district cells
            return settings.DistrictSettings.Select(CreateDistrict).ToList();
        }

        /// <summary>
        /// find all district cells with the same type and make a district from them
        /// </summary>
        private District CreateDistrict(DistrictSettings settings)
        {
            var district = new District {DistrictType = settings.Type };

            //sort all cells by their district type
            //and generate the roads
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
                dc.Roads = _roadBuilder.BuildRoad(dc, _citySettings.GenerateInnerRoads, _citySettings.RoadSubdivision);

                //Generate random positions inside the 


                if(_citySettings.DebugMode)
                    break;

            }

            return district;
        }

        private List<DistrictCell> GenerateDistrictCells(VoronoiDiagram voronoi)
        {
            //Create a district cell from the voronoi cells
            var districtCells = new List<DistrictCell>();
            foreach (var cell in voronoi.GetCellsInBounds())
            {
                //ignore cells that are not valid
                if (cell.Edges.Count < 2)
                {
                    continue;
                }

                districtCells.Add(DistrictCell.FromCell(cell,_citySettings.DistrictSettings[0].Type));
            }


            //tag random cells based on the settings for each district
            foreach (var setting in _citySettings.DistrictSettings)
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
