    
using System.Collections.Generic;
using System.Linq;
using CityGenerator;
using Helpers;
using UnityEngine;
using Voronoi;

public class SplatTexture
{
    public Texture2D Texture;
    public float TileSize = 2.0f;
    public string ID = "";
}

public class GrassTexture
{
    public Texture2D Texture;
    public Color HealthyColor = Color.white;
    public Color DryColor = Color.white;
}

/// <summary>
/// Settings for the terrain
/// </summary>
public class TerrainSettings
{
    //map sizes
    public int HeightmapSize = 1025;
    public int AlphamapSize = 1025;
    public int DetailMapSize = 512;

    //Terrain size
    public int TerrainHeight = 256;
    public int TerrainScaleFactor = 3;

    //Noise Settings
    public int GroundSeed = 0;
    public float GroundFrequency = 800.0f;
    public int MountainSeed = 0;
    public float MountainFrequency = 1200.0f;
    public int TreeSeed = 0;
    public float TreeFrequency = 400.0f;
    public int DetailSeed = 0;
    public float DetailFrequency = 100.0f;

    //prototypes
    public List<SplatTexture> SplatMaps = new List<SplatTexture>(2);
    public List<GameObject> Trees = new List<GameObject>(2);
    public List<GameObject> Props = new List<GameObject>(2);

    public List<GrassTexture> GrassTextures = new List<GrassTexture>(); 

}

/// <summary>
/// Generate a terrain using a specified map
/// </summary>
public class TerrainGenerator
{
    //TerrainSettings
    public TerrainSettings TerrainSettings;
    private GenerationSettings _genSettings;

    //width and height of the terrain
    private int _terrainWidth = 300;
    private int _terrainLength = 300;

    public Terrain Terrain;
    public TerrainData TerrainData
    {
        get { return Terrain.terrainData; }
    }
    private GameObject _terrain;

    //Perlin noise for terrain
    private PerlinNoise _groundNoise;
    private PerlinNoise _mountainNoise;
    private PerlinNoise _treeNoise;
    private PerlinNoise _detailNoise;

    private CityData _cityData;

    //stores texture information of the terrain(splatmaps)
    private float[,,] _alphadata;

    /// <summary>
    /// Are settings correctly set and fix them if not
    /// </summary>
    private bool CreateSettings(GenerationSettings generationSettings)
    {
        //Set up noise
        _groundNoise = new PerlinNoise(TerrainSettings.GroundSeed);
        _mountainNoise = new PerlinNoise(TerrainSettings.MountainSeed);
        _treeNoise = new PerlinNoise(TerrainSettings.TreeSeed);
        _detailNoise = new PerlinNoise(TerrainSettings.DetailSeed);

        if (!Mathf.IsPowerOfTwo(TerrainSettings.HeightmapSize - 1))
        {
            Debug.LogWarning("Terrain Generator:: Height map size must be pow2 + 1!");
            TerrainSettings.HeightmapSize = Mathf.ClosestPowerOfTwo(TerrainSettings.HeightmapSize) + 1;
        }

        //Terrain size
        _terrainWidth = (int)generationSettings.Width * TerrainSettings.TerrainScaleFactor;
        _terrainLength = (int)generationSettings.Length * TerrainSettings.TerrainScaleFactor;

        //everything is valid
        return true;
    }

    /// <summary>
    /// Create terrain using the bounds of the voronoi diagram
    /// </summary>
    public void BuildTerrain(GenerationSettings genSettings, TerrainSettings terrainSettings, GameObject parent)
    {
        //store settings
        TerrainSettings = terrainSettings;
        _genSettings = genSettings;

        //make sure all settings are correct
        if (!CreateSettings(genSettings))
        {
            Debug.LogError("Unable to generate terrain!");
            return;
        }

        //Create terrain data
        var terrainData = new TerrainData
        {
            heightmapResolution = TerrainSettings.HeightmapSize,
            size = new Vector3(_terrainWidth, TerrainSettings.TerrainHeight, _terrainLength)
        };

        //Create prototypes for terrain(textures, trees,...)
        CreatePrototypes(terrainData);

        //create heightmap using perlin noise
        var heightmap = new float[TerrainSettings.HeightmapSize, TerrainSettings.HeightmapSize];
        FillHeightmap(heightmap);
        terrainData.SetHeights(0, 0, heightmap);

        //fill the textures of the terrain with the splatmaps
        FillAlphamap(terrainData);
        _alphadata = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);

        //Create terrain from terrain data
        _terrain = Terrain.CreateTerrainGameObject(terrainData);
        _terrain.transform.parent = parent.transform;
        _terrain.transform.position = new Vector3(-(_terrainWidth/2), 0, -(_terrainLength/2));

        //Access terrain data and set terrain data
        Terrain = _terrain.GetComponent<Terrain>();
        Terrain.terrainData = terrainData;
        Terrain.castShadows = false; //better fps
        Terrain.Flush();
    }

    /// <summary>
    /// Populate the terrain using the generated city information
    /// </summary>
    public void PopulateTerrain(CityData cityData)
    {
        _cityData = cityData;

        //place trees 
        GenerateTrees(TerrainData, cityData);

        //place detail meshes and grass
        GenerateDetail(TerrainData, cityData);

        //apply textures for the roads and stuff
        ApplyTextures(cityData);
    }

    /// <summary>
    /// Apply textures for roads and houses
    /// </summary>
    private void ApplyTextures(CityData cityData)
    {
        var terrainData = Terrain.terrainData;

        _alphadata = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);
        
        foreach (var district in cityData.Districts)
        {
            //ChangeDistrictTexture(district);

            foreach (var cell in district.Cells)
            {
                foreach (var road in cell.Roads)
                {
                    foreach (var building in road.Buildings)
                    {
                        ChangeTextureBuildingBlock(building,5);
                    }
                }

                foreach (var road in cell.Roads)
                {
                    ChangeTextureRoad(road,4);
                }
            }
        }

        terrainData.SetAlphamaps(0, 0, _alphadata);
    }

    #region Generation
    /// <summary>
    /// Fill the height map using perlin noise
    /// </summary>
    private void FillHeightmap(float[,] heightmap)
    {
        var ratio = (float) _terrainWidth/(float) TerrainSettings.HeightmapSize;


        for (int x = 0; x < TerrainSettings.HeightmapSize; x++)
        {
            for (int z = 0; z < TerrainSettings.HeightmapSize; z++)
            {
                var worldPosX = x*ratio;
                var worldPosZ = z*ratio;

                float height = 0;
                float mountains = 0;
                float plain = 0;
                
                mountains = Mathf.Max(0.0f,_mountainNoise.FractalNoise2D(worldPosX, worldPosZ, 6, TerrainSettings.MountainFrequency, 0.2f));
                plain = _groundNoise.FractalNoise2D(worldPosX, worldPosZ, 4, TerrainSettings.GroundFrequency, 0.1f);

                height = plain + mountains;

                heightmap[x, z] = height;

            }
        }
    }

    /// <summary>
    /// Use splatmaps to fill the alpha map of the terrain based on the height
    /// </summary>
    private void FillAlphamap(TerrainData data)
    {
        //Make sure textures have been specified
        if (data.splatPrototypes.Length == 0)
        {
            Debug.LogWarning("Unable to generate terrain textures\nPlease specify textures to use for the terrain.");
            return;
        }

        //use the same size as the height map 
        var size = TerrainSettings.HeightmapSize;
        var alphaHeight = data.heightmapHeight;
        var alphaWidth = data.heightmapWidth;
        var layers = data.alphamapLayers -1; //ignore the road layer


        if (layers < 1)
        {
            Debug.Log("No splatmaps defined!\n");
            return;
        }

        //slatmap data is stored as a 3d array of floats
        float[, ,] map = new float[alphaWidth, alphaHeight, data.alphamapLayers];

        for (int x = 0; x < alphaHeight; x++)
        {
            for (int z = 0; z < alphaWidth; z++)
            {
                //get normalized terrain coordinate that corresponds to the point
                var normX = x * 1.0f / (size - 1);
                var normZ = z * 1.0f / (size - 1);

                normZ = (float) z / (float) alphaHeight;
                normX = (float) x / (float)alphaWidth;

                //sample height at this location
                var height = data.GetHeight(Mathf.RoundToInt(normZ*data.heightmapHeight), Mathf.RoundToInt(normX * data.heightmapWidth));

                //get normalized coordinates relative to the overall terrain dimensions
                var normal = data.GetInterpolatedNormal(normZ, normX);
                
                //get steepnes at the coordinate
                var angle = data.GetSteepness(normZ, normX);

                //Weight rules for the splat maps
                float[] splatWeights = new float[data.alphamapLayers];

                //first splat texture has constant influence
                splatWeights[0] = 0.5f;

                //if there are 2 layers defined
                if (layers > 2)
                {
                    //more influence at lower altitudes
                    splatWeights[1] = Mathf.Clamp01(data.heightmapHeight - height);
                }

                if (layers > 3 )
                {
                    //stronger on flatter surface
                    splatWeights[2] = 1.0f - Mathf.Clamp01(angle*angle/(data.heightmapHeight/2.0f));
                }

                if (layers > 4)
                {
                    //increases with height but only on surfaces with positive Z Axis
                    splatWeights[3] = height * Mathf.Clamp01(normal.z);
                }
               
                //no influence in the terrain (ROAD Texture) always last layer
                splatWeights[data.alphamapLayers - 1] = 0.0f; 
                
                float sum = splatWeights.Sum();

                //go over all terrain textures
                for (int i = 1; i < data.alphamapLayers-1; i++)
                {
                    //normalize
                    splatWeights[i] /= sum;

                    //set weight for correct texture
                    map[x ,z, i] = splatWeights[i];
                }
            }
        }

        data.alphamapResolution = TerrainSettings.HeightmapSize;
        data.SetAlphamaps(0,0,map);

    }

    /// <summary>
    /// Generate trees on the terrain using specified prefabs
    /// </summary>
    private void GenerateTrees(TerrainData data, CityData cityData)
    {
        //make sure prefabs have been specified
        if (data.treePrototypes.Length == 0)
        {
            Debug.LogWarning("Unable to generate trees\nPlease specify prefabs to use for trees.");
            return;
        }

        //create a list of tree instances
        int increment = 3;
        float terrainSize = _terrainWidth;

        //Spawn the trees on the terrain
        for (int x = 0; x < terrainSize; x += increment)
        {
            for (int z = 0; z < terrainSize; z += increment)
            {
                float unit = 1.0f/(terrainSize - 1);
                float normX = x*unit;
                float normZ = z*unit;
                float worldX = x*(terrainSize - 1);
                float worldZ = z*(terrainSize - 1);

                //randomizes the spread
                increment = RandomHelper.RandomInt(1, 3);
                var rng = RandomHelper.RandomInt(0, 100);

                //use different generation rules when inside of the city
                if (!CoordinateInsideOfBounds(x, z,20) && rng > 15)
                {
                    var noise = _treeNoise.FractalNoise2D(worldX, worldZ, 3, TerrainSettings.TreeFrequency, 1.0f);
                    var ht = data.GetInterpolatedHeight(normX, normZ);
                    if (noise > 0.2f && ht < TerrainSettings.TerrainHeight*0.4f)
                    {
                        var tree = new TreeInstance
                        {
                            heightScale = 1,
                            widthScale = 1,
                            prototypeIndex = RandomHelper.RandomInt(0, data.treePrototypes.Length),
                            lightmapColor = Color.white,
                            color = Color.white,
                            position = new Vector3(normX, ht, normZ)
                        };

                        //add the tree
                        Terrain.AddTreeInstance(tree);

                    }
                }
            }
            //send the tree instances to the terrain
            Terrain.Flush();
        }
    }

    private void GenerateDetail(TerrainData data, CityData cityData)
    {
        var detailSize = TerrainSettings.DetailMapSize;
        var terrainSize = (float)_terrainWidth;
        var ratio = terrainSize/detailSize;

        //each layer is drawn separately so if you have a lot of layers your draw calls will increase 
        int[,] detailMap0 = new int[detailSize, detailSize];
        int[,] detailMap1 = new int[detailSize, detailSize];
        int[,] detailMap2 = new int[detailSize, detailSize];

        for (int x = 0; x < detailSize; x++)
        {
            for (int z = 0; z < detailSize; z++)
            {
                detailMap0[z, x] = 0;
                detailMap1[z, x] = 0;
                detailMap2[z, x] = 0;

                float unit = 1.0f / (detailSize - 1);

                float normX = x * unit;
                float normZ = z * unit;

                // Get the steepness value at the normalized coordinate.
                float angle = data.GetSteepness(normX, normZ);

                // Steepness is given as an angle, 0..90 degrees. Divide
                // by 90 to get an alpha blending value in the range 0..1.
                float frac = angle / 90.0f;

                if (frac < 0.5f)
                {
                    float worldPosX = (x + (detailSize - 1)) * ratio;
                    float worldPosZ = (z + (detailSize - 1)) * ratio;

                    float noise = _detailNoise.FractalNoise2D(worldPosX, worldPosZ, 3, TerrainSettings.DetailFrequency, 1.0f);

                    if (noise > 0.0f)
                    {
                        float rnd = Random.value;
                        //Randomly select what layer to use
                        if (rnd < 0.33f)
                            detailMap0[z, x] = 1;
                        else if (rnd < 0.66f)
                            detailMap1[z, x] = 1;
                        else
                            detailMap2[z, x] = 1;
                    }
                }
            }
        }

        Terrain.terrainData.wavingGrassStrength = 0.4f;
        Terrain.terrainData.wavingGrassAmount = 0.2f;
        Terrain.terrainData.wavingGrassSpeed = 0.4f;
        Terrain.terrainData.wavingGrassTint = Color.white;
        Terrain.detailObjectDensity = 4.0f;
        Terrain.detailObjectDistance = 400.0f;
        Terrain.terrainData.SetDetailResolution(detailSize, 32);

       Terrain.terrainData.SetDetailLayer(0, 0, 0, detailMap0);
       Terrain.terrainData.SetDetailLayer(0, 0, 1, detailMap1);
        Terrain.terrainData.SetDetailLayer(0, 0, 2, detailMap2);
    }

    #endregion
    
    #region Textures
    /// <summary>
    /// Apply a specific texture at a specific location in the terrain
    /// </summary>
    private void ChangeTexture(Vector3 pos, int textureIndex, int size = 10,int spread = 4)
    { 
        var coord = WorldToTerrainCoordinate(pos);

        int alphaX = (int)coord.x;
        int alphaZ = (int)coord.z;

        ChangeTextureAtCoordinate(alphaX,alphaZ,textureIndex,spread);
    }

    private void ChangeTextureAtCoordinate(int alphaX, int alphaZ, int textureIndex, int spread)
    {
        for (int i = 0; i < Terrain.terrainData.alphamapLayers; i++)
        {
            int value = (textureIndex == i) ? 1 : 0;

            for (int x = -spread/2; x <= spread/2; ++x)
            {
                _alphadata[alphaZ + x, alphaX + x, i] = value;
            }
        }
    }

    private void ChangeTextureRoad(Road road,int spread, int size = 10)
    {
        //first texture of the splatmaps is the road
        var roadSplatIndex = 0;

        float moveForward = 0.1f;
        Vector3 start = road.Start.ToVector3();
        Vector3 end = road.End.ToVector3();
        float distance = Vector3.Distance(start, end);

        Vector3 currPos = Vector3.MoveTowards(start, end, moveForward);

        //Change Texture on start position
        ChangeTexture(start, roadSplatIndex, size, spread);

        //Change texture between the start and end position
        for (int i = 0; i <= distance/moveForward; i++)
        {
            ChangeTexture(currPos, roadSplatIndex, size, spread);
            currPos = Vector3.MoveTowards(currPos, end, moveForward);
        }

        //Change texture on end position
        ChangeTexture(end, roadSplatIndex, size, spread);
    }

    private void ChangeTextureBuildingBlock(BuildingSite building, int spread)
    {
        //var textureIndex = TerrainData.alphamapLayers - 1;

        //var go = (GameObject) building.UserData;
        //if (go == null)
        //{
        //    return;
        //}
        //var size = go.GetComponent<Collider>().bounds.size;

        //float halfWidth = size.x/2;
        //float halfLength = size.z;

        //float increment = 0.5f;

        //for (float x = -halfWidth; x < halfWidth; x+= increment)
        //{
        //    for (float z = -halfLength; z < halfLength; z+= increment)
        //    {
        //        ChangeTexture(new Vector3((float)building.X + x, 0, (float)building.Y + z),textureIndex,spread);
        //    }
        //}
    }

    public void ChangeDistrictTexture(District district)
    {
        var textureIndex = Terrain.terrainData.alphamapLayers - 2;
        float margin = 1;
        float minX = 99999f;
        float minZ = 99999f;
        float maxX = 0f;
        float maxZ = 0f;

        //find bounds of the district
        foreach (DistrictCell cell in district.Cells)
        {
            var cellPos = cell.SitePoint.ToVector3();

            if (cellPos.x > maxX)
            {
                maxX = cellPos.x;
            }

            if (cellPos.y > maxZ)
            {
                maxZ = cellPos.z;
            }

            if (cellPos.x < minX)
            {
                minX = cellPos.x;
            }

            if (cellPos.y < minZ)
            {
                minZ = cellPos.z;
            }
        }

        for (float x = minX; x <= maxX; x += margin)
        {
            for (float z = minZ; z <= maxZ; z += margin)
            {
                Vector3 currentPos = new Vector3(x, GetY(x, z),z);

                ChangeTexture(currentPos, textureIndex,10,1);
            }
        }
    }
    #endregion

    #region Helpers

    /// <summary>
    /// Create protoypes to be used for the terrain based on settings
    /// </summary>
    private void CreatePrototypes(TerrainData data)
    {
        //Create splatmaps from user textures
        var splatmaps = new List<SplatPrototype>();
        for (int i = 0; i < TerrainSettings.SplatMaps.Count; ++i)
        {
            var map = TerrainSettings.SplatMaps[i];
            if (map != null)
            {
                var splatmap = new SplatPrototype
                {
                    texture = map.Texture,
                    tileSize = new Vector2(map.TileSize, map.TileSize)
                };

                splatmaps.Add(splatmap);
            }
        }
        
        if (splatmaps.Count > 0)
        {
            data.splatPrototypes = splatmaps.ToArray();
        }

        //Create the tree prototypes from the settings
        List<TreePrototype> treePrototypes = new List<TreePrototype>();
        foreach (var prefab in TerrainSettings.Trees)
        {
            if (prefab != null)
            {
                TreePrototype treeProto = new TreePrototype();
                treeProto.prefab = prefab;
                treePrototypes.Add(treeProto);
            }
        }

        //set the tree prototypes to the terrain
        data.treePrototypes = treePrototypes.ToArray();

        //Grass Detail Textures
        var detailPrototypes = new List<DetailPrototype>();
        foreach (var grass in TerrainSettings.GrassTextures)
        {
            if (grass != null)
            {
                var detail = new DetailPrototype
                {
                    prototypeTexture = grass.Texture,
                    renderMode = DetailRenderMode.Grass,
                    healthyColor = grass.HealthyColor,
                    dryColor = grass.DryColor
                };

                detailPrototypes.Add(detail);
            }
        }

        data.detailPrototypes = detailPrototypes.ToArray();


    }

    public float GetY(float x, float z)
    {
        return Terrain.SampleHeight(new Vector3(x, 0, z));
    }

    private Vector3 WorldToTerrainCoordinate(Vector3 worldPos)
    {
        int size = TerrainSettings.TerrainScaleFactor/2;
        Vector3 scale = Terrain.terrainData.heightmapScale;

        var xPos = (int) ((worldPos.x - Terrain.transform.position.x)/scale.x) - size;
        var zPos = (int) ((worldPos.z - Terrain.transform.position.z)/scale.z) - size;

        return new Vector3(xPos, size, zPos);
    }

    private bool CoordinateInsideOfBounds(float x, float y, float margin = 0)
    {
        //x = 0
        float minWidth = (_terrainWidth / TerrainSettings.TerrainScaleFactor) - margin;
        float maxWidth = (_terrainWidth - (_terrainWidth / TerrainSettings.TerrainScaleFactor)) + margin;

        float minLength = (_terrainLength / TerrainSettings.TerrainScaleFactor) - margin;
        float maxLength = (_terrainLength - (_terrainLength / TerrainSettings.TerrainScaleFactor)) + margin;

        return (x > minWidth && x < maxWidth && y > minLength && y < maxLength);
    }


    #endregion
}
