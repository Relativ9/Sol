using UnityEngine;
using TMPro;

namespace Sol
{
    /// <summary>
    /// Simple digital clock display using TextMeshPro
    /// Shows hours:minutes:seconds and month day for 20-hour planetary days
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class DigitalClock : MonoBehaviour, IClock
    {
        [Header("Clock Configuration")]
        [Tooltip("Time manager reference (auto-found if null)")]
        [SerializeField] private TimeManager timeManager;

        [Tooltip("Automatically update display")]
        [SerializeField] private bool autoUpdate = true;

        [Tooltip("Update frequency in seconds")]
        [SerializeField] private float updateInterval = 1f;

        [Header("Display Format")]
        [Tooltip("Show date below time")]
        [SerializeField] private bool showDate = true;

        [Tooltip("Date format: {month} {day} or {day} {month}")]
        [SerializeField] private bool dayFirst = false;

        [Header("Visual Settings")]
        [Tooltip("Text color for normal display")]
        [SerializeField] private Color normalColor = Color.white;

        [Tooltip("Text color when time is paused")]
        [SerializeField] private Color pausedColor = Color.red;

        // Components
        private TextMeshProUGUI textComponent;

        // Update tracking
        private float lastUpdateTime;
        private bool isInitialized;

        #region IClock Implementation

        public bool IsAutoUpdating => autoUpdate;

        public void SetAutoUpdate(bool autoUpdate)
        {
            this.autoUpdate = autoUpdate;
        }

        public void UpdateDisplay(int hours, int minutes, int seconds, string monthName, int dayOfMonth)
        {
            if (textComponent == null) return;

            // Format time (20-hour format: 00-19)
            string timeText = $"{hours:D2}:{minutes:D2}:{seconds:D2}";

            // Format date if enabled
            string dateText = "";
            if (showDate)
            {
                dateText = dayFirst ? $"{dayOfMonth} {monthName}" : $"{monthName} {dayOfMonth}";
            }

            // Combine time and date
            string displayText = showDate ? $"{timeText}\n{dateText}" : timeText;

            // Update display
            textComponent.text = displayText;

            // Update color based on time manager state
            UpdateTextColor();
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Get TextMeshPro component
            textComponent = GetComponent<TextMeshProUGUI>();
            if (textComponent == null)
            {
                Debug.LogError($"[DigitalClock] TextMeshProUGUI component not found on {gameObject.name}!");
                enabled = false;
                return;
            }

            // Set initial color
            textComponent.color = normalColor;
        }

        private void Start()
        {
            // Find TimeManager if not assigned
            if (timeManager == null)
            {
                timeManager = FindObjectOfType<TimeManager>();
                if (timeManager == null)
                {
                    Debug.LogWarning($"[DigitalClock] No TimeManager found in scene!");
                    enabled = false;
                    return;
                }
            }

            // Initial display update
            UpdateFromTimeManager();
            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized || !autoUpdate || timeManager == null) return;

            // Check if enough time has passed for update
            if (updateInterval <= 0f || Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateFromTimeManager();
                lastUpdateTime = Time.time;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Updates display using data from TimeManager
        /// </summary>
        private void UpdateFromTimeManager()
        {
            if (timeManager?.WorldTimeData == null) return;

            // Get time components from TimeManager
            var gameTime = timeManager.GetCurrentGameTime();
            if (gameTime == null) return;

            // Extract hours, minutes, seconds
            int hours = gameTime.hours;
            int minutes = gameTime.minutes;
            int seconds = gameTime.seconds;

            // Get month name and day
            string monthName = timeManager.CurrentMonth.name;
            int dayOfMonth = timeManager.CurrentDayOfMonth;

            // Update display
            UpdateDisplay(hours, minutes, seconds, monthName, dayOfMonth);
        }

        /// <summary>
        /// Updates text color based on time manager state
        /// </summary>
        private void UpdateTextColor()
        {
            if (timeManager == null) return;

            Color targetColor = timeManager.TimeScale > 0f ? normalColor : pausedColor;
            if (textComponent.color != targetColor)
            {
                textComponent.color = targetColor;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually trigger a display update
        /// </summary>
        [ContextMenu("Update Display")]
        public void ManualUpdate()
        {
            UpdateFromTimeManager();
        }

        /// <summary>
        /// Enable or disable date display
        /// </summary>
        public void SetShowDate(bool show)
        {
            showDate = show;
            UpdateFromTimeManager();
        }

        /// <summary>
        /// Set text color
        /// </summary>
        public void SetTextColor(Color color)
        {
            normalColor = color;
            if (textComponent != null && timeManager != null && timeManager.TimeScale > 0f)
            {
                textComponent.color = color;
            }
        }

        /// <summary>
        /// Set date format (day first or month first)
        /// </summary>
        public void SetDayFirst(bool dayFirst)
        {
            this.dayFirst = dayFirst;
            UpdateFromTimeManager();
        }

        #endregion

        #region Editor Helpers

        #if UNITY_EDITOR
        [ContextMenu("Preview Display")]
        private void PreviewDisplay()
        {
            if (textComponent == null)
                textComponent = GetComponent<TextMeshProUGUI>();

            if (textComponent != null)
            {
                // Show sample time with 20-hour format (0-19 hours)
                UpdateDisplay(14, 30, 45, "Glavyr", 15);
            }
        }
        #endif

        #endregion
    }
}