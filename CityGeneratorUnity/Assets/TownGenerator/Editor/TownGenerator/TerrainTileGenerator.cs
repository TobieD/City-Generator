using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voronoi;
using CityGenerator;
using Tools.Extensions;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Tools.TownGenerator
{
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
        public float LakeDepth = 10;

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

        public bool GenerateLake = false;
        public GameObject WaterPrefab = null;

        public int RoadWidth = 4;

        public float GrassDensity = 1.0f;
        public int DetailResolution = 8;

        public int AdditionalProps = 160;
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

        //Noise generators
        private PerlinNoise _groundNoise;
        private PerlinNoise _treeNoise;
        private PerlinNoise _detailNoise;

        //terrain prototypes
        private SplatPrototype[] _splatPrototypes;
        private TreePrototype[] _treePrototypes;
        private DetailPrototype[] _detailPrototypes;

        //store all spawned buildings inside the city
        private int _grassLayers = 0;
        private int _meshLayers = 0;

        /// <summary>
        /// Terrain the city will be created on
        /// </summary>
        public Terrain CityTerrain;
        public bool Generated = false;

        public TerrainSettings Settings
        {
            get { return _terrainSettings; }
        }

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

            //create noise
            _groundNoise = new PerlinNoise(_terrainSettings.GroundSeed);
            _treeNoise = new PerlinNoise(_terrainSettings.TreeSeed);
            _detailNoise = new PerlinNoise(_terrainSettings.DetailSeed);

            //calculate total size of 1 terrain tile based on the city bounds
            _terrainSize = (int)(_voronoiSettings.Width * 2f);

            _grassLayers = 0;
            _meshLayers = 0;

            //create the prototypes used by the generator
            CreatePrototypes();

        }

        /// <summary>
        /// Creates the terrain and applies the height map and alpha    map
        /// </summary>
        public void BuildTerrain()
        {
            //all terrain tiles will be stored in here
            _terrainTiles = new Terrain[_tilesX, _tilesZ];
            _terrainOffset = new Vector2(-_terrainSize * _tilesX * 0.5f, -_terrainSize * _tilesZ * 0.5f);

            //create gameobject and set the parent
            _terrainGameObject = new GameObject("Terrain");
            //_terrainGameObject.SetParent(_parentGameObject);

            //generate the individual tiles
            for (var x = 0; x < _tilesX; ++x)
            {
                for (var z = 0; z < _tilesZ; ++z)
                {
                    //create all the terrain tiles and store them
                    var terrain = CreateTerrainTile(x, z);
                    _terrainTiles[x, z] = terrain;
                }
            }

            //Set neighbours to remove seams
            FixTerrainSeams();

            //Find the terrain the city will be generated on
            CityTerrain = FindCityTerrainTile();

            Generated = true;
        }

        public void Clear()
        {
            Generated = false;

            Object.DestroyImmediate(ExtensionMethods.FindGameObject("Terrain"));

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

        /// <summary>
        /// Create terrain for the given tile
        /// </summary>
        private Terrain CreateTerrainTile(int x, int z)
        {
            float height = _terrainSettings.TerrainHeight;
            var centerTile = x == (_tilesX / 2) && z == (_tilesZ / 2);

            //Create terrain data and set prototypes
            var data = new TerrainData
            {
                heightmapResolution = _terrainSettings.HeightmapSize,
                alphamapResolution = _terrainSettings.AlphamapSize,
                size = new Vector3(_terrainSize, height, _terrainSize),
                splatPrototypes = _splatPrototypes,
                treePrototypes = _treePrototypes,
                detailPrototypes = _detailPrototypes
            };

            //Generate heightmap
            data.SetHeights(0, 0, GenerateHeightMap(x, z));

            //create terrain gameobject and position it correctly
            var xPos = _terrainSize * x + _terrainOffset.x;
            var zPos = _terrainSize * z + _terrainOffset.y;

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

                if (_terrainSettings.GenerateLake && rng < 0.75f)
                {
                    var it = Random.Range(1, 4);
                    for (int i = 0; i < it; i++)
                    {
                        //add a lake o the terrain with a random size
                        AddLake(terrain, x, z, Random.Range(25, 60));
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
        /// </summary>
        float[,] GenerateHeightMap(int tileX, int tileZ)
        {
            //create the heightmap to store the data
            var heightmapSize = _terrainSettings.HeightmapSize;
            var htmap = new float[heightmapSize, heightmapSize];

            var ratio = (float)_terrainSize / (float)heightmapSize;

            for (var x = 0; x < heightmapSize; ++x)
            {
                for (var z = 0; z < heightmapSize; ++z)
                {
                    var worldX = (x + tileX * (heightmapSize - 1)) * ratio;
                    var worldZ = (z + tileZ * (heightmapSize - 1)) * ratio;

                    var height = 0f;

                    //generate height using noise
                    //var mountains = Mathf.Max(0.0f, _mountainNoise.FractalNoise2D(worldX, worldZ, 6, _terrainSettings.MountainFrequency, 0.8f));
                    var ground = _groundNoise.FractalNoise2D(worldX, worldZ, 4, _terrainSettings.GroundFrequency, 0.1f) + _terrainSettings.LakeDepth / 100f;

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
                    var xNorm = (float)x / (float)width;
                    var zNorm = (float)z / (float)height;


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
                        weights[1] = angle / 90f;
                    }

                    if (layers > 3 && h < 0.4f)
                    {
                        weights[2] = 1f;
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

            var maxHeight = _terrainSettings.TerrainHeight * 0.7f;
            var minHeight = _terrainSettings.TerrainHeight * 0.1f;

            for (var x = 0; x < _terrainSize; x += spacing)
            {
                for (var z = 0; z < _terrainSize; z += spacing)
                {
                    var unit = 1.0f / (_terrainSize - 1);

                    var offsetX = Random.value * unit * spacing;
                    var offsetZ = Random.value * unit * spacing;
                    var xNorm = x * unit + offsetX;
                    var zNorm = z * unit + offsetZ;
                    var xWorld = x + tileX * (_terrainSize - 1);
                    var zWorld = z + tileZ * (_terrainSize - 1);

                    //randomizes the spacing
                    spacing = Random.Range(4, 12);

                    // Get the steepness value at the normalized coordinate.
                    var angle = data.GetSteepness(xNorm, zNorm);

                    // Steepness is given as an angle, 0..90 degrees. Divide
                    // by 90 to get an alpha blending value in the range 0..1.
                    var frac = angle / 90.0f;

                    if (frac < 0.7f)
                    {
                        var noise = _treeNoise.FractalNoise2D(xWorld, zWorld, 3, _terrainSettings.TreeFrequency, 1.0f);
                        var height = data.GetInterpolatedHeight(xNorm, zNorm);

                        //no trees on high mountains
                        if (noise > 0.1f && height < maxHeight && height > minHeight)
                        {
                            //Create the tree instance
                            var tree = new TreeInstance()
                            {
                                heightScale = 1,
                                widthScale = 1,
                                prototypeIndex = Random.Range(0, _treePrototypes.Length),
                                lightmapColor = Color.white,
                                color = Color.white,
                                position = new Vector3(xNorm, height, zNorm)
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
            var ratio = (float)_terrainSize / (float)detailSize;

            //create maps for each layer
            for (int i = 0; i < _detailPrototypes.Length; ++i)
            {
                detailLayers.Add(new int[detailSize, detailSize]);
            }

            var centerTile = tileX == (_tilesX / 2) && tileZ == (_tilesZ / 2);

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

                    var unit = 1.0f / (detailSize - 1);

                    var normX = x * unit;
                    var normZ = z * unit;

                    // Get the steepness value at the normalized coordinate.
                    var angle = data.GetSteepness(normX, normZ);

                    // Steepness is given as an angle, 0..90 degrees. Divide
                    // by 90 to get an alpha blending value in the range 0..1.
                    var frac = angle / 90.0f;

                    //select a random type of layer to use(texture or mesh
                    var rng = Random.value;

                    //Select a random grass layer
                    var grassLayer = Random.Range(0, _grassLayers);
                    //select a random prop layer
                    var propLayer = Random.Range(_grassLayers, _meshLayers + 1);

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
                            if (detailLayers.Count > 1)
                            {
                                detailLayers[propLayer][z, x] = 1;
                            }
                        }
                    }
                    else if (frac < 0.5f)
                    {
                        var worldPosX = (x + tileX * (detailSize - 1)) * ratio;
                        var worldPosZ = (z + tileZ * (detailSize - 1)) * ratio;

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
                                if (detailLayers.Count > 1)
                                {
                                    detailLayers[propLayer][z, x] = 1;
                                }
                            }
                        }
                    }
                }
            }

            return detailLayers;
        }

        /// <summary>
        /// Add a lake to the terrain
        /// </summary>
        private void AddLake(Terrain terrain, int tileX, int tileZ, int lakeSize = 150)
        {
            var data = terrain.terrainData;
            //Access height map
            var width = data.heightmapWidth;
            var height = data.heightmapHeight;
            var heightmap = data.GetHeights(0, 0, width, height);

            //random X and Z coordinate and sample height
            Vector3 lakeCenter = terrain.RandomPositionOnTerrain(lakeSize);

            var lakeRadius = lakeSize / 2;
            var coords = terrain.WorldToTerrainMapCoordinates(lakeCenter);
            int xCenter = (int)coords.x;
            int zCenter = (int)coords.z;

            var minDepth = 0.09f;
            for (int x = xCenter - lakeRadius; x < xCenter + lakeRadius; x++)
            {
                for (int z = zCenter - lakeRadius; z < zCenter + lakeRadius; z++)
                {
                    //deeper the closer to the center
                    float centerX = 1 - Math.Abs((float)(xCenter - x) / lakeRadius);
                    float centerZ = 1 - Math.Abs((float)(zCenter - z) / lakeRadius);

                    float avg = (centerX * centerZ) / 2;

                    var depth = minDepth * avg;

                    Mathf.Clamp(depth, minDepth - 0.01f, minDepth + 0.01f);

                    heightmap[z, x] -= depth;
                }
            }
            //Update height map
            data.SetHeights(0, 0, heightmap);

            lakeCenter.y -= 4.5f;

            var waterInstance = (GameObject)GameObject.Instantiate(_terrainSettings.WaterPrefab, lakeCenter, Quaternion.identity);
            waterInstance.SetParent(terrain.gameObject);


        }

        /// <summary>
        /// Find the ideal position for the city to be spawned on
        /// </summary>
        private Terrain FindCityTerrainTile()
        {
            Terrain cityTerrain = null;

            //find center tile ( should probably look for most flat tile)
            cityTerrain = _terrainTiles[_tilesX / 2, _tilesZ / 2];

            cityTerrain.name += " (City)";

            return cityTerrain;
        }
    }
}

