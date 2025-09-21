// Copyright 2023 ReWaffle LLC. All rights reserved.

using UnityEngine;

namespace Naninovel.UI
{
    /// <summary>
    /// Contain common playback controls embedded inside text printers.
    /// </summary>
    public class ControlPanel : MonoBehaviour
    {
        [Tooltip("Activated when input mode is not gamepad.")]
        [SerializeField] private GameObject buttons;
        [Tooltip("Activated when input mode is gamepad.")]
        [SerializeField] private GameObject legend;

        private IInputManager input => Engine.GetService<IInputManager>();

        protected virtual void OnEnable ()
        {
            input.OnInputModeChanged += HandleInputModeChanged;
            HandleInputModeChanged(input.InputMode);
        }

        protected void OnDisable ()
        {
            if (input != null)
                input.OnInputModeChanged -= HandleInputModeChanged;
        }

        protected virtual void HandleInputModeChanged (InputMode mode)
        {
            if (buttons) buttons.SetActive(mode != InputMode.Gamepad);
            if (legend) legend.SetActive(mode == InputMode.Gamepad);
        }
    }
}
