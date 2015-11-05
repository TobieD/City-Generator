using System;
using System.Collections.Generic;
using CityGenerator;
using Helpers;
using UnityEngine;
using Random = UnityEngine.Random;

public class TownZone : MonoBehaviour
{
    private Zone _zone;
    private GameObject _zoneObject;

	// Use this for initialization
	void Start ()
    {
	    
	}
	
	// Update is called once per frame
	void Update ()
    {
	
	}

    public void SetZoneData(Zone zone)
    {
        _zone = zone;
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
        line.SetVertexCount(_zone.ZoneBounds.Points.Count);
        line.material = mat;

        //create all the points of the line
        int i = 0;
        foreach (var point in _zone.ZoneBounds.Points)
        {
            line.SetPosition(i, point.ToVector3());
            i++;
        }
    }

    private void GenerateBuildings()
    {
        var prefabs = GetPrefabsForType(ZoneType.Factory.ToString());

        int index = 0;
        GenerateBuilding(prefabs.GetRandomValue(), String.Format("Building_{0}", index +1));

    }

    private void GenerateBuilding(GameObject prefab, string name)
    {

        //choose a random position inside the zone
        var pos = _zone.ZoneBounds.CellPoint.ToVector3(); //take center

        var randomScale = Random.Range(0.7f, 1.2f);
        var randomRot = Random.rotation;
        randomRot = prefab.transform.rotation;

        var buildingObject = (GameObject)GameObject.Instantiate(prefab,pos,randomRot);
        buildingObject.transform.localScale = new Vector3(randomScale,randomScale,randomScale);

        buildingObject.transform.parent = transform;
        buildingObject.isStatic = true;
        buildingObject.name = name;
    }

    private List<GameObject> GetPrefabsForType(string type)
    {
        var map = TownGenerator.GetInstance().PrefabsPerZone;
        return map[type];
    } 
}
