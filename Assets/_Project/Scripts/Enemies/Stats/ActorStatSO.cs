using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Base stat block for one enemy type (e.g. "Ogre", "Boss Ogre"). One asset per type. Enemies read their
    /// stats from here through ActorStats, so the same StatTypeEnum and modifier path as the player applies.
    /// Stats not listed read as zero.
    /// </summary>
    [CreateAssetMenu(fileName = "New Actor Stats", menuName = "Sol/Actors/Actor Stats")]
    public class ActorStatSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public StatTypeEnum stat;
            public float value;
        }

        [Tooltip("Base value for each stat. Add a row per stat that is not zero.")]
        public List<Entry> baseStats = new List<Entry>();

        /// <summary>Returns the base value for a stat, or zero if the asset does not list it.</summary>
        public float GetBase(StatTypeEnum stat)
        {
            foreach (Entry e in baseStats)
                if (e.stat == stat) return e.value;
            return 0f;
        }
    }
}
