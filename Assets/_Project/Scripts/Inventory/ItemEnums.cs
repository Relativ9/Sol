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
        TowerShield,
        Focus,
        Torch
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
        Offhand,
        // Gauntlets replaced the old left-arm slot. Equipment is saved by slot name, so a renamed slot is skipped on load.
        Gauntlets
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

    // Material tiers from the armour design doc (section 5). Appended, so existing values keep their numbers.
    public enum MaterialTier
    {
        Iron,
        Steel,
        HardenedSteel,
        Titanium,
        AethericWeave
    }

    public static class MaterialTierExtensions
    {
        // Material efficiency multiplies armour value: Weight x Type Multiplier x Efficiency.
        public static float Efficiency(this MaterialTier tier)
        {
            return tier switch
            {
                MaterialTier.Iron => 0.8f,
                MaterialTier.Steel => 1.0f,
                MaterialTier.HardenedSteel => 1.2f,
                MaterialTier.Titanium => 1.6f,
                MaterialTier.AethericWeave => 2.0f,
                _ => 1.0f,
            };
        }
    }

}

