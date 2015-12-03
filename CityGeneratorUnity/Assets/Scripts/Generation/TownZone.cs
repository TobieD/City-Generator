using System.Collections.Generic;
using CityGenerator;
using Helpers;
using UnityEngine;
using Voronoi;

public class TownZone : MonoBehaviour
{
    private DistrictCell _cell;
    private GameObject _zoneObject;
    public string ZoneType;

    private List<GameObject> _prevSpawnedGameObjects = new List<GameObject>();

    public bool bDrawBounds = false;

    public List<Line> DebugLines = new List<Line>();

    void Start()
    {
        bDrawBounds = true;
    }

    public void SetZoneData(DistrictCell cell)
    {
        _cell = cell;
        ZoneType = _cell.DistrictType;

        DrawBounds();
    }

    /// <summary>
    /// Update the District type of this cel
    /// </summary>
    public void SetZoneType(string zoneType)
    {
        _cell.DistrictType = zoneType;
        ZoneType = zoneType;
        Build();
    }

    public void Build()
    {
        //Draw Visualization of Voronoi Cell 
        if (bDrawBounds)
            DrawBounds();
        else
            DestroyImmediate(_zoneObject);

        //clear debug lines of previous build
        DebugLines.Clear();
        DebugLines = new List<Line>();

        //Remove previous spawned gameobjects
        foreach (var bgo in _prevSpawnedGameObjects)
        {
            DestroyImmediate(bgo);
        }
        
        //load prefabs for the type and make sure there are prefabs set
        var prefabs = GetPrefabsForType(ZoneType);
        if (prefabs.Count < 1)
        {
            Debug.LogWarningFormat("No prefab buildings set for {0}!\nPlease add building prefabs.", _cell.DistrictType);
            return;
        }

        //Generate the roads
        //Change roads to be build in the terrain using textures
        for (int i = 0; i < _cell.Roads.Count; ++i)
        {
            var road = _cell.Roads[i];
            var objectName = string.Format("road_{0}",i +1);
            
            //Generate the road
            var roadObject = GenerateRoad(road, objectName);
            if (roadObject == null)
            {
                continue;
            }

            //continue;
            //Generate the buildings on the road
            for (int x = 0; x < road.BuildSites.Count; ++x)
            {
                //Convert the 2D point to 3D
                var position = road.BuildSites[x];

                objectName = string.Format("building_{0}", x + 1);

                //Select a random prefab from the list
                var prefab = prefabs.GetRandomValue();

                //Generate a building using the prefab
                GenerateBuildingFromPrefab(prefab,objectName,position,road, roadObject);
            }

            //break;
        }
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

    private GameObject GenerateRoad(Road r, string roadName)
    {
        //start and end of the road
        
        var start = r.RoadLine.Start.ToVector3();
        var end = r.RoadLine.End.ToVector3();

        //Use prefab tiles of roads to make it look more pretty
        var prefab = TownGenerator.GetInstance().RoadPrefabs.Straight;

        //Calculate how many times the road prefab needs to be spawned
        var length = Vector3.Distance(start, end);
        var bounds = prefab.GetPrefabBounds();
        //int amountOfSpawns = Mathf.RoundToInt(length / bounds.x);


        //Create parent road object
        var road = new GameObject(roadName);
        road.tag = "Road"; //tag it for demo use
        road.transform.position = start;
        road.transform.parent = transform;
        
        //Add for manual cleanup
        _prevSpawnedGameObjects.Add(road);

        return road;

        //var roadObj = new GameObject("Road");

        //roadObj.transform.parent = road.transform;
        ////Keep instantiating the prefab until the end of the line is reached
        ////TODO: Special cases for where lines intersect
        //for (int i = 1; i < amountOfSpawns; i++)
        //{
        //    var pos = start;
        //    pos.x += (bounds.x*i);
        //    pos.z += bounds.z;

        //    var instance = (GameObject)Instantiate(prefab,pos,Quaternion.identity);

        //    instance.transform.parent = roadObj.transform;
        //}

        ////Rotate the parent road
        //road.transform.rotation = Quaternion.FromToRotation(Vector3.right, end - start);

       
    }

    /// <summary>
    /// Generate a building on a road using a prefab
    /// </summary>
    /// <param name="prefab">The Gameobject to instantiate</param>
    /// <param name="objectName">Name of the object</param>
    /// <param name="pos">Position the building will be spawned</param>
    /// <param name="parentRoad">the road the building is part of </param>
    private void GenerateBuildingFromPrefab(GameObject prefab, string objectName, Point pos ,Road parentRoad, GameObject road)
    {
        //Make the building face the road, make a perpendicular line from the building pos to the road
        var lookAtPos = parentRoad.RoadLine.FindPerpendicularPointOnLine(pos);

        //Instantiate the prefab at the location       
        var buildingObject = (GameObject)Instantiate(prefab, pos.ToVector3(), prefab.transform.rotation);

        //Debug Only
        DebugLines.Add(new Line(pos,lookAtPos));

        //Look at the point
        buildingObject.transform.LookAt(lookAtPos.ToVector3());

        //Flip on Y
        buildingObject.transform.RotateAround(pos.ToVector3(),transform.up,180);

        buildingObject.transform.parent = road.transform;
        buildingObject.isStatic = true; //Buildings should be static
        buildingObject.name = objectName;

        //Make sure the object can be removed on a new build
        _prevSpawnedGameObjects.Add(buildingObject);
    }

    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        foreach (var debugLine in DebugLines)
        {
            Gizmos.DrawLine(debugLine.Start.ToVector3(), debugLine.End.ToVector3());
        }
    }

    /// <summary>
    /// Access the user defined prefabs for this district type
    /// </summary>
    private List<GameObject> GetPrefabsForType(string type)
    {
        var map = TownGenerator.GetInstance().PrefabsPerZone;
        return map.ContainsKey(type) ? map[type] : null;
    } 
}
