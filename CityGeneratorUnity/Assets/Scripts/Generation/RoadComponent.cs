using UnityEngine;
using Voronoi;

/// <summary>
/// Creates a planar mesh from Point A to Point B
/// </summary>
public class RoadComponent : MonoBehaviour
{
    private Line _line;

	// Use this for initialization
	void Start ()
	{
        //_line = new Line(Point.Zero, new Point(20,20));
	    //Build();
	}
	
	// Update is called once per frame
	void Update ()
    {
	    //Debug.DrawLine(_line.Start.ToVector3(),_line.End.ToVector3());
	}
    
    public void SetRoadData(Line line)
    {
        _line = line;
        Build();
    }

    private void Build()
    {
        //position the game object in the middle point of the line

        //Create Road mesh
        var meshfilter = gameObject.AddComponent<MeshFilter>();
        meshfilter.mesh = CreateRoadMesh();

        //apply road material to the mesh
        var renderer = gameObject.AddComponent<MeshRenderer>();

        var asset = "Material/road";
        var mat = Resources.Load<Material>(asset);

        renderer.material = mat;
    }

    private Mesh CreateRoadMesh()
    {
        var roadWidth = 15.0f;
        var halfWidth = roadWidth/2;
        var y = transform.position.y;

        var p1X = (float)_line.Start.X;
        var p1Y = (float)_line.Start.Y;

        var p2X = (float)_line.End.X;
        var p2Y = (float)_line.End.Y;

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
