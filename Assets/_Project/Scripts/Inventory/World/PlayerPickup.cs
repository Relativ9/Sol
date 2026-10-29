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

        private IInventoryService _inventory;
        private IWorldItemService _world;
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
            SetTarget(FindTarget());
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

            // Stored. Now the world forgets it. Placed items are marked picked up; dropped items are removed.
            if (target.IsDropped) _world.RemoveDropped(instanceId);
            else _world.MarkPickedUp(instanceId);

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

            _world.AddDropped(instanceId, item.ItemId, position, spawned.transform.rotation);
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

        private void Resolve()
        {
            _inventory ??= ServiceLocator.TryGet<IInventoryService>();
            _world ??= ServiceLocator.TryGet<IWorldItemService>();
        }
    }
}
