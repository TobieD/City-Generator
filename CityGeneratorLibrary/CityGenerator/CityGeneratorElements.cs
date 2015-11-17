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
        //Voronoi lines the road is made up of
        public List<Line> Lines; 

        //Start point of the road
        public Point Start;
        
        //endpoint of the road
        public Point End;

        public Road()
        {
            Lines = new List<Line>();
            Start = Point.Zero;
            End = Point.Zero;
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

    public class DistrictCell
    {
        public string DistrictType;

        public Cell Cell;

        //spots possible buildings will be spawned
        public List<Point> BuildSites;

        //The road that goes through the zone
        public Road Road;


        public DistrictCell(string type, Cell cell)
        {
            DistrictType = type;
            Cell = cell;
            BuildSites = new List<Point>();
        }
    }


}
