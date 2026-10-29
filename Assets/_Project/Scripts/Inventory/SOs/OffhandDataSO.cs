using UnityEngine;

namespace Sol
{
    [CreateAssetMenu(fileName = "New Offhand", menuName = "Sol/Items/Offhand")]
    public class OffhandDataSO : EquipmentDataSO
    {
        [Header("Offhand Properties")]
        public OffhandType _offhandType;

        // Stored footprint comes from the offhand type rule, not the asset's footprint fields.
        public override int GridWidth { get { EquipmentSilhouette.OffhandFootprint(_offhandType, out int w, out _); return w; } }
        public override int GridHeight { get { EquipmentSilhouette.OffhandFootprint(_offhandType, out _, out int h); return h; } }
    }
}
