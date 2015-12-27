using System.Collections.Generic;
using UnityEngine;

using Helpers;
using Voronoi;
using CityGenerator;


public static class ExtensionMethods
{
    public static Vector3 ToVector3(this Point p)
    {
        return new Vector3((float)p.X,0,(float)p.Y);
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


    public static Vector3 GetPrefabBounds(this GameObject prefab)
    {
        //Have to create the prefab

        if(prefab == null)
            return Vector3.one;

        var instance = (GameObject) Object.Instantiate(prefab);

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
            if(d.Type == DetailType.GameObject)
                delayedList.Add(d);

            else
            {
                newList.Add(d);
            }

        }

        newList.AddRange(delayedList);

        return newList;
    }
}