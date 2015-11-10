using System.Collections.Generic;
using Voronoi;

namespace CityGenerator
{
    public class CitySettings
    {
        public List<DistrictSettings> DistrictSettings;
    }

    public class DistrictSettings
    {
        public string Type { get; set; }

        //generation settings

        //radius of the district
        public double Size { get; set; } = 0.5;

        //how many times a district of this type will be generated
        public int Frequency { get; set; } = 2; 

        public DistrictSettings(string type)
        {
            Type = type;
        }
    }


    public static class CityBuilder
    {
        //builder helpers
        private static DistrictBuilder _districtBuilder;
        private static RoadBuilder _roadBuilder;

        public static bool UseRandomStartEndPoint = true;

        public static CityData GenerateCity(CitySettings settings, VoronoiDiagram voronoi)
        {
            //Create helpers if none are created.
            if (_districtBuilder == null)
            {
                _districtBuilder = new DistrictBuilder();
            }

            if (_roadBuilder == null)
            {
                _roadBuilder = new RoadBuilder();
            }

            //Generate the city
            var cityData = new CityData();

            //divide the city into districts
            cityData.Districts = _districtBuilder.CreateCityDistricts(settings,voronoi);

            //generate the roads
            cityData.Roads = _roadBuilder.BuildRoads(voronoi);


            return cityData;
        }
     }

}
