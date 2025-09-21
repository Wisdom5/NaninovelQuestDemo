// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    public class VariableInputPanel : CustomUI, IVariableInputUI
    {
        [Serializable]
        public new class GameState
        {
            public string VariableName;
            public LocalizableText SummaryText;
            public string InputFieldText;
            public bool PlayOnSubmit;
        }

        protected virtual LocalizableText Summary { get; private set; }
        protected virtual TMP_InputField InputField => inputField;
        protected virtual Button SubmitButton => submitButton;
        protected virtual bool ActivateOnShow => activateOnShow;
        protected virtual bool SubmitOnInput => submitOnInput;
        protected virtual GameObject SummaryContainer => summaryContainer;

        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button submitButton;
        [Tooltip("Whether to automatically select and activate input field when the UI is shown.")]
        [SerializeField] private bool activateOnShow = true;
        [Tooltip("Whether to attempt submit input field value when a `Submit` input is activated.")]
        [SerializeField] private bool submitOnInput = true;
        [Tooltip("When assigned, the game object will be de-/activated based on whether summary is assigned.")]
        [SerializeField] private GameObject summaryContainer;
        [SerializeField] private StringUnityEvent onSummaryChanged;
        [SerializeField] private StringUnityEvent onPredefinedValueChanged;

        private IScriptPlayer scriptPlayer;
        private ICustomVariableManager variableManager;
        private IStateManager stateManager;
        private IInputSampler submitInput;
        private string variableName;
        private bool playOnSubmit;

        public virtual void Show (string variableName, LocalizableText summary, 
            LocalizableText predefinedValue, bool playOnSubmit)
        {
            this.variableName = variableName;
            this.playOnSubmit = playOnSubmit;
            SetSummary(summary);
            SetPredefinedValue(predefinedValue);

            Show();

            if (ActivateOnShow)
            {
                InputField.Select();
                InputField.ActivateInputField();
            }
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(InputField, SubmitButton);

            scriptPlayer = Engine.GetService<IScriptPlayer>();
            variableManager = Engine.GetService<ICustomVariableManager>();
            stateManager = Engine.GetService<IStateManager>();
            submitInput = Engine.GetService<IInputManager>().GetSubmit();

            SubmitButton.interactable = false;
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            SubmitButton.onClick.AddListener(HandleSubmit);
            InputField.onValueChanged.AddListener(HandleInputChanged);

            if (submitInput != null && SubmitOnInput)
                submitInput.OnStart += HandleSubmit;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            SubmitButton.onClick.RemoveListener(HandleSubmit);
            InputField.onValueChanged.RemoveListener(HandleInputChanged);

            if (submitInput != null && SubmitOnInput)
                submitInput.OnStart -= HandleSubmit;
        }

        protected override void SerializeState (GameStateMap stateMap)
        {
            base.SerializeState(stateMap);

            var state = new GameState {
                VariableName = variableName,
                SummaryText = Summary,
                InputFieldText = InputField.text,
                PlayOnSubmit = playOnSubmit
            };
            stateMap.SetState(state);
        }

        protected override async UniTask DeserializeState (GameStateMap stateMap)
        {
            await base.DeserializeState(stateMap);

            var state = stateMap.GetState<GameState>();
            if (state is null) return;

            variableName = state.VariableName;
            SetSummary(state.SummaryText);
            InputField.text = state.InputFieldText;
            playOnSubmit = state.PlayOnSubmit;
        }

        protected virtual void SetSummary (LocalizableText value)
        {
            Summary = value;
            onSummaryChanged?.Invoke(value);
            if (SummaryContainer)
                SummaryContainer.SetActive(!value.IsEmpty);
        }

        protected virtual void SetPredefinedValue (LocalizableText value)
        {
            onPredefinedValueChanged?.Invoke(value);
        }

        protected virtual void HandleInputChanged (string text)
        {
            SubmitButton.interactable = !string.IsNullOrWhiteSpace(text);
        }

        protected virtual void HandleSubmit ()
        {
            if (!Visible || string.IsNullOrWhiteSpace(InputField.text)) return;

            stateManager.PeekRollbackStack()?.AllowPlayerRollback();

            variableManager.SetVariableValue(variableName, InputField.text);

            ClearFocus();
            Hide();

            if (playOnSubmit)
            {
                // Attempt to select and play next command.
                var nextIndex = scriptPlayer.PlayedIndex + 1;
                scriptPlayer.Play(nextIndex);
            }
        }
    }
}
