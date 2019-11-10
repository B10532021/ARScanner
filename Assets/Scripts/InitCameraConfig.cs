using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCoreInternal;
using GoogleARCore;

public class InitCameraConfig : MonoBehaviour
{
    private Camera m_Camera;
    private CameraClearFlags m_CameraClearFlags = CameraClearFlags.Skybox;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnEnable()
    {
        m_Camera = GetComponent<Camera>();
    }

    private void OnDisable()
    {
        m_Camera.ResetProjectionMatrix();
    }

    // Update is called once per frame
    void Update()
    {
        if (Session.Status == SessionStatus.Tracking)
        {
            m_Camera.projectionMatrix = Frame.CameraImage.GetCameraProjectionMatrix(m_Camera.nearClipPlane, m_Camera.farClipPlane);
        }
    }
}
