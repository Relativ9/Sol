using System.Collections.Generic;
using UnityEngine;

namespace Sol
{
    public class WeaponService : MonoBehaviour, IWeaponService, IPlayerComponent
    {
                [Header("Weapon Configuration")]
        [SerializeField] private WeaponData[] _availableWeapons;
        [SerializeField] private string _defaultWeaponId = "BasicSword";
        
        // Dependencies
        private IPlayerContext _context;
        private IStatsService _statsService;
        
        // State
        private WeaponData _equippedWeapon;
        private Dictionary<string, WeaponData> _weaponDatabase = new Dictionary<string, WeaponData>();
        
        public void Initialize(IPlayerContext context)
        {
            _context = context;
            _statsService = context.GetService<IStatsService>();
            
            // Initialize weapon database from available weapons
            InitializeWeaponDatabase();
            
            // Equip default weapon
            EquipWeapon(_defaultWeaponId);
            
            Debug.Log($"Weapon service initialized with {_weaponDatabase.Count} weapons");
        }
        
        private void InitializeWeaponDatabase()
        {
            // Clear existing database
            _weaponDatabase.Clear();
            
            // Add all available weapons to the database
            if (_availableWeapons != null && _availableWeapons.Length > 0)
            {
                foreach (var weapon in _availableWeapons)
                {
                    if (weapon != null && !string.IsNullOrEmpty(weapon.weaponId))
                    {
                        _weaponDatabase[weapon.weaponId] = weapon;
                        Debug.Log($"Added weapon to database: {weapon.displayName} ({weapon.weaponId})");
                    }
                    else
                    {
                        Debug.LogWarning("Skipped null or invalid weapon in available weapons list");
                    }
                }
            }
            else
            {
                Debug.LogWarning("No weapons configured in WeaponService");
                
                // Create a fallback weapon if none are configured
                CreateFallbackWeapons();
            }
        }
        
        private void CreateFallbackWeapons()
        {
            // Create a basic sword as fallback
            WeaponData basicSword = ScriptableObject.CreateInstance<WeaponData>();
            basicSword.weaponId = "BasicSword";
            basicSword.displayName = "Basic Sword";
            basicSword.weaponType = WeaponType.OneHanded;
            basicSword.baseDamage = 10f;
            basicSword.attackSpeed = 1.0f;
            basicSword.range = 1.5f;
            
            _weaponDatabase[basicSword.weaponId] = basicSword;
            
            Debug.Log("Created fallback weapon: Basic Sword");
        }
        
        public WeaponType GetEquippedWeaponType()
        {
            return _equippedWeapon != null ? _equippedWeapon.weaponType : WeaponType.None;
        }
        
        public void EquipWeapon(string weaponId)
        {
            if (_weaponDatabase.TryGetValue(weaponId, out WeaponData weapon))
            {
                _equippedWeapon = weapon;
                
                // Update context state
                _context.SetStateValue("EquippedWeaponId", weaponId);
                _context.SetStateValue("EquippedWeaponType", (int)weapon.weaponType);
                
                Debug.Log($"Equipped weapon: {weapon.displayName} ({weapon.weaponType})");
            }
            else
            {
                Debug.LogWarning($"Weapon not found in database: {weaponId}. No weapon equipped.");
                _equippedWeapon = null;
                
                // Update context state
                _context.SetStateValue("EquippedWeaponId", "");
                _context.SetStateValue("EquippedWeaponType", (int)WeaponType.None);
            }
        }
        
        public float GetWeaponStat(string statName)
        {
            // If we have a valid equipped weapon, get its stat
            if (_equippedWeapon != null)
            {
                float baseStat = _equippedWeapon.GetStat(statName);
                
                // Apply player stat modifiers if stats service is available
                if (_statsService != null)
                {
                    // Get weapon type specific modifier
                    string weaponTypeModifier = $"{_equippedWeapon.weaponType}Modifier";
                    float typeModifier = _statsService.GetStat(weaponTypeModifier);
                    
                    // Get stat specific modifier
                    string statModifier = $"{statName}Modifier";
                    float specificModifier = _statsService.GetStat(statModifier);
                    
                    // Apply modifiers
                    return baseStat * typeModifier * specificModifier;
                }
                
                return baseStat;
            }
            
            // Default values for common stats if no weapon equipped
            switch (statName.ToLower())
            {
                case "damage": return 5f;
                case "attackspeed": return 1.0f;
                case "range": return 1.0f;
                default: return 0f;
            }
        }
        
        public bool HasWeaponEquipped()
        {
            return _equippedWeapon != null;
        }
        
        public WeaponData GetEquippedWeaponData()
        {
            return _equippedWeapon;
        }
        
        public bool CanBeActivated()
        {
            return true; // Weapon service is always active
        }
        
        public void OnActivate()
        {
            // Nothing special needed
        }
        
        public void OnDeactivate()
        {
            // Nothing special needed
        }
    }
}
