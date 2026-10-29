using UnityEngine;

namespace Sol
{
    [CreateAssetMenu(fileName = "New Wearable", menuName = "Sol/Items/Wearable")]
    public class WearableDataSO : EquipmentDataSO
    {
        [Header("Wearable Properties")]
        public WearableCategory _wearableCategory;

        // Plate, Flex or Cloth. Only armour pieces set this; rings and other accessories leave it at its default
        // and are excluded from piece counts by WearableCategory.
        [Tooltip("Plate, flex or cloth. Armour pieces only. Plate counts toward deflection and stagger resistance, flex toward bleed resistance, and cloth toward a full set.")]
        public ArmorType _armorType;

        public float _maxDurability;
    }
}
