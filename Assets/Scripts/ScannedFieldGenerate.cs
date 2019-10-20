using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using GoogleARCore;
using HullDelaunayVoronoi.Delaunay;
using HullDelaunayVoronoi.Primitives;
using DrawingTool;

public class ScannedFieldGenerate : MonoBehaviour
{
    // AR parameter
    public Camera Cam;
    public List<Vector3> PointClouds = new List<Vector3>();
    public List<Vector3> PointClouds2D = new List<Vector3>();
    // The delaunay mesh

    private DelaunayTriangulation2 delaunay;
    public Text PointCloudText;
    private string TriangleLog;
    private string PointCloudLog;
    private string triangleFilePath;
    private string pointcloudFilePath;
    // Prefab which is generated for each chunk of the mesh.
    public Transform chunkPrefab = null;
    public GameObject sphere;
    private bool draw = true;

    void Start()
    {
        // storage/android/data/MYAPP/file/TriangleLog.txt
        triangleFilePath = Application.persistentDataPath + "/TriangleLog.txt";
        pointcloudFilePath = Application.persistentDataPath + "/PointCloudLog.txt";

        if (File.Exists(triangleFilePath))
        {
            try
            {
                File.Delete(triangleFilePath);
                Debug.Log("file delete");
            }
            catch (System.Exception e)
            {
                Debug.LogError("cannot delete file");
            }
        }
        if (File.Exists(pointcloudFilePath))
        {
            try
            {
                File.Delete(pointcloudFilePath);
                Debug.Log("file delete");
            }
            catch (System.Exception e)
            {
                Debug.LogError("cannot delete file");
            }
        }
        //Generate();
        //MakeMesh();
    }

    // Update is called once per frame
    void Update()
    {
        // If ARCore is not tracking, clear the caches and don't update.
        if (Session.Status != SessionStatus.Tracking)
        {
            return;
        }
        ClearListPoints();

        if (draw)
        {
            AddAllPointsToList();
            if (PointClouds.Count > 3)
            {
                WorldToScreenCoordinate();
                DelaunayTriangulation();
                DrawPointsAndLines();
                UpdateMesh();
                draw = false;
            }
        }
    }

    private void OnDisable()
    {
        ClearListPoints();
    }

    public void ClearListPoints()
    {
        PointClouds.Clear();
        PointClouds2D.Clear();
    }

    private void AddAllPointsToList()
    {

        if (Frame.PointCloud.IsUpdatedThisFrame && Frame.PointCloud.PointCount >= 10)
        {
            for (int i = 0; i < Frame.PointCloud.PointCount; i++)
            {
                PointCloudPoint point = Frame.PointCloud.GetPointAsStruct(i);
                PointClouds.Add(point.Position);
            }
            PointCloudLog = "PointClouds:" + PointClouds.Count + "\n";
            PointCloudText.text = "PointClouds:" + PointClouds.Count + "\n";
        }
    }

    private void WorldToScreenCoordinate()
    {
        for (int i = 0; i < PointClouds.Count; i++)
        {
            Vector3 point = Cam.WorldToScreenPoint(PointClouds[i]);
            PointClouds2D.Add(new Vector3(point.x, point.y, point.z));

            PointCloudLog += "(" + PointClouds[i].x + ", " + PointClouds[i].y + ", " + PointClouds[i].z + "); ";
            PointCloudLog += "(" + point.x + ", " + point.y + ", " + point.z + "); \n";
        }
        // WriteToFile(pointcloudFilePath, PointCloudLog);
    }

    private void DelaunayTriangulation()
    {
        List<Vertex2> vertices = new List<Vertex2>();
        for (int i = 0; i < PointClouds2D.Count; i++)
        {
            Vertex2 point = new Vertex2(PointClouds2D[i].x, PointClouds2D[i].y);
            point.SetIDAndDepth(i + 1, PointClouds2D[i].z);
            vertices.Add(point);
        }
        delaunay = new DelaunayTriangulation2();
        delaunay.Generate(vertices);

    }

    public List<Vector3> GetWorldCoordinate(Simplex<Vertex2> points)
    {
        List<Vector3> vertices = new List<Vector3>();
        vertices.Add(Cam.ScreenToWorldPoint(new Vector3(points.Vertices[0].X, points.Vertices[0].Y, points.Vertices[0].depth)));
        vertices.Add(Cam.ScreenToWorldPoint(new Vector3(points.Vertices[1].X, points.Vertices[1].Y, points.Vertices[1].depth)));
        vertices.Add(Cam.ScreenToWorldPoint(new Vector3(points.Vertices[2].X, points.Vertices[2].Y, points.Vertices[2].depth)));

        return vertices;
    }

    public void UpdateMesh()
    {
        if (delaunay == null || delaunay.Cells.Count == 0 || delaunay.Vertices.Count == 0)
        {
            return;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();
        int count = 0;
        int triangleCount = 0;

        TriangleLog += "TriangleCount:" + delaunay.Cells.Count + "\nTriangleID:\n";
        foreach (DelaunayCell<Vertex2> cell in delaunay.Cells)
        {
            List<Vector3> triangleVertices = GetWorldCoordinate(cell.Simplex);

            TriangleLog += (count + 1) + ", VerTicesID:";
            TriangleLog += cell.Simplex.Vertices[0].pointID + ", " + cell.Simplex.Vertices[1].pointID + ", " + cell.Simplex.Vertices[2].pointID + "\n";
            TriangleLog += "x:" + triangleVertices[0].x + ", y:" + triangleVertices[0].y + ", z:" + triangleVertices[0].z + "\n";
            TriangleLog += "x:" + triangleVertices[1].x + ", y:" + triangleVertices[1].y + ", z:" + triangleVertices[1].z + "\n";
            TriangleLog += "x:" + triangleVertices[2].x + ", y:" + triangleVertices[2].y + ", z:" + triangleVertices[2].z + "\n";

            triangles.Add(count);
            triangles.Add(count + 1);
            triangles.Add(count + 2);

            vertices.Add(triangleVertices[0]);
            vertices.Add(triangleVertices[1]);
            vertices.Add(triangleVertices[2]);

            Vector3 normal = Vector3.Cross(triangleVertices[1] - triangleVertices[0], triangleVertices[2] - triangleVertices[0]);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);

            uvs.Add(new Vector2(0.0f, 0.0f));
            uvs.Add(new Vector2(0.0f, 0.0f));
            uvs.Add(new Vector2(0.0f, 0.0f));

            triangleCount += 1;
            count += 3;
        }

        Mesh chunkMesh = new Mesh();
        chunkMesh.vertices = vertices.ToArray();
        chunkMesh.uv = uvs.ToArray();
        chunkMesh.triangles = triangles.ToArray();
        chunkMesh.normals = normals.ToArray();

        Transform chunk = Instantiate<Transform>(chunkPrefab, transform.position, transform.rotation);
        chunk.GetComponent<MeshFilter>().mesh = chunkMesh;
        chunk.GetComponent<MeshCollider>().sharedMesh = chunkMesh;
        chunk.transform.parent = transform;

        TriangleLog += "CreateTraingle:" + triangleCount + "\n";
        TriangleLog += "finish meshing\n";
        // WriteToFile(triangleFilePath, TriangleText.text);

    }

    public void DrawPointsAndLines()
    {
        for (int i = 0; i < PointClouds.Count; i++)
        {
            Instantiate(sphere, PointClouds[i], Quaternion.identity);
        }


        foreach (DelaunayCell<Vertex2> cell in delaunay.Cells)
        {
            List<Vector3> triangleVertices = GetWorldCoordinate(cell.Simplex);
            LineDrawer lineDrawer1 = new LineDrawer();
            LineDrawer lineDrawer2 = new LineDrawer();
            LineDrawer lineDrawer3 = new LineDrawer();
            lineDrawer1.DrawLineInGameView(triangleVertices[0], triangleVertices[1], new Color(248f/255f, 197f/255f, 48f/255f, 1.0f));
            lineDrawer2.DrawLineInGameView(triangleVertices[1], triangleVertices[2], new Color(1f, 91f/255f, 0f, 1.0f));
            lineDrawer3.DrawLineInGameView(triangleVertices[2], triangleVertices[0], Color.red);
        }
    }

    public void WriteToFile(string filePath, string log)
    {
        try
        {
            StreamWriter fileWriter = new StreamWriter(filePath, true);
            fileWriter.Write(log);
            fileWriter.Close();
        }
        catch (System.Exception e)
        {
            Debug.LogError("cannot write into file");
        }
    }
}

