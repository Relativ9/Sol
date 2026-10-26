namespace Sol
{
    /// <summary>
    /// Anything that can be hit by a DamageInfo. Attacks don't know what they hit: they only build the hit,
    /// and the receiver runs the calculator and applies whatever it has (vitals, statuses, stats).
    /// IDamageable stays as the older float contract used by the existing enemy attack path.
    /// </summary>
    public interface IDamageReceiver
    {
        void ReceiveHit(DamageInfo info);
    }
}
