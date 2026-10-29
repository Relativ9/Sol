# Sol RPG — World State Persistence

Status: **Design agreed and written (Phase 0 done). Implementation starts at Phase 1.**
Read with `# Sol RPG — Architecture Knowledgebase.md` §12 and §13.

---

## 1. Goals

- Save size and load time must not grow with the number of untouched objects. An untouched world costs nothing to save.
- A save only writes what changed, and only the parts of the world that changed.
- Loading reads only the parts of the world near the player, and does not scan the scene.
- The player file stays small and human-readable. World data is compact and binary.
- A crash mid-save must never lose a cell, and must never leave an item in two places with no way to pick the right one.

Target scale: a 1 km² village with roughly 3,000–5,000 placed items and 40–50 NPCs per 5,000 m². Thousands of enemies and tens of thousands of objects across an open world.

---

## 2. Decisions

| Area | Decision |
|---|---|
| Cell size | **100 m** square cells. Cell = `floor(worldX / 100)`, `floor(worldZ / 100)`. Divides evenly into 200 m terrains. |
| Ownership | An object belongs to the cell that **contains its position**. This applies to placed and dropped items alike. |
| Persistence | **Atomic rewrite** of dirty cells only. No append log, no compaction timer. |
| Save triggers | **Quicksave** hotkey, plus **autosave** every 10 minutes of unpaused play. Nothing writes on each change. |
| Save on quit | **None.** The player uses a Save and Quit entry in its own UI tab. |
| Moving items | Sampled **once per second** while the rigidbody is awake and registered. The final resting pose is recorded on settle. |
| Velocity | Saved for moving items only (linear and angular). Read live at save time. |
| Identity | GUID for designers and runtime. **Build-time integer index** in saves. |
| Player file | JSON. Stats, attributes, talents, equipment, inventory. No world data. |
| World files | Binary, one file per dirty cell per save slot. Interiors keyed by scene ID. |
| Autosave in combat | Allowed for now. Gated by one `CanAutosave()` check so combat can block it later. |

---

## 3. Save layout

```
saves/<slot>/player.json
saves/<slot>/cells/<cellX>_<cellZ>.bin
saves/<slot>/interiors/<sceneId>.bin
```

- Quicksave and autosave are separate slots. An autosave never overwrites a manual slot's world state.
- Only dirty cells are written. A cell that never changed has no file.
- Cell coordinates are signed integers, so negative positions are valid.

---

## 4. Identity

Two IDs exist for every world object, each with a clear job.

| ID | Type | Used by | Stability |
|---|---|---|---|
| `ItemInstanceId` (GUID string) | `WorldItem._itemInstanceId` | Designers, inventory, logs, the runtime | Stable for the life of the object. Written once by the editor tool. |
| `ObjectIndex` (int32) | Build-time table | Cell files | Assigned once at build time. **Never reused.** |

### 4.1 The build index

- A `WorldObjectRegistry` ScriptableObject maps `ObjectIndex` to `ItemInstanceId` and to the item type (`ItemId`). It lives at `Assets/_Project/Systems/WorldObjectRegistry.asset`.
- **Automatic.** Giving a placed object its GUID also registers it, in the same call (`ItemInstanceIdEditor.Regenerate`, through `WorldObjectRegistryAccess`). The scene-save path (`AssignMissing`) goes through `Regenerate`, so both paths register automatically. Designers never create or link the registry.
- **Retirement.** When a scene is saved with a single scene open, any registry entry whose GUID is no longer in the scene is retired. Its index stays reserved. Retirement is skipped when several scenes are open, since an object may live in another scene.
- An index is never reused. Deleting an object leaves a gap in the table. Save files written against an index stay valid after content changes.
- Dropped items are created at runtime. They have no build index. They carry their GUID in the cell file, plus an `ItemId` index.

### 4.2 Item type index

- `ItemId` (string) maps to a `uint16` item index through `ItemTypeRegistry`, at `Assets/_Project/Systems/ItemTypeRegistry.asset`. Index 0 means "no item", so issued indices start at 1.
- **Automatic.** `ItemTypeIndexer` (an `AssetPostprocessor`) registers an item's `ItemId` whenever an item asset is imported or saved. An item with no `ItemId` is logged and skipped, since an index for it would name nothing.
- **Retirement.** After an import pass that touches an item asset or deletes one, any registered `ItemId` with no live asset is retired. Its index stays reserved.
- The string `ItemId` stays the designer-facing name and is still what the player file stores. Cell files use the index.

### 4.3 Runtime lookup

- At load, the registry is read once into two dictionaries: `index → object` and `GUID → index`.
- Nothing in gameplay code uses an index. Gameplay uses `ItemInstanceId`, as it does now. The index exists only at the save boundary.

---

## 5. Cell key and ownership

### 5.1 Key

```
cellX = floor(worldX / 100)
cellZ = floor(worldZ / 100)
```

- Keys are computed from world position only, never from the scene that loaded the object. Two district scenes that share a cell see the same file.
- Each cell is identified by its `(cellX, cellZ)` pair. A cell file name is `<cellX>_<cellZ>.bin`.

### 5.2 Ownership

- Every placed and dropped item is owned by the cell that contains its current position.
- Ownership changes only when the item's position crosses a cell boundary, and the item is at rest or has settled.
- A placed item has a **home cell** (where it was authored). Home cell does not decide ownership. It is used only to find the tombstone that must exist there while the item is displaced (section 6.2).

### 5.3 Moving an item across a cell border

A move between cells touches two files, and the order is fixed:

1. Write the **destination** cell with the item's new record and a higher generation.
2. Write the **source** cell, removing the item or adding a tombstone.

If the game dies between the two writes, the item exists in both cells. The generation number resolves which copy wins (section 6.3). Losing an item is never acceptable. Duplicating one is recoverable.

---

## 6. Records and generations

### 6.1 Records

Every record in a cell file describes one object in that cell.

| Field | Size | Notes |
|---|---|---|
| `kind` | 1 byte | `0` placed, `1` dropped, `2` tombstone |
| `flags` | 1 byte | bit 0 taken, bit 1 moving, bit 2 has velocity |
| `objectIndex` / `itemIndex` | 4 bytes | placed uses `ObjectIndex`; dropped uses `itemIndex` (uint16 widened) |
| `guid` | 16 bytes | dropped only. Runtime GUID. |
| `generation` | 4 bytes | increments on every change to this object |
| `pose` | 12 + 2 bytes | position in cm (3 × int32), yaw in tenths of a degree (uint16) |
| `velocity` | 12 bytes | moving only. Linear and angular, each 3 × int16 (cm/s and centi-rad/s). |

- A resting placed record is about 17 bytes. A resting dropped record is about 33 bytes. A moving record adds 12 bytes.
- Field order and widths are fixed by the format version. Adding a field means a new version.

### 6.2 Tombstones

- A tombstone is written to the **home cell** of a placed item that has left it. It says "this object now lives in cell `(x, z)`, generation `g`", so the object does not respawn at its authored spot on load.
- Tombstones are written only for displaced authored items. A home cell with no displaced items writes nothing.

### 6.3 Generations resolve duplicates

- Each object has a generation. It starts at `0` when authored, and increments on every change to its record.
- If the same object appears in two cells, the record with the **higher generation** is the live one. The other is discarded at load, and that cell is rewritten on the next save.
- A tie is a bug. It is logged, and the copy in the cell that contains the object's saved position is kept.

## 7. Cell file format

### 7.1 Layout

```
Header (fixed 32 bytes)
  magic        4   "SOLC"
  version      2   format version, starts at 1
  reserved     2
  cellX        4   int32
  cellZ        4   int32
  recordCount  4   uint32
  bodyLength   4   uint32, bytes after the header
  crc32        4   uint32, over the body
  reserved     4

Body: recordCount records, back to back
```

- The header is read first. A file whose magic, version, or length is wrong is rejected without reading the body.
- The CRC is checked over the full body before any record is applied. A cell with a bad CRC is not applied, and is logged. Its previous good copy (section 8) is used.
- All integers are little-endian. Writes use `BinaryWriter` on a `MemoryStream`, so the file is built in memory first.

### 7.2 Size

- A typical 100 m cell holds 30–100 placed items. A cell with no changes has no file.
- A cell where a few things moved is a few hundred bytes. A cell where a battle moved dozens of objects is a few KB.
- A whole village (about 100 cells) that is heavily disturbed stays well under a megabyte. Most cells are never written.

### 7.3 Versioning

- The version number changes on any change to a record's fields. The loader reads only the versions it knows, and rejects others with a logged message. Old saves are out of scope while the game is in development, so no migration is required.

---


## 8. Atomic write

Each dirty cell is written the same way.

1. Serialise the cell into a memory buffer, including its CRC.
2. Write `<cell>.tmp`, then flush to disk.
3. Read `<cell>.tmp` back and verify its CRC. A failed verify deletes the temp file and keeps the live file.
4. Move the live `<cell>.bin` to `<cell>.bak`, then move `<cell>.tmp` to `<cell>.bin`.

- If the game dies before step 4, the live file is still the last good copy.
- If it dies between the two moves, `.bak` holds the last good copy and the loader falls back to it when `.bin` is missing or fails its CRC.
- The loader tries `.bin`, then `.bak`. If neither is valid, the cell is treated as untouched and a warning is logged.

### 8.1 Platform note

`File.Move` on Windows fails when the destination exists. The replace therefore uses the move-aside pattern in step 4 (live to `.bak`, then temp to live), not an overwrite. This must be tested on Windows and Linux before the save layer is considered done.

---

## 9. Dirty tracking and saving

- Every change to a cell marks it dirty: a pickup, a drop, a move across or within a cell, a tombstone, a tracker sample that moved a pose by more than 1 cm or 2°, and a settle.
- A save writes the dirty cells, then clears each cell's dirty flag only after its write succeeds. A failed write keeps the cell dirty so the next save retries it.
- A save runs in order: dirty cells first, then `player.json` last. A slot with a new player file therefore always has world files at least as new.
- Only one save runs at a time. A save requested during a save is queued, not run in parallel.


### 9.1 Quicksave and autosave

- **Quicksave** is bound to a hotkey (F5 proposed). It runs immediately, writes to the quicksave slot, and shows a confirmation.
- **Autosave** accumulates unpaused play time from `TimeManager`. Every 10 minutes it writes to the autosave slot.
- Both go through `SaveCoordinator.RequestSave(slot, reason)`. Neither calls the file layer directly.
- Autosave is skipped when a save is already running, when a scene is loading, or when `CanAutosave()` returns false. `CanAutosave()` returns true today. It is the single place combat or other states can block autosave later.

### 9.2 Save and Quit

- Save and Quit is an entry in the pause menu's own UI tab, next to Options. It calls `SaveCoordinator.RequestSave(manual)` and waits for completion before quitting.
- Nothing saves on application quit.

---

## 10. Motion sampling

### 10.1 Who moves

Only physics-driven items are tracked: items whose rigidbody is awake. Static and settled items are not tracked, so a still object costs nothing.

### 10.2 The tracker

- One `WorldMotionTracker` in the persistent scene owns the set of tracked bodies. A body registers when its rigidbody wakes, and unregisters when it sleeps.
- Once per second, the tracker samples each tracked body. If the pose changed by more than 1 cm or 2°, the cell is marked dirty and the sampled pose and velocity are kept.
- A body that stays below a small speed threshold for about 0.5 s is treated as settled. Its final pose is recorded at once, its moving flag is cleared, and it unregisters.
- There is no per-frame work and no allocation in the steady state. The tracker's list is reused.

### 10.3 Live read at save

- A save reads the **live** pose and velocity of every body the tracker holds, not the last 1 Hz sample. This makes a quicksave or autosave taken mid-flight accurate to the frame.
- Only a crash between saves falls back to the last 1 Hz sample, which is at most one second old.


### 10.4 Velocity

- A moving record stores linear and angular velocity. A resting record stores none.
- On load, a moving body is spawned at its saved pose, given its saved velocities, and then woken so physics continues the motion.
- The resume is approximate. Contacts and frame timing mean the path will not match frame-for-frame. It continues the same arc under the same gravity.

### 10.5 Moving items in unloaded cells

- A moving body in a cell that unloads is saved at its unload pose, then despawned. It does not keep flying while unloaded. It resumes from that pose when its cell loads again.
- Cells that contain a moving body, and the cells next to them, are pinned loaded while the body is moving (section 11.3). This stops a ball from flying into an unloaded cell.

---

## 11. Streaming and load

### 11.1 Load ring

- Cells within one ring (a 3×3 block, about 300 m across) of the player's current cell are loaded. The ring size is configurable.
- When the player crosses into a new cell, cells that enter the ring are read, and cells that leave it are released.

### 11.2 Reads and applies

- A cell file is read and its CRC checked on a background thread.
- Records are applied on the main thread. Placed items register with their cell when they spawn, so nothing scans the whole scene. This replaces the current `FindObjectsByType` in `WorldItem.ApplySavedState`.
- A cell leaving the ring is written first if dirty, then released.

### 11.3 The pin rule

- A cell is pinned (never released) while it holds a moving body, a displaced placed item within the ring, or a dropped item the player is carrying.
- Pins are reference-counted by the cell, so a cell is released only when no pin remains.

### 11.4 Interiors

- Interiors are separate additive scenes, loaded async. Each interior has its own file, keyed by scene ID: `interiors/<sceneId>.bin`.
- An interior's records use the same record format. Its key is the scene ID, not world position.
- Interiors do not use the 100 m grid.

---

## 12. Player file

- `player.json` holds: level, attributes, talents, equipped items, inventory grid and stacks, and player vitals.
- It loses `pickedUpItemInstanceIds` and `droppedItemInstances`. World state moves entirely to cell files.
- It stays JSON, pretty-printed, because it is small and easy to inspect.

---

## 13. Phases

| Phase | Work | Status |
|---|---|---|
| 0 | This design doc | Done |
| 1 | Identity: `WorldObjectRegistry` and `ItemTypeRegistry`, both with automatic registration | Written, not yet compiled |
| 2 | Save triggers and slots: quicksave, autosave timer, `SaveCoordinator`, Save and Quit tab. Remove per-change save. | Not started |
| 3 | Cell store: keys, binary codec, CRC, atomic write, dirty tracking | Written, not yet compiled. Files in `Scripts/Core/WorldStore/`: `CellKey`, `CellRecord`, `Crc32`, `CellCodec` (+ `.Read`), `AtomicFile`, `CellStore`. |
| 4 | Streaming: async load, main-thread apply, placed-item registration, pin rule | Not started |
| 5 | Motion tracker: 1 Hz sampling, live read at save, settle, velocity | Not started |
| 6 | Migrate pickup, drop, split, respawn onto the cell store. Player file loses world fields. | Not started |
| 7 | Tests: codec round trip, cell key maths, CRC rejection, atomic replace, generation resolution | Not started |

Phase 2 is independent of the cell work and can be tested alone, so it may be done first.

---

## 14. Open risks

- **Pop-in** for displaced placed items when their home cell is not loaded. The pin rule limits this, but it isn't zero.
- **Migration bugs** for dropped items that settle across a cell border. Tests must cover this.
- **Cross-platform replace** semantics for the atomic write (section 8.1).
- **Save during load.** Autosave must not fire while cells are still being read.
- **Combat gating.** `CanAutosave()` exists but returns true. Combat rules are not yet defined.
- **Test framework.** The Unity Test Framework is not confirmed in `Packages/manifest.json`. Tests in phase 7 depend on it.


