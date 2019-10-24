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
    public int limitDistance;
    // The delaunay mesh

    private DelaunayTriangulation2 delaunay;
    public Text PointCloudText;
    private string TriangleLog = "";
    private string PointCloudLog = "";
    private string DestroyLog = "";
    private string triangleFilePath;
    private string pointcloudFilePath;
    private string destroyFilePath;
    // Prefab which is generated for each chunk of the mesh.
    public Transform chunkPrefab = null;
    public GameObject sphere;

    private int turn = 10;
    private bool draw = true;
    private int triangleCount = 1;

    void Start()
    {
        // storage/android/data/MYAPP/file/TriangleLog.txt
        triangleFilePath = Application.persistentDataPath + "/TriangleLog.txt";
        pointcloudFilePath = Application.persistentDataPath + "/PointCloudLog.txt";
        destroyFilePath = Application.persistentDataPath + "/DestroyLog.txt";


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
        if (File.Exists(destroyFilePath))
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
            if (Frame.PointCloud.IsUpdatedThisFrame && Frame.PointCloud.PointCount >= 10)
            {
                AddAllPointsToList();
                WorldToScreenCoordinate();
                DelaunayTriangulation();
                // DrawPointsAndLines();
                UpdateMesh();
                draw = false;
            }
        }
        if(turn % 30 == 0)
        {
            draw = true;
        }
        turn += 1;
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
        DestroyLog = "";
        if (Frame.PointCloud.IsUpdatedThisFrame && Frame.PointCloud.PointCount >= 10)
        {
            for (int i = 0; i < Frame.PointCloud.PointCount; i++)
            {
                PointCloudPoint point = Frame.PointCloud.GetPointAsStruct(i);               
                // if the distance from point cloud to camera is less than limitDistance meter
                if (Vector3.Distance(point.Position, Cam.transform.position) < limitDistance)
                {
                    PointClouds.Add(point.Position);
                }
                CamRaycast(point.Position);
            }
            WriteToFile(destroyFilePath, DestroyLog);
        }
    }

    private void WorldToScreenCoordinate()
    {
        for (int i = 0; i < PointClouds.Count; i++)
        {
            Vector3 point = Cam.WorldToScreenPoint(PointClouds[i]);
            PointClouds2D.Add(new Vector3(point.x, point.y, point.z));
        }
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
        // positive if points are CCW negative if they're CW
        if((points.Vertices[1].X - points.Vertices[0].X) * (points.Vertices[2].Y - points.Vertices[0].Y)
            - (points.Vertices[2].X - points.Vertices[0].X) * (points.Vertices[1].Y - points.Vertices[0].Y) < 0)
        {
            vertices.Add(Cam.ScreenToWorldPoint(new Vector3(points.Vertices[0].X, points.Vertices[0].Y, points.Vertices[0].depth)));
            vertices.Add(Cam.ScreenToWorldPoint(new Vector3(points.Vertices[1].X, points.Vertices[1].Y, points.Vertices[1].depth)));
            vertices.Add(Cam.ScreenToWorldPoint(new Vector3(points.Vertices[2].X, points.Vertices[2].Y, points.Vertices[2].depth)));
        }
        else
        {
            vertices.Add(Cam.ScreenToWorldPoint(new Vector3(points.Vertices[0].X, points.Vertices[0].Y, points.Vertices[0].depth)));
            vertices.Add(Cam.ScreenToWorldPoint(new Vector3(points.Vertices[2].X, points.Vertices[2].Y, points.Vertices[2].depth)));
            vertices.Add(Cam.ScreenToWorldPoint(new Vector3(points.Vertices[1].X, points.Vertices[1].Y, points.Vertices[1].depth)));
        }

        return vertices;
    }

    public void UpdateMesh()
    {
        if (delaunay == null || delaunay.Cells.Count == 0 || delaunay.Vertices.Count == 0)
        {
            return;
        }

        foreach (DelaunayCell<Vertex2> cell in delaunay.Cells)
        {           
            List<Vector3> triangleVertices = GetWorldCoordinate(cell.Simplex);
            Transform chunk = Instantiate<Transform>(chunkPrefab, transform.position, transform.rotation);
            chunk.GetComponent<ScannedFieldVisualizer>().Initialize(triangleCount, triangleVertices);
            triangleCount += 1;
        }
    }

    private void CamRaycast(Vector3 destination)
    {
        Ray ray = Cam.ScreenPointToRay(Cam.WorldToScreenPoint(destination));
        RaycastHit hit;
        if(Physics.Raycast(ray, out hit))
        {
            PointCloudText.text = "" + hit.transform.name;
            if(hit.distance < Vector3.Distance(destination, Cam.transform.position) && Vector3.Dot(Cam.transform.forward, hit.normal) < 0) // && 面朝向camera
            {
                // destroy the mesh that raycast collide
                DestroyLog += "Destroy " + hit.transform.name + "\n";
                Destroy(hit.transform.gameObject);
                
            }
            else if(hit.distance > Vector3.Distance(destination, Cam.transform.position) && Vector3.Dot(Cam.transform.forward, hit.normal) < 0) // && 面朝向camera
            {
                // remove the pointcloud that already have mesh behind it
                DestroyLog += "Destroy PointCloud (" + PointClouds[PointClouds.Count - 1].x + "," + PointClouds[PointClouds.Count - 1].y + "," + PointClouds[PointClouds.Count - 1].z + ")\n";
                PointClouds.RemoveAt(PointClouds.Count - 1);
            }
        }
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

