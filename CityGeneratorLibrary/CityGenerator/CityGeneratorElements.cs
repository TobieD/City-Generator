using System.Collections.Generic;
using Voronoi;

namespace CityGenerator
{
    public class CityData
    { 

        //Main Road
        public Road MainRoad;
        public List<Road> RoadBranches; 
        
        //Zones(cell + type of buildings to spawn)
        public List<Zone> Zones;

        public CityData()
        {
            MainRoad = new Road();
            Zones = new List<Zone>();
            RoadBranches = new List<Road>();
        }

        public void Clear()
        {
            MainRoad = new Road();
            RoadBranches.Clear();
            Zones.Clear();
        }
    }

    public class Road
    {
        public List<Line> RoadLines = new List<Line>(); 
        public Point StartPoint = Point.Zero;
        public Point EndPoint = Point.Zero;

        public Road()
        {
            RoadLines = new List<Line>();
            StartPoint = Point.Zero;
            EndPoint = Point.Zero;
        }
    }


    /// <summary>
    /// possible types of the zones in the city
    /// </summary>
    public enum ZoneType:int
    {
        Urban,
        Factory,
        Farm,
        Water
    }

    public class Zone
    {
        /// <summary>
        /// Position and bounds of the zone
        /// </summary>
        public Cell ZoneBounds;

        /// <summary>
        /// Indicates the type of the zone
        /// </summary>
        public ZoneType Type;
    }
}
