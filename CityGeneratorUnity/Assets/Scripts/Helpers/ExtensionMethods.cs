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

    public static Mesh CreateZoneBoundMesh(DistrictCell cell, float height)
    {
        var points = cell.Cell.Points;
        
        var normals = new Vector3[points.Count + 1];
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = Vector3.up;

        }

        //Vertices
        var vertices = new Vector3[points.Count + 1];
        Vector3 center = MathHelpers.FindCenteroidOfCell(cell.Cell).ToVector3();
        vertices[0] = center;
        for (int i = 1; i < vertices.Length -1; i++)
        {
            vertices[i] = points[i].ToVector3() - center;
        }

        //indices
        var indices = new int[points.Count * 3];
        for (int i = 0; i < points.Count -1; i++)
        {
            indices[i*3] = i + 2;
            indices[i*3 + 1] = 0;
            indices[i*3 + 2] = i + 1;
        }

        indices[(points.Count - 1)*3] = 1;
        indices[(points.Count - 1) * 3 + 1] = 0;
        indices[(points.Count - 1)*3 + 2] = points.Count;








        var mesh = new Mesh
        {
            name = "Zone Bound",
            vertices = vertices,
            normals = normals,
            triangles = indices
        
        };

        return mesh;
    }
}
