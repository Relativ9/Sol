using UnityEngine;

namespace Sol
{
    /// <summary>
    /// A dropped item saved in the world: which item, which individual, and where it lies. Position and yaw are stored
    /// as whole numbers to keep the save small and stable. Centimetres give more than enough precision for an item on the
    /// ground. Yaw is whole degrees around the vertical axis. Pitch and roll are not saved, since a dropped item settles
    /// flat and any tilt would be arbitrary.
    /// </summary>
    [System.Serializable]
    public struct WorldDropEntry
    {
        public string itemInstanceId;
        public string itemId;

        // Centimetres. A value of 150 is 1.5 m.
        public int xCm;
        public int yCm;
        public int zCm;

        // Whole degrees, 0 to 359, around the vertical axis.
        public int yawDeg;

        public Vector3 Position
        {
            get => new Vector3(xCm, yCm, zCm) * 0.01f;
            set
            {
                xCm = Mathf.RoundToInt(value.x * 100f);
                yCm = Mathf.RoundToInt(value.y * 100f);
                zCm = Mathf.RoundToInt(value.z * 100f);
            }
        }

        public Quaternion Rotation
        {
            get => Quaternion.Euler(0f, yawDeg, 0f);
            set => yawDeg = Mathf.RoundToInt(value.eulerAngles.y) % 360;
        }
    }
}
