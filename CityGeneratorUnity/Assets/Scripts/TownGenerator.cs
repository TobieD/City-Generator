﻿using System;
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
    private GenerationSettings _generationSettings;
    private CitySettings _citySettings;
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
    public void Generate(GenerationSettings generationSettings)
    {
        //clean up previous generation and build
        Clear();

        //save settings
        _generationSettings = generationSettings;

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
    public void Build(CitySettings citySettings)
    {
        if (!_bCanBuild)
        {
            Debug.LogWarning("CityGenerator: Unable to build city. \nPlease generate data first!");
            return;
        }
        
        //store settings
        _citySettings = citySettings;

        //generate city using library
        GenerateCityData();

        

        //CreateRoadGameObjects();
    }

    public void Clear()
    {
        _bCanBuild = false;

        VoronoiDiagram = null;
        _cityData = null;

        //clear root game object, will clear all child objects as well.
        var prev = GameObject.Find("Town");
        Object.DestroyImmediate(prev);

    }

    private void GenerateVoronoiDiagram()
    {
        //seed random
        if (!_generationSettings.UseSeed)
            _generationSettings.Seed = DateTime.Now.GetHashCode();

        //Store Settings
        var width = _generationSettings.Width;
        var length = _generationSettings.Length;
        
        //Make sure the parent is always in the center of the generation plane
        _generationSettings.StartX = Parent.transform.position.x - width / 2.0;
        _generationSettings.StartY = Parent.transform.position.z - length / 2.0;

        //Generate Points and diagram
        var points = PointGenerator.Generate(_generationSettings);

        Debug.LogFormat("Generated {0} points. expected {1}", points.Count, _generationSettings.Amount);

        VoronoiDiagram = VoronoiGenerator.CreateVoronoi(points,_generationSettings);
        
        //Add plane underneath the generated city
        var ren = Parent.AddComponent<MeshRenderer>();
        var mat = Resources.Load<Material>("Material/default");
        ren.material = mat;

        var filt = Parent.AddComponent<MeshFilter>();
        filt.mesh = TownBuilder.CreateGroundPlane((int)width, (int)length,Parent.transform.position.y);




    }

    private void GenerateCityData()
    {
        //generate city districts and roads
        _cityData = CityBuilder.GenerateCity(_citySettings,VoronoiDiagram);

        //Create game objects for each generated istrict
        CreateDistrictGameObjects();
    }

    private void CreateDistrictGameObjects()
    {
        //locate the zone parent object
        var districtParent = FindOrCreate("Districts");
        districtParent.transform.parent = Parent.transform;

        //generate all districts that were specified
        foreach (var district in _cityData.Districts)
        {
            
            //create district Empty Gameobject
            var districtObject = new GameObject(district.DistrictType + " District");
            districtObject.transform.parent = districtParent.transform;

            //Generate all the cells of the district
            for(int i = 0; i < district.Cells.Count; i++)
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

    }

    private void CreateRoadGameObjects()
    {
        //var roadParent = FindOrCreate("Roads");
        //roadParent.transform.parent = Parent.transform;

        //Debug only
        //for(int i = 0; i < _cityData.MainRoad.RoadLines.Count; i++)
        {
            //Line line = _cityData.MainRoad.RoadLines[i];
            //string name = "road_" + (i + 1);
           // CreateRoadGameObjectFromLine(line,name, roadParent);

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
