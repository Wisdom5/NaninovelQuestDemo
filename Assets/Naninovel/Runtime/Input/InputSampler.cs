// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Naninovel
{
    /// <inheritdoc cref="IInputSampler"/>
    public class InputSampler : IInputSampler
    {
        public event Action OnStart;
        public event Action OnEnd;
        public event Action<float> OnChange;

        public virtual InputBinding Binding { get; }
        public virtual bool Enabled { get; set; } = true;
        public virtual bool Active => Value != 0;
        public virtual float Value { get; private set; }
        public virtual bool StartedDuringFrame => Active && Engine.Time.FrameCount == lastActiveFrame;
        public virtual bool EndedDuringFrame => !Active && Engine.Time.FrameCount == lastActiveFrame;

        // ReSharper disable once NotAccessedField.Local (used with new input)
        private readonly IInputManager inputManager;
        private readonly InputConfiguration config;
        private readonly HashSet<GameObject> objectTriggers;
        private UniTaskCompletionSource<bool> onInputTCS;
        private UniTaskCompletionSource onInputStartTCS, onInputEndTCS;
        private CancellationTokenSource onInputStartCTS, onInputEndCTS;
        private int lastActiveFrame;

        // Touch detection for old input.
        #pragma warning disable CS0414, CS0169
        private float lastTouchTime;
        private bool readyForNextTap;
        private Vector2 lastTouchBeganPosition;
        #pragma warning restore CS0414

        #if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_AVAILABLE
        private UnityEngine.InputSystem.InputAction inputAction;
        #endif

        /// <param name="config">Input manager configuration asset.</param>
        /// <param name="binding">Binding to trigger input.</param>
        /// <param name="objectTriggers">Objects to trigger input.</param>
        public InputSampler (InputConfiguration config, InputBinding binding,
            IEnumerable<GameObject> objectTriggers, IInputManager inputManager)
        {
            Binding = binding;
            this.config = config;
            this.objectTriggers = objectTriggers != null ? new HashSet<GameObject>(objectTriggers) : new HashSet<GameObject>();
            this.inputManager = inputManager;
            InitializeInputAction();
        }

        public virtual void AddObjectTrigger (GameObject obj) => objectTriggers.Add(obj);

        public virtual void RemoveObjectTrigger (GameObject obj) => objectTriggers.Remove(obj);

        public virtual async UniTask<bool> WaitForInputAsync ()
        {
            if (onInputTCS is null) onInputTCS = new UniTaskCompletionSource<bool>();
            return await onInputTCS.Task;
        }

        public virtual async UniTask WaitForInputStartAsync ()
        {
            if (onInputStartTCS is null) onInputStartTCS = new UniTaskCompletionSource();
            await onInputStartTCS.Task;
        }

        public virtual async UniTask WaitForInputEndAsync ()
        {
            if (onInputEndTCS is null) onInputEndTCS = new UniTaskCompletionSource();
            await onInputEndTCS.Task;
        }

        public virtual CancellationToken GetInputStartCancellationToken ()
        {
            if (onInputStartCTS is null) onInputStartCTS = new CancellationTokenSource();
            return onInputStartCTS.Token;
        }

        public virtual CancellationToken GetInputEndCancellationToken ()
        {
            if (onInputEndCTS is null) onInputEndCTS = new CancellationTokenSource();
            return onInputEndCTS.Token;
        }

        public virtual void Activate (float value) => SetInputValue(value);

        /// <summary>
        /// Performs the sampling, updating the input status; expected to be invoked on each render loop update.
        /// </summary>
        public virtual void SampleInput ()
        {
            if (!Enabled) return;

            #if ENABLE_LEGACY_INPUT_MANAGER
            if (config.ProcessLegacyBindings && Binding.Keys?.Count > 0)
                SampleKeys();

            if (config.ProcessLegacyBindings && Binding.Axes?.Count > 0)
                SampleAxes();

            if (config.ProcessLegacyBindings && IsTouchSupported() && Binding.Swipes?.Count > 0)
                SampleSwipes();

            if (config.ProcessLegacyBindings && objectTriggers.Count > 0 && IsTriggered())
                SampleObjectTriggers();

            void SampleKeys ()
            {
                foreach (var key in Binding.Keys)
                {
                    if (Input.GetKeyDown(key)) SetInputValue(1);
                    if (Input.GetKeyUp(key)) SetInputValue(0);
                }
            }

            void SampleAxes ()
            {
                var maxValue = 0f;
                foreach (var axis in Binding.Axes)
                {
                    var axisValue = axis.Sample();
                    if (Mathf.Abs(axisValue) > Mathf.Abs(maxValue))
                        maxValue = axisValue;
                }
                if (!Mathf.Approximately(maxValue, Value))
                    SetInputValue(maxValue);
            }

            void SampleSwipes ()
            {
                var swipeRegistered = false;
                foreach (var swipe in Binding.Swipes)
                    if (swipe.Sample())
                    {
                        swipeRegistered = true;
                        break;
                    }
                if (swipeRegistered != Active) SetInputValue(swipeRegistered ? 1 : 0);
            }

            bool IsTriggered () => IsTouchedLegacy() || Input.touchCount == 0 && Input.GetMouseButtonDown(0);

            bool IsTouchedLegacy ()
            {
                if (!IsTouchSupported() || Input.touchCount == 0) return false;

                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    RegisterTouchBegan(touch.position);
                    return false;
                }
                return touch.phase == TouchPhase.Ended && RegisterTouchEnded(touch.position);
            }

            bool IsTouchSupported () => Input.touchSupported || Application.isEditor;
            
            void RegisterTouchBegan (Vector2 position)
            {
                lastTouchBeganPosition = position;
            }

            bool RegisterTouchEnded (Vector2 position)
            {
                var cooldown = Engine.Time.UnscaledTime - lastTouchTime <= config.TouchFrequencyLimit;
                if (cooldown) return false;

                var distance = Vector2.Distance(position, lastTouchBeganPosition);
                var withinDistanceLimit = distance < config.TouchDistanceLimit;
                if (!withinDistanceLimit) return false;

                readyForNextTap = false;
                lastTouchTime = Engine.Time.UnscaledTime;
                return true;
            }
            #endif

            #if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_AVAILABLE
            if (objectTriggers.Count > 0 && (Touched() || Clicked())) SampleObjectTriggers();
            bool Touched () => UnityEngine.InputSystem.Touchscreen.current?.press.wasPressedThisFrame ?? false;
            bool Clicked () => UnityEngine.InputSystem.Mouse.current?.leftButton.wasPressedThisFrame ?? false;
            #endif
        }

        protected virtual void SampleObjectTriggers ()
        {
            if (!EventSystem.current) throw new Error("Failed to find event system. Make sure `Spawn Event System` is enabled in input configuration or manually spawn an event system before initializing Naninovel.");
            var hoveredObject = EventUtils.GetHoveredGameObject();
            if (hoveredObject && objectTriggers.Contains(hoveredObject))
                if (!hoveredObject.TryGetComponent<IInputTrigger>(out var trigger) || trigger.CanTriggerInput())
                    SetInputValue(1f);
        }

        protected void InitializeInputAction ()
        {
            #if ENABLE_INPUT_SYSTEM && INPUT_SYSTEM_AVAILABLE
            if (!config.InputActions) return;
            inputAction = config.InputActions.FindActionMap("Naninovel")?.FindAction(Binding.Name);
            if (inputAction is null) return;
            inputAction.Enable();
            inputAction.performed += HandlePerformed;
            inputAction.canceled += HandleCanceled;

            void HandlePerformed (UnityEngine.InputSystem.InputAction.CallbackContext _)
            {
                if (inputManager.IsSampling(Binding.Name))
                    SetInputValue(inputAction.ReadValue<float>());
            }

            void HandleCanceled (UnityEngine.InputSystem.InputAction.CallbackContext _)
            {
                if (inputManager.IsSampling(Binding.Name))
                    SetInputValue(0);
            }
            #endif
        }

        protected virtual void SetInputValue (float value)
        {
            if (!Mathf.Approximately(Value, value))
                OnChange?.Invoke(value);

            Value = value;
            lastActiveFrame = Engine.Time.FrameCount;

            onInputTCS?.TrySetResult(Active);
            onInputTCS = null;
            if (Active)
            {
                onInputStartTCS?.TrySetResult();
                onInputStartTCS = null;
                onInputStartCTS?.Cancel();
                onInputStartCTS?.Dispose();
                onInputStartCTS = null;
            }
            else
            {
                onInputEndTCS?.TrySetResult();
                onInputEndTCS = null;
                onInputEndCTS?.Cancel();
                onInputEndCTS?.Dispose();
                onInputEndCTS = null;
            }

            if (Active) OnStart?.Invoke();
            else OnEnd?.Invoke();
        }
    }
}
