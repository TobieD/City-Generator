using System.Collections.Generic;
using System.Net;
using Voronoi;

namespace CityGenerator
{
    /// <summary>
    /// Generated Data for a city
    /// </summary>
    public class CityData
    { 
        //districts
        public List<District> Districts;

        public Rectangle Bounds;

        public CityData()
        {
            Districts = new List<District>();
        }

        public void Clear()
        {
            Districts.Clear();
        }
    }

    /// <summary>
    /// Contains Bounds of the building, and the position
    /// </summary>
    public class BuildingSite : Point
    {

        public Road ParentRoad;
        public int Width;
        public int Height;

        public object UserData;

        public BuildingSite(double x, double y):base(x,y)
        {
            Width = 15;
            Height = 15;
            UserData = null;
            ParentRoad = null;
        }
        

        public static BuildingSite FromPoint(Point p)
        {
            var b = new BuildingSite(p.X, p.Y);
            return b;
        }
    }

    /// <summary>
    /// Contains generated building data and the start and end point of the road
    /// </summary>
    public class Road:Line
    {
        //spots possible buildings will be spawned
        public List<BuildingSite> Buildings;
        public DistrictCell ParentCell;

        public Road(Point start,Point end):base(start, end)
        {
            Buildings = new List<BuildingSite>();
        }

        public static Road FromLine(Line l)
        {
            var r = new Road(l.Start, l.End)
            {
                CellLeft = l.CellLeft,
                CellRight = l.CellRight
            };

            return r;
        }
    }

    /// <summary>
    /// Contains road information and district information of a cell
    /// </summary>
    public class DistrictCell :Cell
    {
        public string DistrictType;

        public List<Road> Roads;

        public DistrictCell(string type)
        {
            DistrictType = type;
            Roads = new List<Road>();
        }

        public static DistrictCell FromCell(Cell c, string type)
        {
            var dc = new DistrictCell(type)
            {
                Edges = c.Edges,
                SitePoint = c.SitePoint,
                Points = c.Points
            };

            return dc;
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    /// <summary>
    /// A collection of all cells of with the same type
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
