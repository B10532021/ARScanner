using UnityEngine;
using System.Collections;
// Attach this script to the camera that you want to follow the target
public class FollowTarget : MonoBehaviour
{
    public Transform targetToFollow;
    public Quaternion targetRot;                      // The rotation of the device camera from Frame.Pose.rotation    
    public float distanceToTarget;    // The distance in the XZ plane to the target
    // Use lateUpdate to assure that the camera is updated after the target has been updated.
    void LateUpdate()
    {
        if (!targetToFollow)
            return;

        // Set camera position the same as the target position
        transform.position = targetToFollow.position;
        // Move the camera back in the direction defined by newCamRotYQuat and the amount defined by distanceToTargetXZ
        transform.position -= targetRot * Vector3.forward * distanceToTarget;
        // Finally set the camera height
        //transform.position = new Vector3(transform.position.x, newCamHeight, transform.position.z);
        // Keep the camera looking to the target to apply rotation around X axis
        transform.LookAt(targetToFollow);
    }
}