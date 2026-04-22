namespace Sol
{
    public enum ItemCategory
    {
        Weapon, //Blades, Blunt weapons, Staffs etc.
        Offhand, //One-handed secondary weapons, Shields, Thorch/Lanterns, Magic Focuses, 
        Wearable, //Armor, clothing, jewelery 
        Throwable, //Throwing knives, caltrops, alchemical "granades" (ie simple spell replacements), rocks (lures for stealth), etc. 
        Consumable, //Includes potions, food and drink, temporary weapon enchantments (weapon oils, poison, etc). 
        Ingredient, //Crafting ingredients
        Valuable,  //Non-usable "junk" items that have a sufficiently high value or can be used in place of ore to melt down into ingots (ie 1xSilver Vase = 1 Silver ingot instead of 4x Silver ore)
        Junk //Low value junk items typically not even accepted by most vendors (expect special "junk vendors") should pretty much never be worth picking up. 
    }

    public enum WeaponType
    {
        Blade,
        Blunt, 
        Staff,
        Ranged
    }

    public enum WeaponSize
    {
        OneHanded,
        TwoHanded
    }

    public enum ArmorType
    {
        Plate,
        Flex,
        Cloth
    }

    public enum OffhandType
    {
        Shield,
        Focus,
        Torch,
        SecondaryWeapon
    }

    public enum EquipmentSlot
    {
        None,
        Head,
        Face,
        Neck,
        Torso,
        Hands,
        Legs,
        Feet,
        RingLeft,
        RingRight,
        MainHand,
        Offhand
    }

    public enum SlotStatus
    {
        Empty,
        Occupied,
        Blocked
    }
    
    public enum WearableCategory
    {
        Armor,
        Accessory
    }

}

