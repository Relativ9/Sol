using UnityEngine;

namespace Sol
{
    [CreateAssetMenu(fileName = "PlayerStats", menuName = "Player/PlayerStats")]
    public class PlayerStats : ScriptableObject
    {
        [Header("Movement")]
        public float baseWalkSpeed = 5f;
        public float baseRunSpeed = 8f;
        public float baseCrouchSpeed = 3f;
        public float baseJumpForce = 5f;
    
        [Header("Combat")]
        public float baseHealth = 100f;
        public float baseStamina = 100f;
        public float baseFocus = 100f;
        
    }
}
