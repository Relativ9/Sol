namespace Sol
{
    /// <summary>
    /// Caps on stacked stats, applied to the total from every source (equipment, talents, enchantments).
    /// One rule for all of them: a capped stat never exceeds ResistanceCap points, however many sources add to it.
    /// The stats screen shows the capped value, so "0/400" always means what it says.
    /// </summary>
    public static class StatCaps
    {
        // 400 points is 80% mitigation on the 100 / (100 + x) curve. Resistance and penetration share it.
        public const float ResistanceCap = 400f;

        /// <summary>
        /// True for stats capped at ResistanceCap: penetration and every resistance. Armour value is not here:
        /// it is a plain number on the curve and is shown as one.
        /// </summary>
        public static bool IsCapped(StatTypeEnum stat)
        {
            switch (stat)
            {
                case StatTypeEnum.Penetration:
                case StatTypeEnum.MagicResistance:
                case StatTypeEnum.FireResistance:
                case StatTypeEnum.IceResistance:
                case StatTypeEnum.VoidResistance:
                case StatTypeEnum.SonicResistance:
                case StatTypeEnum.SoulResistance:
                case StatTypeEnum.EnvironmentalResistance:
                case StatTypeEnum.StaggerResistance:
                case StatTypeEnum.BleedResistance:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Returns the value clamped to the cap when the stat is capped. Other stats pass through unchanged.</summary>
        public static float Apply(StatTypeEnum stat, float value)
        {
            if (!IsCapped(stat)) return value;
            return value < 0f ? 0f : (value > ResistanceCap ? ResistanceCap : value);
        }
    }
}
