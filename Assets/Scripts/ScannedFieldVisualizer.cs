using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HullDelaunayVoronoi.Delaunay;
using HullDelaunayVoronoi.Primitives;

public class ScannedFieldVisualizer : MonoBehaviour
{
    private List<Vector3> vertices;
    private List<Vector3> normals = new List<Vector3>();
    private List<Vector2> uvs = new List<Vector2>();
    private List<int> indices = new List<int>();
    private List<Color> colors = new List<Color>();

    /// <summary>
    /// The Unity Awake() method.
    /// </summary>
    public void Awake()
    {
    }

    public void Update()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// Initializes the DetectedPlaneVisualizer with a DetectedPlane.
    /// </summary>
    /// <param name="plane">The plane to vizualize.</param>
    public void Initialize(List<Vector3> triangleVertices)
    {
        vertices = triangleVertices;

        // m_MeshRenderer.material.SetColor("_GridColor", new Color(238f / 255f, 238f / 255f, 209f / 255f, 1f));
        // m_MeshRenderer.material.SetFloat("_UvRotation", Random.Range(0.0f, 360.0f));

        _UpdateMeshIfNeeded();
    }

    /// <summary>
    /// Update mesh with a list of Vector3 and plane's center position.
    /// </summary>
    private void _UpdateMeshIfNeeded()
    {
        colors.Clear();
        colors.Add(new Color(238f / 255f, 238f / 255f, 209f / 255f, 1f));
        colors.Add(new Color(238f / 255f, 238f / 255f, 209f / 255f, 1f));
        colors.Add(new Color(238f / 255f, 238f / 255f, 209f / 255f, 1f));

        indices.Clear();
        indices.Add(0);
        indices.Add(1);
        indices.Add(2);

        Vector3 normal = Vector3.Cross(vertices[1] - vertices[0], vertices[2] - vertices[0]);
        normals.Clear();
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);

        uvs.Clear();
        uvs.Add(new Vector2(0.0f, 0.0f));
        uvs.Add(new Vector2(0.0f, 0.0f));
        uvs.Add(new Vector2(0.0f, 0.0f));

        Mesh chunkMesh = new Mesh();
        chunkMesh.vertices = vertices.ToArray();
        chunkMesh.uv = uvs.ToArray();
        chunkMesh.triangles = indices.ToArray();
        chunkMesh.normals = normals.ToArray();

        this.GetComponent<MeshFilter>().mesh = chunkMesh;
        this.GetComponent<MeshCollider>().sharedMesh = chunkMesh;
        this.transform.parent = transform;
    }

}
