using UnityEngine;

namespace Sol
{
    public class UpdateFrequencyOptimizer : IUpdateFrequencyOptimizer
    {
        private float _updateFrequency;
        private float _lastUpdateTime;

        /// <summary>
        /// Creates a new update frequency optimizer
        /// </summary>
        /// <param name="updateFrequency">Updates per second (e.g., 30 = 30 FPS)</param>
        public UpdateFrequencyOptimizer(float updateFrequency)
        {
            _updateFrequency = Mathf.Max(0.1f, updateFrequency);
            _lastUpdateTime = 0f;
        }

        public bool ShouldUpdate(float currentTime)
        {
            if (currentTime - _lastUpdateTime >= (1f / _updateFrequency))
            {
                _lastUpdateTime = currentTime;
                return true;
            }
            return false;
        }

        public void SetUpdateFrequency(float newFrequency)
        {
            _updateFrequency = Mathf.Max(0.1f, newFrequency);
        }

        public float CurrentFrequency => _updateFrequency;

        public void Reset()
        {
            _lastUpdateTime = 0f;
        }
    }
}
