# Sol: Architecture Map

Unity 6000.4.6f1, HDRP, Input System, Cinemachine, Visual Scripting.

Gameplay code lives in `Assets/_Project/Scripts/` (asmdef `Sol_Scripts`, namespace `Sol`). Other folders hold third-party content (Kevin Iglesias animations, TextMesh Pro), editor tools, and backup or scratch folders.

## 1. Layers

```
BOOTSTRAP (Persistant Scene)
  ServiceLocator    static registry, DontDestroyOnLoad, DefaultExecutionOrder(-100)
  GameInitializer   Awake: create SaveService + ProgressionService
                    Start: load or create save, init stats, progression, talents

CORE SERVICES (MonoBehaviours or plain C#)
  Time/World : TimeManager (ITimeManager), CelestialCalculator (ICelestialCalculator)
  Stats      : StatsService (IStatsService)
  Progression: ProgressionService (IProgressionService, plain C#)
  Talents    : TalentTreeGenerator (ITalentTreeGenerator)
               TalentStateService (ITalentStateService, plain C#)
  Save       : SaveManager (ISaveManager, debounced) -> SaveService (ISaveService)
  UI         : UIStateService, UIManager, VirtualCursor (+ VirtualCursorController),
               UIInputController, TalentTreeController

EVENT BUS (ScriptableObject)
  GameEvent.Raise() -> GameEventListener.OnEventRaised() -> UnityEvent
  Assets: OnDayChanged, OnSeasonChanged, OnYearChanged, OnStatsChanged,
          OnTalentChanged, OnGameStateChanged, OnImmediateSaveRequest,
          TalentWheelToggle, TalentWheelReady

DATA (ScriptableObjects)
  Time   : WorldTimeData, SeasonalData, WeatherData
  Stats  : PlayerStatSO, PlayerProgressionSO
  Items  : WeaponDataSO, EquipmentDataSO, OffhandDataSO, WearableDataSO
  Talents: TalentNodeDataSO, TreeLayoutSO, TreeLayoutCollectionSO, TalentTreeSettingsSO
  UI     : UIState

PLAYER (PlayerController implements IPlayerContext)
  PlayerController  input routing, movement-state selection,
                    local service dictionary (GetService<T>)
  Locomotion        Movement, Jumping, GravityController, GroundChecker,
                    CameraController, PlayerAnimationController
  Combat            CombatStateController, AnimationEventDispatcher
  Modifiers         Modifier_MovementSpeedBoost, Modifier_GravityDirectionZone,
                    Modifier_GravityMultiplierTrigger (use IStatsService)

WORLD / PRESENTATION
  CelestialRotator, SkyboxRotator, CloudController, WindController,
  SkyAndFogController (HDRP Volume), SnowCoverController, Rotator,
  SpeedVisualizer, ClockAndDate
```

## 2. Service registry

Global registry, via `ServiceLocator.RegisterService<I>()` and `ServiceLocator.Get<I>()`:

| Interface | Implementation | Registered in | Consumers |
|---|---|---|---|
| `ISaveService` | `SaveService` (plain C#) | `GameInitializer.Awake` | `SaveManager`, `GameInitializer` |
| `IProgressionService` | `ProgressionService` (plain C#) | `GameInitializer.Awake` | `SaveManager`, `TalentTreeController` |
| `ITimeManager` | `TimeManager` | `TimeManager.Awake` | `PlayerController`, `CelestialRotator`, `CloudController`, `UIManager` |
| `ICelestialCalculator` | `CelestialCalculator` | `TimeManager.Awake` | `CelestialRotator` |
| `IStatsService` | `StatsService` | `StatsService.Awake` | `Movement`, `GravityController`, the 3 `Modifier_*` scripts, `TalentTreeController` |
| `ITalentTreeGenerator` | `TalentTreeGenerator` | `TalentTreeGenerator.Awake` | `GameInitializer`, `TalentTreeController`, `UIManager` |
| `ITalentStateService` | `TalentStateService` (plain C#) | `TalentTreeGenerator.Awake` | `SaveManager`, `GameInitializer`, `TalentTreeController`, `UIManager` |
| `ISaveManager` | `SaveManager` | `SaveManager.Awake` | `UIManager` |
| `IUIStateService` | `UIStateService` | `UIStateService.Awake` | `UIManager` |
| `IUIManager` | `UIManager` | `UIManager.Awake` | `UIInputController` |
| `IVirtualCursor` | `VirtualCursor` (plain C#) | `VirtualCursorController` | `UIManager` |
| `ICameraController` | `CameraController` | `CameraController.Awake` | `UIManager`, `Movement` |

Local registry: `PlayerController` fills a `Dictionary<Type, object>` in its own `Awake`, and components call `context.GetService<T>()`. It covers `IGroundChecker`, `IAnimationController`, `ICameraController`, `ICombatController`, and `IGravityController`.

## 3. Key data flows

1. **Startup:** `GameInitializer.Awake` creates and registers `SaveService` and `ProgressionService`. `Start` loads or creates the save, then initializes `StatsService`, `ProgressionService`, `TalentTreeGenerator.LoadData()`, and `TalentStateService`, and sets the spent-points count.
2. **Save:** Talent changes raise events. A `GameEventListener` on the `SaveManager` object calls `QueueSave()`. `SaveManager` debounces (`debounceInterval = 2s`) and writes through `SaveService`. `UIManager` can request an immediate save.
3. **Time:** `TimeManager` raises `OnDayChanged`, `OnSeasonChanged`, and `OnYearChanged`. Other systems poll its properties (`CelestialTime`, `CurrentSeasonIndex`) and read `WorldTimeData`.
4. **Input:** The Input System calls `PlayerController.OnAttack`, `OnJump`, and related handlers, which forward to the player components. `DetermineActiveMovementState()` picks the active movement state each frame from the `IBaseMovement.CanBeActivated()` priority list.
5. **Stats:** Modifiers call `IStatsService` to apply effects. `StatsService` marks its cache dirty and raises `OnStatsChanged`.
6. **UI:** `UIInputController` -> `UIManager` -> `UIStateService` / `VirtualCursor` / `TalentTreeController`. `UIManager` pauses time through `ITimeManager.TogglePause()`.

## 4. Scenes and assemblies

- **Scenes:** `Persistant Scene.unity` holds the bootstrap objects. `OutdoorsScene`, `CharacterCreation`, and `Test Scene` are gameplay scenes. `OutdoorsScene.unity` contains none of the core class names, so it likely depends on the persistent scene being loaded alongside it (not verified).
- **Assemblies:** `Assets/_Project/Scripts/Sol_Scripts.asmdef` is the only asmdef in the project. Code elsewhere (for example `Assets/_Project/Systems/PlayerInput.cs`, `Assets/Editor/*`) is outside it.

## 5. Risks and notes

- **Global locator:** Most systems find each other through `ServiceLocator`, so the persistent scene must load first. `Get` returns null and logs an error if a service isn't registered yet.
- **Init-order coupling:** `GameInitializer` depends on `Awake` order across several objects.
- **Mixed patterns:** Interfaces, events, the global locator, the local player registry, and direct `[SerializeField]` references are all in use.
- **Inactive code:** `WeatherManager.cs` is fully commented out. `WeaponService.cs`, `WeaponRigController.cs`, and others are also commented out.
- **Stray files:** `Assets/SnowCover.cs` sits outside `_Project`. Several `_oldBackup`, `_Recovery`, and `Untitled` folders exist.

