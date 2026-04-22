using UnityEngine;

namespace Sol
{
    [CreateAssetMenu(fileName = "New Wearable", menuName = "Sol/Items/Wearable")]
    public class WearableDataSO : EquipmentDataSO
    {
        [Header("Wearable Properties")]
        public WearableCategory _wearableCategory;
        public ArmorType _armorType; //Needs a conditional in an editor script, to ensure only armors (not rings etc) have a armor type.
        public float _maxDurability;
    }
}

