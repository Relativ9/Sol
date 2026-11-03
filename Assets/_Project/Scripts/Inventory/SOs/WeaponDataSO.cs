using UnityEngine;

namespace Sol
{
    [CreateAssetMenu(fileName = "New Weapon", menuName = "Sol/Items/Weapon")]
    public class WeaponDataSO : EquipmentDataSO
    {
        [Header("Weapon Properties")] 
        public WeaponType weaponType;
        public WeaponSize weaponSize;

        // Stored footprint comes from the weapon's type and size, not from the asset's footprint fields.
        public override int GridWidth { get { EquipmentSilhouette.WeaponFootprint(weaponType, weaponSize, out int w, out _); return w; } }
        public override int GridHeight { get { EquipmentSilhouette.WeaponFootprint(weaponType, weaponSize, out _, out int h); return h; } }
        private float _maxDurability = 100f;
        
        [Header("Weapon Damage")]
        [Tooltip("The weapon's own attack value. Added to the wielder's Melee Damage, so Brawn counts with any weapon. " +
                 "Unarmed attacks use Melee Damage alone.")]
        public float weaponDamage = 10f;
        [Tooltip("The status this weapon's hits can cause. Blades bleed; blunt weapons stagger. Leave empty for none.")]
        [SerializeField] private StatusEffectSO _statusEffect;

        // The physical part's damage type is always Physical. Elemental damage is in elementalAttacks, not here.
        public DamageType DamageType => DamageType.Physical;

        // Tags come from the weapon's type, so the doctrine always matches what the weapon is.
        public DamageTag DamageTags
        {
            get
            {
                switch (weaponType)
                {
                    case WeaponType.Blade: return DamageTag.Melee | DamageTag.Blade;
                    case WeaponType.Blunt: return DamageTag.Melee | DamageTag.Blunt;
                    case WeaponType.Ranged: return DamageTag.Ranged;
                    default: return DamageTag.Melee;
                }
            }
        }

        // Blades can only cause bleed and blunt weapons only stagger. The one effect applies only to the matching type.
        // The effect follows the same tags as the chance, so the two can never disagree.
        public StatusEffectSO StaggerEffect => (DamageTags & DamageTag.Blunt) != 0 ? _statusEffect : null;
        public StatusEffectSO BleedEffect => (DamageTags & (DamageTag.Blade | DamageTag.Piercing)) != 0 ? _statusEffect : null;

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

