// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using Naninovel.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Naninovel
{
    public class BacklogMessageUI : ScriptableUIBehaviour
    {
        [Serializable]
        private class OnMessageChangedEvent : UnityEvent<string> { }

        [Serializable]
        private class OnAuthorChangedEvent : UnityEvent<string> { }

        protected virtual LocalizableText Text { get; private set; }
        protected virtual string AuthorId { get; private set; }
        protected virtual PlaybackSpot RollbackSpot { get; private set; } = PlaybackSpot.Invalid;
        protected virtual List<string> VoicePaths { get; } = new List<string>();
        protected virtual GameObject AuthorPanel => authorPanel;
        protected virtual Button PlayVoiceButton => playVoiceButton;
        protected virtual Button RollbackButton => rollbackButton;

        [Tooltip("Panel hosting author name text (optional). When assigned will be de-activated based on whether author is assigned.")]
        [SerializeField] private GameObject authorPanel;
        [Tooltip("Button to replay voice associated with the message (optional).")]
        [SerializeField] private Button playVoiceButton;
        [Tooltip("Button to perform rollback to the moment the messages was added (optional).")]
        [SerializeField] private Button rollbackButton;
        [SerializeField] private OnMessageChangedEvent onMessageChanged;
        [SerializeField] private OnAuthorChangedEvent onAuthorChanged;

        private IAudioManager audioManager => Engine.GetService<IAudioManager>();
        private IStateManager stateManager => Engine.GetService<IStateManager>();
        private ICharacterManager charManager => Engine.GetService<ICharacterManager>();

        public virtual BacklogMessage GetState () => new BacklogMessage(Text, AuthorId, RollbackSpot, VoicePaths.ToArray());

        public virtual void Initialize (BacklogMessage message)
        {
            SetText(message.Text);
            SetAuthor(message.AuthorId);
            SetVoice(message.Voice);
            SetRollbackSpot(message.RollbackSpot);
        }

        public virtual void Append (LocalizableText text, string voicePath = null)
        {
            SetText(Text + text);

            if (!string.IsNullOrEmpty(voicePath))
            {
                VoicePaths.Add(voicePath);
                if (PlayVoiceButton)
                    PlayVoiceButton.gameObject.SetActive(true);
            }
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            if (PlayVoiceButton)
                PlayVoiceButton.onClick.AddListener(HandlePlayVoiceButtonClicked);

            if (RollbackButton)
                RollbackButton.onClick.AddListener(HandleRollbackButtonClicked);
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            if (PlayVoiceButton)
                PlayVoiceButton.onClick.RemoveListener(HandlePlayVoiceButtonClicked);

            if (RollbackButton)
                RollbackButton.onClick.RemoveListener(HandleRollbackButtonClicked);
        }

        protected virtual void SetText (LocalizableText text)
        {
            Text = text;
            onMessageChanged?.Invoke(text);
        }

        protected virtual void SetAuthor (string authorId)
        {
            AuthorId = authorId;
            var name = charManager.GetDisplayName(authorId) ?? authorId;
            if (AuthorPanel) AuthorPanel.SetActive(!string.IsNullOrWhiteSpace(name));
            onAuthorChanged?.Invoke(name);
        }

        protected virtual void SetVoice (IReadOnlyList<string> voicePaths)
        {
            VoicePaths.Clear();
            VoicePaths.AddRange(voicePaths);
            if (PlayVoiceButton)
                PlayVoiceButton.gameObject.SetActive(VoicePaths.Count > 0);
        }

        protected virtual void SetRollbackSpot (PlaybackSpot spot)
        {
            RollbackSpot = spot;
            var canRollback = spot.Valid && stateManager.CanRollbackTo(s => s.PlaybackSpot == spot);
            if (RollbackButton) RollbackButton.gameObject.SetActive(canRollback);
        }

        protected virtual async void HandlePlayVoiceButtonClicked ()
        {
            PlayVoiceButton.interactable = false;
            await audioManager.PlayVoiceSequenceAsync(VoicePaths.ToArray(), authorId: AuthorId);
            PlayVoiceButton.interactable = true;
        }

        protected virtual async void HandleRollbackButtonClicked ()
        {
            RollbackButton.interactable = false;
            await stateManager.RollbackAsync(s => s.PlaybackSpot == RollbackSpot);
            RollbackButton.interactable = true;
            GetComponentInParent<IBacklogUI>()?.Hide();
        }
    }
}
