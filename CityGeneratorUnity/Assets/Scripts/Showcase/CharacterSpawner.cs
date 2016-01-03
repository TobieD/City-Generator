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

        var pos = terrain.RandomPositionOnTerrain();
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
	
}
