Sol RPG - Armor System Design Overview

## 1. System Overview

The armor system rejects traditional "Light vs. Heavy" binaries in favor of a **physics-based classification** determined by material construction: **Plate (Rigid)**, **Flex (Flexible)**, and **Cloth**. All protection derives from **Weight** modified by **Type** and **Material Efficiency**, creating a spectrum where protection density, mobility, and status vulnerability exist in tension.

**Core Philosophy:**

- Weight determines raw protection value
- Armor type (plate, flex, cloth) determines mobility penalties and status resistances.
- Material efficiency allows lighter, stronger gear without breaking the weight-based formula (IE, Dragon Bone Armor (IE Titanium is lighter than Steel, but still also provides a higher armor value)
- No armor provides immunity; only degrees of mitigation and probability management

---

All armor uses diminishing returns, so damage can never be fully negated and the most any defence can do is 80%.

**Step 1: Armor value.** Armor Value = Weight (kg) × Type Multiplier × Material Efficiency.

**How it reaches the character.** Armor value is a contribution, not a base. `ArmorValue` keeps its starting value from the stat asset (the character's own armour, `baseArmorValue`, which is 0 by default). The equipped armour total is added on top through `SetSourceContribution` under `Equipment_ArmourTotals`, so it is recalculated from the equipped pieces on every change and nothing compounds. Weight and piece counts use the same path, and resistances read them through `GetStat`.

**Step 2: Effective resistance.** Effective Resistance = the resistance for the damage type, minus the attacker's Penetration, kept between 0 and 400 points.

**Step 3: Damage taken.** Damage Taken = Raw Damage × 100 / (100 + Effective Resistance).

**Points:** resistance and penetration are entered as points, where 5 = 5%. The same curve applies to every damage type, physical included.

**Cap:** 400 points, which is 80% mitigation at most. Penetration is capped at the same 400, so it can cancel at most 80% of a defence.

**Elemental and magic resistance:** elemental resistance (fire, ice, void, sonic, soul) is strong but only works against its own element. Magic resistance is weaker but general, and it counts for half its value toward each elemental type. Physical damage uses Armor Value alone.

**Penetration:** an attacker stat that cancels resistance. It comes from Finesse, equipment and talents, and it is capped with resistance.

**Display:** capped stats show as `value/400` on the stats screen. Armor value is a plain number.

---

## 2. Damage Mitigation Formula



## 3. Armor Classifications

### 3.1 Plate (Rigid)

_Solid plates, scales, laminates, or solid chitin/ceramic matrices that resist deformation._

Like Flex, Plate armor can be heavy or light (though usually never quite as light as lightest flex armor), the main difference is the increased armor value (4x weight instead of 3x) and the ability to deflect hits. For this increased protection however, you have to sacrifice some mobility and the armor cannot be folded up to take less space in the inventory. In addition to this plate armor provides Stagger Resistence, instead of Bleed resistence which Flex armor provides.

- **Type Multiplier:** 4× Weight
- **Deflection:** 3% per equipped plate piece, up to 15% for a full set of five (see section 6)
- **Energy Penalty:** +20% sprint cost, +10% jump cost, +50% climbing cost
- **Noise Penalty:** +40% enemy detection radius (loud clanking)
- **Inventory:** Occupies full grid space; cannot be packed or compressed
- **Status Mitigation:** Contributes **Stagger Resistance** at 0.2 points per kg of plate (rigid weight x 0.2)
- **Vulnerability:** Susceptible to **Bleed** (no native bleed resistance)

### 3.2 Flex (Flexible)

_Interlinked rings, woven metal, layered leather, brigandine, or articulated scale._ 

Flex armor can be heavy or light (though usually never quite as heavy as the heaviest plate armor), the main difference from plate armor is that it has no mobility cost debuffs, detection radius penalty, can be packed smaller in the inventory, and provides no deflection chance. The other main difference is that unlike Plate armor it provides no protection against stagger effects, though it does provide a meaningful protection against bleed (which Plate doesn't). 

- **Type Multiplier:** 3× Weight
- **Base Deflection:** 0% (no deflection)
- **Energy Penalty:** None;
- **Noise Penalty:** +10% enemy detection radius (jingles/creaks)
- **Inventory:** Can be **Packed** (rolled/folded), reducing grid space by 50%; requires 3 seconds to unpack
- **Status Mitigation:** Contributes **Bleed Resistance** at 0.333 points per kg of flex (flexible weight x 0.333)
- **Vulnerability:** Susceptible to **Stagger** (no native stagger resistance)

### 3.3 Cloth (Unarmored Protection)

_Mainly aesthetic casual, survival, and fancy clothes. Intended as options for people who want to go for full spellcaster and/or nightstalker archetypes, not suitable for direct combat but can increase lethality_

Mostly intended as aesthetic (base clothes) while it does technically provide some armor (as it follows the same weight based rules) its multiplier is 1x and cloth naturally weighs very little so it ends up being super weak protection wise. But cloth does come with some benefits, being completely silent, taking minimal grid space, and lowering the energy cost of all physical feats (running, jumping, climbing, and melee attacking). Cloth also doesn't have its own talent tree associated with it but gets 2-3 talents focused on it in the Flex tree (cloth armor value calculated just like Flex armor (x3 armor weight), wearing full cloth gives passive dodge chance (10½), and wearing full cloth increases spell power by 20%). 

- **Type Multiplier:** 1× Weight
- **Base Deflection:** 0%
- **Energy Penalty:** -25% dodge/roll cost (maximum mobility)
- **Noise Penalty:** 0% (silent)
- **Inventory:** Minimal grid space; no packing required
- **Status Mitigation:** None
- **Talent Synergy:** Enables "Unarmored" talent tree bonuses

---

## 4. Status Effects & Cross-Vulnerability

**Critical rule:** both plate and flex wearers can suffer **both** stagger and bleed. Armor type decides which one you resist, not which one you can suffer.

**How a status chance is worked out, for every status:**

1. The **attacker** sets a base chance. It comes from the weapon's damage type and tags (the doctrine below), plus the attacker's own bonuses from stats, enchantments and talents.
2. The **receiver** subtracts its own status resistance from that chance. The resistance is points, so 5 points = 5 percentage points off.
3. The result is never below **5%**.
4. The status is rolled **once**, with the chance from step 3.

A hit with no status effect (for example, a weapon with no stagger effect) can never cause that status, whatever the armor.

### 4.1 Stagger (Concussive Interrupt)

**Effect:** 0.5-second movement and combat lock, a knockback away from the attacker, and a 15 energy drain. It cancels any attack in progress.

**Base chance:** 20%, plus **15%** if the hit is physical and blunt (blunt and polearm weapons). Total 35% against an unarmored target.

**Resistance:** plate armor gives **Stagger Resistance** at **0.2 points per kg** of plate. Flex and cloth give none.

**Example:** a 60 kg mail wearer has no stagger resistance, so a mace has its full 35% chance. A 75 kg plate wearer has 75 × 0.2 = 15 points of resistance, so the mace's 35% drops to 20%.

**Cross-vulnerability:** flex armor adds nothing to stagger resistance, so a flex wearer is always fully exposed to stagger.

### 4.2 Bleed (Penetration Trauma)

**Effect:** damage over time equal to **30% of the raw weapon damage**, spread over 5 seconds, one tick per second. Each tick deals 6% of the raw damage. Bleed stacks per source, and a new bleed from the same source refreshes its duration.

**Proc condition:** the hit must deal more than zero damage after mitigation.

**Base chance:** 25%, plus **20%** if the hit is physical and blade or piercing. Total 45% against an unarmored target.

**Resistance:** flex armor gives **Bleed Resistance** at **0.333 points per kg** of flex. Plate and cloth give none.

**Example:** a 60 kg flex wearer has 60 × 0.333 = 20 points of bleed resistance, so a sword's 45% drops to 25%.

**Cross-vulnerability:** plate armor adds nothing to bleed resistance, so a plate wearer is always fully exposed to bleed.

### 4.3 Doctrine summary

| Status | Base | Weapon bonus | Resistance from | Points per kg | Floor |
|---|---|---|---|---|---|
| Stagger | 20% | +15% if physical blunt | Plate (rigid weight) | 0.2 | 5% |
| Bleed | 25% | +20% if physical blade or piercing | Flex (flexible weight) | 0.333 | 5% |

Both are subtractive, not multiplicative, and both are rolled once per hit.

---


## 5. Material Efficiency Tiers

Material efficiency represents technological/magical advancement, allowing endgame armors to break the "heavy = strong" paradigm without invalidating weight-based formulas.

|Material|Efficiency|Description|Weight Trait|
|---|---|---|---|
|Iron|0.8|Heavy, crude, early-game|High mass|
|Steel|1.0|Baseline standard|Standard|
|Hardened Steel|1.2|Tempered, improved durability|Standard|
|Titanium|1.6|Advanced alloy, aerospace/light|**Lightweight**|
|Aetheric Weave|2.0|Magical/enhanced materials|**Minimal weight**|

**Example:** 10kg Titanium Plate (10×4×1.6 = **64 Armor**) vs 15kg Steel Plate (15×4×1.0 = **60 Armor**). Titanium is lighter AND stronger, but provides **less Stagger Resistance** (2% vs 3% per kg).

---

## 6. Deflection System (Plate Only)

Plate armor can deflect an attack entirely. A deflected hit does no damage and applies no status effects.

- **Source:** each equipped plate piece adds **3%** deflection. A full set of five plate pieces gives **15%**, the maximum before talents.
- **Flex and cloth never deflect.** Only plate contributes.
- **Derivation:** Deflection = number of plate pieces (up to 5) × 3%. It is derived from equipment. No attribute feeds it.
- **Mechanic:** roll on each physical hit. If it succeeds, damage is zero and no status effect is applied.
- **Visual:** metallic sparks, blade skidding, arrow ricochet.
- **Talent scaling:** talents may raise deflection above 15%. The ceiling is not yet decided.
- **Limitation:** a failed deflection roll does not stop stagger or bleed. Only a successful deflection does.

---


## 7. Weapon Counter-Synergy

Weapons directly counter specific armor types through status bonuses.

|Weapon Class|Stagger Bonus|Bleed Bonus|Optimal Target|Avoid Against|
|---|---|---|---|---|
|**Daggers**|+0%|+10%|Weak points/Cloth|Heavy Flex (bleed immune)|
|**Swords**|+5%|+20%|Plate (high bleed proc)|High Flex (60kg+)|
|**Axes**|+10%|+15%|General purpose|—|
|**Maces/Hammers**|**+15%**|+0%|**Plate** (forced stagger)|Cloth (overkill)|
|**Spears/Picks**|+5%|+5%|Plate (AP ignores armor)|—|
|**Greatswords**|+10%|+15%|Mixed|—|

**Example:** War Hammer (35% base stagger) vs 75kg Plate wearer: 35%−15%=20% stagger chance35%−15%=20% stagger chance (still dangerous).

---

## 8. Talent Integration

**Underlayer (Plate Tree):** Adds **+15kg Virtual Flexible Weight** to Bleed calculations only. Simulates gambeson worn beneath plate. Does not add to Armor Value or inventory weight.

**Distributed Padding (Flex Tree):** Adds **+10kg Virtual Rigid Weight** to Stagger calculations only. Represents hardened leather splints sewn into mail.

**Silent Movement (Flex Tree):** Reduces Flex noise penalty from +10% to +2%.

**Colossus (Plate Tree):** Heavy Spaulders unlock, adding +14kg Rigid Weight exclusive to Stagger calculation (can exceed normal equip load for status purposes only).

---

## 9. Example Armor Reference

### 9.1 Early Era (Iron/Steel)

_Standardized militia gear, heavy but reliable._

|Set|Component|Weight|Material|Type|Armor Value|Stagger|Bleed|Notes|
|---|---|---|---|---|---|---|---|---|
|**Gambeson**|Full Set|3kg|Cloth (1.0)|Cloth (1×)|3|20%|18%|Silent, -25% dodge cost|
|**Iron Mail**|Torso|12kg|Iron (0.8)|Flex (3×)|29|20%|21%|Packable|
|**Iron Mail**|Legs|8kg|Iron (0.8)|Flex (3×)|19|20%|22.3%|—|
|**Steel Plate**|Torso|15kg|Steel (1.0)|Plate (4×)|**60**|17%|25%|+15% Deflection|
|**Steel Plate**|Full Set|45kg|Steel (1.0)|Plate (4×)|180|11%|25%|**Heavy**, loud|
|**Heavy Plate**|Full Set|75kg|Steel (1.0)|Plate (4×)|300|**5%**|25%|Stagger-immune, slow|

### 9.2 Mid Era (Hardened/Ceramic)

_Professional adventurer gear, specialization emerges._

|Set|Weight|Material|Type|Armor Value|Dmg Mitig*|Stagger|Bleed|
|---|---|---|---|---|---|---|---|
|**Brigandine** (Flex)|14kg|Steel (1.0)|Flex (3×)|42|17.4%|20%|20.3%|
|**Tempered Mail**|20kg|H. Steel (1.2)|Flex (3×)|72|26.5%|20%|18.3%|
|**Ceramic Plate**|6kg|Ceramic (1.4)|Plate (4×)|34|14.5%|18.8%|25%|
|**Heavy Steel Plate**|22kg|Steel (1.0)|Plate (4×)|88|30.6%|15.6%|25%|

*Mitigation vs a 50 damage hit, using the curve in section 2 with no penetration: Damage taken = 50 x 100 / (100 + Armor Value). Shown in the table as a percentage reduction.

### 9.3 Late Era (Exotic Materials)

_Masterwork and magical constructs._

|Set|Weight|Material|Type|Armor Value|Stagger|Bleed|Special|
|---|---|---|---|---|---|---|---|
|**Titanium Plate**|10kg|Ti (1.6)|Plate (4×)|64|18%|25%|Light, mobile plate|
|**Titanite Plate**|14kg|Ti (1.6)|Plate (4×)|90|17.2%|25%|High protection, low stagger resist|
|**Titanite Weave**|60kg|Ti (1.6)|Flex (3×)|288|20%|**5%**|**Bleed-immune**, packable|
|**Aetherweave**|8kg|Aether (2.0)|Flex (3×)|48|20%|22.3%|Self-repairing, 0 noise|
|**Voidsteel**|12kg|Aether (2.0)|Plate (4×)|96|17.6%|25%|Absorbs magical damage|

---

## 10. Strategic Build Archetypes

### The Colossus (75kg Pure Plate)

- **Armor:** Full Steel/Titanite Plate with Tower Shield
- **Mitigation vs 50 dmg:** ~30-35% reduction
- **Stagger:** 5% (immune to interrupt)
- **Bleed:** 25% (vulnerable to blades)
- **Tactics:** Frontline anchor,_requires underlayer talent or bleed potions to survive sustained blade assaults. Cannot climb, limited inventory space.

### The Warden (Hybrid 40/20)

- **Armor:** 40kg Plate Torso/Legs + 20kg Mail Underlay
- **Mitigation:** Mixed (~25% total)
- **Stagger:** 12% (40kg × 0.2 = 8% reduction)
- **Bleed:** 18.3% (20kg × 0.333 = 6.6% reduction)
- **Tactics:** Jack-of-all-trades, can pack the mail when not needed, moderate noise, versatile against mixed weapon types.

### The Scout (60kg Pure Flex)

- **Armor:** Full Titanite/Riveted Mail
- **Mitigation vs 50 dmg:** ~25-28% reduction
- **Stagger:** 20% (vulnerable to hammers)
- **Bleed:** 5% (immune to hemorrhage)
- **Tactics:** Long-range expeditionary, carries massive loot (packed armor), silent movement talents, fears maces more than swords.

### The Ghost (3kg Cloth)

- **Armor:** Quilted gambeson + Unarmored talents
- **Mitigation:** <2%
- **Status:** Full vulnerability (20% stagger, 25% bleed)
- **Tactics:** Total reliance on dodge i-frames, no inventory penalties, climbs/swims effortlessly, exploits backstab multipliers (1.2× damage).

---

## 11. Repair & Maintenance

- **Plate:** Requires **Armorer's Kit** (2×2 grid, 3kg) or anvil access; cannot field repair easily
- **Flex:** Requires **Scraps/Leather** (common, 1×1 grid); can be repaired mid-dungeon with needle and thread
- **Cloth:** Repaired with basic cloth; no tools required

This loop reinforces the **Plate = Base Assault** vs **Flex = Expedition** dichotomy: Plate users must return to civilization, while Flex users can survive indefinitely in the wilderness.

---

**Document Control:**  
Version 2.0 supersedes all previous armor calculation documents.  
**Related Systems:** Energy Mechanics, Inventory Grid, Talent Trees (Plate/Flex/Unarmored), Weapon Penetration Values.