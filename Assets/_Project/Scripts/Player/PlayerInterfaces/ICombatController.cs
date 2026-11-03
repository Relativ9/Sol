using UnityEngine;

namespace Sol
{
    public interface ICombatController : IPlayerComponent
    {
        void ToggleWeaponState();
        bool IsWeaponDrawn();
        void HandleAttackInput();
        CombatState GetCurrentCombatState();

        /// <summary>True from the start of an attack until its swing has ended. The weapon rig uses it to release the hand.</summary>
        bool IsAttacking();

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
