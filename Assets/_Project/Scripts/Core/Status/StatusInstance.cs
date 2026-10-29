namespace Sol
{
    /// <summary>
    /// One active status on a target. Stacks are counted here, not as separate instances, so a listing screen
    /// can show "Bleeding x3" with the remaining time. Plain data: StatusController owns the rules.
    /// </summary>
    public class StatusInstance
    {
        public IStatusEffect Effect;
        public string SourceId;
        public int Stacks;
        public float RemainingSeconds;      // Scaled game seconds left.
        public float SecondsUntilNextTick;  // Scaled game seconds until the next tick. Unused when the effect has no ticks.
        public float DamagePerTickPerStack; // Health removed per stack, per tick. 0 when the effect has no ticks.
    }
}
