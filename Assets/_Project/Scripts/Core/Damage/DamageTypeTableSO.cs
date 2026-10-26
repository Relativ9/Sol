using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Designer-facing table: one row per DamageType, giving its name, colour, and the stat that mitigates it.
    /// Physical is mitigated by ArmorValue with the same formula as every other type, so no type is special-cased in code.
    /// </summary>
    [CreateAssetMenu(fileName = "DamageTypeTable", menuName = "Sol/Combat/Damage Type Table")]
    public class DamageTypeTableSO : ScriptableObject, IDamageTypeTable
    {
        [Serializable]
        public class Entry
        {
            public DamageType type;
            public string displayName;
            public Color color = Color.white;
            [Tooltip("Stat that mitigates this type. Fractions for percentages, e.g. 0.2 = 20%.")]
            public StatTypeEnum resistanceStat;
        }

        public List<Entry> entries = new();

        public bool TryGetResistanceStat(DamageType type, out StatTypeEnum resistanceStat)
        {
            if (TryGet(type, out Entry entry))
            {
                resistanceStat = entry.resistanceStat;
                return true;
            }
            resistanceStat = default;
            return false;
        }

        public bool TryGet(DamageType type, out Entry entry)
        {
            foreach (Entry e in entries)
            {
                if (e != null && e.type == type)
                {
                    entry = e;
                    return true;
                }
            }
            entry = null;
            return false;
        }

        /// <summary>Damage types with no row in this table. Checked at startup so a missing row is caught early.</summary>
        public IEnumerable<DamageType> MissingTypes()
        {
            foreach (DamageType type in Enum.GetValues(typeof(DamageType)))
            {
                if (!TryGet(type, out _)) yield return type;
            }
        }

        [ContextMenu("Fill missing types with defaults")]
        void FillMissingWithDefaults()
        {
            foreach (DamageType type in MissingTypes())
            {
                entries.Add(new Entry
                {
                    type = type,
                    displayName = type.ToString(),
                    color = Color.white,
                    resistanceStat = DefaultResistance(type),
                });
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        static StatTypeEnum DefaultResistance(DamageType type)
        {
            return type switch
            {
                DamageType.Physical => StatTypeEnum.ArmorValue,
                DamageType.Fire => StatTypeEnum.FireResistance,
                DamageType.Ice => StatTypeEnum.IceResistance,
                DamageType.Void => StatTypeEnum.VoidResistance,
                DamageType.Sonic => StatTypeEnum.SonicResistance,
                DamageType.Soul => StatTypeEnum.SoulResistance,
                DamageType.Magic => StatTypeEnum.MagicResistance,
                _ => StatTypeEnum.MagicResistance,
            };
        }
    }
}
