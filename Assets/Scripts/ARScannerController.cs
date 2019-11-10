using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using GoogleARCore;
using UnityEngine.UI;
using GoogleARCore.Examples.ComputerVision;

public class ARScannerController : MonoBehaviour
{
    public GameObject m_firstPersonCamera;
    public Camera m_innerCamera;
    public Text CameraIntrinsicsOutput;
    private Vector3 m_prevARPosePosition;
    private bool trackingStarted = false;

    /// <summary>
    /// True if the app is in the process of quitting due to an ARCore connection error,
    /// otherwise false.
    /// </summary>
    private bool m_IsQuitting = false;

    public void Start()
    {
        m_prevARPosePosition = Vector3.zero;
    }

    public void Update()
    {
        _UpdateApplicationLifecycle();
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Vector3 currentARPosition = Frame.Pose.position;
        if (!trackingStarted)
        {
            trackingStarted = true;
            m_prevARPosePosition = Frame.Pose.position;
        }
        //Remember the previous position so we can apply deltas
        Vector3 deltaPosition = currentARPosition - m_prevARPosePosition;
        m_prevARPosePosition = currentARPosition;
        CameraIntrinsicsOutput.text = _CameraIntrinsicsToString(Frame.CameraImage.TextureIntrinsics, "GPU Texture");
    }

    /// <summary>
    /// Check and update the application lifecycle.
    /// </summary>
    private void _UpdateApplicationLifecycle()
    {
        // Exit the app when the 'back' button is pressed.
        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }

        // Only allow the screen to sleep when not tracking.
        if (Session.Status != SessionStatus.Tracking)
        {
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }
        else
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        if (m_IsQuitting)
        {
            return;
        }

        // Quit if ARCore was unable to connect and give Unity some time for the toast to
        // appear.
        if (Session.Status == SessionStatus.ErrorPermissionNotGranted)
        {
            _ShowAndroidToastMessage("Camera permission is needed to run this application.");
            m_IsQuitting = true;
            Invoke("_DoQuit", 0.5f);
        }
        else if (Session.Status.IsError())
        {
            _ShowAndroidToastMessage(
                "ARCore encountered a problem connecting.  Please start the app again.");
            m_IsQuitting = true;
            Invoke("_DoQuit", 0.5f);
        }
    }

    private void _ShowAndroidToastMessage(string message)
    {
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject unityActivity =
            unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        if (unityActivity != null)
        {
            AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
            unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                AndroidJavaObject toastObject =
                    toastClass.CallStatic<AndroidJavaObject>(
                        "makeText", unityActivity, message, 0);
                toastObject.Call("show");
            }));
        }
    }

    private string _CameraIntrinsicsToString(CameraIntrinsics intrinsics, string intrinsicsType)
    {
        float fovX = 2.0f * Mathf.Rad2Deg * Mathf.Atan2(
            intrinsics.ImageDimensions.x, 2 * intrinsics.FocalLength.x);
        float fovY = 2.0f * Mathf.Rad2Deg * Mathf.Atan2(
            intrinsics.ImageDimensions.y, 2 * intrinsics.FocalLength.y);

        string message = string.Format(
            "Unrotated Camera {4} Intrinsics:{0}  Focal Length: {1}{0}  " +
            "Principal Point: {2}{0}  Image Dimensions: {3}{0}  " +
            "Unrotated Field of View: ({5}°, {6}°){0}",
            Environment.NewLine, intrinsics.FocalLength.ToString(),
            intrinsics.PrincipalPoint.ToString(), intrinsics.ImageDimensions.ToString(),
            intrinsicsType, fovX, fovY);
        return message;
    }

}

