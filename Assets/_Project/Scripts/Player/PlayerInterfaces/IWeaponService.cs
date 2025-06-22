using UnityEngine;

namespace Sol
{
    public interface IWeaponService
    {
        WeaponType GetEquippedWeaponType();
        void EquipWeapon(string weaponId);
        float GetWeaponStat(string statName);
        bool HasWeaponEquipped();
        WeaponData GetEquippedWeaponData();

    }
    
    public enum WeaponType
    {
        None, 
        Unarmed,
        OneHanded,
        TwoHanded,
        Dagger,
        Staff,
        Bow
    }
}
