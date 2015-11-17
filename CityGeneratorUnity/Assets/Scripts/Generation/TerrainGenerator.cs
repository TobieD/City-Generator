
using System.Linq;
using UnityEngine;
using Voronoi;

public class TerrainGenerator
{

    SplatPrototype[] _splatmap;
    GameObject _terrain;

    float _width = 100;
    float _height = 100;

    TerrainData _terrainData;


    public void CreateTerrain(GenerationSettings settings, GameObject parent)
    {
        //Create default textures for the height difference
        CreateSplatmaps();

        //Create Terrain Data
        _terrainData = new TerrainData();
        var terrainScale = 4;
        float terrainHeight = 20;
        float maxHeight = 1500;

        //Terrain Width and Height
        _width = (float) settings.Width*terrainScale;
        _height = (float) settings.Length*terrainScale;



        //Size of the terrain
        _terrainData.size = new Vector3(_width, maxHeight, _height);
        _terrainData.heightmapResolution = 1;
        _terrainData.splatPrototypes = _splatmap;


        float[,] heightmap = new float[_terrainData.heightmapWidth, _terrainData.heightmapHeight];


        var widthRatio = _width/_terrainData.heightmapWidth;
        var lengthRatio = _height/_terrainData.heightmapHeight;

        var cityWidth = settings.Width;
        var cityLength = settings.Length;

        for (int x = 0; x < _terrainData.heightmapWidth; x++)
        {
            for (int y = 0; y < _terrainData.heightmapHeight; y++)
            {
                var w = x*widthRatio;
                var l = y*lengthRatio;

                if (w < cityWidth || l < cityLength)
                    
                {
                    //heightmap[x, y] = Random.Range(0.5f, 0.7f);
                }
                else
                {
                    
                }

                heightmap[x, y] = 0.5f;
            }
        }

        _terrainData.SetHeights(0, 0, heightmap);

        //Make Gameobject for terrain
        _terrain = new GameObject("Terrain");
        _terrain.transform.parent = parent.transform;

        //Position gameobject
        var posX = parent.transform.position.x - (_width/2);
        var posZ = parent.transform.position.z - (_height/2);

        var posY = parent.transform.position.y - maxHeight / 2;

        _terrain.transform.position = new Vector3(posX, posY, posZ);

        //Add terrain components
        var collider = _terrain.AddComponent<TerrainCollider>();
        var terrain = _terrain.AddComponent<Terrain>();

        collider.terrainData = _terrainData;
        terrain.terrainData = _terrainData;

        //Add water below the terrain
        CreateWaterPlane();

        CreateRoads();
    }

    public void CreateRoads()
    {
        var height = _terrainData.alphamapHeight;
        var width = _terrainData.alphamapWidth;

        var splatmapData = new float[width, height, _terrainData.alphamapLayers];

        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                //normalise x/y coordinates
                float yNormal = (float) y/(float) height;
                float xNormal = (float)x / (float)width;

                //sample height at this location
                var heightLoc = _terrainData.GetHeight(Mathf.RoundToInt(yNormal*height), Mathf.RoundToInt(xNormal*width));

                //calculate normal of terrain
                var normal = _terrainData.GetInterpolatedNormal(yNormal, xNormal);

                //steepness
                var steepness = _terrainData.GetSteepness(yNormal, xNormal);

                var splatWeights = new float[_terrainData.alphamapLayers];

                // Texture[0] has constant influence
                splatWeights[0] = 0.5f;

                // Texture[1] is stronger at lower altitudes
                splatWeights[1] = Mathf.Clamp01((height - heightLoc));

                // Texture[2] stronger on flatter terrain
                // Note "steepness" is unbounded, so we "normalise" it by dividing by the extent of heightmap height and scale factor
                // Subtract result from 1.0 to give greater weighting to flat surfaces
                splatWeights[2] = 1.0f - Mathf.Clamp01(steepness * steepness / (height / 5.0f));


                // Sum of all textures weights must add to 1, so calculate normalization factor from sum of weights
                float z = splatWeights.Sum();

                // Loop through each terrain texture
                for (int i = 0; i < _terrainData.alphamapLayers; i++)
                {

                    // Normalize so that sum of all texture weights = 1
                    splatWeights[i] /= z;

                    // Assign this point to the splatmap array
                    splatmapData[x, y, i] = splatWeights[i];
                }
            }
        }

        // Finally assign the new splatmap to the terrainData:
        _terrainData.SetAlphamaps(0, 0, splatmapData);
    }

    private void CreateSplatmaps()
    {
        _splatmap = new SplatPrototype[3];

        _splatmap[0] = new SplatPrototype();
        _splatmap[0].texture = Resources.Load<Texture2D>("Textures/Grass");

        _splatmap[1] = new SplatPrototype();
        _splatmap[1].texture = Resources.Load<Texture2D>("Textures/City");
        _splatmap[1].smoothness = 0;

        _splatmap[2] = new SplatPrototype();
        _splatmap[2].texture = Resources.Load<Texture2D>("Textures/Road");

        _splatmap[2] = new SplatPrototype();
        _splatmap[2].texture = Resources.Load<Texture2D>("Textures/Snow");




    }

    /// <summary>
    /// Create a simple plane with a texture below the terrain object to represent water
    /// </summary>
    private void CreateWaterPlane()
    {
        var waterObj = new GameObject("Water");

        var filter = waterObj.AddComponent<MeshFilter>();

        filter.mesh = TownBuilder.CreateGroundPlane((int)_width, (int)_height, _terrain.transform.position.y + 150);

        var render = waterObj.AddComponent<MeshRenderer>();
        render.material = Resources.Load < Material>("Material/Water");


        waterObj.transform.parent = _terrain.transform;
    }
}
