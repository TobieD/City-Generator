using System;
using System.Collections.Generic;
using Tools.TownGenerator;
using UnityEngine;

using Voronoi;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;


namespace Tools.Extensions
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Convert a 2D point of the voronoi library to a Vector3
        /// </summary>
        public static Vector3 ToVector3(this Point p)
        {
            return new Vector3((float)p.X, 0, (float)p.Y);
        }

        /// <summary>
        /// Finds or create a game object with the given name
        /// </summary>
        public static GameObject FindGameObject(string objectName)
        {
            //find the object
            var go = GameObject.Find(objectName);

            //when it is found destroy it, this will also destroy its child objects.
            if (go != null)
            {
                Object.DestroyImmediate(go);
            }

            //create road object as parent
            return new GameObject(objectName);
        }

        /// <summary>
        /// Get the bounds of a prefab
        /// </summary>
        public static Vector3 GetPrefabBounds(this GameObject prefab)
        {
            //Have to create the prefab
            if (prefab == null)
                return Vector3.one;

            var instance = (GameObject)Object.Instantiate(prefab);

            //Get Actual size of the prefab
            var prefabRenderer = instance.GetComponentInChildren<MeshRenderer>();
            if (prefabRenderer == null)
            {
                Debug.LogError("Prefab has no render component on it or its children\nUnable to get bounds!");
                return Vector3.zero;
            }

            var bounds = prefabRenderer.bounds.size;

            Object.DestroyImmediate(instance);

            return bounds;
        }

        /// <summary>
        /// Make is easier to set a game objects parent
        /// </summary>
        public static void SetParent(this GameObject go, GameObject parent)
        {
            go.transform.parent = parent.transform;
        }

        public static List<DetailObject> SortByType(this List<DetailObject> details)
        {
            var newList = new List<DetailObject>();
            var delayedList = new List<DetailObject>();

            foreach (var d in details)
            {
                if (d.Type == DetailType.GameObject)
                    delayedList.Add(d);

                else
                {
                    newList.Add(d);
                }

            }

            newList.AddRange(delayedList);

            return newList;
        }

        public static Vector3 RandomPositionOnTerrain(this Terrain terrain, int offset = 0)
        {
            var terrainPos = terrain.transform.position;
            var size = terrain.terrainData.size;

            var pos = Vector3.zero;
            pos.x = Random.Range(terrainPos.x + offset, terrainPos.x + size.x - offset);
            pos.z = Random.Range(terrainPos.z + offset, terrainPos.z + size.z - offset);

            pos.y = terrain.SampleHeight(pos);

            return pos;
        }

        /// <summary>
        /// Converts world position to a position on the terrain
        /// </summary>
        public static Vector3 WorldToTerrainCoordinates(this Terrain terrain, Vector3 world)
        {
            var convertedPos = Vector3.zero;
            var terrainPos = terrain.transform.position;

            //for some reason terrain pos coordinates need to be swapped
            convertedPos.x = ((world.x - terrainPos.z) / terrain.terrainData.size.x);
            convertedPos.z = ((world.z - terrainPos.x) / terrain.terrainData.size.z);

            return convertedPos;
        }

        public static Vector3 WorldToTerrainMapCoordinates(this Terrain terrain, Vector3 world)
        {
            var convertedPos = Vector3.zero;
            //var terrainPos = terrain.WorldToTerrainCoordinates(world);

            ////for some reason terrain pos coordinates need to be swapped
            //convertedPos.x = terrainPos.x*terrain.terrainData.heightmapHeight;
            //convertedPos.z = terrainPos.z*terrain.terrainData.heightmapWidth;

            var heightScale = terrain.terrainData.heightmapScale;
            convertedPos.x = (int)((world.x - terrain.transform.position.x) / heightScale.x);
            convertedPos.z = (int)((world.z - terrain.transform.position.z) / heightScale.z);

            return convertedPos;
        }

        public static Vector3 TerrainToWorldCoordinates(this Terrain terrain, Vector3 terrainPos)
        {
            var convertedPos = Vector3.zero;

            //for some reason terrain pos coordinates need to be swapped
            convertedPos.x = terrain.transform.position.x + terrainPos.x * terrain.terrainData.size.x;
            convertedPos.z = terrain.transform.position.z + terrainPos.z * terrain.terrainData.size.z;


            convertedPos.y = terrain.SampleHeight(convertedPos);

            return convertedPos;
        }

        public static Vector3 TerrainMapToWorldCoordinates(this Terrain terrain, int x, int z)
        {
            var convertedPos = Vector3.zero;

            //for some reason terrain pos coordinates need to be swapped
            convertedPos.x = x / terrain.terrainData.heightmapWidth;
            convertedPos.z = z / terrain.terrainData.heightmapHeight;

            return terrain.TerrainToWorldCoordinates(convertedPos);

        }

    }


    public class Singleton<T>
    {
        private static T _instance = default(T);

        public static T GetInstance()
        {
            if (_instance == null)
            {
                _instance = (T)Activator.CreateInstance(typeof(T), new object[] { });
            }

            return _instance;
        }
    }


}
