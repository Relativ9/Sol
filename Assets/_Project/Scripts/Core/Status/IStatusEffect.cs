namespace Sol
{
    /// <summary>
    /// What a status effect does, as seen by the receiver. Every effect type implements this, so StatusController
    /// never checks concrete types. Adding silence or root means a new subclass, with no change to the receiver.
    /// </summary>
    public interface IStatusEffect
    {
        string DisplayName { get; }
        string ExpireMessage { get; }
        float DurationSeconds { get; }
        StatusStackingMode StackingMode { get; }

        bool LocksMovement { get; }
        bool LocksCombat { get; }

        float EnergyCostOnApply { get; }
        float KnockbackSpeed { get; }
        string AnimatorTrigger { get; }

        /// <summary>Fraction of the raw hit damage dealt over the full duration. 0 = no ticks.</summary>
        float DamageFractionOfRaw { get; }
        float TickIntervalSeconds { get; }
    }
}
