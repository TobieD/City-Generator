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

        var pos = RandomPositionOnTerrain(terrain);
        pos.y += FallHeight;

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

    private Vector3 RandomPositionOnTerrain(Terrain terrain, int offset = 0)
    {
        var terrainPos = terrain.transform.position;
        var size = terrain.terrainData.size;

        var pos = Vector3.zero;
        pos.x = Random.Range(terrainPos.x + offset, terrainPos.x + size.x - offset);
        pos.z = Random.Range(terrainPos.z + offset, terrainPos.z + size.z - offset);

        pos.y = terrain.SampleHeight(pos);

        return pos;
    }


}
