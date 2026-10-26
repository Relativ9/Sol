namespace Sol
{
    public interface ITalentEffectContext
    {
        IStatsService StatsService { get; }
        // Future: IAbilityService AbilityService { get; }
    }
}
