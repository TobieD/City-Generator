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
    private DistrictSettings _settings;

    public bool bDrawBounds = false;

    public List<Line> DebugLines = new List<Line>();

    void Start()
    {
        bDrawBounds = true;
    }

    public void SetZoneData(DistrictCell cell,DistrictSettings settings)
    {
        _cell = cell;
        ZoneType = _cell.DistrictType;
        _settings = settings;

        DrawRoadPreview();
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
            DrawRoadPreview();
        else
        {
            DestroyImmediate(_zoneObject);
        }

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
            return;
        }
        

        //Generate the roads
        //Change roads to be build in the terrain using textures
        for (int i = 0; i < _cell.Roads.Count; ++i)
        {
            //Generate a road GameObject
            var road = _cell.Roads[i];
            var objectName = string.Format("road_{0}",i +1);
            
            //Generate the road
            var roadObject = GenerateRoad(road, objectName);
            if (roadObject == null)
            {
                continue;
            }

            //The Building generation per road needs to be done knowing information about the prefab
            int offset = _settings.Offset;

            offset = 5;
            int minDistance = 1;
            int index = 0;

            //0. Get the offset line from this road towards the center of the cell
            Line offsetLine = road.GenerateOffsetParallelTowardsPoint(offset, _cell.SitePoint);

            //1. Calculate the total length of the line
            double totalLength = offsetLine.Length();
            double lengthTraveled = minDistance;

            //keep repeating until the end is reached
            while (lengthTraveled < totalLength - minDistance) 
            {
                //3. get point on line using normalized values [0,1]
                var pc = lengthTraveled / totalLength;
                var p = offsetLine.FindRandomPointOnLine(pc, pc);

                //4.Create q building site from this point
                var bs = BuildingSite.FromPoint(p);

                //Place the building in the world
                var prefab = prefabs.GetRandomValue();
                objectName = string.Format("building_{0}", index + 1);
                bs.UserData = GenerateBuildingFromPrefab(prefab,objectName,p,road,roadObject);

                var prefabBounds = prefab.GetPrefabBounds();
                bs.Width = (int)prefabBounds.x + minDistance;
                bs.Height = (int)prefabBounds.z;

                //5. travel along the line using the width of the building site
                lengthTraveled += (minDistance + prefabBounds.x);

                _cell.Roads[i].Buildings.Add(bs);
            }
        }

        //spawn a building in the center of the cell
        
        //GenerateBuildingFromPrefab(prefabs.GetRandomValue(),"building_center",_cell.Center(),_cell.Roads[0],gameObject);
    }

    private void DrawRoadPreview()
    {
        if (_zoneObject != null)
        {
            return;
        }

        //Create mesh to visualize the bounds of the zone
        _zoneObject = new GameObject("ZoneVisual");

        _zoneObject.transform.parent = transform;

        var x = transform.position.x;
        var z = transform.position.z + 75;
        var y = transform.position.y;
        _zoneObject.transform.position = new Vector3(x,y,z);

        //only add line renderer when the gameobject doesn't have it yet
        if (_zoneObject.GetComponent<LineRenderer>() != null)
        {
            return;
        }
        
        //Load the material and change color based on zone type
        var mat = Resources.Load <Material>("Materials/ZoneBorder_mat");

        //Add line renderer
        var line = _zoneObject.AddComponent<LineRenderer>();
        line.enabled = true;
        line.SetVertexCount(_cell.Points.Count + 1);
        line.material = mat;

        List<Vector3> positions = new List<Vector3>();
        //create all the points of the line
        int i = 0;
        foreach (var point in _cell.Points)
        {
            var pos = point.ToVector3();
            pos.y = TownGenerator.GetInstance().Terrain.SampleHeight(new Vector3(pos.x, 0, pos.z)) + 10;
            positions.Add(pos);
             i++;
        }
        positions.Add(positions[0]);

        line.SetPositions(positions.ToArray());

    }

    private GameObject GenerateRoad(Road r, string roadName)
    {
        var start = r.Start.ToVector3();

        //Create parent road object
        var road = new GameObject(roadName);
        road.tag = "Road"; //tag it for demo use
        road.transform.position = start;
        road.transform.parent = transform;

        //Add for manual cleanup
        _prevSpawnedGameObjects.Add(road);

        return road;
     }

    /// <summary>
    /// Generate a building on a road using a prefab
    /// </summary>
    /// <param name="prefab">The Gameobject to instantiate</param>
    /// <param name="objectName">Name of the object</param>
    /// <param name="pos">Position the building will be spawned</param>
    /// <param name="parentRoad">the road the building is part of </param>
    private GameObject GenerateBuildingFromPrefab(GameObject prefab, string objectName, Point pos ,Road parentRoad, GameObject road)
    {
        //Make the building face the road, make a perpendicular line from the building pos to the road
        var lookAtPos = parentRoad.FindPerpendicularPointOnLine(pos);

        var t = TownGenerator.GetInstance().Terrain;
        //Instantiate the prefab at the location       

        var position = pos.ToVector3();
        position.y = t.SampleHeight(position);

        var lookat = lookAtPos.ToVector3();

        lookat.y = position.y;
        //lookAtPos.Y = position.y;


        var size = prefab.GetPrefabBounds();
        var colliderPos = position;
        colliderPos.y += size.y / 2;

        var collisions = Physics.OverlapBox(colliderPos, size);
        if (collisions.Length > 1) //ignore terrain
        {
            return null;
        }


        //Spawn the building
        var buildingObject = (GameObject)Instantiate(prefab, position, prefab.transform.rotation);

        //Look at the point
        buildingObject.transform.LookAt(lookat);

        //Flip on Y
        buildingObject.transform.RotateAround(pos.ToVector3(),transform.up,180);
        buildingObject.transform.parent = road.transform;
        buildingObject.isStatic = true; //Buildings should be static
        buildingObject.name = objectName;

        //Make sure the object can be removed on a new build
        _prevSpawnedGameObjects.Add(buildingObject);

        return buildingObject;
    }

    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        foreach (var debugLine in DebugLines)
        {
            var s = debugLine.Start.ToVector3();
            s.y = TownGenerator.GetInstance().Terrain.SampleHeight(s);

            var e = debugLine.End.ToVector3();
            e.y = TownGenerator.GetInstance().Terrain.SampleHeight(e);

            Gizmos.DrawLine(s,e);
        }

        if (_cell == null)
        {
            return;
        }

        //Draw Debug Sphere
        foreach (var roads in _cell.Roads)
        {
            foreach (var building in roads.Buildings)
            {

                var go = (GameObject)building.UserData;
                if (go != null)
                {
                    var pos = go.transform.position;
                    var size = go.GetComponent<Collider>().bounds.size;
                        
                    pos.y += size.y/2;

                    Gizmos.DrawCube(pos, size);
                }
            }
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
