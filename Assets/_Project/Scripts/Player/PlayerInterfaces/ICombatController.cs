using UnityEngine;

namespace Sol
{
    public interface ICombatController : IPlayerComponent
    {
        void ToggleWeaponState();
        bool IsWeaponDrawn();
        void HandleAttackInput();
        CombatState GetCurrentCombatState();

        /// <summary>Cancels an attack in progress (e.g. when staggered). Safe to call when not attacking.</summary>
        void CancelAttack();
    }
    
    public enum CombatState
    {
        Sheathed,
        Unsheathing,
        Unsheathed,
        Sheathing,
        Attacking,
        ChargingAttack,
        DirectionalAttackPending
    }
}
