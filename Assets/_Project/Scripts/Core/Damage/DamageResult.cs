namespace Sol
{
    /// <summary>Outcome of the mitigation pipeline. Status chances are returned for the receiver to roll.</summary>
    public readonly struct DamageResult
    {
        public readonly float FinalAmount;
        public readonly bool Deflected;
        public readonly bool UnmappedType;   // No DamageTypeTable row for this type. Mitigation was skipped.
        public readonly float StaggerChance; // Proc chance from the armour rules. 0 if not physical blunt.
        public readonly float BleedChance;   // Proc chance from the armour rules. 0 if not physical blade or piercing, or no damage.

        public DamageResult(float finalAmount, bool deflected, bool unmappedType, float staggerChance, float bleedChance)
        {
            FinalAmount = finalAmount;
            Deflected = deflected;
            UnmappedType = unmappedType;
            StaggerChance = staggerChance;
            BleedChance = bleedChance;
        }
    }
}
