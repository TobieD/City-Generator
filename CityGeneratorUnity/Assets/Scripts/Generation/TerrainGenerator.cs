
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
}

/// <summary>
/// Settings for the terrain
/// </summary>
public class TerrainSettings
{
    //Settings
    public int HeightmapSize = 1025;
    public int AlphamapSize = 1025;
    public int TerrainHeight = 256;

    //Noise Settings
    public int GroundSeed = 0;
    public float GroundFrequency = 800.0f;
    public int MountainSeed = 0;
    public float MountainFrequency = 1200.0f;

    //prototypes
    public List<SplatTexture> SplatMaps = new List<SplatTexture>(2);

    public SplatTexture RoadTexture = new SplatTexture();
}

/// <summary>
/// Generate a terrain using a specified map
/// </summary>
public class TerrainGenerator
{
    //TerrainSettings
    public TerrainSettings TerrainSettings;

    //width and height of the terrain
    private int _terrainWidth = 300;
    private int _terrainLength = 300;

    Terrain _terrain;

    //textures used based on height
    private SplatPrototype[] _splatPrototypes;
    private SplatPrototype _roadSplatPrototype;

    //Perlin noise for terrain
    PerlinNoise _groundNoise, _mountainNoise;

    //stores texture information of the terrain(splatmaps)
    private float[,,] _alphadata;

    /// <summary>
    /// Create terrain using the bounds of the voronoi diagram
    /// </summary>
    public void BuildTerrain(GenerationSettings genSettings, TerrainSettings terrainSettings, GameObject parent)
    {
        //store settings
        TerrainSettings = terrainSettings;

        //make sure all settings are correct
        if (!CreateSettings(genSettings))
        {
            Debug.LogError("Unable to generate terrain!");
            return;
        }

        //create heightmap using perlin noise
        float[,] heightmap = new float[TerrainSettings.HeightmapSize, TerrainSettings.HeightmapSize];
        FillHeightmap(heightmap);
        
        //Create terrain data
        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = TerrainSettings.HeightmapSize;
        terrainData.SetHeights(0,0,heightmap);
        terrainData.size = new Vector3(_terrainWidth, TerrainSettings.TerrainHeight, _terrainLength);

        //make sure a splat map is defined
        if (_splatPrototypes[0].texture != null)
        {
            terrainData.splatPrototypes = _splatPrototypes;
        }

        //fill the textures of the terrain with the splatmaps
        FillAlphamap(terrainData);

        //Create terrain from terrain data
        var terrainObj = Terrain.CreateTerrainGameObject(terrainData);
        terrainObj.transform.parent = parent.transform;
        terrainObj.transform.position = new Vector3(-(_terrainWidth/2),0,-(_terrainLength/2));
        

        //Access terrain data and set terrain data
        _terrain = terrainObj.GetComponent<Terrain>();

        _terrain.terrainData = terrainData;
        _terrain.castShadows = false; //better fps
    }

    public void ApplyRoadData(CityData cityData)
    {
        var terrainData = _terrain.terrainData;

        _alphadata = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);

        foreach (var district in cityData.Districts)
        {
            foreach (var cell in district.Cells)
            {
                foreach (var road in cell.Roads)
                {
                    ChangeTextureRoad(road);
                }
            }
        }

        terrainData.SetAlphamaps(0,0, _alphadata);
    }

    /// <summary>
    /// Are settings correctly set and fix them if not
    /// </summary>
    private bool CreateSettings(GenerationSettings generationSettings)
    {
        //Set up noise
        _groundNoise = new PerlinNoise(TerrainSettings.GroundSeed);
        _mountainNoise = new PerlinNoise(TerrainSettings.MountainSeed);

        //Create prototypes for terrain(textures, trees,...)
        CreatePrototypes();

        if (!Mathf.IsPowerOfTwo(TerrainSettings.HeightmapSize - 1))
        {
            Debug.LogWarning("Terrain Generator:: Height map size must be pow2 + 1!");
            TerrainSettings.HeightmapSize = Mathf.ClosestPowerOfTwo(TerrainSettings.HeightmapSize) + 1;
        }
        
        //Terrain size
        _terrainWidth = (int)generationSettings.Width * 2;
        _terrainLength = (int)generationSettings.Length* 2;

        if (_splatPrototypes.Length < 1)
        {
            Debug.LogWarning("Terrain Generator:: No splatmaps set!");
            return false;
        }

        //everything is valid
        return true;
    }

    /// <summary>
    /// Fill the height map using perlin noise
    /// </summary>
    private void FillHeightmap(float[,] heightmap)
    {
        var ratio = (float) _terrainWidth/(float)TerrainSettings.HeightmapSize;
        for (int x = 0; x < TerrainSettings.HeightmapSize; x++)
        {
            for (int z = 0; z < TerrainSettings.HeightmapSize; z++)
            {
                var worldPosX = x * ratio;
                var worldPosZ = z * ratio; 

                float height = 0;
                var mountains = Mathf.Max(0.0f, _mountainNoise.FractalNoise2D(worldPosX, worldPosZ, 6, TerrainSettings.MountainFrequency, 0.7f));
                var plain = _groundNoise.FractalNoise2D(worldPosX, worldPosZ, 4, TerrainSettings.GroundFrequency, 0.1f);

                height = plain + mountains;
                height = 0;

                heightmap[x, z] = height;
            }
        }
    }

    /// <summary>
    /// Use splatmaps to fill the alpha map of the terrain based on the height
    /// </summary>
    private void FillAlphamap(TerrainData data)
    {
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
                    splatWeights[2] = 1.0f - Mathf.Clamp01(angle*angle/(data.heightmapHeight/5.0f));
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
                for (int i = 0; i < data.alphamapLayers; i++)
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
    /// Create protoypes to be used for the terrain based on settings
    /// <remarks> only 5 splatmaps can be used(4 for terrain, 1 for road)</remarks>
    /// </summary>
    private void CreatePrototypes()
    {
        //Create splatmaps from user textures
        _splatPrototypes = new SplatPrototype[TerrainSettings.SplatMaps.Count + 1];
        for(int i = 0; i < TerrainSettings.SplatMaps.Count; ++i)
        {
            var map = TerrainSettings.SplatMaps[i];

            _splatPrototypes[i] = new SplatPrototype
            {
                texture = map.Texture,
                tileSize = new Vector2(map.TileSize, map.TileSize)
            };
        }

        //Create a splat for the road at the end of the splat prototypes
        _roadSplatPrototype = new SplatPrototype
        {
            texture = TerrainSettings.RoadTexture.Texture,
            tileSize = new Vector2(TerrainSettings.RoadTexture.TileSize, TerrainSettings.RoadTexture.TileSize)
        };

        _splatPrototypes[_splatPrototypes.Length -1] = _roadSplatPrototype;
    }

    /// <summary>
    /// Apply a specific texture at a specific location in the terrain
    /// </summary>
    private void ChangeTexture(Vector3 pos, int textureIndex, int size = 10,int spread = 2)
    {
        var alphaMapsNb = _terrain.terrainData.alphamapLayers;

        var coord = WorldToTerrainCoordinate(pos);

        int alphaX = (int)coord.x;
        int alphaZ = (int)coord.z;

        int halfSpread = spread/2;
        for (int i = 0; i < alphaMapsNb; i++)
        {
            var value = (textureIndex == i) ? 1.0f : 0.0f;
            _alphadata[alphaZ, alphaX, i] = value;

            for(int x = 0; x < halfSpread; ++x)
            {
                _alphadata[alphaZ- x, alphaX- x, i] = value;
                _alphadata[alphaZ+ x, alphaX+ x, i] = value;
            }

        }
    }

    private void ChangeTextureRoad(Road road, int size = 10)
    {
        var roadSplatIndex = _terrain.terrainData.alphamapLayers;
        
        float moveForward = 0.1f;
        Vector3 start = road.RoadLine.Start.ToVector3();
        Vector3 end = road.RoadLine.End.ToVector3();
        float distance = Vector3.Distance(start, end);

        Vector3 currPos = Vector3.MoveTowards(start, end, moveForward);

        //Change Texture on start position
        ChangeTexture(start, roadSplatIndex, size);

        //Change texture between the start and end position
        for (int i = 0; i <= distance/moveForward; i++)
        {
            ChangeTexture(currPos, roadSplatIndex, size);
            currPos = Vector3.MoveTowards(currPos, end, moveForward);
        }

        //Change texture on end position
        ChangeTexture(end, roadSplatIndex, size);
    }

    private Vector3 WorldToTerrainCoordinate(Vector3 worldPos)
    {
        int size = 10;
        Vector3 scale = _terrain.terrainData.heightmapScale;

        int xPos = (int) ((worldPos.x - _terrain.transform.position.x)/scale.x - (size/2));
        int zPos = (int)((worldPos.z - _terrain.transform.position.z) / scale.z - (size / 2));

        return new Vector3(xPos,size,zPos);

    }

}
