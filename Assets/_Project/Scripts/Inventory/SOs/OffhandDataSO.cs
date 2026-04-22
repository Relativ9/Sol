using UnityEngine;

namespace Sol
{
    [CreateAssetMenu(fileName = "New Offhand", menuName = "Sol/Items/Offhand")]
    public class OffhandDataSO : EquipmentDataSO
    {
        [Header("Offhand Properties")]
        public OffhandType _offhandType;
        
    }

}
