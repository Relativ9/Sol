using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Lets the player pick up and drop world items. Pickup needs the item within reach and roughly in front of the
    /// player. The item only leaves the world once the inventory has accepted it, so a full inventory never loses one.
    /// Drop spawns the item's prefab at the player's feet and records it in the world, so it persists.
    /// Input is routed here by PlayerController, the same way other player actions are.
    /// </summary>
    public class PlayerPickup : MonoBehaviour, IPlayerComponent, IPlayerPickup
    {
        [SerializeField] private float _reach = 2.5f;

        [Tooltip("How far in front of the player the item must be, measured as the cosine of the angle from forward. " +
                 "0.5 is roughly a 60 degree cone.")]
        [SerializeField, Range(0f, 1f)] private float _viewCone = 0.5f;

        [Tooltip("Logs every candidate's distance and facing when a pickup is attempted. Turn off once tuned.")]
        [SerializeField] private bool _debugPickup = true;

        [Tooltip("Where a dropped item is placed, relative to the player.")]
        [SerializeField] private Vector3 _dropOffset = new Vector3(0f, 0.2f, 0.6f);

        [Tooltip("Metres in front of the view that a grabbed item is held at.")]
        [SerializeField] private float _carryDistance = 1f;

        [Tooltip("The fastest a grabbed item can be moved, in metres per second.")]
        [SerializeField] private float _carryMaxSpeed = 6f;

        // The base pull of a grabbed item toward its target. Lift Strength multiplies it, so the gain is never set by hand.
        // Private: a designer tunes the stats, not this number.
        private const float CarryGain = 2f;

        // How much of each point of Lift Strength reaches the carry. 0.2 means a lift of 10 carries as strongly as a lift of 2
        // at one-to-one, so lift is a steady input rather than a direct multiplier on the pull.
        private const float CarryRatio = 0.2f;


        [Tooltip("The most acceleration a base-strength carrier can give a held item, in m/s². Heavier items need more force " +
                 "to reach this, so a weak carrier cannot lift a heavy item quickly.")]
        [SerializeField] private float _carryMaxAcceleration = 30f;

        private IInventoryService _inventory;
        private IWorldCellWriter _writer;
        private IWorldMotionTracker _motion;
        private IStatsService _stats;

        // The hold state and the carrier. Both are plain C#; this component only feeds them the view and the physics.
        private ItemGrabber _grabber;
        private ItemCarrier _carrier;
        private Transform _view;

        // A taken placed item is recorded in its home cell. A taken dropped pile has its record removed from its cell.
        private void RecordWorldChange(WorldItem target)
        {
            _writer ??= ServiceLocator.TryGet<IWorldCellWriter>();
            if (_writer == null) return;

            if (target.IsDropped)
            {
                if (System.Guid.TryParse(target.ItemInstanceId, out System.Guid guid)) _writer.RecordDroppedRemoved(guid);
            }
            else
            {
                _writer.RecordPlacedTaken(target.ItemInstanceId, target.HomePosition, target.Generation);
            }
        }
        public void Initialize(IPlayerContext context)
        {
            // Services are resolved lazily in the methods, because they are registered after the player starts.
        }

        // The item the player could pick up right now, or null. Refreshed every frame, so the highlight and the label
        // always agree with what E would pick up.
        public WorldItem CurrentTarget { get; private set; }

        // Per-candidate logging is only useful on a pickup attempt, so it is switched on for that one search.
        private bool _logThisSearch;

        void Update()
        {
            if (_grabber == null) _grabber = new ItemGrabber(_carryDistance);
            if (_carrier == null) _carrier = new ItemCarrier(_carryMaxSpeed, CarryGain, _carryMaxAcceleration);

            // While an item is carried, it is the target and the glow stays on it. Otherwise the target is what the view holds.
            if (_grabber.IsGrabbing) SetTarget(null);
            else SetTarget(FindTarget());
        }

        // The carry is driven by Lift Strength, a derived stat that comes from Brawn. The grab reads it through the interface
        // and owns no balance numbers of its own. Without a stats service it falls back to 1, the base lift.
        private float LiftStrength()
        {
            if (_stats == null) return CarryRatio;
            return Mathf.Max(_stats.GetStat(StatTypeEnum.LiftStrength) * CarryRatio, 0.25f);
        }

        // Physics steps move a carried item, since the carry is a velocity that physics then resolves against the world.
        void FixedUpdate()
        {
            if (_grabber == null || !_grabber.IsGrabbing) return;

            // A carried item destroyed mid-carry (for example by a trigger) ends the grab, so nothing is steered at null.
            // The item is gone, so there is no exclusion left to clear on it.
            if (_grabber.Carried == null)
            {
                _grabber.Cancel();
                return;
            }
            if (_view == null) Resolve();
            if (_view == null) return;

            Vector3 target = _view.position + _view.forward.normalized * _carryDistance;
            _carrier.Steer(_grabber.Carried.GetComponent<Rigidbody>(), target, LiftStrength());
        }

        // Moves the target, switching the glow off on the old one and on for the new one. Only the changed items are
        // touched, so an unchanged target is not redrawn every frame.
        private void SetTarget(WorldItem next)
        {
            if (next == CurrentTarget) return;

            if (CurrentTarget != null && CurrentTarget.TryGetComponent(out WorldItemHighlight previous))
                previous.SetTargeted(false);

            CurrentTarget = next;

            if (next != null && next.TryGetComponent(out WorldItemHighlight glow))
                glow.SetTargeted(true);
        }



        /// <summary>
        /// The Grab action's hold completed. The item under the view is picked up into the carry, not looted.
        /// </summary>
        public void BeginGrab()
        {
            Resolve();
            if (_grabber == null) _grabber = new ItemGrabber(_carryDistance);

            // The Input System has already decided this is a hold (the Grab action's Hold interaction). The item under the view
            // is the one carried. Nothing is picked up.
            WorldItem target = FindTarget();
            if (!_grabber.Begin(target)) return;

            // The carried item leaves the world's record and the glow. Its physics is steered from FixedUpdate.
            _motion?.SetCarried(target, true);

            // The carried item passes through the player's body only while held. It collides normally otherwise.
            target.SetPlayerCollisionIgnored(true);
        }

        /// <summary>
        /// The Grab action was released, or cancelled. Ends the carry and leaves the item where it is. The tracker then records
        /// the final pose, so the drop is saved.
        /// </summary>
        public void EndGrab()
        {
            if (_grabber == null || !_grabber.IsGrabbing) return;

            WorldItem released = _grabber.End();
            if (released != null)
            {
                _motion?.SetCarried(released, false);
                released.SetPlayerCollisionIgnored(false);
            }
        }

        /// <summary>Called by PlayerController on the pickup input. Picks up the current target, if any.</summary>
        public void TryPickUp()
        {
            Resolve();
            _logThisSearch = _debugPickup;
            WorldItem target = FindTarget();
            _logThisSearch = false;
            SetTarget(target);
            if (target == null)
            {
                if (_debugPickup) Debug.Log("[PlayerPickup] Pickup pressed, but no item is in reach and in view.", this);
                return;
            }

            ItemDataSO item = target.Item;
            if (item == null) return;

            // The item keeps the identity it had in the world. The inventory and the world record then refer to the same
            // item, so a held item can be recognised on load and its placed copy is not spawned again.
            string instanceId = target.ItemInstanceId;
            if (string.IsNullOrEmpty(instanceId)) return;

            // Each pickup is one individual with its own ID. It joins the first stack of its item that has room, which needs
            // no new cell. Only when no stack has room does it need a cell of its own, and it leads a new stack there.
            bool joined = _inventory.TryAddToStack(item, instanceId);
            if (!joined)
            {
                if (!_inventory.CanStoreAnywhere(item.GridWidth, item.GridHeight))
                {
                    ServiceLocator.TryGet<ICombatLog>()?.Add($"No room for {item.DisplayName}", CombatLogKind.Warning);
                    return;
                }
                if (!_inventory.TryStore(instanceId, item)) return;
            }

            // Stored. The world records it: a placed item is marked taken in its home cell, and a dropped pile's record is removed.
            RecordWorldChange(target);

            target.MarkTaken();
            Destroy(target.gameObject);
            ServiceLocator.TryGet<ICombatLog>()?.Add($"Picked up {item.DisplayName}", CombatLogKind.Info);
        }

        /// <summary>
        /// Drops a stored item at the player's feet. The prefab is spawned from the item's own WorldItem prefab, and the
        /// drop is recorded so it reappears after a reload. Fails, and keeps the item, if the prefab is not set up.
        /// </summary>
        public bool TryDrop(string instanceId)
        {
            Resolve();
            ItemDataSO item = _inventory.GetStored(instanceId);
            if (item == null) return false;

            // The item's own world model is used. It must exist and carry a WorldItem, or the drop is refused before
            // anything is removed from the inventory.
            GameObject prefab = item.WorldPrefab;
            if (prefab == null || prefab.GetComponent<WorldItem>() == null) return false;

            // One individual is dropped. Dropping a stack drops each of its members in turn, so each lands as its own pile
            // with its own instance ID. The individual keeps its identity, so picking it up again matches the same record.
            if (!_inventory.TryRemove(instanceId)) return false;

            Vector3 position = transform.TransformPoint(_dropOffset);
            GameObject spawned = Instantiate(prefab, position, Quaternion.identity);

            var worldItem = spawned.GetComponent<WorldItem>();
            if (worldItem != null) worldItem.Assign(item, instanceId, true);

            // The pile's record goes to the cell it lies in. The pile keeps its GUID, so a later pickup removes this exact record.
            ServiceLocator.TryGet<IWorldCellWriter>()?.RecordDropped(
                System.Guid.Parse(instanceId), item.ItemId, position, spawned.transform.rotation, generation: 0);
            ServiceLocator.TryGet<ICombatLog>()?.Add($"Dropped {item.DisplayName}", CombatLogKind.Info);
            return true;
        }

        // Nearest WorldItem within reach and inside the view cone. The cone uses the camera's forward when there is one,
        // and the player's own forward otherwise.
        // The item under the crosshair: a ray from the centre of the view, within reach. The first world item it hits is
        // the target, so the label and E always refer to the item the player is looking at, never a neighbour. The ray
        // starts at the camera, so it runs straight through the centre of the screen.
        private WorldItem FindTarget()
        {
            // Aiming is the shared service's job. Pickup only asks what the centre of the view is pointing at, and keeps the
            // hit only if it is a world item that can still be aimed at.
            IAimService aim = ServiceLocator.TryGet<IAimService>();
            if (aim == null || !aim.TryAimAtCentre(_reach, out AimHit hit)) return null;

            if (hit.Target is not WorldItem item || !item.IsAimable) return null;

            if (_logThisSearch)
                Debug.Log($"[PlayerPickup] Aimed at '{item.name}' at {hit.Distance:0.00} m.", item);
            return item;
        }

        // Registered here as well as with PlayerController, because the UI looks services up through the ServiceLocator.
        void Awake()
        {
            ServiceLocator.RegisterService<IPlayerPickup>(this);
        }

        // Stats are fetched here, after every Awake has registered, as the KB's Start rule requires.
        void Start()
        {
            _stats = ServiceLocator.TryGet<IStatsService>();

        }

        private void Resolve()
        {
            _inventory ??= ServiceLocator.TryGet<IInventoryService>();
            _writer ??= ServiceLocator.TryGet<IWorldCellWriter>();
            _motion ??= ServiceLocator.TryGet<IWorldMotionTracker>();

            // The view the carried item is held in front of. The camera's transform, through its interface.
            if (_view == null)
                _view = ServiceLocator.TryGet<ICameraController>()?.GetCameraTransform();
        }
    }
}
