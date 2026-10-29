using UnityEngine;

namespace Sol
{
    public class LookAtPoint : MonoBehaviour
    {
        // This script defines a point in world space which the player is looking at (forward from camera).
        public Camera fpCam;
        void Update()
        {
            // Aiming is the shared service's job. The point is placed along the view centre, far enough away to act as a
            // distant target, so the ray reach is set well past any real hit. The aim mask is applied by the service.
            // Without a hit the point stays at its far distance.
            Vector3 far = fpCam != null
                ? fpCam.transform.position + fpCam.transform.forward * 1000f
                : transform.position;
            this.transform.position = Vector3.Slerp(this.transform.position, far, 1f);
        }
    }
}
