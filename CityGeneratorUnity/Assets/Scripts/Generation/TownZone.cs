using System;
using System.Collections.Generic;
using CityGenerator;
using Helpers;
using UnityEngine;
using Voronoi;
using Random = UnityEngine.Random;

public class TownZone : MonoBehaviour
{
    private DistrictCell _cell;
    private GameObject _zoneObject;
    public string ZoneType;

    private List<GameObject> _buildings = new List<GameObject>();

    public bool bDrawBounds = false;

    void Start()
    {
        //Destroy(_zoneObject);

        bDrawBounds = true;
    }

    public void SetZoneData(DistrictCell cell)
    {
        _cell = cell;
        ZoneType = _cell.DistrictType;
        //Build();

        DrawBounds();
    }

    public void SetZoneType(string zoneType)
    {
        _cell.DistrictType = zoneType;
        ZoneType = zoneType;
        Build();
    }

    public void Build()
    {
        if (bDrawBounds)
        {
            DrawBounds();
        }
        else
        {
            DestroyImmediate(_zoneObject);
        }

        GenerateRoads();


        //GenerateBuildings();
    }

    private void DrawBounds()
    {
        if (_zoneObject != null)
        {
            return;
        }

        //Create mesh to visualize the bounds of the zone
        _zoneObject = new GameObject("ZoneVisual");

        DrawBounds();

        _zoneObject.transform.parent = transform;

        var x = transform.position.x;
        var y = transform.position.y;
        var z = transform.position.z + 75;
        _zoneObject.transform.position = new Vector3(x,y,z);


        //only add line renderer when the gameobject doesn't have it yet
        if (_zoneObject.GetComponent<LineRenderer>() != null)
        {
            return;
        }
        
        //Load the material and change color based on zone type
        var mat = Resources.Load <Material>("Material/ZoneBorder_mat");

        //Add line renderer
        var line = _zoneObject.AddComponent<LineRenderer>();
        line.enabled = true;
        line.SetVertexCount(_cell.Cell.Points.Count);
        line.material = mat;

        //create all the points of the line
        int i = 0;
        foreach (var point in _cell.Cell.Points)
        {
            var pos = point.ToVector3();
            //pos.z = z;
            line.SetPosition(i, pos);
            i++;
        }
    }

    private void GenerateRoads()
    {
        //Debug.Log("Generating Roads in the zone");

        int i = 0;
        foreach (var line in _cell.Road.Lines)
        {
            i++;

            GenerateRoad(line,string.Format("road_{0}",i));

        }

    }

    private void GenerateRoad(Line line, string name)
    {
        var start = line.Point1.ToVector3();
        var end = line.Point2.ToVector3();
        var length = Vector3.Distance(start, end);
        var width = 2.0f;

        if (length < 1)
        {
            return;
        }

        //create road game object
        var road = new GameObject(name);
        road.transform.position = start + new Vector3(0, 0.05f, 0);
        road.transform.parent = transform;
        road.transform.rotation = Quaternion.FromToRotation(Vector3.right, end - start);


        //Create mesh
        #region Mesh Creation
        Vector3[] vertices =
        {
            new Vector3(0, 0, -width/2),
            new Vector3(length, 0, -width/2),
            new Vector3(length, 0, width/2),
            new Vector3(0, 0, width/2),
        };

        int[] triangles =
        {
            1, 0, 2,
            2, 0, 3
        };

        Vector2[] uv =
        {
            new Vector2(0,0),
            new Vector2(length,0),
            new Vector2(length,1),
            new Vector2(0,1),
        };

        Vector3[] normals = {
               - Vector3.up,
               - Vector3.up,
               - Vector3.up,
               - Vector3.up
            };

        //create mesh
        Mesh mesh = new Mesh
        {
            
            vertices = vertices,
            triangles = triangles,
            normals = normals,
            uv = uv
        };

        mesh.name = "road";

        #endregion

        var filter = road.AddComponent<MeshFilter>();
        filter.mesh = mesh;

        var renderer = road.AddComponent<MeshRenderer>();
        var collider = road.AddComponent<MeshCollider>();

    }

    private void GenerateBuildings()
    {
        //load prefabs for the type
        var prefabs = GetPrefabsForType(ZoneType);

        if (prefabs.Count < 1)
        {
            Debug.LogWarningFormat("No prefab buildings set for {0}!\nPlease add building prefabs.",_cell.DistrictType);
            return;
        }

        //remove previous buildings
        foreach (var building in _buildings)
        {
            DestroyImmediate(building);
        }

        //Create new random building
        for (int i = 0; i < _cell.BuildSites.Count; i++)
        {
            GenerateBuilding(prefabs.GetRandomValue(), String.Format("Building_{0}", i + 1),_cell.BuildSites[i].ToVector3());
        }

    }

    private void GenerateBuilding(GameObject prefab, string name, Vector3 position)
    {
        var randomScale = Random.Range(0.7f, 1.2f);

        randomScale = 0.2f;
        var randomRot = Random.rotation;
        randomRot.x = -90.0f;
        randomRot.y = 0;

        var buildingObject = (GameObject)GameObject.Instantiate(prefab, position, prefab.transform.rotation);

        buildingObject.GetComponent<MeshRenderer>().material = Resources.Load < Material>("Material/Buildings_Default");
        buildingObject.transform.localScale = new Vector3(randomScale,randomScale,randomScale);

        buildingObject.transform.parent = transform;
        buildingObject.isStatic = false;
        buildingObject.name = name;

        _buildings.Add(buildingObject);
    }

    private List<GameObject> GetPrefabsForType(string type)
    {
        var map = TownGenerator.GetInstance().PrefabsPerZone;
        if(map.ContainsKey(type))
            return map[type];

        return null;
    } 
}
