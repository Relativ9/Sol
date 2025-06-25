using UnityEngine;

namespace Sol
{
    public class LookAtPoint : MonoBehaviour
    {
        // This script defines a point in world space which the player is looking at (forward from camera).
        public Camera fpCam;
        void Update()
        {
            Ray ray = fpCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            this.transform.position = Vector3.Slerp(this.transform.position, ray.GetPoint(1000), 1f);
        }
    }
}
