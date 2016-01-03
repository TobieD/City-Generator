using System.Collections.Generic;
using CityGenerator;
using Helpers;
using UnityEngine;
using Voronoi;

/// <summary>
/// Uses the city data to create a city on a terrain
/// </summary>
public class TownBuilder
{
    //Settings
    private TerrainSettings _terrainSettings;
    private Dictionary<string, List<GameObject>> _buildingsPrefabsPerZone;

    //data
    private Terrain _terrain;
    private CityData _cityData;

    //terrain map data
    private float[,,] _alphamapData;
    private List<int[,]> _detailLayers;

    //internal data for optimization
    private List<DistrictCell> _cells = null;
    private List<Road> _roads = null;
    private List<BuildingSite> _buildings = null;
    private List<GameObject> _spawnedProps;

    //parent gameObjects
    private GameObject _townParent;
    private GameObject _propParent;
    private GameObject _buildingParent;

    private bool _isInitialized = false;

    /// <summary>
    /// Initialize with required data
    /// </summary>
    public void Initialize(Terrain terrain, CityData cityData, TerrainSettings terrainSettings, GameObject townParent, Dictionary<string, List<GameObject>> prefabs)
    {
        //store data
        _terrain = terrain;
        _cityData = cityData;
        _terrainSettings = terrainSettings;
        _buildingsPrefabsPerZone = prefabs;

        _townParent = townParent;

        _isInitialized = true;
    }
    
    /// <summary>
    /// Build a town on the terrain using the data provided
    /// </summary>
    public void Build()
    {
        //make sure all the data is set
        if (!_isInitialized)
        {
            return;
        }
        
        //split the city data to reduce loops 
        SplitCityData();

        //generate buildings on roads
        foreach (var road in _roads)
        {
           GenerateBuildingsOnRoad(road);
        }

        //Store alpha and detail maps of the terrain
        GetAlphaMap();
        GetDetailMaps();

        //Generate additional props or trees inside the cells
        foreach (var cell in _cells)
        {
            GenerateObjectsInCell(cell);
        }
        
        //draw the roads on the terrain and remove details on the road
        foreach (var road in _roads)
        {
            DrawRoad(road);
        }

        //Draw buildings on the terrain and remove detail inside the house
        foreach (var building in _buildings)
        {
           RemoveDetailsAroundGameObject((GameObject)building.UserData);
        }

        //update the terrain with the adjusted alpha and detail maps
        SetAlphaMap();
        SetDetailMaps();

        //make sure the changes are made
        _terrain.Flush();
    }

    public void Clear()
    {
        _isInitialized = false;
    }

    /// <summary>
    /// Generate Buildings on the edges of the road
    /// </summary>
    private void GenerateBuildingsOnRoad(Road road)
    {
        //create building parent if none exists
        if (_buildingParent == null)
        {
            _buildingParent = new GameObject("Buildings");
            _buildingParent.SetParent(_townParent);
        }

        var cell = road.ParentCell;
        int offset = _terrainSettings.RoadWidth;
        const float minDistance = 0.2f;

        //access building prefabs for this cell
        var prefabs = GetPrefabsForType(cell.DistrictType);

        if (prefabs == null)
        {
            return;
        }

        //Create an offset line of this road towards the inside of the cell
        var offsetLine = road.GenerateOffsetParallelTowardsPoint(offset, cell.SitePoint);

        //calculate total length of the line
        var length = offsetLine.Length();
        var traveled = minDistance;
        
        //keep repeating until the end is reached
        while (traveled < length - minDistance)
        {
            //get point on line using normalized values [0,1]
            var pc = traveled / length;
            var pos = offsetLine.FindRandomPointOnLine(pc, pc);

            //Select a random prefab
            var prefab = prefabs.GetRandomValue();

            //Create a building site from this point
            var bs = BuildingSite.FromPoint(pos);
            bs.ParentRoad = road;

            //Spawn the building
            SpawnBuilding(pos.ToVector3(),prefab,bs);
            
            //travel along the line using the width of the building site
            traveled += (minDistance + bs.Width / 2);
        }

    }

    /// <summary>
    /// Generate some additional gameobjects to the inside of a cell
    /// </summary>
    private void GenerateObjectsInCell(DistrictCell cell)
    {
        //using an inset create a smaller cell from the cell 
        //this is to avoid spawning objects on buildings or roads
        var insetCell = cell.Inset(15);

        var amount = 40;

        //Generate the spawn points inside the cell
        var points = insetCell.GenerateRandomPoints(Random.Range(amount - 5, amount + 5));

        foreach (var p in points)
        {
            //create a 3D vector from the 2D point
            var position = p.ToVector3();

            //randomize what will spawn
            var r = Random.value;

            //spawn a tree
            if (r < 0.5f)
            {
                SpawnTree(position);
            }
            else //Spawn a prop
            {
                SpawnProp(position);
            }
        }

    }
    
    /// <summary>
    /// Spawn a building on the position
    /// </summary>
    private void SpawnBuilding(Vector3 position, GameObject prefab, BuildingSite building)
    {
        var road = building.ParentRoad;

        //Make the building face the road, make a perpendicular line from the building pos to the road
        var lookAtPos = road.FindPerpendicularPointOnLine(new Point(position.x, position.z)).ToVector3();

        //get correct spawn height from the terrain
        position.y = _terrain.SampleHeight(position);
        lookAtPos.y = position.y;

        //Get bounds of the prefab and store them
        var size = prefab.GetPrefabBounds();
        building.Width = (int)size.x;
        building.Height = (int)size.z;

        //don't spawn when there is a collision
        var colliderPos = position;
        colliderPos.y += size.y/2;
        if (isOverlapping(colliderPos,size))
        {
            return;
        }

        //instantiate the building
        var go = (GameObject) Object.Instantiate(prefab, position, prefab.transform.rotation);

        //look at the road
        go.transform.LookAt(lookAtPos);

        //flip on Y
        go.transform.RotateAround(position,go.transform.up,180);
        go.isStatic = true;
        go.SetParent(_buildingParent);

        building.UserData = go;

        //Remove grass and other details around the object
        //RemoveDetailsAroundGameObject((GameObject)go);

        //Add to the buildings list
        _buildings.Add(building);
    }

    /// <summary>
    /// Spawn a random prop on the terrain
    /// </summary>
    private void SpawnProp(Vector3 position)
    {
        //create a parent to keep the hiearchy clean
        if (_propParent == null)
        {
            _propParent = new GameObject("Props");
            _propParent.SetParent(_townParent);
        }

        //select a random prop
        var prefab = _terrainSettings.Props.GetRandomValue();

        //Get the height of the terrain at the spawn position
        position.y = _terrain.SampleHeight(position);

        var colliderPos = position;
        var size = prefab.GetPrefabBounds();
        colliderPos.y += size.y/2;
        if (isOverlapping(colliderPos, size))
        {
            return;
        }

        //instantiate the prefab
        var prop = (GameObject)GameObject.Instantiate(prefab, position, Quaternion.identity);
        prop.SetParent(_propParent);

        //remove detail around the prop
        RemoveDetailsAroundGameObject(prop);

        //Add the prop for manual deleting
        _spawnedProps.Add(prop);
    }

    /// <summary>
    /// Spawn a random tree on the terrain
    /// </summary>
    private void SpawnTree(Vector3 position)
    {
        //get the correct position to spawn the tree on the terrain
        var treePos = _terrain.WorldToTerrainCoordinates(position);

        //TODO: Extra checks to see the tree doesn't spawn on a house ,prop or road

        var tree = new TreeInstance()
        {
            color = Color.white,
            heightScale = 1,
            widthScale = 1,
            lightmapColor = Color.white,
            position = treePos,
            prototypeIndex = Random.Range(0,_terrain.terrainData.treePrototypes.Length)
        };

        _terrain.AddTreeInstance(tree);
    }

    /// <summary>
    /// traverse a road line and draw the road texture on the terrain
    /// </summary>
    private void DrawRoad(Road road)
    {
        //road texture is always the 1st splatmap of the terrain
        const int roadTextureIndex = 0;
        const float inc = 0.05f;

        var roadWidth = _terrainSettings.RoadWidth;

        //Get start and end position of the road
        var start = road.Start.ToVector3();
        var end = road.End.ToVector3();

        //calculate distance between the start and end
        var distance = Vector3.Distance(start, end);

        //change texture at start position
        ChangeTerrainTexture(start,roadTextureIndex,roadWidth);

        //go from start to the end and change the texture at each position
        var currPos = Vector3.MoveTowards(start, end, inc);
        for (var i = 0; i <= distance / inc; i++)
        {
            ChangeTerrainTexture(currPos, roadTextureIndex, roadWidth);
            currPos = Vector3.MoveTowards(currPos, end, inc);

        }

        //change texture at end position
        ChangeTerrainTexture(end, roadTextureIndex, roadWidth);
    }

    /// <summary>
    /// Removes details spawned around the gameobject
    /// </summary>
    private void RemoveDetailsAroundGameObject(GameObject gameObject)
    {
        //is the object valid
        if (gameObject == null)
        {
            return;
        }

        //Get size of the object
        var size = gameObject.GetComponentInChildren<MeshRenderer>().bounds.size;

        //add a little border to the bounds
        const float checkMargin = 1.2f;

        //access bounds
        var hw = (size.x * checkMargin)/2;
        var hh = (size.z * checkMargin)/2;
        var pos = gameObject.transform.position;
        
        const float increment = 0.5f;
        for (var x = -hw; x < hw; x += increment)
        {
            for (var z = -hh; z < hh; z += increment)
            {
                var position = new Vector3(pos.x + x, 0, pos.z + z);

                //don't draw, just remove details
                ChangeTerrainTexture(position, 0, 1, false);
            }
        }
    }
    
    #region Helpers

    /// Check if there is an overlap
    private bool isOverlapping(Vector3 pos, Vector3 bounds)
    {
        var collisions = Physics.OverlapBox(pos, bounds);
        return collisions.Length > 1;
    }

    /// <summary>
    /// Store Alpha map data from the terrain
    /// </summary>
    private void GetAlphaMap()
    {
        //Access map size
        var w = _terrain.terrainData.alphamapWidth;
        var h = _terrain.terrainData.alphamapHeight;
        
        //store the data
        _alphamapData = _terrain.terrainData.GetAlphamaps(0, 0,w, h);
    }

    /// <summary>
    /// Store Detail map data from the terrain
    /// </summary>
    private void GetDetailMaps()
    {
        //create new list if needed
        if (_detailLayers == null)
        {
            _detailLayers = new List<int[,]>();
        }

        //clear previous data
        _detailLayers.Clear();

        //access detail map size
        var w = _terrain.terrainData.detailWidth;
        var h = _terrain.terrainData.detailHeight;
        var detailLayers = _terrain.terrainData.detailPrototypes.Length; //every prototype is a seperate layer

        //add all layers
        for (var i = 0; i < detailLayers; i++)
        {
            var layer = _terrain.terrainData.GetDetailLayer(0, 0, w, h, i);
            _detailLayers.Add(layer);
        }
    }

    /// <summary>
    /// Update alpha map of the terrain
    /// </summary>
    private void SetAlphaMap()
    {
        //Update the alpha map of the terrain
        _terrain.terrainData.SetAlphamaps(0, 0, _alphamapData);
    }
    
    /// <summary>
    /// Update detail map of the terrain
    /// </summary>
    private void SetDetailMaps()
    {
        for (var i = 0; i < _detailLayers.Count; i++)
        {
            _terrain.terrainData.SetDetailLayer(0, 0, i, _detailLayers[i]);
        }
    }

    /// <summary>
    /// Create or clear city data storage
    /// </summary>
    private void InitializeStorage()
    {
        //Create or clear previous storage
        if (_cells == null)
        {
            _cells = new List<DistrictCell>();
        }
        else
        {
            _cells.Clear();
        }

        if (_roads == null)
        {
            _roads = new List<Road>();
        }
        else
        {
            _roads.Clear();
        }

        if (_buildings == null)
        {
            _buildings = new List<BuildingSite>();
        }
        else
        {
            _buildings.Clear();
        }

        if (_spawnedProps == null)
        {
            _spawnedProps = new List<GameObject>();
        }
        else
        {
            //destroy spawned props
            foreach (var prop in _spawnedProps)
            {
                Object.DestroyImmediate(prop);
            }

            _spawnedProps.Clear();
        }

        
    }

    /// <summary>
    /// split up the data from the city in seperate list to allow for easier access
    /// </summary>
    private void SplitCityData()
    {
        //create or clear lists
        InitializeStorage();

        //loop all districts
        foreach (var district in _cityData.Districts)
        {
            //loop all cells
            foreach (var cell in district.Cells)
            {
                _cells.Add(cell);
                foreach (var road in cell.Roads)
                {
                    _roads.Add(road);
                    foreach (var building in road.Buildings)
                    {
                        _buildings.Add(building);
                    }
                }
            }

        }
    }

    /// <summary>
    /// Change the alpha map data to the specified texture at the positon in world coordinates
    /// </summary>
    private void ChangeTerrainTexture(Vector3 worldPos, int texture, int width, bool draw = true)
    {
        //convert world pos to terrain coordinates
        var coord = _terrain.WorldToTerrainMapCoordinates(worldPos);

        ChangeTerrainTexture((int)coord.x, (int)coord.z, texture, width, draw);
    }

    /// <summary>
    /// Change the alpha map data to the texture at the map index  [z,x]
    /// </summary>
    private void ChangeTerrainTexture(int x, int z, int texture, int width, bool draw = true)
    {
        //the spread of the texture
        var halfwidth = width / 2;
        var alphaLayers = _terrain.terrainData.alphamapLayers;

        //go over all layers and flip the right one on
        for (var i = 0; i < alphaLayers; ++i)
        {
            var value = (texture == i) ? 1 : 0;

            for (int w = -halfwidth; w <= halfwidth; ++w)
            {
                if (draw)
                {
                    _alphamapData[z + w, x + w, i] = value;
                }

                //Check if there is a detail at this location and remove it
                foreach (var layer in _detailLayers)
                {
                    layer[z + w, x + w] = 0;
                }
            }
        }
    }

    /// <summary>
    /// Access the user defined prefabs for this district type
    /// </summary>
    private List<GameObject> GetPrefabsForType(string type)
    {
        return _buildingsPrefabsPerZone.ContainsKey(type) ? _buildingsPrefabsPerZone[type] : null;
    }

    #endregion
}
