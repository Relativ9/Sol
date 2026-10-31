using System.Collections.Generic;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Records where physics-driven world items come to rest, and how fast they move while they fly. Only bodies that are
    /// awake are tracked, so a still object costs nothing. Once a second a tracked body's pose is sampled and written if it
    /// moved. A body that stays nearly still for half a second is settled: its resting pose is written at once and it is
    /// dropped from the list. A save reads the live pose of every tracked body, so a save taken mid-flight is accurate.
    /// </summary>
    public class WorldMotionTracker : MonoBehaviour, IWorldMotionTracker
    {
        [Tooltip("Seconds between pose samples for a moving body.")]
        [SerializeField] private float _sampleInterval = 1f;

        [Tooltip("Below this speed (m/s) a body counts as still for settling.")]
        [SerializeField] private float _restSpeed = 0.05f;

        [Tooltip("How long a body must stay still before it is settled, in seconds.")]
        [SerializeField] private float _settleTime = 0.5f;

        // The pose a body last had when it was written, so a sample only writes a real change.
        private const float PositionThreshold = 0.01f;   // metres (1 cm)
        private const float AngleThreshold = 2f;         // degrees

        private sealed class Tracked
        {
            public WorldItem Item;
            public Rigidbody Body;
            public Vector3 LastWrittenPosition;
            public float LastWrittenYaw;
            public float StillTime;
            public CellKey PinnedCell;
            public bool Carried;
        }

        private readonly List<Tracked> _tracked = new();
        private float _sampleTimer;

        // Resolved once, in Start, after every Awake has registered its services. Never fetched in Awake.
        private IWorldCellWriter _writer;
        private IWorldStreamer _streamer;

        /// <summary>The number of bodies being tracked right now.</summary>
        public int TrackedCount => _tracked.Count;

        // Items call Track from their own Start, which can run before this tracker's Start. So the services are resolved on
        // first use, from whichever comes first. By then every Awake has registered, which is the rule the services need.
        private void Resolve()
        {
            _writer ??= ServiceLocator.TryGet<IWorldCellWriter>();
            _streamer ??= ServiceLocator.TryGet<IWorldStreamer>();
        }

        private void Update()
        {
            _sampleTimer += Time.deltaTime;
            bool sampleNow = _sampleTimer >= _sampleInterval;
            if (sampleNow) _sampleTimer = 0f;

            for (int i = _tracked.Count - 1; i >= 0; i--)
            {
                Tracked t = _tracked[i];
                if (t.Item == null || t.Body == null)
                {
                    Release(t);
                    _tracked.RemoveAt(i);
                    continue;
                }

                // A carried body is held by the player, not by physics. It is neither settled nor sampled until it is released.
                if (t.Carried) continue;

                // A body that has gone to sleep is still at rest. Count the stillness, and settle once it lasts.
                bool still = t.Body.IsSleeping() || t.Body.linearVelocity.magnitude < _restSpeed;
                t.StillTime = still ? t.StillTime + Time.deltaTime : 0f;

                if (t.StillTime >= _settleTime)
                {
                    WriteBody(t, moving: false);
                    Release(t);
                    _tracked.RemoveAt(i);
                    continue;
                }

                // The body may have moved into another cell since it was pinned, so the pin follows it.
                MovePinIfNeeded(t);

                if (sampleNow) WriteBody(t, moving: true);
            }
        }

        /// <summary>Starts tracking a physics-driven item. Called when its body wakes. Items already tracked are ignored.</summary>
        public void Track(WorldItem item)
        {
            if (item == null || item.IsTaken) return;
            if (_tracked.Exists(t => t.Item == item)) return;

            var body = item.GetComponent<Rigidbody>();
            if (body == null || body.isKinematic) return;

            var t = new Tracked
            {
                Item = item,
                Body = body,
                LastWrittenPosition = item.transform.position,
                LastWrittenYaw = item.transform.eulerAngles.y,
            };

            // A moving body keeps its cell loaded, so it cannot fly into one that has been released.
            Resolve();
            Pin(t);
            _tracked.Add(t);
        }

        // Pins the cell the body is in. The cell is recorded so the matching unpin releases the same one.
        private void Pin(Tracked t)
        {
            if (_streamer == null) return;
            t.PinnedCell = CellKey.FromWorld(t.Item.transform.position);
            _streamer.Pin(t.PinnedCell);
        }

        // Moves the pin to the body's new cell when the body crosses a border while in flight.
        private void MovePinIfNeeded(Tracked t)
        {
            if (_streamer == null) return;
            CellKey now = CellKey.FromWorld(t.Item.transform.position);
            if (now == t.PinnedCell) return;

            _streamer.Unpin(t.PinnedCell);
            _streamer.Pin(now);
            t.PinnedCell = now;
        }

        // Releases a body's pin once it has settled or is gone.
        private void Release(Tracked t)
        {
            if (_streamer == null) return;
            _streamer.Unpin(t.PinnedCell);
        }

        /// <summary>Marks a tracked item as carried or released. On release, its pose is written at once, so the carry ends saved.</summary>
        public void SetCarried(WorldItem item, bool carried)
        {
            Tracked t = _tracked.Find(x => x.Item == item);
            if (t == null)
            {
                // Not tracked (for example a body that was not awake). Track it so the release can be saved.
                if (!carried) Track(item);
                return;
            }

            t.Carried = carried;
            if (!carried)
            {
                t.StillTime = 0f;
                WriteBody(t, moving: false);
            }
        }

        /// <summary>
        /// Writes the live pose of every tracked body. Called before a save, so the save reflects the frame it is taken on, not
        /// the last one-second sample.
        /// </summary>
        public void FlushLive()
        {
            Resolve();
            foreach (Tracked t in _tracked)
            {
                if (t.Item == null || t.Body == null) continue;

                // A carried item is held by the player. Its pose is not a world change until it is released, so it is not saved.
                if (t.Carried) continue;

                WriteBody(t, moving: t.Body.linearVelocity.magnitude >= _restSpeed);
            }
        }

        // Writes a body's pose through the cell writer, but only when it has really moved, so jitter writes nothing.
        private void WriteBody(Tracked t, bool moving)
        {
            Vector3 position = t.Item.transform.position;
            float yaw = t.Item.transform.eulerAngles.y;

            bool changed = Vector3.Distance(position, t.LastWrittenPosition) > PositionThreshold
                           || Mathf.Abs(Mathf.DeltaAngle(yaw, t.LastWrittenYaw)) > AngleThreshold;
            if (!changed && moving) return;

            if (_writer != null)
            {
                if (t.Item.IsDropped)
                {
                    if (System.Guid.TryParse(t.Item.ItemInstanceId, out System.Guid guid))
                    {
                        _writer.RecordDropped(guid, t.Item.Item.ItemId, position, t.Item.transform.rotation, ++t.Item.Generation);
                    }
                }
                else
                {
                    // The body's velocity is saved with a moving record, so a load can resume the motion.
                    Rigidbody body = t.Body;
                    _writer.RecordPlacedMoved(t.Item.ItemInstanceId, t.Item.HomePosition, position, t.Item.transform.rotation,
                        ++t.Item.Generation, body.linearVelocity, body.angularVelocity, moving);
                }
            }

            t.LastWrittenPosition = position;
            t.LastWrittenYaw = yaw;
        }
    }
}
