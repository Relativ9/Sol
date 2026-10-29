namespace Sol
{
    /// <summary>
    /// Counts unpaused play time and asks for an autosave every interval. It only counts while the game is running, so
    /// menus and pauses do not trigger saves. It does not decide whether a save is allowed: that is the coordinator's job.
    /// Plain C#, driven by a host's Update with the frame's delta.
    /// </summary>
    public class AutosaveTimer
    {
        private readonly SaveCoordinator _coordinator;
        private readonly float _intervalSeconds;
        private float _elapsed;

        /// <param name="coordinator">Receives the autosave requests.</param>
        /// <param name="intervalSeconds">Play time between autosaves. 600 is ten minutes.</param>
        public AutosaveTimer(SaveCoordinator coordinator, float intervalSeconds)
        {
            _coordinator = coordinator;
            _intervalSeconds = intervalSeconds > 0f ? intervalSeconds : 600f;
        }

        /// <summary>Seconds of unpaused play since the last autosave request.</summary>
        public float Elapsed => _elapsed;

        /// <summary>
        /// Adds play time. Call once per frame with the frame's delta. While paused nothing is added. When the interval is
        /// reached, an autosave is requested and the count restarts. A refused request still restarts the count, so a
        /// refusal (for example during combat) does not cause a burst of saves the moment it ends.
        /// </summary>
        public void Tick(float deltaSeconds, bool isPaused)
        {
            if (isPaused || deltaSeconds <= 0f) return;

            _elapsed += deltaSeconds;
            if (_elapsed < _intervalSeconds) return;

            _elapsed = 0f;
            _coordinator.RequestSave(SaveReason.Autosave);
        }
    }
}
