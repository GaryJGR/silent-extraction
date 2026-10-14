using UnityEngine;

namespace SilentExtraction
{
    public sealed class FollowCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0, 19, -13);
        Vector3 velocity;
        Vector3 FramingPoint()
        {
            // Keep the facility in view near its boundaries while retaining gentle tracking.
            Vector3 p = target.position;
            return new Vector3(Mathf.Clamp(p.x, -3, 3), 0, Mathf.Clamp(p.z, -6, 6));
        }
        void Start()
        {
            if (target != null) transform.position = FramingPoint() + offset;
            transform.rotation = Quaternion.Euler(56, 0, 0);
        }
        void LateUpdate()
        {
            if (target != null)
                transform.position = Vector3.SmoothDamp(transform.position, FramingPoint() + offset, ref velocity, .12f, Mathf.Infinity, Time.unscaledDeltaTime);
        }
    }
}
