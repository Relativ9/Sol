using System.Collections.Generic;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// The blade's hit detection. A trigger collider on the held weapon. It is enabled only during a swing (set by
    /// WeaponVisualManager from the attack events), so resting or sheathed contact does nothing.
    ///
    /// One contact with a receiver delivers every part of the weapon's damage as one hit set: the physical part (Weapon Damage
    /// plus the wielder's Melee Damage), then each elemental part. Each part keeps its own type, so the receiver applies that
    /// type's resistance. Each receiver is hit once per swing, and several receivers can be hit by the same swing.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WeaponHitbox : MonoBehaviour
    {
        // How much of the wielder's attunement to an element adds to that element's weapon damage. Elemental parts scale with
        // attunement only, not with Melee Damage.
        public const float ElementalAttunementScale = 0.2f;

        private readonly HashSet<IDamageReceiver> _hitThisSwing = new HashSet<IDamageReceiver>();
        private IStatsService _stats;
        private IStatusController _status;
        private WeaponDataSO _weapon;
        private Collider _collider;
        private bool _swinging;

        // Supplied when the held weapon is built. A null weapon is an unarmed hitbox, which has no parts of its own.
        public void Configure(WeaponDataSO weapon, IStatsService stats, IStatusController status)
        {
            _weapon = weapon;
            _stats = stats;
            _status = status;
        }

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;
            _collider.enabled = false;
        }

        // Opens the hitbox for a swing. Clears the list so this swing can hit each receiver once.
        public void BeginSwing()
        {
            _hitThisSwing.Clear();
            _swinging = true;
            _collider.enabled = true;
        }

        // Closes the hitbox. Contact outside a swing does nothing.
        public void EndSwing()
        {
            _swinging = false;
            _collider.enabled = false;
            _hitThisSwing.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_swinging) return;

            // A staggered attacker cannot land a hit. The swing is cancelled, not resolved.
            if (_status != null && _status.IsCombatLocked) return;

            // The receiver is on the collider's body, which may be a parent of the collider.
            IDamageReceiver receiver = other.GetComponentInParent<IDamageReceiver>();
            if (receiver == null) return;

            // One contact per receiver per swing. All parts arrive together, so a receiver is never hit twice by one swing.
            if (!_hitThisSwing.Add(receiver)) return;

            foreach (DamageInfo hit in BuildHits(other))
                receiver.ReceiveHit(hit);
        }

        // Builds every part of this contact. The physical part is first, then one part per elemental attack on the weapon.
        private List<DamageInfo> BuildHits(Collider other)
        {
            var hits = new List<DamageInfo>();
            Vector3 origin = transform.root.position;
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            float penetration = _stats != null ? _stats.GetStat(StatTypeEnum.Penetration) : 0f;

            hits.Add(BuildPhysicalPart(origin, hitPoint, penetration));

            if (_weapon == null) return hits;
            foreach (EquipmentDataSO.ElementalAttack elemental in _weapon.elementalAttacks)
            {
                if (elemental == null) continue;
                float amount = elemental.damage + AttunementFor(elemental.damageType) * ElementalAttunementScale;
                if (amount <= 0f) continue;

                // Elemental parts carry no status chances: those belong to the physical doctrine.
                hits.Add(new DamageInfo(gameObject.name, elemental.damageType, DamageTag.Melee, amount, true, origin,
                    default, hitPoint, penetration));
            }
            return hits;
        }

        // Weapon Damage plus the wielder's Melee Damage. Unarmed, this is Melee Damage alone. Status chances come from the
        // doctrine for the weapon's type and tags, and the weapon's own status effect.
        private DamageInfo BuildPhysicalPart(Vector3 origin, Vector3 hitPoint, float penetration)
        {
            float meleeDamage = _stats != null ? _stats.GetStat(StatTypeEnum.MeleeDamage) : 0f;
            float weaponDamage = _weapon != null ? _weapon.weaponDamage : 0f;
            DamageType type = _weapon != null ? _weapon.DamageType : DamageType.Physical;
            DamageTag tags = _weapon != null ? _weapon.DamageTags : DamageTag.Melee;

            float stagger = StatusChanceCalculator.AttackerStaggerChance(type, tags, 0f);
            float bleed = StatusChanceCalculator.AttackerBleedChance(type, tags, 0f);
            var statuses = new StatusAttack(_weapon != null ? _weapon.StaggerEffect : null, stagger,
                                            _weapon != null ? _weapon.BleedEffect : null, bleed);

            return new DamageInfo(gameObject.name, type, tags, meleeDamage + weaponDamage, true, origin,
                statuses, hitPoint, penetration);
        }

        // The wielder's attunement to an element. Physical has none, and elements without an attunement stat add nothing.
        private float AttunementFor(DamageType type)
        {
            if (_stats == null) return 0f;
            switch (type)
            {
                case DamageType.Fire: return _stats.GetStat(StatTypeEnum.FireAttunement);
                case DamageType.Ice: return _stats.GetStat(StatTypeEnum.IceAttunement);
                case DamageType.Void: return _stats.GetStat(StatTypeEnum.VoidAttunement);
                case DamageType.Sonic: return _stats.GetStat(StatTypeEnum.SonicAttunement);
                case DamageType.Soul: return _stats.GetStat(StatTypeEnum.SoulAttunement);
                case DamageType.Magic: return _stats.GetStat(StatTypeEnum.MagicAttunement);
                default: return 0f;
            }
        }
    }
}
