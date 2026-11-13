using UnityEngine;
using System;

// Tracks the combo rule only: whether a press arrived during the hold window, and how long the window stays open.
// It never touches the Animator. The Animator plays the swings and decides transitions from the parameters
// CombatStateController sets from this state.
namespace Sol
{
    internal class CombatStateMachine
    {
        private readonly float _comboHoldSeconds;
        private readonly float _comboHoldStartDelay;

        private bool _comboHolding;
        private float _comboHoldOpensAt = -10f;
        private float _comboHoldEndsAt = -10f;
        private bool _comboPressed;

        // Raised when the hold window ends with no press, so the controller can close the gate.
        public event Action OnHoldExpired;

        public CombatStateMachine(float comboHoldSeconds, float comboHoldStartDelay)
        {
            _comboHoldSeconds = comboHoldSeconds;
            _comboHoldStartDelay = comboHoldStartDelay;
        }

        // True from the end of the start delay until the hold closes. Presses count only inside this window.
        public bool IsPressWindowOpen => _comboHolding && Time.time >= _comboHoldOpensAt;

        public bool IsComboHolding => _comboHolding;

        // A press during a swing or the hold. Returns true when the press lands inside the press window,
        // so the controller knows to set CanStringToCombo true.
        public bool RegisterPress()
        {
            _comboPressed = true;
            return IsPressWindowOpen;
        }

        // A new attack from idle starts with nothing pressed and no hold.
        public void StartNewCombo()
        {
            _comboHolding = false;
            _comboPressed = false;
        }

        // The swing's recovery: the hold window opens, unless a press already arrived during the swing.
        public void BeginComboHold()
        {
            if (_comboPressed) return;
            _comboHolding = true;
            _comboHoldOpensAt = Time.time + _comboHoldStartDelay;
            _comboHoldEndsAt = _comboHoldOpensAt + _comboHoldSeconds;
        }

        // The swing finished. A press during the swing has already been consumed by the Animator's transition.
        public void EndSwing()
        {
            _comboPressed = false;
        }

        public void EndCombo()
        {
            _comboHolding = false;
            _comboPressed = false;
        }

        // Runs every frame. Closes the hold window when it runs out with no press.
        public void UpdateComboHoldTimer()
        {
            if (!_comboHolding) return;
            if (Time.time < _comboHoldEndsAt) return;

            EndCombo();
            OnHoldExpired?.Invoke();
        }
    }
}
