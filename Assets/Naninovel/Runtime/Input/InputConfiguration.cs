// Copyright 2023 ReWaffle LLC. All rights reserved.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static Naninovel.InputNames;

namespace Naninovel
{
    [EditInProjectSettings]
    public class InputConfiguration : Configuration
    {
        [Tooltip("Whether to spawn an event system when initializing.")]
        public bool SpawnEventSystem = true;
        [Tooltip("A prefab with an `EventSystem` component to spawn for input processing. Will spawn a default one when not specified.")]
        public EventSystem CustomEventSystem;
        [Tooltip("Whether to spawn an input module when initializing.")]
        public bool SpawnInputModule = true;
        [Tooltip("A prefab with an `InputModule` component to spawn for input processing. Will spawn a default one when not specified.")]
        public BaseInputModule CustomInputModule;
        #if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_AVAILABLE
        [Tooltip("When Unity's new input system is installed, assign input actions asset here.\n\nTo map input actions to Naninovel's input bindings, create `Naninovel` action map and add actions with names equal to the binding names (found below under `Control Scheme` -> Bindings list).\n\nBe aware, that 2-dimensional (Vector2) axes are not supported.")]
        public UnityEngine.InputSystem.InputActionAsset InputActions;
        #endif
        [Tooltip("Whether to process legacy input bindings. Disable in case you're using Unity's new input system and don't want the legacy bindings to work in addition to input actions.")]
        public bool ProcessLegacyBindings = true;
        [Tooltip("Limits frequency of the registered touch inputs, in seconds. For legacy input only.")]
        public float TouchFrequencyLimit = .1f;
        [Tooltip("Limits distance of the registered touch inputs, in pixels. For legacy input only.")]
        public float TouchDistanceLimit = 25f;
        [Tooltip("Whether to change input mode when associated device is activated. Eg, switch to gamepad when any gamepad button is pressed and switch back to mouse when mouse button clicked. Requires new input system.")]
        public bool DetectInputMode = true;

        [Header("Control Scheme"), Tooltip("Bindings to process input for; find descriptions for each default input in the 'Input Processing' guide.")]
        public List<InputBinding> Bindings = new List<InputBinding> {
            new InputBinding {
                Name = Submit,
                Keys = new List<KeyCode> { KeyCode.Return, KeyCode.JoystickButton0 },
                AlwaysProcess = true
            },
            new InputBinding {
                Name = Cancel,
                Keys = new List<KeyCode> { KeyCode.Escape, KeyCode.JoystickButton1 },
                AlwaysProcess = true
            },
            new InputBinding {
                Name = Delete,
                Keys = new List<KeyCode> { KeyCode.Delete, KeyCode.JoystickButton7 },
                AlwaysProcess = true
            },
            new InputBinding {
                Name = NavigateX,
                AlwaysProcess = true
            },
            new InputBinding {
                Name = NavigateY,
                AlwaysProcess = true
            },
            new InputBinding {
                Name = ScrollY,
                Axes = new List<InputAxisTrigger> { new InputAxisTrigger { AxisName = "Vertical", TriggerMode = InputAxisTriggerMode.Both } },
                AlwaysProcess = true
            },
            new InputBinding {
                Name = Page,
                AlwaysProcess = true
            },
            new InputBinding {
                Name = Tab,
                AlwaysProcess = true
            },
            new InputBinding {
                Name = Continue,
                Keys = new List<KeyCode> { KeyCode.Return, KeyCode.KeypadEnter, KeyCode.JoystickButton0 },
                Axes = new List<InputAxisTrigger> { new InputAxisTrigger { AxisName = "Mouse ScrollWheel", TriggerMode = InputAxisTriggerMode.Negative } },
                Swipes = new List<InputSwipeTrigger> { new InputSwipeTrigger { Direction = InputSwipeDirection.Left } }
            },
            new InputBinding {
                Name = Pause,
                Keys = new List<KeyCode> { KeyCode.Backspace, KeyCode.JoystickButton7 }
            },
            new InputBinding {
                Name = Skip,
                Keys = new List<KeyCode> { KeyCode.LeftControl, KeyCode.RightControl, KeyCode.JoystickButton1 }
            },
            new InputBinding {
                Name = ToggleSkip,
                Keys = new List<KeyCode> { KeyCode.Tab, KeyCode.JoystickButton9 }
            },
            new InputBinding {
                Name = AutoPlay,
                Keys = new List<KeyCode> { KeyCode.A, KeyCode.JoystickButton2 }
            },
            new InputBinding {
                Name = ToggleUI,
                Keys = new List<KeyCode> { KeyCode.Space, KeyCode.JoystickButton3 },
                Swipes = new List<InputSwipeTrigger> { new InputSwipeTrigger { Direction = InputSwipeDirection.Down } }
            },
            new InputBinding {
                Name = ShowBacklog,
                Keys = new List<KeyCode> { KeyCode.L, KeyCode.JoystickButton5 },
                Swipes = new List<InputSwipeTrigger> { new InputSwipeTrigger { Direction = InputSwipeDirection.Up } }
            },
            new InputBinding {
                Name = Rollback,
                Keys = new List<KeyCode> { KeyCode.JoystickButton4 },
                Axes = new List<InputAxisTrigger> { new InputAxisTrigger { AxisName = "Mouse ScrollWheel", TriggerMode = InputAxisTriggerMode.Positive } },
                Swipes = new List<InputSwipeTrigger> { new InputSwipeTrigger { Direction = InputSwipeDirection.Right } },
            },
            new InputBinding {
                Name = CameraLookX,
                Axes = new List<InputAxisTrigger> {
                    new InputAxisTrigger { AxisName = "Horizontal", TriggerMode = InputAxisTriggerMode.Both },
                    new InputAxisTrigger { AxisName = "Mouse X", TriggerMode = InputAxisTriggerMode.Both }
                }
            },
            new InputBinding {
                Name = CameraLookY,
                Axes = new List<InputAxisTrigger> {
                    new InputAxisTrigger { AxisName = "Vertical", TriggerMode = InputAxisTriggerMode.Both },
                    new InputAxisTrigger { AxisName = "Mouse Y", TriggerMode = InputAxisTriggerMode.Both }
                }
            },
            new InputBinding {
                Name = ToggleConsole,
                Keys = new List<KeyCode> { KeyCode.BackQuote },
                AlwaysProcess = true
            }
        };
    }
}
