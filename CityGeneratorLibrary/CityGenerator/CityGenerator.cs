using System;
using System.Collections.Generic;
using Helpers;
using Voronoi;

namespace CityGenerator
{
    public class CitySettings
    {
        public List<DistrictSettings> DistrictSettings;

        public bool GenerateInnerRoads = false;
        public int RoadSubdivision = 1; //  best limitied to 2

        public bool DebugMode = false;
    }

    public class DistrictSettings
    {
        public string Type { get; set; }

        //generation settings

        //radius of the district
        public double Size { get; set; } = 0.5;

        //how many times a district of this type will be generated
        public int Frequency { get; set; } = 2;

        public int AmountOfBuildpoints { get; set; } = 8;

        public int Offset { get; set; } = 5;

        public double Percentage { get; set; }= 10;

        public DistrictSettings(string type)
        {
            Type = type;
        }
    }

    public static class CityBuilder
    {
        //builder helpers
        private static DistrictBuilder _districtBuilder;

        public static CityData GenerateCity(CitySettings settings, VoronoiDiagram voronoi)
        {
            if (voronoi.VoronoiCells.Count < 1)
                return null;
            
            //Create helpers if none are created.
            if (_districtBuilder == null)
            {
                _districtBuilder = new DistrictBuilder();
            }

            //Generate the city
            var cityData = new CityData();

            voronoi.RefreshVoronoi();

            //divide the city into districts
            cityData.Districts = _districtBuilder.CreateCityDistricts(settings,voronoi);

            cityData.Bounds = MathHelpers.GetCityBounds(cityData);

            return cityData;
        }
     }

}
