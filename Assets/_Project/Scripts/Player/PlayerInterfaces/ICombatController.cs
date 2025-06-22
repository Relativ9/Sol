using UnityEngine;

namespace Sol
{
    public interface ICombatController : IPlayerComponent
    {
        void ToggleWeaponState();
        bool IsWeaponDrawn();
        void HandleAttackInput();
        CombatState GetCurrentCombatState();
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
