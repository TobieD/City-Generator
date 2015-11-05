
using System.Collections.Generic;
using Helpers;
using Voronoi;

namespace CityGenerator
{
    /// <summary>
    /// Helper for generating districts
    /// </summary>
    internal class DistrictBuilder
    {
        private List<DistrictCell> _districtCells; 

        public List<District> CreateCityDistricts(CitySettings settings,VoronoiDiagram voronoi)
        {
            var districts = new List<District>();

            //create districts cells from the voronoi cells
            _districtCells = GenerateDistrictCells(settings, voronoi);
            
            //create districts from the cells.
            foreach (var districtType in settings.DistrictSettings)
            {
                districts.Add(CreateDistrict(districtType));
            }

            return districts;
        }

        /// <summary>
        /// find all district cells with the same type and make a district from them
        /// </summary>
        private District CreateDistrict(DistrictSettings settings)
        {
            var district = new District {DistrictType = settings.Type };

            //find all cells with the corresponding district type

            foreach (var dc in _districtCells)
            {
                if (dc.DistrictType == district.DistrictType)
                {
                    district.Cells.Add(dc.Cell);
                }
            }

            return district;
        }

        private List<DistrictCell> GenerateDistrictCells(CitySettings settings, VoronoiDiagram voronoi)
        {
            var districtCells = new List<DistrictCell>();

            //convert all cells to the same type
            foreach (var cell in voronoi.VoronoiCells)
            {

                var cellCenter = MathHelpers.FindCenteroidOfCell(cell);

                //check if zone is in bounds
                if (cellCenter.X > voronoi.Bounds.Right || cellCenter.Y > voronoi.Bounds.Bottom
                    || cellCenter.X < voronoi.Bounds.Left || cellCenter.Y < voronoi.Bounds.Top)
                {
                    continue;
                }
                

                var dc = new DistrictCell
                {
                    DistrictType = settings.DistrictSettings[0].Type,
                    Cell = cell
                };
                districtCells.Add(dc);
            }

            //tag random cells
            foreach (var setting in settings.DistrictSettings)
            {
                for (int i = 0; i < setting.Frequency; ++i)
                {
                    //Get a random start cell from the voronoi
                    var startCell = districtCells.GetRandomValue();
                    var size = setting.Size * ((voronoi.Bounds.Right + voronoi.Bounds.Bottom)/8);



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
