using System;
using System.Collections.Generic;
using CityGenerator;
using Points;
using UnityEngine;
using Voronoi;
using Object = UnityEngine.Object;

/// <summary>
/// Parts used to construct a network of roads
/// </summary>
public class RoadPrefabs
{
    /// <summary>
    /// Tilable straight part of a road
    /// </summary>
    public GameObject Straight;
}

/// <summary>
/// Handles Generating a voronoi Diagram and a town based on this diagram
/// </summary>
public class TownGenerator:Singleton<TownGenerator>
{
    //Settings
    public GenerationSettings GenerationSettings;
    public CitySettings CitySettings;
    private bool _bCanBuild = false;

    //Terrain Generator is used for roads
    private TerrainGenerator _terrainGenerator;

    //Library generated data
    private CityData _cityData;
    public VoronoiDiagram VoronoiDiagram { get; private set; }

    //Unity specific data for generation
    public GameObject Parent = null;
    public Dictionary<string, List<GameObject>> PrefabsPerZone;
    public RoadPrefabs RoadPrefabs;

    private List<GameObject> _zoneObjects = new List<GameObject>();

    public TownGenerator()
    {
        //Create the terrainGenerator
        _terrainGenerator = new TerrainGenerator();
    }

    /// Generates a voronoi diagram as a basic layout of the town
    public void Generate(GenerationSettings generationSettings,CitySettings citySettings,TerrainSettings terrainSettings)
    {
        //clean up previous generation and build
        Clear();

        //save settings
        GenerationSettings = generationSettings;
        CitySettings = citySettings;

        //create a parent object at 0,0,0 if none is set
        if (Parent == null)
        {
            Parent = new GameObject("Town");
            Parent.transform.position = Vector3.zero;
        }
        
        //Generate terrain
        _terrainGenerator.BuildTerrain(GenerationSettings, terrainSettings, Parent);

        //Generate the city
        GenerateCity();

        //Build a visualization of the city
        CreateDistrictGameObjects();

        //Debug.Log("CityGenerator: Generated Voronoi diagram! \nPress 'Build' to create a city.");

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

        _terrainGenerator.ApplyRoadData(_cityData);

        //Build houses
        foreach (var zoneObject in _zoneObjects) 
        {
            zoneObject.GetComponent <TownZone>().Build();
        }

        
    }

    public void Clear()
    {
        _bCanBuild = false;

        VoronoiDiagram = null;
        _cityData = null;

        _zoneObjects.Clear();

        //clear root game object, will clear all child objects as well.
        var prev = GameObject.Find("Town");
        Object.DestroyImmediate(prev);

    }

    #region Generation 

    private void GenerateCity()
    {
        //seed random
        if (!GenerationSettings.UseSeed)
        {
           GenerationSettings.Seed = DateTime.Now.GetHashCode();
        }
        //Store Settings
        var width = GenerationSettings.Width;
        var length = GenerationSettings.Length;
        
        //Make sure the parent is always in the center of the generation plane
        GenerationSettings.StartX = Parent.transform.position.x -width / 2.0;
        GenerationSettings.StartY = Parent.transform.position.z -length / 2.0;

        //Generate Points and diagram
        var points = PointGenerator.Generate(GenerationSettings);
        VoronoiDiagram = VoronoiGenerator.CreateVoronoi(points,GenerationSettings);

        //generate city districts and roads
        _cityData = CityBuilder.GenerateCity(CitySettings, VoronoiDiagram);
    }

    private void CreateDistrictGameObjects()
    {
        //locate the zone parent object
        var districtParent = ExtensionMethods.FindGameObject("Districts");
        districtParent.transform.parent = Parent.transform;

        //generate all districts that were specified
        foreach (var district in _cityData.Districts)
        {
            //create district Empty Gameobject
            var districtObject = new GameObject(district.DistrictType + " District");
            districtObject.transform.parent = districtParent.transform;

            //Generate all the cells of the district
            for (int i = 0; i < district.Cells.Count; i++)
            {
                var cell = district.Cells[i];
                var name = "Cell " + (i + 1);
                CreateDistrictCell(cell, name, districtObject);
            }
        }


    }

    private void CreateDistrictCell(DistrictCell cell, string name, GameObject parent)
    {
        //Create simple game object
        var newGameObj = new GameObject(name);
        newGameObj.transform.parent = parent.transform;
        newGameObj.transform.position = cell.Cell.SitePoint.ToVector3();

        //Add town Zone Component
        var townZone = newGameObj.AddComponent<TownZone>();

        townZone.SetZoneData(cell);

        _zoneObjects.Add(newGameObj);
    }

    #endregion
}
