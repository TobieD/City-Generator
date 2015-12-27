using UnityEngine;
using System.Collections;
using Helpers;

/// <summary>
/// Spawns the character on the terrain
/// </summary>
public class CharacterSpawner : MonoBehaviour
{

    public float FallHeight = 5f;

	// Use this for initialization
	void Start ()
    { 

        RandomizeSpawn();

    }

    private void RandomizeSpawn()
    {
        var terrain = GameObject.Find("tile1x1 (City)").GetComponent<Terrain>();


        if (terrain == null)
        {
            return;;
        }


        //random X and Z coordinate and sample height
        var randX = Random.Range(terrain.transform.position.x, terrain.transform.position.x + terrain.terrainData.size.x);
        var randZ = Random.Range(terrain.transform.position.z, terrain.transform.position.z + terrain.terrainData.size.z);

        var pos = new Vector3(randX, 0, randZ);
        pos.y = terrain.SampleHeight(pos) + FallHeight;

        //move gameobject
        gameObject.transform.position = pos;
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RandomizeSpawn();
        }
    }
	
}
