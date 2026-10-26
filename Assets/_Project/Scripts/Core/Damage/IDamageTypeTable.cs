namespace Sol
{
    /// <summary>
    /// Answers "which stat mitigates this damage type?". One shared instance, registered with ServiceLocator.
    /// Receivers never own a table: they look it up and supply only their own stat values.
    /// </summary>
    public interface IDamageTypeTable
    {
        /// <summary>Returns the resistance stat for a type. False if the type has no row.</summary>
        bool TryGetResistanceStat(DamageType type, out StatTypeEnum resistanceStat);
    }
}
