using System;
using System.Collections.Generic;
using Helpers;
using Voronoi;

namespace CityGenerator
{
    public class CitySettings
    {
        public List<DistrictSettings> DistrictSettings;

        public RoadSettings RoadSettings = new RoadSettings(75,1, 1,"Road");

        public bool DebugMode = true;
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

    public class RoadSettings
    {
        public int Amount { get; set; }

        //number of branches going from the original road   
        public int Branches { get; set; }

        //max amount of lines connected
        public int Max { get; set; }

        public string Type { get; private set; }

        public int Width { get; set; }
        public bool GenerateInnerRoads { get; set; }

        public RoadSettings(int max, int branches, int amount,string type, int width = 10)
        {
            Amount = amount;
            Max = max;
            Branches = branches;
            Type = type;
            Width = width;
            GenerateInnerRoads = false;
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
