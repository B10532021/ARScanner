using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InnerCameraView : MonoBehaviour
{
    private Camera Cam;
    private Text Log;

    private void Awake()
    {
        Cam = GameObject.Find("ScannedField Generate").GetComponent<ScannedFieldGenerate>().InnerCam;
        Log = GameObject.Find("ScannedField Generate").GetComponent<ScannedFieldGenerate>().LogText;
    }

    public bool IsInView(Vector3 worldPos)
    {
        Vector3 pointOnScreen = Cam.WorldToScreenPoint(worldPos);

        //Is in front
        if (pointOnScreen.z < 0)
        {
            
            return false;
        }

        //Is in FOV
        if ((pointOnScreen.x < 0) || (pointOnScreen.x > Screen.width) || (pointOnScreen.y < 0) || (pointOnScreen.y > Screen.height))
        {
            return false;
        }
        return true;
    }

    void Update()
    {
        List<Color> colors = new List<Color>();
        if (IsInView(this.GetComponentInChildren<Renderer>().bounds.center) && Vector3.Dot(Cam.transform.forward, this.GetComponent<MeshFilter>().mesh.normals[0]) < 0)
        {
            GameObject.Find("ScannedField Generate").GetComponent<ScannedFieldGenerate>().AddVisibleObject(this.gameObject);
            colors.Add(new Color(30f / 255f, 144f / 255f, 1, 0.3f));
            colors.Add(new Color(30f / 255f, 144f / 255f, 1, 0.3f));
            colors.Add(new Color(30f / 255f, 144f / 255f, 1, 0.3f));
        }
        else
        {
            GameObject.Find("ScannedField Generate").GetComponent<ScannedFieldGenerate>().RemoveVisibleObject(this.gameObject);
            colors.Add(new Color(238f / 255f, 238f / 255f, 209f / 255f, 0.3f));
            colors.Add(new Color(238f / 255f, 238f / 255f, 209f / 255f, 0.3f));
            colors.Add(new Color(238f / 255f, 238f / 255f, 209f / 255f, 0.3f));
        }
        this.GetComponent<MeshFilter>().mesh.colors = colors.ToArray();
        this.GetComponent<MeshCollider>().sharedMesh.colors = colors.ToArray();
    }
}
