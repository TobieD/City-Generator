using System;
using System.Collections.Generic;
using CityGenerator;
using Points;
using UnityEngine;
using Voronoi;
using Object = UnityEngine.Object;

/// <summary>
/// Handles Generating a voronoi Diagram and a town based on this diagram
/// </summary>
public class TownGenerator:Singleton<TownGenerator>
{
    //Settings
    public GenerationSettings GenerationSettings;
    public CitySettings CitySettings;
    private bool _bCanBuild = false;

    //Library generated data
    private CityData _cityData;
    public VoronoiDiagram VoronoiDiagram { get; private set; }

    //Unity specific data for generation
    public GameObject Parent = null;
    public Dictionary<string, List<GameObject>> PrefabsPerZone;

    //City object that gets spawned
    private List<GameObject> _zoneObjects = new List<GameObject>();

    //Helper for generating terrain
    private TownBuilder _townBuilder;
    public TerrainTileGenerator TerrainTileGenerator;
    public TerrainData TerrainData
    {
        get { return TerrainTileGenerator.CityTerrain.terrainData; }
    }
    public Terrain Terrain
    {
        get { return TerrainTileGenerator.CityTerrain; }
    }

    public TownGenerator()
    {
        //Create the terrainGenerator
        TerrainTileGenerator = new TerrainTileGenerator();
        _townBuilder = new TownBuilder();;
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

        //Generate the city
        GenerateCity();

        //set the terrain settings and build the terrain
        TerrainTileGenerator.InitializeSettings(terrainSettings,GenerationSettings,Parent, CitySettings);
        TerrainTileGenerator.BuildTerrain();

        _bCanBuild = true;

        if (!_bCanBuild)
        {
            Debug.LogWarning("CityGenerator: Unable to build city. \nPlease generate data first!");
            return;
        }

        //Build the town
        _townBuilder.Initialize(TerrainTileGenerator.CityTerrain, _cityData, TerrainTileGenerator.Settings, Parent,PrefabsPerZone);
        _townBuilder.Build();
    }

    public void Clear()
    {
        _bCanBuild = false;

        //clear data
        VoronoiDiagram = null;  
        _cityData = null;

        _zoneObjects.Clear();

        //clear terrain and town
        TerrainTileGenerator.Clear();
        _townBuilder.Clear();

        //clear root game object, will clear all child objects as well.
        var prev = GameObject.Find("Town");
        Object.DestroyImmediate(prev);

    }

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

        //Parent.transform.position = citypos;
        GenerationSettings.StartX = Parent.transform.position.x -width / 2.0;
        GenerationSettings.StartY = Parent.transform.position.z -length / 2.0;

        //Generate Points and diagram
        var points = PointGenerator.Generate(GenerationSettings);
        VoronoiDiagram = VoronoiGenerator.CreateVoronoi(points,GenerationSettings);

        //generate city districts and roads
        _cityData = CityBuilder.GenerateCity(CitySettings, VoronoiDiagram);
    }
}
