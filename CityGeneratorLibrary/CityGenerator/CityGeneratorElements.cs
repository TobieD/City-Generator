using System.Collections.Generic;
using System.Net;
using Voronoi;

namespace CityGenerator
{
    public class CityData
    { 
        //districts
        public List<District> Districts; 

        public CityData()
        {
            Districts = new List<District>();
        }

        public void Clear()
        {
            Districts.Clear();
        }
    }

    public class Road
    {
        //the line the road is made of
        public Line RoadLine;

        //spots possible buildings will be spawned
        public List<Point> BuildSites;

        public Road()
        {
            BuildSites = new List<Point>();
        }

        public Road(Line line)
        {
            RoadLine = line;
            BuildSites = new List<Point>();
        }
    }


   

    public class DistrictCell
    {
        public string DistrictType;

        public Cell Cell;


        public List<Road> Roads;
       

        public DistrictCell(string type, Cell cell)
        {
            DistrictType = type;
            Cell = cell;
            Roads = new List<Road>();
            
        }

        public override string ToString()
        {
            return Cell.ToString();
        }
    }

    /// <summary>
    /// A city can consist of multiple districts
    /// </summary>
    public class District
    {
        //type of the district
        public string DistrictType;

        //all Voronoi cells part of this district
        public List<DistrictCell> Cells;

        public District()
        {
            Cells = new List<DistrictCell>();
        }
    }


}
