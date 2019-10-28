using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using GoogleARCore;
using HullDelaunayVoronoi.Delaunay;
using HullDelaunayVoronoi.Primitives;
using DrawingTool;
using DataStructures.ViliWonka.KDTree;

public class ScannedFieldGenerate : MonoBehaviour
{
    // AR parameter
    public Camera Cam;
    public GameObject CamTarget;
    private List<Vector3> PointClouds = new List<Vector3>();
    private List<Vector3> PointClouds2D = new List<Vector3>();
    private List<List<int>> Clusters = new List<List<int>>();
    public int limitDistance;
    public float radius;

    // The delaunay mesh
    private DelaunayTriangulation2 delaunay;
    public Text PointCloudText;
    private string Log = "";
    private string FilePath;
    // Prefab which is generated for each chunk of the mesh.
    public Transform chunkPrefab = null;
    public GameObject sphere;

    private int triangleCount = 1;
    private bool draw = true;
    private int turn = 0;

    void Start()
    {
        // storage/android/data/MYAPP/file/TriangleLog.txt
        FilePath = Application.persistentDataPath + "/Log.txt";
        if (File.Exists(FilePath))
        {
            try
            {
                File.Delete(FilePath);
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
            ClearListPoints();
            return;
        }
        ClearListPoints();
        if (draw && Frame.PointCloud.IsUpdatedThisFrame && Frame.PointCloud.PointCount > 10)
        {
            AddAllPointsToList();
            WorldToScreenCoordinate();
            ClusterExtraction();
            for (int i = 0; i < Clusters.Count; i++)
            {
                if(Clusters[i].Count < 3)
                {
                    continue;
                }
                DelaunayTriangulation(Clusters[i]);
                UpdateMesh();
            }
            draw = false;  
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
        Clusters.Clear();
    }

    private void AddAllPointsToList()
    {
        if (Frame.PointCloud.IsUpdatedThisFrame)
        {
            for (int i = 0; i < Frame.PointCloud.PointCount; i++)
            {
                PointCloudPoint point = Frame.PointCloud.GetPointAsStruct(i);               
                // if the distance from point cloud to camera is less than limitDistance meter
                if (Vector3.Distance(point.Position, CamTarget.transform.position) < limitDistance && CamRaycast(point.Position))
                {
                    PointClouds.Add(point.Position);
                }
            }
        }
    }

    private void ClusterExtraction()
    {
        int maxPointsPerLeafNode = 32;
        KDTree tree = new KDTree(PointClouds.ToArray(), maxPointsPerLeafNode);
        KDQuery query = new KDQuery();
        List<int> results = new List<int>();
        List<int> temp = new List<int>();
        int clusterCounts = 0;

        Log = " Frame PointClouds Count:" + Frame.PointCloud.PointCount + "\n";
        Log += " PointClouds Count:" + PointClouds.Count + "\n";
        for (int i = 0; i < PointClouds.Count; i++)
        {
            bool next = false;
            for (int j = 0; j < Clusters.Count; j++)
            {
                if (Clusters[j].Contains(i))
                {
                    next = true;
                    break;
                }
            }
            if (next)
            {
                continue;
            }
            results.Clear();
            temp.Clear();

            query.Radius(tree, PointClouds[i], radius, temp);
            results.AddRange(temp);
            for (int j = 1; j < results.Count; j++)
            {
                temp.Clear();
                query.Radius(tree, PointClouds[results[j]], radius, temp);

                for (int k = 0; k < temp.Count; k++)
                {
                    if (!results.Contains(temp[k]))
                    {
                        results.Add(temp[k]);
                    }
                }
            }
            Clusters.Add(new List<int>());
            Clusters[clusterCounts].AddRange(results);
            clusterCounts += 1;
        }

        for (int i = 0; i < Clusters.Count; i++)
        {
            Log += "Cluster" + i + "\n";
            for (int j = 0; j < Clusters[i].Count; j++)
            {
                Log += Clusters[i][j] + ",";
            }
            Log += "\n";
        }
        WriteToFile(FilePath, Log);
    }

    private void WorldToScreenCoordinate()
    {
        for (int i = 0; i < PointClouds.Count; i++)
        {
            Vector3 point = Cam.WorldToScreenPoint(PointClouds[i]);
            PointClouds2D.Add(new Vector3(point.x, point.y, point.z));
        }
    }

    private void DelaunayTriangulation(List<int> pointsIndices)
    {
        /*List<Vertex2> vertices = new List<Vertex2>();
        for (int i = 0; i < PointClouds2D.Count; i++)
        {
            Vertex2 point = new Vertex2(PointClouds2D[i].x, PointClouds2D[i].y);
            point.SetIDAndDepth(i + 1, PointClouds2D[i].z);
            vertices.Add(point);
        }
        delaunay = new DelaunayTriangulation2();
        delaunay.Generate(vertices);*/
        if(pointsIndices.Count < 3)
        {
            return;
        }
        List<Vertex2> vertices = new List<Vertex2>();
        for (int i = 0; i < pointsIndices.Count; i++)
        {
            Vertex2 point = new Vertex2(PointClouds2D[pointsIndices[i]].x, PointClouds2D[pointsIndices[i]].y);
            point.SetIDAndDepth(i + 1, PointClouds2D[pointsIndices[i]].z);
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
            if(Vector3.Distance(triangleVertices[0], triangleVertices[1]) > limitDistance || Vector3.Distance(triangleVertices[0], triangleVertices[2]) > limitDistance || Vector3.Distance(triangleVertices[1], triangleVertices[2]) > limitDistance)
            {
                continue;
            }
            Transform chunk = Instantiate<Transform>(chunkPrefab, transform.position, transform.rotation);
            chunk.GetComponent<ScannedFieldVisualizer>().Initialize(triangleCount, triangleVertices);
            triangleCount += 1;
        }
    }

    private bool CamRaycast(Vector3 destination)
    {
        RaycastHit hit;
        if(Physics.Raycast(CamTarget.transform.position, destination, out hit))
        {
            PointCloudText.text = "" + hit.transform.name;
            if(hit.distance <= Vector3.Distance(destination, CamTarget.transform.position)) // && 面朝向camera
            {
                // destroy the mesh that raycast collide
                Destroy(hit.transform.gameObject);
                
            }
            else if(hit.distance > Vector3.Distance(destination, CamTarget.transform.position) && Vector3.Dot(CamTarget.transform.forward, hit.normal) < 0) // && 面朝向camera
            {
                // remove the pointcloud that already have mesh behind it
                return false;
                // PointClouds.RemoveAt(PointClouds.Count - 1);
            }
        }
        return true;
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

