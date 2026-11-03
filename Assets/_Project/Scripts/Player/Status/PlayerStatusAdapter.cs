namespace Sol
{
    /// <summary>
    /// The player's side of IStatusTarget. A plain C# adapter: it translates the status contract into the player's own
    /// services (vitals, animation, combat). It reads them through the player context when a status acts, not when it is
    /// built, because those services may register after PlayerController.Awake. Built by PlayerController and pushed into
    /// the player's StatusController. Immunity comes from temporary player modifiers, not from a type flag.
    /// </summary>
    public class PlayerStatusAdapter : IStatusTarget
    {
        private readonly IPlayerContext _context;

        public PlayerStatusAdapter(IPlayerContext context)
        {
            _context = context;
        }

        public string StatusName => "You";

        public void ApplyHealthDelta(float delta, string source) =>
            ServiceLocator.TryGet<IPlayerVitals>()?.ApplyHealthDelta(delta, source);

        public void ApplyEnergyDelta(float delta, string source) =>
            ServiceLocator.TryGet<IPlayerVitals>()?.ApplyEnergyDelta(delta, source);

        public void SetAnimatorTrigger(string trigger) => _context.GetService<IAnimationController>()?.SetTrigger(trigger);

        public void CancelCombat() => _context.GetService<ICombatController>()?.CancelAttack();

        // The player has no immunities yet. Temporary modifiers (potions, talents) will be checked here when they exist.
        public bool IsImmuneTo(IStatusEffect effect) => false;
    }
}
