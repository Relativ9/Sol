namespace Sol
{
    /// <summary>
    /// The attacker's contribution to a hit's status procs. The attacker says WHAT effect it can cause and its BASE chance
    /// (from its own stats and enchantments). The receiver then reduces that chance by its own resistance and rolls once.
    /// An effect of null means that status cannot be caused by this hit.
    /// </summary>
    public readonly struct StatusAttack
    {
        public readonly IStatusEffect StaggerEffect;
        public readonly float StaggerBaseChance;
        public readonly IStatusEffect BleedEffect;
        public readonly float BleedBaseChance;

        public StatusAttack(IStatusEffect staggerEffect, float staggerBaseChance, IStatusEffect bleedEffect, float bleedBaseChance)
        {
            StaggerEffect = staggerEffect;
            StaggerBaseChance = staggerBaseChance;
            BleedEffect = bleedEffect;
            BleedBaseChance = bleedBaseChance;
        }
    }
}
