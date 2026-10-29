using System.Collections.Generic;

namespace Sol
{
    /// <summary>
    /// Runtime stat sheet for one enemy. Plain C#, owned by the enemy's component. Reads its base from an ActorStatSO
    /// and holds modifiers (enchantments, boss traits, temporary buffs) through the same StatModifier model as the player.
    /// Exposes GetStat with the same shape as IStatsService, so attacker and defender calculations read the same way.
    /// </summary>
    public class ActorStats
    {
        private readonly ActorStatSO _base;
        private readonly List<StatModifier> _modifiers = new List<StatModifier>();
        private readonly IStatCalculator _calculator = new StandardStatCalculator();

        public ActorStats(ActorStatSO baseStats)
        {
            _base = baseStats;
        }

        /// <summary>Adds a modifier. A non-empty sourceId replaces any existing modifier from the same source.</summary>
        public string ApplyOrReplaceModifier(StatModifier modifier)
        {
            if (string.IsNullOrEmpty(modifier.id))
                modifier.id = System.Guid.NewGuid().ToString();

            if (!string.IsNullOrEmpty(modifier.sourceId))
                _modifiers.RemoveAll(m => m.sourceId == modifier.sourceId && m.statType == modifier.statType);

            _modifiers.Add(modifier);
            return modifier.id;
        }

        public void RemoveModifiersFromSource(string sourceId)
        {
            _modifiers.RemoveAll(m => m.sourceId == sourceId);
        }

        public float GetStat(StatTypeEnum stat)
        {
            float baseValue = _base != null ? _base.GetBase(stat) : 0f;

            List<StatModifier> applicable = new List<StatModifier>();
            foreach (StatModifier m in _modifiers)
                if (m.statType == stat) applicable.Add(m);

            return _calculator.Calculate(stat, baseValue, applicable.ToArray());
        }
    }
}
