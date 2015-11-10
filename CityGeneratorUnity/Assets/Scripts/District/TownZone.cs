using System;
using System.Collections.Generic;
using CityGenerator;
using Helpers;
using UnityEngine;
using Random = UnityEngine.Random;

public class TownZone : MonoBehaviour
{
    private DistrictCell _cell;
    private GameObject _zoneObject;

	// Use this for initialization
	void Start ()
    {
	    
	}
	
	// Update is called once per frame
	void Update ()
    {
	
	}

    public void SetZoneData(DistrictCell cell)
    {
        _cell = cell;
        Build();
    }

    private void Build()
    {
        DrawBounds();

        GenerateBuildings();
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
        _zoneObject.transform.position = transform.position;

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
            line.SetPosition(i, point.ToVector3());
            i++;
        }
    }

    private void GenerateBuildings()
    {
        var prefabs = GetPrefabsForType(_cell.DistrictType);

        if (prefabs.Count < 1)
        {
            Debug.LogWarningFormat("No prefab buildings set for {0}!\nPlease add building prefabs.",_cell.DistrictType);
            return;
        }

        for (int i = 0; i < _cell.BuildSites.Count; i++)
        {
            GenerateBuilding(prefabs.GetRandomValue(), String.Format("Building_{0}", i + 1),_cell.BuildSites[i].ToVector3());
        }

    }

    private void GenerateBuilding(GameObject prefab, string name, Vector3 position)
    {
        var randomScale = Random.Range(0.7f, 1.2f);
        var randomRot = Random.rotation;
        randomRot.x = -90.0f;
        randomRot.y = 0;

        var buildingObject = (GameObject)GameObject.Instantiate(prefab, position, prefab.transform.rotation);
        buildingObject.transform.localScale = new Vector3(randomScale,randomScale,randomScale);

        buildingObject.transform.parent = transform;
        buildingObject.isStatic = true;
        buildingObject.name = name;
    }

    private List<GameObject> GetPrefabsForType(string type)
    {
        var map = TownGenerator.GetInstance().PrefabsPerZone;
        if(map.ContainsKey(type))
            return map[type];

        return null;
    } 
}
