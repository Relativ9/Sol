using UnityEngine;

namespace Sol
{
    [CreateAssetMenu(fileName = "PlayerStats", menuName = "Player/PlayerStats")]
    public class PlayerStats : ScriptableObject
    {
        [Header("Movement")]
        public float baseMoveSpeed = 3f;
        public float baseRunMultiplier = 1.6f;
        public float baseDeceleration = 8f;
        public float baseCrouchSpeed = 3f;
        
        [Header("Jump")]
        public float baseJumpForce = 5f;
        public float baseJumpDirectionBoost = 1.0f;
        public int baseMaxDoubleJump = 0;
        public float baseJumpCooldown = 0.1f;

        [Header("Gravity")]
        public float baseGravityMultiplier = 1f;
        public float baseTerminalVelocity = -20f;
    
        [Header("Combat")]
        public float baseHealth = 100f;
        public float baseStamina = 100f;
        public float baseFocus = 100f;
        
        // [Header("Camera")]
        // public float baseFOV = 60f;
        
    }
}
