using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Zooming : MonoBehaviour
{
    public GameObject m_firstPersonCamera;
    public Text camPoseText;
    private float zoomInCur = 0; //current zoom in distance
    private float zoomInMin = 0;
    private float zoomInMax = 15;
    // Start is called before the first frame update
    void Start()
    {
        Input.multiTouchEnabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float currentMagnitude = (touchZero.position - touchOne.position).magnitude;

            float difference = currentMagnitude - prevMagnitude;
            camPoseText.text = "" + difference;
            if (zoomInCur >= zoomInMin && zoomInCur <= zoomInMax)
            {
                zoomInCur = zoomInCur - difference * 0.01f;
                if (zoomInCur <= zoomInMin)
                {
                    zoomInCur = zoomInMin;
                }
                else if (zoomInCur >= zoomInMax)
                {
                    zoomInCur = zoomInMax;
                }
            }
            m_firstPersonCamera.GetComponent<FollowTarget>().distanceToTarget = zoomInCur;
        }
    }
}
