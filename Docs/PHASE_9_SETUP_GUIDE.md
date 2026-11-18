# Phase 9: Enemy System - Complete Setup Guide

**Setup instructions for basic Bandit enemy prefab with full AI, combat, and loot systems.**

---

## Part 1: Create Enemy Stats Asset (ActorStatSO)

### Step 1.1: Create Bandit_Stats

1. Navigate to: `Assets/_Project/ScriptableObjects/Enemies/` (create folder if needed)
2. Right-click → **Create** → **Sol/Enemies/Enemy Stats**
3. Name it: **`Bandit_Stats`**

### Step 1.2: Configure Stats (Inspector)

**CORE ATTRIBUTES:**
- Brawn: 12 (more damage)
- Finesse: 11 (better accuracy/stealth)
- Insight: 9 (**KEY**: Drives perception! 9 * 0.05 = 45% vision/hearing boost)
- Focus: 10 (crit chance)
- Vigor: 14 (health: 14 * 5 = 70 HP)
- Willpower: 8 (energy)

**COMBAT:**
- Base Armor Value: 2.0 (light leather)
- All resistances: 0 (no special resistance)
- Rigid/Flex/Cloth Weight: Set to match equipment (0 for no armor)
- Stagger/Bleed Resistance: 0

**STATUS IMMUNITIES:**
- Immunities: None (bandits affected by all status effects)

**EQUIPMENT & LOOT:**
- Equipped Items: [Bandit Sword] (assign sword ItemDataSO if available)
- Loot Table: [Gold Coin, Lockpick] (random drops on death)

**AI BEHAVIOR:**
- Disposition: -1.0 (hostile to player)
- Aggressiveness: 0.7 (charges, but tactical)

**ATTRIBUTE SCALING:**
(Leave defaults unless tuning difficulty)
- insightToPerception: 0.05 ← **KEY SETTING**

**Perception Calc:** Insight 9 * 0.05 = 0.45 → Vision 20m * 1.45 = 29m, Hearing 10m * 1.45 = 14.5m


---

## Part 2: Create Enemy Prefab in Hierarchy

### Step 2.1: Base GameObject

1. Create empty GameObject: **`Bandit`** at (0, 1, 0)
2. Add child **Cube** (body placeholder)
   - Scale: (0.6, 1.8, 0.6) ← humanoid
   - Material: Red (visibility)
3. Add child **Capsule** (weapon placeholder, named **`Weapon`**)
   - Scale: (0.1, 0.6, 0.1)
   - Position: (0.4, 0.8, 0) ← sword at side
   - Material: Gray/metallic

### Step 2.2: Physics

On **Bandit** (root):
- Add **Rigidbody**: Dynamic, Gravity ON, Freeze Rotation (X,Y,Z)
- Add **CapsuleCollider**: Radius 0.3, Height 1.8, Center (0, 0.9, 0), **Is Trigger OFF**

On **Weapon** (child):
- Add **SphereCollider**: Radius 0.2, Center (0, 0.3, 0), **Is Trigger ON** ← **CRITICAL!**
- This is your attack hitbox. Player hit when weapon overlaps player's collider.

### Step 2.3: AI Components (Bandit root)

Add in order:
1. **EnemyStats** → Actor Stat SO: Bandit_Stats
2. **TargetHealth** → Max Health: 70, Display Name: "Bandit"
3. **StatusController** → (auto-init with TargetHealth)
4. **EnemySenses** → Vision Range: 20, Hearing Range: 10, Sprint Threshold: 5, Vision Interval: 0.1
5. **EnemyLocomotion** → Stopping Dist: 0.5, Max Speed: 3.5, Speed Param: "Speed"
6. **EnemyBehavior** → Attack Range: 2.0, Alert Timeout: 5s, Attack Cooldown: 2.0s
7. **EnemyMeleeAttack** → **Drag Weapon/SphereCollider** into Weapon Hitbox field, Damage Profile: Bandit_Sword_Damage
8. **NavMeshAgent** → Agent Type: Humanoid, Base Offset: 0.9, Stopping Dist: 0.5
9. **Animator** → Controller: Bandit_Animator (create next)

### Step 2.4: Animator Setup

1. Create **Animator Controller**: `Assets/_Project/Animators/Bandit_Animator`
2. Add **5 States**: Idle (default), Walk, Attack, Stagger, Dead
3. Add **Transitions**:
   ```
   Idle ↔ Walk        (Speed > 0.5, bidirectional)
   Idle → Attack      ("Attack" trigger, exit time OFF)
   Attack → Idle      (exit time 1.0)
   Idle → Stagger     ("Stagger" trigger, exit time OFF)
   Stagger → Idle     (exit time 0.5)
   Any → Dead         ("Death" trigger, exit time OFF)
   ```
4. Add **Tags** to states: "Idle", "Walk", "Attack", "Stagger", "Dead"
5. Add **Parameters**: Speed (float), Attack (trigger), Stagger (trigger), Death (trigger)
6. Assign to Animator component on Bandit

### Step 2.5: Damage Profile

If creating new:
1. Right-click in Project → **Create** → **Sol/Combat/Damage Profile**
2. Name: **`Bandit_Sword_Damage`**
3. Configure:
   - Damage Type: Physical
   - Base Amount: 15
   - Tags: Blade (enables bleed procs)
   - Stagger Effect: (assign if available)
   - Bleed Effect: (assign if available)


---

## Part 3: Scene Setup & NavMesh

### Step 3.1: NavMesh Baking (CRITICAL!)

Without NavMesh, EnemyLocomotion won't work.

1. **Window** → **AI** → **Navigation**
2. Click **Bake** tab
3. Configure:
   - Agent Radius: 0.3
   - Agent Height: 1.8
   - Max Slope: 45
   - Step Height: 0.3
4. In Hierarchy, select terrain/ground
   - Inspector → **Navigation Static**: ON
   - Mark as "Walkable"
5. Select obstacles
   - Mark as "Not Walkable"
6. Click **Bake** button
   - Should see light blue overlay on terrain

### Step 3.2: Create Test Scene

1. **File** → **New Scene** → Save as `Test_Enemy_AI.unity`
2. Add scene elements:
   - **Plane** (scale 10x10, mark Walkable in Navigation)
   - **Cube** obstacle (position 2, 0.5, 2, mark Not Walkable)
   - **Directional Light**
   - **Player** (from your prefab, tag it "Player" ← **CRITICAL!**)
   - **Bandit** prefab instance
3. Bake NavMesh (as above)

### Step 3.3: Make Bandit a Prefab

Drag **Bandit** from Hierarchy to:
`Assets/_Project/Prefabs/Enemies/Bandit.prefab`

Instance in Hierarchy turns blue = now linked to prefab


---

## Part 4: Testing

### Step 4.1: Play

1. **Play** button
2. Move player toward Bandit
   - At ~29m: Bandit detects (green vision gizmo)
   - Enters Alerted state
   - State transition logged to console
3. Approach within 2m
   - Bandit Attacks
   - Attack animation plays
   - Weapon hitbox activates
   - You take damage
4. Move away (> 2m)
   - Bandit Pursues
   - Navigates around obstacles (NavMesh)
5. Sprint near Bandit
   - Hearing range increases 1.5x (yellow gizmo expands)
6. Get hit & staggered
   - Bandit attack cancels mid-swing
   - Stagger animation plays
   - Bandit idles, cooldown resets

### Step 4.2: Debug

**Scene View (while playing):**
- Toggle **Gizmos** (top-right)
- Green sphere = vision range (20m base + perception bonus)
- Yellow sphere = hearing range (10m base, 1.5x when sprinting)
- Green line = LOS to player
- Light blue = NavMesh

**Console:**
- `[EnemyBehavior] Bandit: Idle → Alerted` = state transitions
- Errors = missing components

**Common Issues:**
- Bandit doesn't detect: Check Insight > 0, EnemySenses assigned, Player tagged
- Doesn't move: Check NavMesh baked, NavMeshAgent assigned
- Attack doesn't hit: Check Weapon collider Is Trigger ON
- No animation: Check Animator assigned, params match (Speed, Attack, Stagger)


---

## Part 5: Tuning Difficulty

### Make Bandit See Farther

Open `Bandit_Stats`:
- Increase **Insight** (9 → 11)
- Perception = 11 * 0.05 = 0.55 (55% bonus)
- Vision: 20 * 1.55 = 31m

### Make Bandit Hit Harder

Option 1: Open `Bandit_Sword_Damage`
- Increase **Base Amount** (15 → 25)

Option 2: Open `Bandit_Stats`
- Increase **Brawn** (12 → 15)
- Adds Brawn * 0.5 = +7.5 damage

### Make Bandit Attack Faster

Open `EnemyBehavior` on Bandit:
- Decrease **Attack Cooldown Seconds** (2.0 → 1.5)

### Make Bandit Tougher

Open `Bandit_Stats`:
- Increase **Vigor** (14 → 18)
- Health = 18 * 5 = 90 HP

### Make Bandit More Aggressive

Open `Bandit_Stats`:
- Increase **Aggressiveness** (0.7 → 0.9)
- (Used by future tactics AI, affects behavior decisions)

