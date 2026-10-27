using UnityEngine;
 
public class CameraFollow : MonoBehaviour
{
    public Transform target;                            // drag the Player here
    public float smoothTime = 0.2f;                     // smaller = faster follow
    public Vector3 offset = new Vector3(0f, 1f, -10f);  // keep Z at -10!
 
    private Vector3 velocity = Vector3.zero;
 
    // LateUpdate runs AFTER the player has moved this frame
    void LateUpdate()
    {
        if (target == null) return;
 
        Vector3 targetPos = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
    }
}
