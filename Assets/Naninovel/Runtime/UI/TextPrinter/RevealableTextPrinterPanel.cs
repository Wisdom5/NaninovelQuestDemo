// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Naninovel.UI
{
    /// <summary>
    /// A <see cref="UITextPrinterPanel"/> implementation that uses <see cref="IRevealableText"/> to reveal text over time.
    /// </summary>
    /// <remarks>
    /// A <see cref="IRevealableText"/> component should be attached to the underlying game object or one of it's child objects.
    /// </remarks>
    public class RevealableTextPrinterPanel : UITextPrinterPanel
    {
        [Serializable]
        protected class CharsToSfx
        {
            [Tooltip("The characters for which to trigger the SFX. Leave empty to trigger on any character.")]
            public string Characters;
            [Tooltip("The name (local path) of the SFX to trigger for the specified characters.")]
            [ResourcePopup(AudioConfiguration.DefaultAudioPathPrefix)]
            public string SfxName;
        }

        [Serializable]
        protected class CharsToPlaylist
        {
            [Tooltip("The characters for which to trigger the command. Leave empty to trigger on any character.")]
            public string Characters;
            [Tooltip("The text of the script command to execute for the specified characters.")]
            public string CommandText;
            public ScriptPlaylist Playlist { get; set; }
        }

        [Serializable]
        private class AuthorChangedEvent : UnityEvent<string> { }

        public virtual IRevealableText RevealableText => (IRevealableText)revealableText;
        public override string PrintedText { get => RevealableText.Text; set => RevealableText.Text = value; }
        public override string AuthorNameText { get => AuthorNamePanel ? AuthorNamePanel.Text : null; set => SetAuthorNameText(value); }
        public override float RevealProgress { get => RevealableText.RevealProgress; set => SetRevealProgress(value); }
        public override string Appearance { get => GetActiveAppearance(); set => SetActiveAppearance(value); }

        protected const string DefaultAppearanceName = "Default";
        protected virtual string AuthorId { get; private set; }
        protected virtual CharacterMetadata AuthorMeta { get; private set; }
        protected virtual IInputIndicator InputIndicator => (IInputIndicator)inputIndicator;
        protected virtual AuthorNamePanel AuthorNamePanel => authorNamePanel;
        protected virtual AuthorImage AuthorAvatarImage => authorAvatarImage;
        protected virtual bool PositionIndicatorOverText => positionIndicatorOverText;
        protected virtual List<CanvasGroup> Appearances => appearances;
        protected virtual List<CharsToSfx> CharsSfx => charsSfx;
        protected virtual List<CharsToPlaylist> CharsCommands => charsCommands;

        [Tooltip("Revealable text component. Expected to implement `IRevealableText` interface.")]
        [SerializeField] private MonoBehaviour revealableText;
        [Tooltip("Panel to display name of the currently printed text author (optional).")]
        [SerializeField] private AuthorNamePanel authorNamePanel;
        [Tooltip("Image to display avatar of the currently printed text author (optional).")]
        [SerializeField] private AuthorImage authorAvatarImage;
        [Tooltip("Object to use as an indicator when player is supposed to activate a `Continue` input to progress further. Expected to implement `IInputIndicator` interface.")]
        [SerializeField] private MonoBehaviour inputIndicator;
        [Tooltip("Whether to automatically move input indicator so it appears after the last revealed text character.")]
        [SerializeField] private bool positionIndicatorOverText = true;
        [Tooltip("Assigned canvas groups will represent printer appearances. Game object name of the canvas group represents the appearance name. Alpha of the group will be set to 1 when the appearance is activated and vice-versa.")]
        [SerializeField] private List<CanvasGroup> appearances;
        [Tooltip("Allows binding an SFX to play when specific characters are revealed.")]
        [SerializeField] private List<CharsToSfx> charsSfx = new List<CharsToSfx>();
        [Tooltip("Allows binding a script command to execute when specific characters are revealed.")]
        [SerializeField] private List<CharsToPlaylist> charsCommands = new List<CharsToPlaylist>();
        [Tooltip("Invoked when author (character ID) of the currently printed text is changed.")]
        [SerializeField] private AuthorChangedEvent onAuthorChanged;
        [Tooltip("Invoked when text reveal is started.")]
        [SerializeField] private UnityEvent onRevealStarted;
        [Tooltip("Invoked when text reveal is finished.")]
        [SerializeField] private UnityEvent onRevealFinished;

        private TextRevealer revealer;
        private Color defaultMessageColor, defaultNameColor;
        private IAudioManager audioManager;
        private IScriptPlayer scriptPlayer;

        public override async UniTask InitializeAsync ()
        {
            await base.InitializeAsync();

            if (CharsSfx != null && CharsSfx.Count > 0)
            {
                var loadTasks = new List<UniTask>();
                foreach (var charSfx in CharsSfx)
                    if (!string.IsNullOrEmpty(charSfx.SfxName))
                        loadTasks.Add(audioManager.AudioLoader.LoadAndHoldAsync(charSfx.SfxName, this));
                await UniTask.WhenAll(loadTasks);
            }

            if (CharsCommands != null && CharsCommands.Count > 0)
                foreach (var charsCommand in CharsCommands)
                    if (!string.IsNullOrEmpty(charsCommand.CommandText))
                        charsCommand.Playlist = new ScriptPlaylist(Script.FromTransient($"`{name}` printer `{charsCommand.Characters}` char command", charsCommand.CommandText));

            // Required for TMPro text to update the text info before applying actor state (reveal progress).
            await AsyncUtils.WaitEndOfFrameAsync();
        }

        public override async UniTask RevealPrintedTextOverTimeAsync (float revealDelay, AsyncToken token)
        {
            onRevealStarted?.Invoke();

            // Force-hide the indicator. Required when printing by non-played commands (eg, PlayScript component),
            // while the script player is actually waiting for input.
            SetWaitForInputIndicatorVisible(false);

            if (revealDelay <= 0) RevealableText.RevealProgress = 1f;
            else await revealer.RevealAsync(revealDelay, token);

            if (scriptPlayer.WaitingForInput)
                SetWaitForInputIndicatorVisible(true);

            onRevealFinished?.Invoke();
        }

        public override void SetWaitForInputIndicatorVisible (bool visible)
        {
            if (visible)
            {
                InputIndicator.Show();
                if (PositionIndicatorOverText) PlaceInputIndicatorOverText();
            }
            else InputIndicator.Hide();
        }

        public override void SetFontSize (int dropdownIndex)
        {
            base.SetFontSize(dropdownIndex);
            if (PositionIndicatorOverText) PlaceInputIndicatorOverText();
        }

        public override void SetFont (TMP_FontAsset font)
        {
            base.SetFont(font);
            if (PositionIndicatorOverText) PlaceInputIndicatorOverText();
        }

        public override void OnAuthorChanged (string authorId, CharacterMetadata authorMeta)
        {
            AuthorId = authorId;
            AuthorMeta = authorMeta;

            RevealableText.TextColor = authorMeta.UseCharacterColor ? authorMeta.MessageColor : defaultMessageColor;

            if (AuthorNamePanel)
                AuthorNamePanel.TextColor = authorMeta.UseCharacterColor ? authorMeta.NameColor : defaultNameColor;

            if (AuthorAvatarImage)
            {
                var avatarTexture = CharacterManager.GetAvatarTextureFor(authorId);
                AuthorAvatarImage.ChangeTextureAsync(avatarTexture).Forget();
            }

            onAuthorChanged?.Invoke(authorId);
        }

        protected override void Awake ()
        {
            base.Awake();

            if (RevealableText == null) throw new Error($"Revealable Text is not assigned on {gameObject.name} text printer.");
            if (InputIndicator == null) throw new Error($"Input Indicator is not assigned on {gameObject.name} text printer.");

            defaultMessageColor = RevealableText.TextColor;
            defaultNameColor = AuthorNamePanel ? AuthorNamePanel.TextColor : default;
            audioManager = Engine.GetService<IAudioManager>();
            scriptPlayer = Engine.GetService<IScriptPlayer>();
            revealer = new TextRevealer(RevealableText, HandleCharRevealed);
            SetAuthorNameText(null);
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            CharacterManager.OnCharacterAvatarChanged += HandleAvatarChanged;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            if (CharacterManager != null)
                CharacterManager.OnCharacterAvatarChanged -= HandleAvatarChanged;
        }

        protected override void OnDestroy ()
        {
            base.OnDestroy();

            if (CharsSfx != null && CharsSfx.Count > 0)
                foreach (var charSfx in CharsSfx)
                    if (!string.IsNullOrEmpty(charSfx.SfxName))
                        audioManager?.AudioLoader?.Release(charSfx.SfxName, this);
        }

        protected override void HandleVisibilityChanged (bool visible)
        {
            base.HandleVisibilityChanged(visible);

            if (!visible && AuthorAvatarImage && AuthorAvatarImage.isActiveAndEnabled)
                AuthorAvatarImage.ChangeTextureAsync(null).Forget();
        }

        protected virtual async void PlaceInputIndicatorOverText ()
        {
            // Wait for TMPro to update text info (force-update doesn't work).
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            if (!this) return;
            var pos = RevealableText.GetLastRevealedCharPosition();
            if (float.IsNaN(pos.x) || float.IsNaN(pos.y)) return;
            InputIndicator.RectTransform.position = new Vector3(pos.x, pos.y, InputIndicator.RectTransform.position.z);
        }

        protected virtual string GetActiveAppearance ()
        {
            if (Appearances is null || Appearances.Count == 0)
                return DefaultAppearanceName;
            foreach (var grp in Appearances)
                if (Mathf.Approximately(grp.alpha, 1f))
                    return grp.gameObject.name;
            return DefaultAppearanceName;
        }

        protected virtual void SetActiveAppearance (string appearance)
        {
            if (Appearances is null || Appearances.Count == 0 || !Appearances.Any(g => g.gameObject.name == appearance))
                return;

            foreach (var grp in Appearances)
                grp.alpha = grp.gameObject.name == appearance ? 1 : 0;
        }

        protected virtual void SetRevealProgress (float value)
        {
            RevealableText.RevealProgress = value;
        }

        protected virtual void SetAuthorNameText (string text)
        {
            if (!AuthorNamePanel) return;

            var isActive = !string.IsNullOrWhiteSpace(text);
            AuthorNamePanel.gameObject.SetActive(isActive);
            if (!isActive) return;

            AuthorNamePanel.Text = text;
        }

        protected virtual void HandleAvatarChanged (CharacterAvatarChangedArgs args)
        {
            if (!AuthorAvatarImage || args.CharacterId != AuthorId) return;

            AuthorAvatarImage.ChangeTextureAsync(args.AvatarTexture).Forget();
        }

        protected virtual void HandleCharRevealed (char character, AsyncToken token)
        {
            if (AuthorMeta != null && !string.IsNullOrEmpty(AuthorMeta.MessageSound))
                PlayAuthorSound();
            if (CharsSfx != null && CharsSfx.Count > 0)
                PlayRevealSfxForChar(character);
            if (CharsCommands != null && CharsCommands.Count > 0)
                ExecuteCommandForCharAsync(character, token).Forget();
        }

        protected virtual void PlayAuthorSound ()
        {
            audioManager.PlaySfxFast(AuthorMeta.MessageSound,
                restart: AuthorMeta.MessageSoundPlayback == MessageSoundPlayback.OneShotClipped,
                additive: AuthorMeta.MessageSoundPlayback != MessageSoundPlayback.Looped);
        }

        protected virtual void PlayRevealSfxForChar (char character)
        {
            foreach (var chars in CharsSfx)
                if (ShouldPlay(chars))
                    audioManager.PlaySfxFast(chars.SfxName);

            bool ShouldPlay (CharsToSfx chars) =>
                !string.IsNullOrEmpty(chars.SfxName) &&
                (string.IsNullOrEmpty(chars.Characters) || chars.Characters.IndexOf(character) >= 0);
        }

        protected virtual async UniTask ExecuteCommandForCharAsync (char character, AsyncToken token)
        {
            foreach (var chars in CharsCommands)
                if (ShouldExecute(chars))
                    await scriptPlayer.PlayTransient(chars.Playlist, token);

            bool ShouldExecute (CharsToPlaylist chars) =>
                chars.Playlist != null && chars.Playlist.Count > 0 &&
                (string.IsNullOrEmpty(chars.Characters) || chars.Characters.IndexOf(character) >= 0);
        }
    }
}
