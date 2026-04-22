using UnityEngine;

namespace Sol
{
    [CreateAssetMenu(fileName = "New Weapon", menuName = "Sol/Items/Weapon")]
    public class WeaponDataSO : EquipmentDataSO
    {
        [Header("Weapon Properties")] 
        public WeaponType weaponType;
        public WeaponSize weaponSize;
        private float _maxDurability = 100f;
        
        [Header("Visuals")]
        public GameObject weaponPrefab;
        public Vector3 sheathedPosition;
        public Vector3 sheathedRotation;
        public Vector3 unsheathedPosition;
        public Vector3 unsheathedRotation;
        public GameObject hitEffectPrefab;
        
        [Header("Audio")]
        public AudioClip _hitSound;
        public AudioClip _swingSound;
        public AudioClip _sheathSound;
        public AudioClip _unsheathSound;
        
        [Header("Animation")]
        public string equipAnimationTrigger;
        public string unequipAnimationTrigger;
    }
}

