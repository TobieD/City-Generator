using System;
using System.Collections.Generic;
using System.Linq;
using CityGenerator;
using Helpers;
using UnityEditor;
using UnityEngine;
using Voronoi;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class SplatTexture
{
    public Texture2D Texture;
    public float TileSize = 2.0f;
    public string ID = "";
}

public enum DetailType
{
    Texture,
    GameObject
}

/// <summary>
/// Either a texture or a gameobject
/// </summary>
public class DetailObject
{
    public Object Detail;
    public DetailType Type;

    public override string ToString()
    {
        return Type.ToString();
    }
}

/// <summary>
/// Settings for the terrain
/// </summary>
public class TerrainSettings
{
    //map sizes
    public int HeightmapSize = 513;
    public int AlphamapSize = 513;
    public int DetailMapSize = 513;

    //Terrain size
    public int TerrainHeight = 512;

    //Noise Settings
    public bool UseSeed = false;
    public int GroundSeed = 0;
    public int MountainSeed = 1;
    public int TreeSeed = 2;
    public int DetailSeed = 3;
    public float GroundFrequency = 800.0f;
    public float MountainFrequency = 1500.0f;
    public float TreeFrequency = 400.0f;
    public float DetailFrequency = 100.0f;

    //prototypes
    public List<SplatTexture> SplatMaps = new List<SplatTexture>(2);
    public List<GameObject> Trees = new List<GameObject>(2);
    public List<GameObject> Props = new List<GameObject>(2);

    public List<DetailObject> Details = new List<DetailObject>();

    public float GrassDensity = 1.0f;
    public int DetailResolution = 8;

    
}

/// <summary>
/// Generates terrain using tiles
/// the center tile should always be the tile the town gets build on
/// </summary>
public class TerrainTileGenerator 
{
    //Settings
    private TerrainSettings _terrainSettings;
    private GenerationSettings _voronoiSettings;
    private CitySettings _citySettings;
    private GameObject _parentGameObject;
    private GameObject _terrainGameObject;

    //Terrain data
    private Terrain[,] _terrainTiles;
    private Vector2 _terrainOffset;
    private int _terrainSize = 600;
    private int _tilesX = 3;
    private int _tilesZ = 3;
    private int _tileIndex = 0;

    //City Data
    private CityData _cityData;

    //Noise
    private PerlinNoise _groundNoise;
    private PerlinNoise _mountainNoise;
    private PerlinNoise _treeNoise;
    private PerlinNoise _detailNoise;

    private Texture2D _heightmapNoiseTexture;

    //prototypes
    private SplatPrototype[] _splatPrototypes;
    private TreePrototype[] _treePrototypes;
    private DetailPrototype[] _detailPrototypes;

    //city tile data
    private float[,,] _alphamapData;
    private List<int[,]> _detailLayers;

    //store all spawned buildings inside the city
    private List<BuildingSite> _cityBuildings = new List<BuildingSite>();
    private List<GameObject> _props = new List<GameObject>();
    private int _grassLayers = 0;
    private int _meshLayers = 0;

    /// <summary>
    /// Terrain the city will be created on
    /// </summary>
    public Terrain CityTerrain;
    public bool Generated = false;

    /// <summary>
    /// set required data for generating the terrain
    /// </summary>
    public void InitializeSettings(TerrainSettings terrainSettings, GenerationSettings voronoiSettings, GameObject parent, CitySettings citySettings)
    {
        //store settings
        _terrainSettings = terrainSettings;
        _voronoiSettings = voronoiSettings;
        _citySettings = citySettings;
        _parentGameObject = parent;

        //initialize noise
        //if no seed is specified generate a random one
        if (!terrainSettings.UseSeed)
        {
            _terrainSettings.GroundSeed = Random.Range(int.MinValue, int.MaxValue);
            _terrainSettings.MountainSeed = Random.Range(int.MinValue, int.MaxValue);
            _terrainSettings.TreeSeed = Random.Range(int.MinValue, int.MaxValue);
            _terrainSettings.DetailSeed = Random.Range(int.MinValue, int.MaxValue);
        }
        
        _groundNoise = new PerlinNoise(_terrainSettings.GroundSeed);
        _mountainNoise = new PerlinNoise(_terrainSettings.MountainSeed);
        _treeNoise = new PerlinNoise(_terrainSettings.TreeSeed);
        _detailNoise = new PerlinNoise(_terrainSettings.DetailSeed);

        _heightmapNoiseTexture = _groundNoise.GetPreviewTexture();


        //calculate total size of 1 terrain tile based on the city bounds
        _terrainSize = (int)(_voronoiSettings.Width*2f);

        _grassLayers = 0;
        _meshLayers = 0;

        //create the prototypes used by the generator
        CreatePrototypes();
    }

    /// <summary>
    /// Creates the terrain and applies the height map and alpha map
    /// </summary>
    public void BuildTerrain()
    {
        //all terrain tiles will be stored in here
        _terrainTiles = new Terrain[_tilesX,_tilesZ];
        _terrainOffset = new Vector2(-_terrainSize* _tilesX * 0.5f, -_terrainSize* _tilesZ * 0.5f);

        //create gameobject and set the parent
        _terrainGameObject = new GameObject("Terrain");
        _terrainGameObject.SetParent(_parentGameObject);

        //generate the individual tiles
        _tileIndex = 0;
        for (var x = 0; x < _tilesX; ++x)
        {
            for (var z = 0; z < _tilesZ; ++z)
            {
                //create all the terrain tiles and store them
                var terrain = CreateTerrainTile(x, z);
                _terrainTiles[x, z] = terrain;
                _tileIndex++;
            }
        }

        //Set neighbours to remove seams
        FixTerrainSeams();

        //Find the terrain the city will be generated on
        CityTerrain = FindCityTerrainTile();

        Generated = true;
    }

    /// <summary>
    /// Fills the terrain with trees,Grass, Props/ ...
    /// </summary>
    public void PopulateTerrain(CityData cityData)
    {
        _cityData = cityData;

        //adjust the roads of the city to use the road texture
        DrawCityOnTerrain(cityData);
    }

    public void Clear()
    {
        Generated = false;
    }

    /// <summary>
    /// Create splatmaps, trees and detail prototypes
    /// </summary>
    private void CreatePrototypes()
    {
        //Create splatmap prototypes
        var splats = new List<SplatPrototype>();
        foreach (var tex in _terrainSettings.SplatMaps)
        {
            //make sure the texture is valid
            if (tex == null) continue;

            var splat = new SplatPrototype()
            {
                texture = tex.Texture,
                tileSize = new Vector2(tex.TileSize, tex.TileSize)
            };

            splats.Add(splat);
        }

        //store it
        _splatPrototypes = splats.ToArray();

        //create tree prototypes
        var trees = new List<TreePrototype>();
        foreach (var prefab in _terrainSettings.Trees)
        {
            if (prefab == null) continue;

            var treeProto = new TreePrototype
            {
                prefab = prefab
            };
            trees.Add(treeProto);
        }


        _treePrototypes = trees.ToArray();


        //Sort the detail list based on type ( textures  < prefabs)
        var detailPrefabs = _terrainSettings.Details;

        detailPrefabs = detailPrefabs.SortByType();

        //create detail prototypes textures
        var details = new List<DetailPrototype>();
        foreach (var d in detailPrefabs)
        {
            if (d == null) continue;

            var detail = new DetailPrototype
            {
                healthyColor = Color.white,
                dryColor = Color.white,
                maxHeight = 0.8f,
                maxWidth = 1.0f
            };

            //set correct data based on type
            switch (d.Type)
            {
                case DetailType.Texture:
                {
                    detail.renderMode = DetailRenderMode.GrassBillboard;
                    detail.prototypeTexture = (Texture2D)d.Detail;
                    _grassLayers++;
                    break;
                }
                case DetailType.GameObject:
                {
                    detail.renderMode = DetailRenderMode.VertexLit;
                    detail.prototype = (GameObject)d.Detail;
                    detail.usePrototypeMesh = true;
                    _meshLayers++;
                    break;
                }
            }

            //Add the detail
            details.Add(detail);
        }

        _detailPrototypes = details.ToArray();


        //notify user of any missed prototypes
        if (_splatPrototypes.Length < 1)
        {
            Debug.LogWarning("Unable to generate terrain textures\nPlease specify textures to use for the terrain.");
        }


        if (_treePrototypes.Length == 0)
        {
            Debug.LogWarning("Unable to generate trees\nPlease specify prefabs to use for trees.");
        }

        if (_detailPrototypes.Length == 0)
        {
            Debug.LogWarning("Unable to generate details\nPlease specify prefabs to use for details.");
        }
    }

    #region Terrain Generation Helpers

    /// <summary>
    /// Create terrain for the given tile
    /// </summary>
    private Terrain CreateTerrainTile(int x, int z)
    {
        float height = _terrainSettings.TerrainHeight;

        var centerTile = x == (_tilesX/2) && z == (_tilesZ/2);


        //Create terrain data and set prototypes
        var data = new TerrainData
        {
            heightmapResolution = _terrainSettings.HeightmapSize, alphamapResolution = _terrainSettings.AlphamapSize, size = new Vector3(_terrainSize, height, _terrainSize), splatPrototypes = _splatPrototypes, treePrototypes = _treePrototypes, detailPrototypes = _detailPrototypes
        };

        //Generate heightmap
        data.SetHeights(0, 0, GenerateHeightMap(x, z));
       
        //create terrain gameobject and position it correctly
        var xPos = _terrainSize*x + _terrainOffset.x;
        var zPos = _terrainSize*z + _terrainOffset.y;

        var terrain = Terrain.CreateTerrainGameObject(data).GetComponent<Terrain>();
        terrain.transform.position = new Vector3(xPos, 0, zPos);
        terrain.gameObject.SetParent(_terrainGameObject);
        terrain.name = string.Format("tile{0}x{1}", x, z);
        terrain.castShadows = false;
        terrain.detailObjectDensity = _terrainSettings.GrassDensity;
        terrain.detailObjectDistance = 200.0f;
        terrain.treeDistance = 1000.0f;
        terrain.treeBillboardDistance = 400.0f;
        terrain.treeCrossFadeLength = 20.0f;
        terrain.treeMaximumFullLODCount = 400;

        //Generate trees
        if (!centerTile)
        {
            //Possibly generate some lakes
            var rng = Random.value;

            if (rng < 0.45f)
            {
                var it = Random.Range(1, 5);
                for (int i = 0; i < it; i++)
                {
                    //AddLake(terrain, x, z, Random.Range(25, 75));
                }
            }

            var trees = GenerateTrees(data, x, z);
            if (trees != null)
            {
                foreach (var tree in trees)
                {
                    terrain.AddTreeInstance(tree);
                }
            }

            
        }

        //Generate alphamap if valid
        var alphamap = GenerateAlphaMap(data);
        if (alphamap != null)
        {
            data.SetAlphamaps(0, 0, alphamap);
        }


        //Generate Detailsmap if valid for every layer
        var detailLayers = GenerateDetailLayers(data, x, z);
        if (detailLayers != null)
        {
            //set some detail settings
            data.SetDetailResolution(_terrainSettings.DetailMapSize, _terrainSettings.DetailResolution);
            data.wavingGrassStrength = 0.1f;
            data.wavingGrassAmount = 0.1f;
            data.wavingGrassSpeed = 0.2f;
            data.wavingGrassTint = Color.white;
            for (int layer = 0; layer < detailLayers.Count; ++layer)
            {
                data.SetDetailLayer(0, 0, layer, detailLayers[layer]);
            }
        }

      

        //make sure the changes are updated
        terrain.Flush();


        return terrain;
    }

    /// <summary>
    /// Fix seams between tiles by correctly setting the neighbours
    /// </summary>
    private void FixTerrainSeams()
    {
        for (int x = 0; x < _tilesX; ++x)
        {
            for (int z = 0; z < _tilesZ; ++z)
            {
                Terrain right = null;
                Terrain left = null;
                Terrain bottom = null;
                Terrain top = null;

                if (x > 0)
                {
                    left = _terrainTiles[x - 1, z];
                }
                if (x < _tilesX - 1)
                {
                    right = _terrainTiles[x + 1, z];
                }

                if (z > 0)
                {
                    bottom = _terrainTiles[x, z - 1];
                }
                if (z < _tilesZ - 1)
                {
                    top = _terrainTiles[x, z + 1];
                }

                //set the neighbourss
                _terrainTiles[x, z].SetNeighbors(left, top, right, bottom);
            }
        }
    }

    /// <summary>
    /// Generate height information for the terrain at the given tile
    /// <remarks>maximum 3 textures can be used for the terrain</remarks>
    /// </summary>
    float[,] GenerateHeightMap(int tileX, int tileZ)
    {
        //create the heightmap to store the data
        var heightmapSize = _terrainSettings.HeightmapSize;
        var htmap = new float[heightmapSize, heightmapSize];

        var ratio = (float) _terrainSize/(float) heightmapSize;

        for (var x = 0; x < heightmapSize; ++x)
        {
            for (var z = 0; z < heightmapSize; ++z)
            {
                var worldX = (x + tileX*(heightmapSize - 1))*ratio;
                var worldZ = (z + tileZ*(heightmapSize - 1))*ratio;

                var height = 0f;

                //generate height using noise
                //var mountains = Mathf.Max(0.0f, _mountainNoise.FractalNoise2D(worldX, worldZ, 6, _terrainSettings.MountainFrequency, 0.8f));
                var ground = _groundNoise.FractalNoise2D(worldX, worldZ, 4, _terrainSettings.GroundFrequency, 0.1f) + 0.1f;

                height = ground;

                htmap[z, x] = height;
            }
        }

        return htmap;
    }

    /// <summary>
    /// Generate textures for the terrain
    /// </summary>
    float[,,] GenerateAlphaMap(TerrainData data)
    {
        //make sure textures have been set
        if (_splatPrototypes.Length < 1)
        {
            return null;
        }

        //create map
        var width = data.alphamapWidth;
        var height = data.alphamapHeight;
        var layers = data.alphamapLayers - 1; //ignore the road layer
        var alphamap = new float[width, height, layers + 1];

        for (int x = 0; x < width; ++x)
        {
            for (int z = 0; z < height; ++z)
            {
                var xNorm = (float) x/(float) width;
                var zNorm = (float) z/(float) height;


                //sample height at this location
                var h = data.GetHeight(z, x);

                //get normalized coordinates relative to the overall terrain dimensions
                var normal = data.GetInterpolatedNormal(zNorm, xNorm);

                //get steepnes at the coordinate
                var angle = data.GetSteepness(zNorm, xNorm);

                var weights = new float[layers];

                if (layers > 1)
                {
                    weights[0] = 0.5f;
                }

                if (layers > 2)
                {
                    //more influence at steep heights
                    weights[1] = angle/90f;
                }

                if (layers > 3)
                {
                    weights[2] = h*Mathf.Clamp01(normal.z);
                }

                float sum = weights.Sum();

                //go over all terrain textures
                for (int i = 1; i < layers; i++)
                {
                    //normalize
                    weights[i - 1] /= sum;

                    //set weight for correct texture
                    alphamap[x, z, i] = weights[i - 1];
                }
            }
        }

        return alphamap;
    }

    /// <summary>
    /// Generate trees on the terrain
    /// </summary>
    List<TreeInstance> GenerateTrees(TerrainData data, int tileX, int tileZ)
    {
        //make sure prototypes have been set
        if (_treePrototypes.Length < 1)
        {
            return null;
        }

        var trees = new List<TreeInstance>();
        int spacing = 8;

        for (var x = 0; x < _terrainSize; x += spacing)
        {
            for (var z = 0; z < _terrainSize; z += spacing)
            {
                var unit = 1.0f/(_terrainSize - 1);

                var offsetX = Random.value*unit*spacing;
                var offsetZ = Random.value*unit*spacing;
                var xNorm = x*unit + offsetX;
                var zNorm = z*unit + offsetZ;
                var xWorld = x + tileX*(_terrainSize - 1);
                var zWorld = z + tileZ*(_terrainSize - 1);

                //randomizes the spacing
                spacing = Random.Range(4, 12);

                // Get the steepness value at the normalized coordinate.
                var angle = data.GetSteepness(xNorm, zNorm);

                // Steepness is given as an angle, 0..90 degrees. Divide
                // by 90 to get an alpha blending value in the range 0..1.
                var frac = angle/90.0f;

                if (frac < 0.5f)
                {
                    var noise = _treeNoise.FractalNoise2D(xWorld, zWorld, 3, _terrainSettings.TreeFrequency, 1.0f);
                    var height = data.GetInterpolatedHeight(xNorm, zNorm);

                    //no trees on high mountains
                    if (noise > 0.0f && height < _terrainSettings.TerrainHeight*0.7f)
                    {
                        //Create the tree instance
                        var tree = new TreeInstance()
                        {
                            heightScale = 1, widthScale = 1, prototypeIndex = Random.Range(0, _treePrototypes.Length), lightmapColor = Color.white, color = Color.white, position = new Vector3(xNorm, height, zNorm)
                        };

                        trees.Add(tree);
                    }
                }
            }
        }

        return trees;
    }

    /// <summary>
    /// Generate detail like grass and props on the terrain 
    /// </summary>
    List<int[,]> GenerateDetailLayers(TerrainData data, int tileX, int tileZ)
    {
        //make sure there are details set
        if (_detailPrototypes.Length < 1)
        {
            return null;
        }

        //create all layers
        //note: each layer is a seperate draw call so try to keep it low
        var detailLayers = new List<int[,]>();
        var detailSize = _terrainSettings.DetailMapSize;
        var ratio = (float) _terrainSize/(float) detailSize;

        //create maps for each layer
        for (int i = 0; i < _detailPrototypes.Length; ++i)
        {
            detailLayers.Add(new int[detailSize, detailSize]);
        }

        var centerTile = tileX == (_tilesX/2) && tileZ == (_tilesZ/2);

        //fill the layers
        for (int x = 0; x < detailSize; x++)
        {
            for (int z = 0; z < detailSize; z++)
            {
                //Set all detail to 0
                foreach (var layer in detailLayers)
                {
                    layer[z, x] = 0;
                }

                var unit = 1.0f/(detailSize - 1);

                var normX = x*unit;
                var normZ = z*unit;

                // Get the steepness value at the normalized coordinate.
                var angle = data.GetSteepness(normX, normZ);

                // Steepness is given as an angle, 0..90 degrees. Divide
                // by 90 to get an alpha blending value in the range 0..1.
                var frac = angle/90.0f;

                //select a random type of layer to use(texture or mesh
                var rng = Random.value;

                //Select a random grass layer
                var grassLayer = Random.Range(0, _grassLayers);
                //select a random prop layer
                var propLayer = Random.Range(_grassLayers, _meshLayers + 1 );

                //in the center tile fill it completely with grass, and remove it on locations of props and buildings later
                if (centerTile)
                {
                    //prefer to spawn grass
                    if (rng < 0.99f)
                    {
                        detailLayers[grassLayer][z, x] = 1;
                    }
                    else
                    {
                        detailLayers[propLayer][z, x] = 1;
                    }
                }
                else if (frac < 0.5f)
                {
                    var worldPosX = (x + tileX*(detailSize - 1))*ratio;
                    var worldPosZ = (z + tileZ*(detailSize - 1))*ratio;

                    var noise = _detailNoise.FractalNoise2D(worldPosX, worldPosZ, 3, _terrainSettings.DetailFrequency, 1.0f);

                    if (noise > 0.0f)
                    {
                        //prefer to spawn grass
                        if (rng < 0.99f)
                        {
                            detailLayers[grassLayer][z, x] = 1;
                        }
                        else
                        {
                            detailLayers[propLayer][z, x] = 1;
                        }
                    }
                }
            }
        }

        return detailLayers;
    }

    private void AddLake(Terrain terrain, int tileX, int tileZ,int lakeSize = 150)
    {
        var data = terrain.terrainData;
        //Access height map
        var width = data.heightmapWidth;
        var height = data.heightmapHeight;
        var heightmap = data.GetHeights(0, 0, width, height);

        //find a random coordinate to start from but avoid edges
        var xCenter = Random.Range(lakeSize, width - lakeSize);
        var zCenter = Random.Range(lakeSize, height - lakeSize);
        var lakeRadius = lakeSize/2;

        //a lake should go deeper towards the center
        //find lowest point of the lake
        for (int x = xCenter - lakeRadius; x < xCenter + lakeRadius; x++)
        {
            for (int z = zCenter - lakeRadius; z < zCenter + lakeRadius; z++)
            {
                float centerX = Math.Abs((float)(xCenter - x)/lakeRadius);
                float centerZ = Math.Abs((float)(zCenter - z)/lakeRadius);


                var depth = heightmap[z, x] * ((1 - centerX * centerZ) * 0.7f);
                heightmap[z, x] -= depth;
            }
        }
       
        var lakeWorldpos = TerrainMapCoordinatesToWorldCoordinates(terrain, xCenter, zCenter);
        lakeWorldpos.y = -5.0f;
        var waterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Water.prefab");

        var waterInstance = (GameObject) GameObject.Instantiate(waterPrefab, lakeWorldpos, Quaternion.identity);
        waterInstance.SetParent(terrain.gameObject);

        //Update height map
        data.SetHeights(0,0,heightmap);
    }

    #endregion

    #region City Helpers

    private void DrawCityOnTerrain(CityData data)
    {
        //Access alpha and detail maps to adjust terrain based on city data
        _alphamapData = CityTerrain.terrainData.GetAlphamaps(0, 0, CityTerrain.terrainData.alphamapWidth, CityTerrain.terrainData.alphamapHeight);
        _detailLayers = new List<int[,]>();
        for (int i = 0; i < _detailPrototypes.Length; i++)
        {
            _detailLayers.Add(CityTerrain.terrainData.GetDetailLayer(0, 0, CityTerrain.terrainData.detailWidth, CityTerrain.terrainData.detailHeight, i));
        }

        //remove all previous spawned trees
        RemoveTreesOnTerrain(CityTerrain);

        foreach (var gameObject in _props)
        {
            GameObject.DestroyImmediate(gameObject);
        }

        _props.Clear();
        int width = 4;
        //go over every district
        foreach (var district in data.Districts)
        {
            //first pass to apply road data to the terrain
            foreach (var cell in district.Cells)
            {
                //Draw the roads on the terrain
                foreach (var road in cell.Roads)
                {
                    foreach (var building in road.Buildings)
                    {
                        _cityBuildings.Add(building);
                        DrawBuildingTexture((GameObject)building.UserData);
                    }

                    DrawRoadTexture(road, width);
                }
            }


            //Do another pass of all cells for spawning props or trees
            foreach (var cell in district.Cells)
            {
                var positions = cell.GenerateRandomPoints(10);
                foreach (var pos in positions)
                {
                    var position = pos.ToVector3();

                    //check if it will spawn on a road
                    if (!IsOnRoad(position) && !IsOnHouse(position))
                    {
                        var rng = Random.value;

                        if (rng < 0.66f)
                        {
                            SpawnTreeAtPosition(CityTerrain, position);
                        }
                        else if (rng < 0.85f)
                        {
                           
                        }
                        else
                        {
                            SpawnPropOnTerrain(CityTerrain, position);
                            
                        }
                    }
                }

                // Go over all buildings and see if they are spawned on a road, remove the building if that is the case
                foreach (var road in cell.Roads)
                {
                    foreach (var building in road.Buildings)
                    {
                        if (IsOnRoad(building.ToVector3()))
                        {
                            //Object.DestroyImmediate((GameObject)building.UserData);
                        }
                    }
                }
            }
        }

        //update the alpha map and detail maps
        CityTerrain.terrainData.SetAlphamaps(0, 0, _alphamapData);
        for (int i = 0; i < _detailLayers.Count; i++)
        {
            CityTerrain.terrainData.SetDetailLayer(0, 0, i, _detailLayers[i]);
        }
    }

    /// <summary>
    /// spawn a random tree on the terrain at the given world positon
    /// </summary>
    void SpawnTreeAtPosition(Terrain terrain, Vector3 pos)
    {
        //convert world pos to terrain pos vlue between 0 and 1 based on terrain size
        Vector3 treePos = WorldToTerrainCoordinates(terrain, pos);

        treePos.y = terrain.SampleHeight(pos);

        //Create the tree instance at the position
        var tree = new TreeInstance()
        {
            heightScale = 1, widthScale = 1, prototypeIndex = Random.Range(0, _treePrototypes.Length), lightmapColor = Color.white, color = Color.white, position = treePos
        };
        terrain.AddTreeInstance(tree);

        //update the terrain to apply changes
        terrain.Flush();
    }

    /// <summary>
    /// Spawn a random prop from the specified props on the terrain at the specified position
    /// </summary>
    private void SpawnPropOnTerrain(Terrain terrain, Vector3 position)
    {
        //select a random prefab
        var prefab = _terrainSettings.Props.GetRandomValue();

        position.y = terrain.SampleHeight(position);

        var go = (GameObject) GameObject.Instantiate(prefab, position, Quaternion.identity);

        go.SetParent(_terrainGameObject);

        DrawBuildingTexture(go);
        _props.Add(go);
    }

    /// <summary>
    /// Find the ideal position for the city to be spawned on
    /// </summary>
    private Terrain FindCityTerrainTile()
    {
        Terrain cityTerrain = null;

        //find center tile ( should probably look for most flat tile)
        cityTerrain = _terrainTiles[_tilesX/2, _tilesZ/2];

        cityTerrain.name += " (City)";

        return cityTerrain;
    }

    /// <summary>
    /// Check if at the given world pos there is a road
    /// </summary>
    private bool IsOnRoad(Vector3 worldPos)
    {
        var mapPos = WorldToTerrainMapCoordinates(CityTerrain, worldPos);

        var width = 3;

        //check in the area around
        //road texture is always first
        for (int i = -width; i <= width; i++)
        {
            if (_alphamapData[(int) mapPos.x + i, (int) mapPos.z + i, 0] >= 0.9f)
            {
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Check if the position given is near a house
    /// </summary>
    private bool IsOnHouse(Vector3 worldPos)
    {
        //go over all positions of buildings
        foreach (var b in _cityBuildings)
        {
            var width = b.Width/2;  
            var length = b.Height/2;
            var pos = b.ToVector3();

            if (worldPos.x > pos.x - width && worldPos.x < pos.x + width && worldPos.z > pos.z - length && worldPos.z < pos.z + length)
            {
                return true;
            }
        }


        return false;
    }

    /// <summary>
    /// remove all trees spawned on the terrain
    /// </summary>
    private void RemoveTreesOnTerrain(Terrain terrain)
    {
        //remove previous spawned trees inside the city limits
        //outside trees stay
        var trees = new List<TreeInstance>();
        trees.AddRange(terrain.terrainData.treeInstances);
        //var treesToRemove = new List<int>();

        //for (int i = 0; i < trees.Count; i++)
        //{
        //    var tree = trees[i];

        //    //convert from terrain to world
        //    var worldTreepos = TerrainToWorldCoordinates(terrain, tree.position);

        //    //check if the tree is inside the city and remove it
        //    if (IsInsideCity(worldTreepos))
        //    {
        //        //trees.Remove(tree);
        //        treesToRemove.Add(i);
        //    }

        //}

        //foreach (var i in treesToRemove)
        //{
        //    trees.RemoveAt(i);
        //}

        trees.Clear();
        terrain.terrainData.treeInstances = trees.ToArray();
        terrain.Flush();

        _cityBuildings.Clear();
        _cityBuildings = new List<BuildingSite>();
    }

    //check if a position is inside the city
    private bool IsInsideCity(Vector3 pos)
    {
        var p = CityTerrain.transform.position;
        var cityLeft = _cityData.Bounds.Left;
        var cityRight = _cityData.Bounds.Right;
        var cityTop = _cityData.Bounds.Top;
        var cityBottom = _cityData.Bounds.Bottom;


        if (pos.x > cityLeft && pos.x < cityRight || pos.z < cityBottom && pos.z > cityTop)
        {
            return true;
        }

        return false;
    }

    #endregion

    #region Texturing Helpers

    private void DrawRoadTexture(Road road, int width)
    {
        //the road texture is the dfirst of the splat textures
        var texture = 0;
        var increment = 0.1f;
        var start = road.Start.ToVector3();
        var end = road.End.ToVector3();
        var distance = Vector3.Distance(start, end);

        var currPos = Vector3.MoveTowards(start, end, increment);

        ChangeTerrainTexture(CityTerrain, start, texture, width);
        for (int i = 0; i <= distance/increment; i++)
        {
            ChangeTerrainTexture(CityTerrain, currPos, texture, width);
            currPos = Vector3.MoveTowards(currPos, end, increment);
        }

        ChangeTerrainTexture(CityTerrain, end, texture, width);
    }

    private void DrawBuildingTexture(GameObject go)
    {
        if (go == null)
        {
            return;
            
        }
        var col = go.GetComponent<Collider>();
        if (col == null)
        {
            Debug.Log("No Collider");
            return;
        }

        var size = col.bounds.size;
        var pos = go.transform.position;

        float halfWidth = size.x/2;
        float halfLength = size.z/2;

        float increment = 0.5f;

        for (float x = -halfWidth; x < halfWidth; x += increment)
        {
            for (float z = -halfLength; z < halfLength; z += increment)
            {
                ChangeTerrainTexture(CityTerrain, new Vector3((float) pos.x+ x, 0, (float) pos.z + z), 0, 1, false);
            }
        }
    }

    private void ChangeTerrainTexture(Terrain terrain, Vector3 worldPos, int texture, int width, bool draw = true)
    {
        //convert world pos to terrain coordinates
        var coord = WorldToTerrainMapCoordinates(terrain, worldPos);

        ChangeTerrainTexture(terrain, (int) coord.x, (int) coord.z, texture, width, draw);
    }

    /// <summary>
    /// Change the alpha map at the given location to use the texture specified
    /// Also cleans up the detail map
    /// </summary>
    private void ChangeTerrainTexture(Terrain terrain, int x, int z, int texture, int width, bool draw = true)
    {
        var halfwidth = width/2;

        //go over all layers and flip the right one on
        for (int i = 0; i < terrain.terrainData.alphamapLayers; ++i)
        {
            int value = (texture == i) ? 1 : 0;

            for (int w = -halfwidth; w <= halfwidth; ++w)
            {
                if (draw)
                    _alphamapData[z + w, x + w, i] = value;

                //Check if there is a detail at this location and remove it
                foreach (var layer in _detailLayers)
                {
                    layer[z + w, x + w] = 0;
                }
            }
        }
    }

    #endregion

    #region General Helpers

    /// <summary>
    /// Converts world position to a position on the terrain
    /// </summary>
    private Vector3 WorldToTerrainCoordinates(Terrain terrain, Vector3 world)
    {
        var convertedPos = Vector3.zero;
        var terrainPos = terrain.transform.position;

        //for some reason terrain pos coordinates need to be swapped
        convertedPos.x = ((world.x - terrainPos.z)/terrain.terrainData.size.x);
        convertedPos.z = ((world.z - terrainPos.x)/terrain.terrainData.size.z);

        return convertedPos;
    }

    private Vector3 WorldToTerrainMapCoordinates(Terrain terrain, Vector3 world)
    {
        var convertedPos = Vector3.zero;
        var terrainPos = WorldToTerrainCoordinates(terrain, world);

        //for some reason terrain pos coordinates need to be swapped
        convertedPos.x = terrainPos.x*terrain.terrainData.alphamapWidth;
        convertedPos.z = terrainPos.z*terrain.terrainData.alphamapHeight;

        return convertedPos;
    }

    private Vector3 TerrainToWorldCoordinates(Terrain terrain, Vector3 terrainPos)
    {
        var convertedPos = Vector3.zero;

        //for some reason terrain pos coordinates need to be swapped
        convertedPos.x = terrain.transform.position.x + terrainPos.x*terrain.terrainData.size.x;
        convertedPos.z = terrain.transform.position.z + terrainPos.z*terrain.terrainData.size.z;

        return convertedPos;
    }

    private Vector3 TerrainMapCoordinatesToWorldCoordinates(Terrain terrain, int x,int z)
    {
        var convertedPos = Vector3.zero;

        //for some reason terrain pos coordinates need to be swapped
        convertedPos.x = x/terrain.terrainData.heightmapWidth;
        convertedPos.z = z/terrain.terrainData.heightmapHeight;

        return TerrainToWorldCoordinates(terrain,convertedPos);

    }

    public Texture2D GetHeightmapTexture()
    {
        return _heightmapNoiseTexture;
    }

    #endregion
}

