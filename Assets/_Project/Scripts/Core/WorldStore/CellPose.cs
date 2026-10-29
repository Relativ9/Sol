using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Encodes a pose into the compact record fields and back. Position is stored in whole centimetres, and yaw in tenths of a
    /// degree (0 to 3599). Pitch and roll are not stored: a dropped or resting item settles flat. One place does the conversion,
    /// so writers and readers cannot disagree about units.
    /// </summary>
    public static class CellPose
    {
        public static void Encode(Vector3 position, Quaternion rotation, out int x, out int y, out int z, out ushort yawTenths)
        {
            x = Mathf.RoundToInt(position.x * 100f);
            y = Mathf.RoundToInt(position.y * 100f);
            z = Mathf.RoundToInt(position.z * 100f);

            int tenths = Mathf.RoundToInt(rotation.eulerAngles.y * 10f) % 3600;
            if (tenths < 0) tenths += 3600;
            yawTenths = (ushort)tenths;
        }

        public static Vector3 PositionOf(CellRecord record) => new Vector3(record.X, record.Y, record.Z) * 0.01f;

        public static Quaternion RotationOf(CellRecord record) => Quaternion.Euler(0f, record.YawTenths * 0.1f, 0f);
    }
}
