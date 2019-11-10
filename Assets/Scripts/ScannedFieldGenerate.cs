using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using GoogleARCore;
using GoogleARCore.Examples.ComputerVision;
using HullDelaunayVoronoi.Delaunay;
using HullDelaunayVoronoi.Primitives;
using DrawingTool;
using DataStructures.ViliWonka.KDTree;
using VoxelSystem;

public class ScannedFieldGenerate : MonoBehaviour
{
    // AR parameter
    public Camera InnerCam;
    private List<Vector3> PointClouds = new List<Vector3>();
    private List<List<int>> Clusters = new List<List<int>>();
    private List<GameObject> VisibleObject = new List<GameObject>();
    private int newPointCloudsNum;
    public int limitDistance;
    public float Radius;

    // The delaunay mesh
    private DelaunayTriangulation2 delaunay;

    // Voxelization + TSDF
    private float voxelSize = 0.04f;


    // Prefab which is generated for each chunk of the mesh.
    public Transform chunkPrefab = null;
    public GameObject sphere;

    private int triangleCount = 1;
    private bool draw = true;
    private int turn = 0;

    public Text LogText;
    private string Log = "";
    private string FilePath;

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
            return;
        }
        ClearListPoints();
        if (draw && Frame.PointCloud.IsUpdatedThisFrame && Frame.PointCloud.PointCount > 10)
        {
            AddPointsToList();
            AddMeshVerticesToList();
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
        if(turn % 15 == 0)
        {
            draw = true;
        }
        turn += 1;
    }

    private void OnDisable()
    {
        ClearListPoints();
    }

    private void ClearListPoints()
    {
        PointClouds.Clear();
        Clusters.Clear();
    }

    private void AddPointsToList()
    {
        for (int i = 0; i < Frame.PointCloud.PointCount; i++)
        {
            PointCloudPoint point = Frame.PointCloud.GetPointAsStruct(i);

            if (Vector3.Distance(point.Position, InnerCam.transform.position) < limitDistance && CamRaycast(point.Position))
            {
                    PointClouds.Add(point.Position);
            }
        }
        newPointCloudsNum = PointClouds.Count;
    }

    private void AddMeshVerticesToList()
    {
        if(VisibleObject.Count == 0)
        {
            return;
        }
        for (int i = 0; i < VisibleObject.Count; i++)
        {
            Vector3[] vertices = VisibleObject[i].GetComponent<MeshFilter>().mesh.vertices;
            foreach (Vector3 vertex in vertices)
            {
                Vector3 pointOnScreen = InnerCam.WorldToScreenPoint(vertex);
                //Is in FOV
                if ((pointOnScreen.x >= 0) && (pointOnScreen.x < Screen.width) && (pointOnScreen.y >= 0) || (pointOnScreen.y < Screen.height))
                {
                    PointClouds.Add(vertex);
                }
            }
        }
    }

    private void ClusterExtraction()
    {
        // KDTree
        int maxPointsPerLeafNode = 32;
        KDTree tree = new KDTree(PointClouds.ToArray(), maxPointsPerLeafNode);
        KDQuery query = new KDQuery();
        List<int> results = new List<int>();
        List<int> temp = new List<int>();
        int clusterCounts = 0;

        for (int i = 0; i < newPointCloudsNum; i++)
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

            query.Radius(tree, PointClouds[i], Radius, temp);
            results.AddRange(temp);
            for (int j = 1; j < results.Count; j++)
            {
                if (results[j] < newPointCloudsNum)
                {
                    temp.Clear();
                    query.Radius(tree, PointClouds[results[j]], Radius, temp);

                    for (int k = 0; k < temp.Count; k++)
                    {
                        if (!results.Contains(temp[k]))
                        {
                            results.Add(temp[k]);
                        }
                    }
                } 
            }
            Clusters.Add(new List<int>());
            Clusters[clusterCounts].AddRange(results);
            clusterCounts += 1;
        }
    }

    private void DelaunayTriangulation(List<int> pointsIndices)
    {
        if(pointsIndices.Count < 3)
        {
            return;
        }
        List<Vertex2> vertices = new List<Vertex2>();
        for (int i = 0; i < pointsIndices.Count; i++)
        {
            Vector3 pointcloud2D = InnerCam.WorldToScreenPoint(PointClouds[pointsIndices[i]]);
            Vertex2 point = new Vertex2(pointcloud2D.x, pointcloud2D.y);
            point.SetIDAndDepth(i + 1, pointcloud2D.z);
            vertices.Add(point);
        }
        delaunay = new DelaunayTriangulation2();
        delaunay.Generate(vertices);

    }

    private List<Vector3> GetWorldCoordinate(Simplex<Vertex2> points)
    {
        List<Vector3> vertices = new List<Vector3>();
        // positive if points are CCW negative if they're CW
        if((points.Vertices[1].X - points.Vertices[0].X) * (points.Vertices[2].Y - points.Vertices[0].Y)
            - (points.Vertices[2].X - points.Vertices[0].X) * (points.Vertices[1].Y - points.Vertices[0].Y) < 0)
        {
            vertices.Add(InnerCam.ScreenToWorldPoint(new Vector3(points.Vertices[0].X, points.Vertices[0].Y, points.Vertices[0].depth)));
            vertices.Add(InnerCam.ScreenToWorldPoint(new Vector3(points.Vertices[1].X, points.Vertices[1].Y, points.Vertices[1].depth)));
            vertices.Add(InnerCam.ScreenToWorldPoint(new Vector3(points.Vertices[2].X, points.Vertices[2].Y, points.Vertices[2].depth)));
        }
        else
        {
            vertices.Add(InnerCam.ScreenToWorldPoint(new Vector3(points.Vertices[0].X, points.Vertices[0].Y, points.Vertices[0].depth)));
            vertices.Add(InnerCam.ScreenToWorldPoint(new Vector3(points.Vertices[2].X, points.Vertices[2].Y, points.Vertices[2].depth)));
            vertices.Add(InnerCam.ScreenToWorldPoint(new Vector3(points.Vertices[1].X, points.Vertices[1].Y, points.Vertices[1].depth)));
        }

        return vertices;
    }

    private void Voxelization()
    {
        if (delaunay == null || delaunay.Cells.Count == 0 || delaunay.Vertices.Count == 0)
        {
            return;
        }

        foreach (DelaunayCell<Vertex2> cell in delaunay.Cells)
        {
            List<Vector3> triangleVertices = GetWorldCoordinate(cell.Simplex);
        }
    }

    private void UpdateMesh()
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

    // 這個還有問題
    private bool CamRaycast(Vector3 destination)
    {
        RaycastHit hit;
        if(Physics.Raycast(InnerCam.transform.position, destination, out hit) && Vector3.Dot(InnerCam.transform.forward, hit.normal) < 0)
        {
            if(hit.distance < Vector3.Distance(destination, InnerCam.transform.position)) // && 面朝向camera
            {
                if (Vector3.Distance(destination, InnerCam.transform.position) - hit.distance < 0.03)
                {
                    Vector3[] vertices = hit.transform.gameObject.GetComponent<MeshFilter>().mesh.vertices;
                    foreach (Vector3 vert in vertices)
                    {
                        if (Vector3.Distance(vert, destination) < 0.01)
                        {
                            break;
                        }
                    }
                    return false;
                }
                else
                {
                    RemoveVisibleObject(hit.transform.gameObject);
                    Destroy(hit.transform.gameObject);
                    return true;
                }               
            }
            else if (hit.distance >= Vector3.Distance(destination, InnerCam.transform.position) && hit.distance - Vector3.Distance(destination, InnerCam.transform.position) < 0.03)
            { 
                // remove the pointcloud that already have mesh behind it
                return false;
            }
            return true;
        }
        return true;
    }

    public void AddVisibleObject(GameObject gameobject)
    {
        if (!VisibleObject.Contains(gameobject))
        {
            VisibleObject.Add(gameobject);
        }
    }

    public void RemoveVisibleObject(GameObject gameobject)
    {
        if (VisibleObject.Contains(gameobject))
        {
            VisibleObject.Remove(gameobject);
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

