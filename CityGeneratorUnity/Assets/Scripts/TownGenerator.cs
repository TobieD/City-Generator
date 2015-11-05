using System;
using System.Collections.Generic;
using CityGenerator;
using UnityEngine;
using Voronoi;
using Object = UnityEngine.Object;

/// <summary>
/// Generation settings adjustable by the window
/// </summary>
public class TownGenerationSettings
{
    public bool UseSeed = false;
    public int Seed = 0;

    

    public int Width = 2500;
    public int Height = 2500;
    public int Amount = 500;
    public VoronoiAlgorithm Algorithm = VoronoiAlgorithm.BoywerWatson;

}

/// <summary>
/// Handles Generating a voronoi Diagram and a town based on this diagram
/// </summary>
public class TownGenerator:Singleton<TownGenerator>
{
    //Properties
    private TownGenerationSettings _settings;
    private bool _bCanBuild = false;

    //Library generated data
    private CityData _cityData;
    public VoronoiDiagram VoronoiDiagram { get; private set; }

    //Unity specific data for generation
    public GameObject Parent = null;
    public Dictionary<string, List<GameObject>> PrefabsPerZone;

    public TownGenerator()
    {

    }

    /// Generates a voronoi diagram as a basic layout of the town
    public void Generate(TownGenerationSettings settings)
    {
        //clean up previous generation and build
        Clear();

        //save settings
        _settings = settings;

        //create a parent object at 0,0,0 if none is set
        if (Parent == null)
        {
            Parent = new GameObject("Town");
            Parent.transform.position = Vector3.zero;
        }

        //generate points + Voronoi
        GenerateVoronoiDiagram();

        Debug.Log("CityGenerator: Generated Voronoi diagram! \nPress 'Build' to create a city.");

        _bCanBuild = true;
    }

    /// Build the town based on the generated voronoi Diagram
    public void Build()
    {
        if (!_bCanBuild)
        {
            Debug.LogWarning("CityGenerator: Unable to build city. \nPlease generate data first!");
            return;
        }

        _cityData = CityBuilder.GenerateCity(VoronoiDiagram);

        //Create game objects for each generated zone
        CreateZoneGameObjects();

        //CreateRoadGameObjects();
    }

    public void Clear()
    {
        _bCanBuild = false;

        VoronoiDiagram = null;

        //clear root game object, will clear all child objects as well.
        var prev = GameObject.Find("Town");
        Object.DestroyImmediate(prev);

    }

    private void GenerateVoronoiDiagram()
    {
        //seed random
        if (!_settings.UseSeed)
            _settings.Seed = DateTime.Now.GetHashCode();

        //Store Settings
        int amount = _settings.Amount;
        int width = _settings.Width;
        int height = _settings.Height;
        VoronoiAlgorithm algorithm = _settings.Algorithm;
        int seed = _settings.Seed;
        
        //Make sure the parent is always in the center of the generation plane
        Point startPoint = Point.Zero;
        startPoint.X = Parent.transform.position.x - width / 2.0;
        startPoint.Y = Parent.transform.position.z - height / 2.0;

        //Generate Points and diagram
        var points = VoronoiGenerator.GenerateRandomPoints(amount, startPoint, width, height, seed);
        VoronoiDiagram = VoronoiGenerator.CreateVoronoi(points, algorithm);
        VoronoiDiagram.Sites = points;
        VoronoiDiagram.Bounds = new Point(width,height);


        //Add plane underneath the generated city
        var ren = Parent.AddComponent<MeshRenderer>();
        var mat = Resources.Load<Material>("Material/default");
        ren.material = mat;
        mat.mainTextureScale = new Vector2(width/100, height/100);

        var filt = Parent.AddComponent<MeshFilter>();
        filt.mesh = TownBuilder.CreateGroundPlane(width, height, 0);



    }

    private void CreateZoneGameObjects()
    {
        //locate the zone parent object
        var zoneParent = FindOrCreate("Zones");
        zoneParent.transform.parent = Parent.transform;

        //Create game objects for each point and add the TownZone script
        //TownZone handles generating meshes for each zone based on its zone type.
        for (var i = 0; i < VoronoiDiagram.VoronoiCells.Count; ++i)
        {
            //access the current cell
            Cell cell = VoronoiDiagram.VoronoiCells[i];
            string name = "Zone_" + (i + 1);

            CreateZoneGameObjectFromCell(cell, name, zoneParent);
        }
    }
    
    private void CreateZoneGameObjectFromCell(Cell cell, string name, GameObject parent)
    {
        //Create simple game object(sphere will be removed eventually)
        var newGameObj = new GameObject(name);
        newGameObj.transform.parent = parent.transform;
        newGameObj.transform.position = cell.CellPoint.ToVector3();

        //Add town Zone Component
        var townZone = newGameObj.AddComponent<TownZone>();
        var zone = new Zone { ZoneBounds = cell };
        townZone.SetZoneData(zone);

        //WARNING: Move this to library
        //zone.ZoneBounds.Points.FilterDoubleValues();
    }

    private void CreateRoadGameObjects()
    {
        var roadParent = FindOrCreate("Roads");
        roadParent.transform.parent = Parent.transform;

        //Debug only
        for(int i = 0; i < _cityData.MainRoad.RoadLines.Count; i++)
        {
            Line line = _cityData.MainRoad.RoadLines[i];
            string name = "road_" + (i + 1);
            CreateRoadGameObjectFromLine(line,name, roadParent);

        }
        

    }

    private void CreateRoadGameObjectFromLine(Line line, string name, GameObject parent)
    {
        //Create the object
        var roadObject = new GameObject(name);
        roadObject.transform.parent = parent.transform;

        //Add the road component
        var roadComp = roadObject.AddComponent<RoadComponent>();

        roadComp.SetRoadData(line);


    }

    /// <summary>
    /// Finds or create a game object with the given name
    /// </summary>
    private GameObject FindOrCreate(string objectName)
    { 
        //find the object
        var go = GameObject.Find(objectName);

        //when it is found destroy it, this will also destroy its child objects.
        if (go != null)
        {
            Object.DestroyImmediate(go);
        }

        //create road object as parent
        return new GameObject(objectName);
    }

    
}
