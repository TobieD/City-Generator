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

}

public static class TownBuilder
{
    public static Mesh CreateGroundPlane(int width, int height, float z = -50.0f)
    {

        float halfWidth = width/2.0f;
        float halfHeight = height / 2.0f;

        //Vertices
        var verticesTemp = new Vector3[4];
        verticesTemp[0] = new Vector3(-halfWidth ,z ,halfHeight);
        verticesTemp[1] = new Vector3(halfWidth  ,z ,halfHeight);
        verticesTemp[2] = new Vector3(halfWidth  ,z , -halfHeight);
        verticesTemp[3] = new Vector3(-halfWidth ,z , -halfHeight);

        //normals
        var normalsTemp = new Vector3[4];
        for (int i = 0; i < 4; ++i)
        {
            normalsTemp[i] = Vector3.up;
        }

        var indices = new int[] {0,1,2,0,2,3};

        var uvs = new[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };

        //Create Mesh
        Mesh mesh = new Mesh
        {
            name = "Plane",
            vertices = verticesTemp,
            normals = normalsTemp,
            triangles = indices,
            uv = uvs
        };

        mesh.Optimize();
        mesh.RecalculateNormals();

        return mesh;
    }
   
    public static Mesh CreateRoadMesh(Line line, GameObject parent, float width)
    {
        var halfWidth = width / 2;
        var y = parent.transform.position.y + 7;

        var p1X = (float)line.Start.X;
        var p1Y = (float)line.Start.Y;

        var p2X = (float)line.End.X;
        var p2Y = (float)line.End.Y;

        var verticesTemp = new Vector3[4]
        {
            new Vector3(p1X + halfWidth, y, p1Y + halfWidth),
            new Vector3(p1X - halfWidth, y, p1Y - halfWidth),
            new Vector3(p2X - halfWidth, y, p2Y - halfWidth),
            new Vector3(p2X + halfWidth, y, p2Y + halfWidth)
        };

        //normals
        var normalsTemp = new Vector3[4];
        for (int i = 0; i < 4; ++i)
        {
            normalsTemp[i] = Vector3.up;
        }

        //indices
        var indices = new int[] { 0, 1, 2, 0, 2, 3 };

        //Create Mesh
        Mesh mesh = new Mesh
        {
            name = "Plane",
            vertices = verticesTemp,
            normals = normalsTemp,
            triangles = indices
        };


        return mesh;
    }
}
